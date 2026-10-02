using Commandes.Domain;

namespace Commandes.Application;

public sealed record CreerCommandeCommand(
    string ClientId,
    Guid DroneId,
    string CleIdempotence,
    IReadOnlyList<string> Services
);

public sealed class DroneIntrouvableException : Exception
{
    public Guid DroneId { get; }

    public DroneIntrouvableException(Guid droneId)
        : base($"Le drone avec l'identifiant '{droneId}' est introuvable ou n'appartient pas à ce client.")
    {
        DroneId = droneId;
    }
}

public sealed class ServiceInconnuAuCatalogueException : CommandeDomainException
{
    public string TypeService { get; }

    public ServiceInconnuAuCatalogueException(string typeService)
        : base($"Le service '{typeService}' n'existe pas au catalogue des offres.")
    {
        TypeService = typeService;
    }
}
