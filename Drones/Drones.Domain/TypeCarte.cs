namespace Drones.Domain;

public enum TypeCarte
{
    Sim,
    ESim
}

public static class TypeCarteExtensions
{
    public static bool TryParseTypeCarte(string? valeur, out TypeCarte typeCarte)
    {
        if (string.IsNullOrWhiteSpace(valeur))
        {
            typeCarte = default;
            return false;
        }

        var v = valeur.Trim();
        if (string.Equals(v, "SIM", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(v, "Sim", StringComparison.OrdinalIgnoreCase))
        {
            typeCarte = TypeCarte.Sim;
            return true;
        }

        if (string.Equals(v, "eSIM", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(v, "ESim", StringComparison.OrdinalIgnoreCase))
        {
            typeCarte = TypeCarte.ESim;
            return true;
        }

        typeCarte = default;
        return false;
    }
}
