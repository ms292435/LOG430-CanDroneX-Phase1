namespace Drones.Domain;

public readonly record struct DroneId(Guid Value)
{
    public static DroneId Nouveau() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
