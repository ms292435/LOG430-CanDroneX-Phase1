using Drones.Domain;

namespace Drones.Application;

// Port sortant : ce que le module Drones REQUIERT de l'infrastructure.
// Nommé selon le besoin du domaine, pas selon la techno (pas "IDroneEfRepository").
public interface IDroneRepository
{
    Task SaveAsync(Drone drone, CancellationToken ct = default);
    Task<Drone?> GetByImsiAsync(string imsi, CancellationToken ct = default);
    Task<Drone?> GetByIdAsync(DroneId id, CancellationToken ct = default);
}
