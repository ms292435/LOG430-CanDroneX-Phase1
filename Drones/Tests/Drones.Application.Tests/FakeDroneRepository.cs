using System.Collections.Concurrent;
using Drones.Application;
using Drones.Domain;

namespace Drones.Application.Tests;

// Faux dépôt en mémoire : permet de tester DroneUseCase sans BDD réelle,
// puisque le use case ne dépend que du port IDroneRepository.
public sealed class FakeDroneRepository : IDroneRepository
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

    public int NombreDeDronesEnregistres => _drones.Count;

    public void Clear() => _drones.Clear();
}
