namespace Drones.Contracts;

/// <summary>
/// Contrat public offert par le module Drones aux autres modules (notamment Commandes).
/// </summary>
public interface IDronesApi
{
    Task<DroneInfo?> ObtenirDroneDuClientAsync(Guid droneId, string clientId, CancellationToken ct = default);
}
