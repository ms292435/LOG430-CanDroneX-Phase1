using Commandes.Application;
using Commandes.Domain;

namespace Commandes.Api;

public sealed record CreerCommandeRequest(
    Guid DroneId,
    IReadOnlyList<string>? Services
)
{
    public Dictionary<string, string[]> Valider()
    {
        var erreurs = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (DroneId == Guid.Empty)
        {
            erreurs["droneId"] = ["L'identifiant du drone est obligatoire."];
        }

        if (Services is null || Services.Count == 0)
        {
            erreurs["services"] = ["La liste des services demandés est obligatoire et doit contenir au moins un élément."];
        }
        else
        {
            var doublons = Services
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .GroupBy(s => s.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (doublons.Count > 0)
            {
                erreurs["services"] = [$"Le même service ne peut pas être commandé deux fois dans une même commande ({string.Join(", ", doublons)})."];
            }
        }

        return erreurs;
    }

    public CreerCommandeCommand ToCommand(string clientId, string cleIdempotence) =>
        new(
            clientId.Trim(),
            DroneId,
            cleIdempotence.Trim(),
            Services?.Select(s => s.Trim()).ToList() ?? (IReadOnlyList<string>)Array.Empty<string>()
        );
}

public sealed record ElementCommandeResponse(
    Guid Id,
    string TypeService,
    string Etat,
    string? CauseEchec
);

public sealed record CommandeResponse(
    Guid Id,
    string ClientId,
    Guid DroneId,
    string CleIdempotence,
    string Etat,
    DateTime DateCreation,
    IReadOnlyList<ElementCommandeResponse> Elements
)
{
    public static CommandeResponse FromDomain(Commande commande) =>
        new(
            commande.Id,
            commande.ClientId,
            commande.DroneId,
            commande.CleIdempotence,
            commande.Etat.ToString(),
            commande.DateCreation,
            commande.Elements.Select(e => new ElementCommandeResponse(
                e.Id,
                e.TypeService,
                e.Etat.ToString(),
                e.CauseEchec
            )).ToList()
        );
}
