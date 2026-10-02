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
        // 1. Validation de l'agrégat Drone (garantit invariants métier)
        var drone = Drone.Enregistrer(commande.ClientId, commande.Imsi, commande.TypeCarte, commande.Modele);

        // 2. Vérification unicité de l'IMSI (A2 de UC-02)
        var existant = await _repository.GetByImsiAsync(drone.Imsi, ct);
        if (existant is not null)
            throw new ImsiDejaUtiliseException(drone.Imsi);

        // 3. Persistance
        await _repository.SaveAsync(drone, ct);
        return drone.Id;
    }

    public async Task<Drone?> ObtenirDroneAsync(DroneId id, string clientId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            return null;

        return await _repository.GetByIdAndClientIdAsync(id, clientId.Trim(), ct);
    }
}
