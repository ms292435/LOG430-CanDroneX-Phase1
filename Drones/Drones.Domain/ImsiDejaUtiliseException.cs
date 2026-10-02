namespace Drones.Domain;

/// <summary>
/// Exception levée quand un drone avec le même IMSI existe déjà (conflit d'unicité, HTTP 409).
/// </summary>
public sealed class ImsiDejaUtiliseException : DroneDomainException
{
    public string Imsi { get; }

    public ImsiDejaUtiliseException(string imsi) 
        : base($"Un drone avec l'IMSI spécifié est déjà enregistré.")
    {
        Imsi = imsi;
    }
}
