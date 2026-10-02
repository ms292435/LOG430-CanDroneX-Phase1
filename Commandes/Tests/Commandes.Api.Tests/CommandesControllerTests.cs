using Catalogue.Contracts;
using Commandes.Api;
using Commandes.Application;
using Commandes.Domain;
using Drones.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Commandes.Api.Tests;

internal sealed class FakeCommandeRepoForApi : ICommandeRepository
{
    private readonly Dictionary<Guid, Commande> _commandes = new();

    public Task SaveAsync(Commande commande, CancellationToken ct = default)
    {
        _commandes[commande.Id] = commande;
        return Task.CompletedTask;
    }

    public Task<Commande?> GetByIdAndClientIdAsync(Guid id, string clientId, CancellationToken ct = default)
    {
        _commandes.TryGetValue(id, out var cmd);
        if (cmd is not null && cmd.ClientId == clientId)
            return Task.FromResult<Commande?>(cmd);

        return Task.FromResult<Commande?>(null);
    }

    public Task<Commande?> GetByCleIdempotenceAsync(string clientId, string cleIdempotence, CancellationToken ct = default)
    {
        var cmd = _commandes.Values.FirstOrDefault(c =>
            c.ClientId == clientId && c.CleIdempotence == cleIdempotence);

        return Task.FromResult(cmd);
    }

    public int Count => _commandes.Count;
}

internal sealed class FakeDronesApiForController : IDronesApi
{
    private readonly Dictionary<(Guid DroneId, string ClientId), DroneInfo> _drones = new();

    public void EnregistrerDrone(Guid droneId, string clientId)
    {
        _drones[(droneId, clientId)] = new DroneInfo(
            droneId,
            clientId,
            "123456789012345",
            "Sim",
            "DJI Matrice 300",
            "Enregistre",
            DateTime.UtcNow
        );
    }

    public Task<DroneInfo?> ObtenirDroneDuClientAsync(Guid droneId, string clientId, CancellationToken ct = default)
    {
        _drones.TryGetValue((droneId, clientId), out var drone);
        return Task.FromResult(drone);
    }
}

internal sealed class FakeCatalogueApiForController : ICatalogueApi
{
    private readonly HashSet<string> _services = new(StringComparer.OrdinalIgnoreCase)
    {
        "C2_URLLC",
        "IMAGERIE_EMBB"
    };

    public Task<OffreInfo?> ObtenirOffreAsync(string typeService, CancellationToken ct = default)
    {
        if (_services.Contains(typeService))
            return Task.FromResult<OffreInfo?>(new OffreInfo(typeService, "Libellé", "Profil"));

        return Task.FromResult<OffreInfo?>(null);
    }

    public Task<IReadOnlyList<OffreInfo>> ObtenirToutesLesOffresAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<OffreInfo>>(
            _services.Select(s => new OffreInfo(s, s, "Profil")).ToList()
        );
}

public class CommandesControllerTests
{
    private const string ClientIdValide = "client-demo";
    private const string AutreClientId = "autre-client";
    private static readonly Guid DroneIdValide = Guid.NewGuid();
    private const string CleIdempotenceValide = "idemp-key-100";
    private static readonly string[] ServicesValides = new[] { "C2_URLLC", "IMAGERIE_EMBB" };

    private readonly FakeCommandeRepoForApi _repo;
    private readonly FakeDronesApiForController _dronesApi;
    private readonly FakeCatalogueApiForController _catalogueApi;
    private readonly CommandesController _controller;

