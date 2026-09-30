namespace Drones.Domain;

/// Exception levée quand une règle métier du module Drones est violée.
/// Ne jamais laisser fuir une exception technique (SQL, EF Core...) à travers celle-ci.
public sealed class DroneDomainException : Exception
{
    public DroneDomainException(string message) : base(message) { }
}
