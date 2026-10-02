using System.Diagnostics;
using Commandes.Application;
using Commandes.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Commandes.Api;

[ApiController]
[Route("api/commandes")]
public sealed class CommandesController : ControllerBase
{
    private readonly ICommandePort _commandePort;

    public CommandesController(ICommandePort commandePort)
    {
        _commandePort = commandePort;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CommandeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromHeader(Name = "X-Client-Id")] string? clientId,
        CancellationToken ct)
    {
        // 1. Authentification minimale (§8.3 / §8.5)
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return CreerReponseProbleme(
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "L'en-tête 'X-Client-Id' est obligatoire pour identifier le client."
            );
        }

        // 2. Recherche filtrée par client (isolation B2B)
        var commande = await _commandePort.ObtenirCommandeAsync(id, clientId, ct);
        if (commande is null)
        {
            return CreerReponseProbleme(
                StatusCodes.Status404NotFound,
                "Not Found",
                $"Aucune commande trouvée avec l'identifiant '{id}'."
            );
        }

        return Ok(CommandeResponse.FromDomain(commande));
    }

    [HttpPost]
    [ProducesResponseType(typeof(CommandeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreerCommandeRequest? request,
        [FromHeader(Name = "X-Client-Id")] string? clientId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        // 1. Vérification de l'identification client (A5 / 401)
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return CreerReponseProbleme(
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "L'en-tête 'X-Client-Id' est obligatoire pour identifier le client."
            );
        }

        // 2. Vérification de la clé d'idempotence obligatoire (§8.2, ADR-004)
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return CreerReponseProbleme(
                StatusCodes.Status400BadRequest,
                "Bad Request",
                "L'en-tête 'Idempotency-Key' est obligatoire pour créer une commande."
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

        // 3. Validation de premier niveau (forme de la requête)
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
            // 4. Délégation au use case applicatif
            var commandeApp = request.ToCommand(clientId, idempotencyKey);
            var (commande, estRejeu) = await _commandePort.CreerCommandeAsync(commandeApp, ct);

            var reponse = CommandeResponse.FromDomain(commande);

            // Retourne 201 Created avec l'en-tête Location vers la ressource (§8.3 / §6.1)
            return CreatedAtAction(
                nameof(GetById),
                new { id = commande.Id },
                reponse
            );
        }
        catch (DroneIntrouvableException ex)
        {
            // A2 : Drone inconnu ou appartenant à un autre client -> 404 Not Found sans distinction
            return CreerReponseProbleme(
                StatusCodes.Status404NotFound,
                "Not Found",
                ex.Message
            );
        }
        catch (IdempotenceConflitException ex)
        {
            // A1 : Clé déjà utilisée avec contenu différent -> 409 Conflict
            return CreerReponseProbleme(
                StatusCodes.Status409Conflict,
                "Conflict",
                ex.Message
            );
        }
        catch (ServiceInconnuAuCatalogueException ex)
        {
            // A3 : Service absent du catalogue -> 400 Bad Request
            return CreerReponseProbleme(
                StatusCodes.Status400BadRequest,
                "Bad Request",
                ex.Message
            );
        }
        catch (CommandeDomainException ex)
        {
            // A4 : Règle métier violée (ex. commande sans élément ou service en double) -> 400 Bad Request
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
