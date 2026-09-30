using Drones.Application;
using Drones.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Drones.Api;

[ApiController]
[Route("api/drones")]
public sealed class DroneController : ControllerBase
{
    private readonly IDronePort _dronePort;

    public DroneController(IDronePort dronePort) => _dronePort = dronePort;

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DroneResponse>> GetById(Guid id, CancellationToken ct)
    {
        var drone = await _dronePort.ObtenirDroneAsync(new DroneId(id), ct);
        if (drone is null)
            return NotFound(new { erreur = $"Aucun drone trouvé avec l'identifiant {id}." });
        var response = new DroneResponse(
            drone.Id.Value,
            drone.Imsi,
            drone.Modele,
            drone.Statut.ToString(),
            drone.DateEnregistrement);
        return Ok(response);
    }

    // Traduit HTTP/JSON vers le port. Aucune règle métier ici.
    [HttpPost]
    public async Task<ActionResult<DroneCreatedResponse>> Create(
        [FromBody] EnregistrerDroneRequest request, CancellationToken ct)
    {
        try
        {
            // Conversion du DTO vers la commande applicative
            var commande = new EnregistrerDroneCommand(request.Imsi, request.Modele);
            var id = await _dronePort.EnregistrerDroneAsync(commande, ct);
            // Redirection vers le GET via nameof(GetById)
            return CreatedAtAction(
                nameof(GetById),
                new { id = id.Value },
                new DroneCreatedResponse(id.Value));
        }
        catch (DroneDomainException ex)
        {
            return BadRequest(new { erreur = ex.Message });
        }
    }
}
