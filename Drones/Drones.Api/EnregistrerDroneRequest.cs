using Drones.Application;
using Drones.Domain;

namespace Drones.Api;

public sealed record EnregistrerDroneRequest(
    string? Imsi,
    string? TypeCarte,
    string? Modele
)
{
    public Dictionary<string, string[]> Valider()
    {
        var erreurs = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        // Validation du modèle
        if (string.IsNullOrWhiteSpace(Modele))
        {
            erreurs["modele"] = ["Le modèle du drone est obligatoire."];
        }

        // Validation de l'IMSI
        if (string.IsNullOrWhiteSpace(Imsi))
        {
            erreurs["imsi"] = ["L'IMSI est obligatoire."];
        }
        else if (!Drones.Domain.Imsi.EstValide(Imsi.Trim()))
        {
            erreurs["imsi"] = ["L'IMSI doit contenir entre 14 et 15 chiffres."];
        }

        // Validation du type de carte
        if (string.IsNullOrWhiteSpace(TypeCarte))
        {
            erreurs["typeCarte"] = ["Le type de carte est obligatoire (SIM ou eSIM)."];
        }
        else if (!TypeCarteExtensions.TryParseTypeCarte(TypeCarte, out _))
        {
            erreurs["typeCarte"] = ["Le type de carte doit être 'SIM' ou 'eSIM'."];
        }

        return erreurs;
    }

    public EnregistrerDroneCommand ToCommand(string clientId)
    {
        TypeCarteExtensions.TryParseTypeCarte(TypeCarte, out var typeCarte);
        return new EnregistrerDroneCommand(
            clientId.Trim(),
            Imsi!.Trim(),
            typeCarte,
            Modele!.Trim()
        );
    }
}
