namespace Commandes.Contracts;

public sealed record ElementCommandeInfo(
    Guid Id,
    string TypeService,
    string Etat,
    string? CauseEchec
);

public sealed record CommandeInfo(
    Guid Id,
    string ClientId,
    Guid DroneId,
    string CleIdempotence,
    string Etat,
    DateTime DateCreation,
    IReadOnlyList<ElementCommandeInfo> Elements
);

public interface ICommandesApi
{
    Task<CommandeInfo?> ObtenirCommandeDuClientAsync(Guid commandeId, string clientId, CancellationToken ct = default);
}
