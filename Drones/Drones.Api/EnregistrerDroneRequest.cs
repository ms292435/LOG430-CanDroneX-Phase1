using Drones.Application;

namespace Drones.Api;

// DTO exposé au monde extérieur. Ne jamais réutiliser Drone (entité) ici.
public sealed record EnregistrerDroneRequest(string Imsi, string Modele)
{
    public EnregistrerDroneCommand ToCommand() => new(Imsi, Modele);
}
