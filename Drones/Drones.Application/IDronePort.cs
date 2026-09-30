using Drones.Domain;

namespace Drones.Application;

// Port entrant : le contrat que le module Drones FOURNIT aux appelants
// (le contrôleur REST, ou un jour un autre module).
public interface IDronePort
{
    Task<DroneId> EnregistrerDroneAsync(EnregistrerDroneCommand commande, CancellationToken ct = default);
    Task<Drone?> ObtenirDroneAsync(DroneId id, CancellationToken ct = default);
}
