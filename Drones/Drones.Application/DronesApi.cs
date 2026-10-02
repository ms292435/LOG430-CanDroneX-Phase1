using Drones.Contracts;
using Drones.Domain;

namespace Drones.Application;

public sealed class DronesApi : IDronesApi
{
    private readonly IDroneRepository _repository;

    public DronesApi(IDroneRepository repository)
    {
        _repository = repository;
    }

    public async Task<DroneInfo?> ObtenirDroneDuClientAsync(Guid droneId, string clientId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            return null;

        var drone = await _repository.GetByIdAndClientIdAsync(new DroneId(droneId), clientId.Trim(), ct);
        if (drone is null)
            return null;

        return new DroneInfo(
            drone.Id.Value,
            drone.ClientId,
            drone.Imsi,
            drone.TypeCarte.ToString(),
            drone.Modele,
            drone.Statut.ToString(),
            drone.DateEnregistrement
        );
    }
}
