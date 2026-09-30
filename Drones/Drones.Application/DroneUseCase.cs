using Drones.Domain;

namespace Drones.Application;

public sealed class DroneUseCase : IDronePort
{
    private readonly IDroneRepository _repository;

    public DroneUseCase(IDroneRepository repository)
    {
        _repository = repository;
    }

    public async Task<DroneId> EnregistrerDroneAsync(EnregistrerDroneCommand commande, CancellationToken ct = default)
    {
        var existant = await _repository.GetByImsiAsync(commande.Imsi, ct);
        if (existant is not null)
            throw new DroneDomainException($"Un drone avec l'IMSI {commande.Imsi} est déjà enregistré.");

        var drone = Drone.Enregistrer(commande.Imsi, commande.Modele);
        await _repository.SaveAsync(drone, ct);
        return drone.Id;
    }

    public async Task<Drone?> ObtenirDroneAsync(DroneId id, CancellationToken ct = default)
    {
        return await _repository.GetByIdAsync(id, ct);
    }
}
