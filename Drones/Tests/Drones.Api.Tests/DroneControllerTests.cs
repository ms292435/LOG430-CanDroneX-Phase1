using Drones.Api;
using Drones.Application;
using Drones.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using Xunit;

namespace Drones.Api.Tests;

// Faux dépôt en mémoire pour les tests d'API du module Drones
internal sealed class FakeDroneRepositoryForApi : IDroneRepository
{
    private readonly ConcurrentDictionary<Guid, Drone> _drones = new();

    public Task SaveAsync(Drone drone, CancellationToken ct = default)
    {
        _drones[drone.Id.Value] = drone;
        return Task.CompletedTask;
    }

    public Task<Drone?> GetByImsiAsync(string imsi, CancellationToken ct = default) =>
        Task.FromResult(_drones.Values.FirstOrDefault(d => d.Imsi == imsi));

    public Task<Drone?> GetByIdAsync(DroneId id, CancellationToken ct = default) =>
        Task.FromResult(_drones.TryGetValue(id.Value, out var d) ? d : null);

    public Task<Drone?> GetByIdAndClientIdAsync(DroneId id, string clientId, CancellationToken ct = default) =>
        Task.FromResult(_drones.TryGetValue(id.Value, out var d) && d.ClientId == clientId ? d : null);

    public int Count => _drones.Count;
}

public class DroneControllerTests
{
    private const string ClientIdValide = "client-demo";
    private const string AutreClientId = "autre-client";
    private const string ImsiValide = "123456789012345";
    private const string TypeCarteValide = "SIM";
    private const string ModeleValide = "DJI Matrice 300";

    private readonly FakeDroneRepositoryForApi _repository;
    private readonly DroneController _controller;

    public DroneControllerTests()
    {
        _repository = new FakeDroneRepositoryForApi();
        var useCase = new DroneUseCase(_repository);
        _controller = new DroneController(useCase)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task Create_SansClientId_Retourne401Unauthorized()
    {
        var request = new EnregistrerDroneRequest(ImsiValide, TypeCarteValide, ModeleValide);

        var result = await _controller.Create(request, clientId: null, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Contains("X-Client-Id", problem.Detail);
        Assert.Equal(0, _repository.Count);
    }

    [Fact]
    public async Task Create_AvecImsiInvalideEtModeleAbsent_Retourne400BadRequestAvecDeuxChampsEnCause_ScenarioQ1()
    {
        // Scénario Q1 du dossier d'architecture (§10.2) :
        // "Une requête d'enregistrement de drone est soumise avec un IMSI invalide et un modèle absent.
        // Le système répond 400 en listant les deux champs en cause, sans rien écrire en base."
        var request = new EnregistrerDroneRequest(Imsi: "123", TypeCarte: "SIM", Modele: "");

        var result = await _controller.Create(request, ClientIdValide, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        var validationProblem = Assert.IsType<ValidationProblemDetails>(badRequestResult.Value);

        Assert.True(validationProblem.Errors.ContainsKey("imsi"), "L'erreur sur IMSI doit être listée");
        Assert.True(validationProblem.Errors.ContainsKey("modele"), "L'erreur sur le modèle doit être listée");
        Assert.Equal(0, _repository.Count); // Rien n'est écrit en base
    }

    [Fact]
    public async Task Create_AvecTypeCarteInvalide_Retourne400BadRequest()
    {
        var request = new EnregistrerDroneRequest(ImsiValide, TypeCarte: "INCONNU", Modele: ModeleValide);

        var result = await _controller.Create(request, ClientIdValide, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var validationProblem = Assert.IsType<ValidationProblemDetails>(badRequestResult.Value);
        Assert.True(validationProblem.Errors.ContainsKey("typeCarte"));
        Assert.Equal(0, _repository.Count);
    }

    [Fact]
    public async Task Create_AvecDonneesValides_Retourne201CreatedAvecLocationEtId()
    {
        var request = new EnregistrerDroneRequest(ImsiValide, TypeCarteValide, ModeleValide);

        var result = await _controller.Create(request, ClientIdValide, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.Equal(nameof(DroneController.GetById), createdResult.ActionName);

        var response = Assert.IsType<DroneCreatedResponse>(createdResult.Value);
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal(1, _repository.Count);
    }

    [Fact]
    public async Task Create_AvecImsiDejaUtilise_Retourne409ConflictSansDetailSurLeDroneExistant()
    {
        var request = new EnregistrerDroneRequest(ImsiValide, TypeCarteValide, ModeleValide);
        await _controller.Create(request, ClientIdValide, CancellationToken.None); // 1er enregistrement

        // 2e enregistrement avec le même IMSI (A2 de UC-02)
        var result = await _controller.Create(request, ClientIdValide, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("Conflict", problem.Title);
        Assert.Equal(1, _repository.Count); // Pas de doublon
    }

    [Fact]
    public async Task GetById_SansClientId_Retourne401Unauthorized()
    {
        var result = await _controller.GetById(Guid.NewGuid(), clientId: null, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetById_PourDroneInexistant_Retourne404NotFound()
    {
        var result = await _controller.GetById(Guid.NewGuid(), ClientIdValide, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetById_PourDroneDUnAutreClient_Retourne404NotFoundSansDistinction_ScenarioQ2()
    {
        // Scénario Q2 (§10.2) et §8.5 :
        // Un drone d'un autre client est traité comme inexistant (404 sans distinction).
        var request = new EnregistrerDroneRequest(ImsiValide, TypeCarteValide, ModeleValide);
        var createResult = await _controller.Create(request, ClientIdValide, CancellationToken.None);
        var created = Assert.IsType<DroneCreatedResponse>(((CreatedAtActionResult)createResult).Value);

        // Tentative de consultation par un autre client B2B
        var getResult = await _controller.GetById(created.Id, AutreClientId, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(getResult);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetById_PourDroneDuClient_Retourne200OkAvecDroneResponse()
    {
        var request = new EnregistrerDroneRequest(ImsiValide, "eSIM", ModeleValide);
        var createResult = await _controller.Create(request, ClientIdValide, CancellationToken.None);
        var created = Assert.IsType<DroneCreatedResponse>(((CreatedAtActionResult)createResult).Value);

        var getResult = await _controller.GetById(created.Id, ClientIdValide, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(getResult);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var response = Assert.IsType<DroneResponse>(okResult.Value);

        Assert.Equal(created.Id, response.Id);
        Assert.Equal(ClientIdValide, response.ClientId);
        Assert.Equal(ImsiValide, response.Imsi);
        Assert.Equal(TypeCarte.ESim.ToString(), response.TypeCarte);
        Assert.Equal(ModeleValide, response.Modele);
        Assert.Equal(StatutDrone.Enregistre.ToString(), response.Statut);
    }
}
