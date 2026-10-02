using System.Diagnostics;
using Drones.Application;
using Drones.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Drones.Api;

[ApiController]
[Route("api/drones")]
public sealed class DroneController : ControllerBase
{
    private readonly IDronePort _dronePort;

    public DroneController(IDronePort dronePort)
    {
        _dronePort = dronePort;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DroneResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromHeader(Name = "X-Client-Id")] string? clientId,
        CancellationToken ct)
    {
        // 1. Vérification de l'identification du client B2B (401 si absent)
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return CreerReponseProbleme(
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "L'en-tête 'X-Client-Id' est obligatoire pour identifier le client."
            );
        }

        // 2. Recherche du drone filtrée par client (isolation stricte des données)
        var drone = await _dronePort.ObtenirDroneAsync(new DroneId(id), clientId, ct);
        if (drone is null)
        {
            // 404 sans distinction entre inexistant et appartenant à un autre client (Q2 / §8.3 / §8.5)
            return CreerReponseProbleme(
                StatusCodes.Status404NotFound,
                "Not Found",
                $"Aucun drone trouvé avec l'identifiant '{id}'."
            );
        }

        var response = new DroneResponse(
            drone.Id.Value,
            drone.ClientId,
            drone.Imsi,
            drone.TypeCarte.ToString(),
            drone.Modele,
            drone.Statut.ToString(),
            drone.DateEnregistrement);

        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(DroneCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] EnregistrerDroneRequest? request,
        [FromHeader(Name = "X-Client-Id")] string? clientId,
        CancellationToken ct)
    {
        // 1. Vérification de l'identification du client B2B (401 si absent)
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return CreerReponseProbleme(
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "L'en-tête 'X-Client-Id' est obligatoire pour identifier le client."
            );
        }

        if (request is null)
        {
            return CreerReponseProbleme(
                StatusCodes.Status400BadRequest,
                "Bad Request",
                "Le corps de la requête est obligatoire."
            );
        }

        // 2. Validation à deux niveaux : niveau contrôleur/forme (scénario Q1)
        var erreurs = request.Valider();
        if (erreurs.Count > 0)
        {
            var problem = new ValidationProblemDetails(erreurs)
            {
                Title = "Requête invalide",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Un ou plusieurs champs sont invalides."
            };
            var traceId = ObtenirTraceId();
            if (traceId is not null)
                problem.Extensions["traceId"] = traceId;

            return BadRequest(problem);
        }

        try
        {
            // 3. Délégation au use case et validation métier par le domaine
            var commande = request.ToCommand(clientId);
            var id = await _dronePort.EnregistrerDroneAsync(commande, ct);

            return CreatedAtAction(
                nameof(GetById),
                new { id = id.Value },
                new DroneCreatedResponse(id.Value));
        }
        catch (ImsiDejaUtiliseException ex)
        {
            // A2 de UC-02 : Conflit d'IMSI déjà associé -> 409 Conflict sans détail sur le drone existant
            return CreerReponseProbleme(
                StatusCodes.Status409Conflict,
                "Conflict",
                ex.Message
            );
        }
        catch (DroneDomainException ex)
        {
            // Violation de règle métier du domaine -> 400 Bad Request
            return CreerReponseProbleme(
                StatusCodes.Status400BadRequest,
                "Bad Request",
                ex.Message
            );
        }
    }

    private ObjectResult CreerReponseProbleme(int statusCode, string title, string detail)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = HttpContext?.Request?.Path
        };

        var traceId = ObtenirTraceId();
        if (traceId is not null)
            problemDetails.Extensions["traceId"] = traceId;

        return StatusCode(statusCode, problemDetails);
    }

    private string ObtenirTraceId() =>
        Activity.Current?.Id ?? HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString();
}
