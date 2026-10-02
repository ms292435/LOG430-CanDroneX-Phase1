using Commandes.Domain;

namespace Commandes.Application;

public interface ICommandeRepository
{
    Task SaveAsync(Commande commande, CancellationToken ct = default);
    Task<Commande?> GetByIdAndClientIdAsync(Guid id, string clientId, CancellationToken ct = default);
    Task<Commande?> GetByCleIdempotenceAsync(string clientId, string cleIdempotence, CancellationToken ct = default);
}

public interface ICommandePort
{
    Task<(Commande Commande, bool EstRejeu)> CreerCommandeAsync(CreerCommandeCommand commande, CancellationToken ct = default);
    Task<Commande?> ObtenirCommandeAsync(Guid commandeId, string clientId, CancellationToken ct = default);
}
