namespace Drones.Domain;

public sealed record Imsi
{
    public string Valeur { get; }

    public Imsi(string valeur)
    {
        if (string.IsNullOrWhiteSpace(valeur))
            throw new DroneDomainException("L'IMSI est obligatoire.");

        var imsiNettoye = valeur.Trim();
        if (!EstValide(imsiNettoye))
            throw new DroneDomainException("L'IMSI doit contenir entre 14 et 15 chiffres.");

        Valeur = imsiNettoye;
    }

    public static bool EstValide(string imsi) =>
        imsi.Length is >= 14 and <= 15 && imsi.All(char.IsDigit);

    public override string ToString() => Valeur;

    public static implicit operator string(Imsi imsi) => imsi.Valeur;
}
