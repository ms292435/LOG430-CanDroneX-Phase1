namespace Catalogue.Contracts;

/// <summary>
/// DTO d'échange représentant une offre de service du catalogue (types simples uniquement selon §4.2).
/// </summary>
public sealed record OffreInfo(
    string TypeService,
    string Libelle,
    string ProfilReseau
);
