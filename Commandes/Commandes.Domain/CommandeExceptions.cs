namespace Commandes.Domain;

public class CommandeDomainException : Exception
{
    public CommandeDomainException(string message) : base(message) { }
    public CommandeDomainException(string message, Exception innerException) : base(message, innerException) { }
}

public sealed class IdempotenceConflitException : CommandeDomainException
{
    public string CleIdempotence { get; }

    public IdempotenceConflitException(string cleIdempotence)
        : base($"La clé d'idempotence '{cleIdempotence}' a déjà été utilisée avec un contenu de commande différent.")
    {
        CleIdempotence = cleIdempotence;
    }
}
