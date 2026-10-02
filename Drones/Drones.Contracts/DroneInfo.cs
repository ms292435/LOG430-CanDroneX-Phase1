namespace Drones.Contracts;

/// <summary>
/// Objet d'échange inter-modules représentant les informations d'un drone (types simples uniquement).
/// </summary>
public sealed record DroneInfo(
    Guid Id,
    string ClientId,
    string Imsi,
    string TypeCarte,
    string Modele,
    string Statut,
    DateTime DateEnregistrement
);
