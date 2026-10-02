using Commandes.Application;
using Commandes.Domain;

namespace Commandes.Infrastructure;

public sealed class CommandeRepositorySql : ICommandeRepository
{
    private readonly CommandeDao _dao;

    public CommandeRepositorySql(CommandeDao dao)
    {
        _dao = dao;
    }

    public async Task SaveAsync(Commande commande, CancellationToken ct = default)
    {
        var record = new CommandeRecord(
            commande.Id,
            commande.ClientId,
            commande.DroneId,
            commande.CleIdempotence,
            commande.EmpreinteRequete,
            commande.DateCreation
        );

        var elementRecords = commande.Elements.Select(e => new ElementCommandeRecord(
            e.Id,
            commande.Id,
            e.TypeService,
            e.Etat.ToString(),
            e.CauseEchec
        )).ToList();

        await _dao.InsertTransactionnelAsync(record, elementRecords, ct);
    }

    public async Task<Commande?> GetByIdAndClientIdAsync(Guid id, string clientId, CancellationToken ct = default)
    {
        var (cmdRecord, elemRecords) = await _dao.GetByIdAndClientIdAsync(id, clientId, ct);
        if (cmdRecord is null)
            return null;

        return Reconstituer(cmdRecord, elemRecords);
    }

    public async Task<Commande?> GetByCleIdempotenceAsync(string clientId, string cleIdempotence, CancellationToken ct = default)
    {
        var (cmdRecord, elemRecords) = await _dao.GetByCleIdempotenceAsync(clientId, cleIdempotence, ct);
        if (cmdRecord is null)
            return null;

        return Reconstituer(cmdRecord, elemRecords);
    }

    private static Commande Reconstituer(CommandeRecord commande, IEnumerable<ElementCommandeRecord> elements)
    {
        var domainElements = elements.Select(e =>
        {
            var etat = Enum.Parse<EtatElement>(e.Etat, ignoreCase: true);
            return ElementDeCommande.Reconstituer(e.Id, e.CommandeId, e.TypeService, etat, e.CauseEchec);
        }).ToList();

        return Commande.Reconstituer(
            commande.Id,
            commande.ClientId,
            commande.DroneId,
            commande.CleIdempotence,
            commande.EmpreinteRequete,
            commande.DateCreation,
            domainElements
        );
    }
}
