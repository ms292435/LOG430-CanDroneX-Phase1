using Drones.Domain;

namespace Drones.Application;

// Port entrant : contrat fourni aux adaptateurs d'entrée (ex. DroneController)
public interface IDronePort
{
    Task<DroneId> EnregistrerDroneAsync(EnregistrerDroneCommand commande, CancellationToken ct = default);
    Task<Drone?> ObtenirDroneAsync(DroneId id, string clientId, CancellationToken ct = default);
}
