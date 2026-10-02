using Drones.Domain;

namespace Drones.Application;

public sealed record EnregistrerDroneCommand(
    string ClientId,
    string Imsi,
    TypeCarte TypeCarte,
    string Modele
);
