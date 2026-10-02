namespace Drones.Domain;

/// <summary>
/// Exception de base levée quand une règle métier du module Drones est violée.
/// Ne jamais laisser fuir une exception technique (SQL, EF Core...) à travers celle-ci.
/// </summary>
public class DroneDomainException : Exception
{
    public DroneDomainException(string message) : base(message) { }
    public DroneDomainException(string message, Exception innerException) : base(message, innerException) { }
}
