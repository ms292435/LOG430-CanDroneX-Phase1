namespace Catalogue.Domain;

public class CatalogueDomainException : Exception
{
    public CatalogueDomainException(string message) : base(message) { }
    public CatalogueDomainException(string message, Exception innerException) : base(message, innerException) { }
}
