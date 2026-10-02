namespace Drones.Domain;

public sealed record IdentiteReseau
{
    public Imsi Imsi { get; }
    public TypeCarte TypeCarte { get; }

    public IdentiteReseau(Imsi imsi, TypeCarte typeCarte)
    {
        Imsi = imsi ?? throw new DroneDomainException("L'IMSI est obligatoire pour l'identité réseau.");
        TypeCarte = typeCarte;
    }

    public IdentiteReseau(string imsi, TypeCarte typeCarte)
        : this(new Imsi(imsi), typeCarte)
    {
    }
}
