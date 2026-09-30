using Drones.Application;
using Drones.Domain;

namespace Drones.Infrastructure;

public sealed class DroneRepositorySql : IDroneRepository
{
    private readonly DroneDao _dao;

    public DroneRepositorySql(DroneDao dao)
    {
        _dao = dao;
    }

    public async Task SaveAsync(Drone drone, CancellationToken ct = default)
    {
        var record = new DroneRecord(
            drone.Id.Value,
            "client-demo", // Préparé pour le clientId de l'étape 4
            drone.Imsi,
            drone.Modele,
            drone.Statut.ToString(),
            drone.DateEnregistrement
        );

        await _dao.InsertAsync(record, ct);
    }

    public async Task<Drone?> GetByIdAsync(DroneId id, CancellationToken ct = default)
    {
        var record = await _dao.GetByIdAsync(id.Value, ct);
        return record is null ? null : Reconstituer(record);
    }

    public async Task<Drone?> GetByImsiAsync(string imsi, CancellationToken ct = default)
    {
        var record = await _dao.GetByImsiAsync(imsi, ct);
        return record is null ? null : Reconstituer(record);
    }

    private static Drone Reconstituer(DroneRecord record)
    {
        var statut = Enum.Parse<StatutDrone>(record.Statut);
        return Drone.Reconstituer(
            new DroneId(record.Id),
            record.Imsi,
            record.Modele,
            statut,
            record.DateEnregistrement
        );
    }
}