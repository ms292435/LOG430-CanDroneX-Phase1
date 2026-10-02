using Drones.Domain;

namespace Drones.Application;

// Port sortant : ce que le module Drones requiert de l'infrastructure de persistance.
public interface IDroneRepository
{
    Task SaveAsync(Drone drone, CancellationToken ct = default);
    Task<Drone?> GetByImsiAsync(string imsi, CancellationToken ct = default);
    Task<Drone?> GetByIdAsync(DroneId id, CancellationToken ct = default);
    Task<Drone?> GetByIdAndClientIdAsync(DroneId id, string clientId, CancellationToken ct = default);
}
