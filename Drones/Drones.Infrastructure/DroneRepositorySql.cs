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
            drone.ClientId,
            drone.Imsi,
            drone.TypeCarte.ToString(),
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

    public async Task<Drone?> GetByIdAndClientIdAsync(DroneId id, string clientId, CancellationToken ct = default)
    {
        var record = await _dao.GetByIdAndClientIdAsync(id.Value, clientId, ct);
        return record is null ? null : Reconstituer(record);
    }

    public async Task<Drone?> GetByImsiAsync(string imsi, CancellationToken ct = default)
    {
        var record = await _dao.GetByImsiAsync(imsi, ct);
        return record is null ? null : Reconstituer(record);
    }

    private static Drone Reconstituer(DroneRecord record)
    {
        var statut = Enum.Parse<StatutDrone>(record.Statut, ignoreCase: true);
        if (!TypeCarteExtensions.TryParseTypeCarte(record.TypeCarte, out var typeCarte))
        {
            typeCarte = TypeCarte.Sim;
        }

        return Drone.Reconstituer(
            new DroneId(record.Id),
            record.ClientId,
            record.Imsi,
            typeCarte,
            record.Modele,
            statut,
            record.DateEnregistrement
        );
    }
}