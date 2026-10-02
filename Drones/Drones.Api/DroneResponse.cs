namespace Drones.Api;

public sealed record DroneResponse(
    Guid Id,
    string ClientId,
    string Imsi,
    string TypeCarte,
    string Modele,
    string Statut,
    DateTime DateEnregistrement
);