    public CommandesControllerTests()
    {
        _repo = new FakeCommandeRepoForApi();
        _dronesApi = new FakeDronesApiForController();
        _catalogueApi = new FakeCatalogueApiForController();

        _dronesApi.EnregistrerDrone(DroneIdValide, ClientIdValide);

        var useCase = new CommandeUseCase(_repo, _dronesApi, _catalogueApi);
        _controller = new CommandesController(useCase)
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
        var request = new CreerCommandeRequest(DroneIdValide, ServicesValides);

        var result = await _controller.Create(request, clientId: null, idempotencyKey: CleIdempotenceValide, CancellationToken.None);

        var objResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, objResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objResult.Value);
        Assert.Contains("X-Client-Id", problem.Detail);
    }

    [Fact]
    public async Task Create_SansIdempotencyKey_Retourne400BadRequest()
    {
        var request = new CreerCommandeRequest(DroneIdValide, ServicesValides);

        var result = await _controller.Create(request, ClientIdValide, idempotencyKey: null, CancellationToken.None);

        var objResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, objResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objResult.Value);
        Assert.Contains("Idempotency-Key", problem.Detail);
    }

    [Fact]
    public async Task Create_DroneInconnuOuAutreClient_Retourne404NotFoundSansDistinction_ScenarioA2EtQ2()
    {
        // DroneIdValide appartient à ClientIdValide, pas à AutreClientId
        var request = new CreerCommandeRequest(DroneIdValide, ServicesValides);

        var result = await _controller.Create(request, AutreClientId, CleIdempotenceValide, CancellationToken.None);

        var objResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, objResult.StatusCode);
    }

    [Fact]
    public async Task Create_ServiceAbsentDuCatalogue_Retourne400BadRequest_ScenarioA3()
    {
        var request = new CreerCommandeRequest(DroneIdValide, new[] { "C2_URLLC", "SERVICE_INCONNU" });

        var result = await _controller.Create(request, ClientIdValide, CleIdempotenceValide, CancellationToken.None);

        var objResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, objResult.StatusCode);
    }

    [Fact]
    public async Task Create_MemeServiceDemandeDeuxFois_Retourne400BadRequest_ScenarioA4()
    {
        var request = new CreerCommandeRequest(DroneIdValide, new[] { "C2_URLLC", "C2_URLLC" });

        var result = await _controller.Create(request, ClientIdValide, CleIdempotenceValide, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var validationProblem = Assert.IsType<ValidationProblemDetails>(badRequestResult.Value);
        Assert.True(validationProblem.Errors.ContainsKey("services"));
    }

    [Fact]
    public async Task Create_CommandeValide_Retourne201CreatedAvecLocationEtElementsEnAttente()
    {
        var request = new CreerCommandeRequest(DroneIdValide, ServicesValides);

        var result = await _controller.Create(request, ClientIdValide, CleIdempotenceValide, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.Equal(nameof(CommandesController.GetById), createdResult.ActionName);

        var response = Assert.IsType<CommandeResponse>(createdResult.Value);
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal(ClientIdValide, response.ClientId);
        Assert.Equal(DroneIdValide, response.DroneId);
        Assert.Equal(EtatCommande.RECUE.ToString(), response.Etat);
        Assert.Equal(2, response.Elements.Count);
        Assert.All(response.Elements, e => Assert.Equal(EtatElement.EN_ATTENTE.ToString(), e.Etat));
        Assert.Equal(1, _repo.Count);
    }

    [Fact]
    public async Task Create_RejeuMemeCleMemeContenu_Retourne201AvecMemeCommande_ScenarioQ3()
    {
        var request = new CreerCommandeRequest(DroneIdValide, ServicesValides);
        var premier = await _controller.Create(request, ClientIdValide, CleIdempotenceValide, CancellationToken.None);
        var resp1 = Assert.IsType<CommandeResponse>(((CreatedAtActionResult)premier).Value);

        // Rejeu avec la même clé et les mêmes services
        var deuxieme = await _controller.Create(request, ClientIdValide, CleIdempotenceValide, CancellationToken.None);
        var resp2 = Assert.IsType<CommandeResponse>(((CreatedAtActionResult)deuxieme).Value);

        Assert.Equal(resp1.Id, resp2.Id);
        Assert.Equal(1, _repo.Count); // Aucun nouvel élément en base
    }

    [Fact]
    public async Task Create_RejeuMemeCleContenuDifferent_Retourne409Conflict()
    {
        var request1 = new CreerCommandeRequest(DroneIdValide, new[] { "C2_URLLC" });
        await _controller.Create(request1, ClientIdValide, CleIdempotenceValide, CancellationToken.None);

        var request2 = new CreerCommandeRequest(DroneIdValide, new[] { "IMAGERIE_EMBB" });
        var result2 = await _controller.Create(request2, ClientIdValide, CleIdempotenceValide, CancellationToken.None);

        var objResult = Assert.IsType<ObjectResult>(result2);
        Assert.Equal(StatusCodes.Status409Conflict, objResult.StatusCode);
    }

    [Fact]
    public async Task GetById_SansClientId_Retourne401Unauthorized()
    {
        var result = await _controller.GetById(Guid.NewGuid(), clientId: null, CancellationToken.None);

        var objResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, objResult.StatusCode);
    }

    [Fact]
    public async Task GetById_InexistantOuAutreClient_Retourne404NotFound_ScenarioQ2()
    {
        var request = new CreerCommandeRequest(DroneIdValide, ServicesValides);
        var createResult = await _controller.Create(request, ClientIdValide, CleIdempotenceValide, CancellationToken.None);
        var cree = Assert.IsType<CommandeResponse>(((CreatedAtActionResult)createResult).Value);

        // Tentative d'accès par un autre client
        var getResult = await _controller.GetById(cree.Id, AutreClientId, CancellationToken.None);

        var objResult = Assert.IsType<ObjectResult>(getResult);
        Assert.Equal(StatusCodes.Status404NotFound, objResult.StatusCode);
    }

    [Fact]
    public async Task GetById_Valide_Retourne200OkAvecCommandeEtElements()
    {
        var request = new CreerCommandeRequest(DroneIdValide, ServicesValides);
        var createResult = await _controller.Create(request, ClientIdValide, CleIdempotenceValide, CancellationToken.None);
        var cree = Assert.IsType<CommandeResponse>(((CreatedAtActionResult)createResult).Value);

        var getResult = await _controller.GetById(cree.Id, ClientIdValide, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(getResult);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var response = Assert.IsType<CommandeResponse>(okResult.Value);
        Assert.Equal(cree.Id, response.Id);
        Assert.Equal(2, response.Elements.Count);
    }
}
