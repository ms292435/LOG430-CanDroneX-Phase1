namespace Catalogue.Domain;

public static class TypeServiceCatalogue
{
    public const string C2Urllc = "C2_URLLC";
    public const string ImagerieEmbb = "IMAGERIE_EMBB";

    public static readonly IReadOnlyList<string> TypesValides = new[]
    {
        C2Urllc,
        ImagerieEmbb
    };

    public static string Normaliser(string? typeService)
    {
        if (string.IsNullOrWhiteSpace(typeService))
            return string.Empty;

        var clean = typeService.Trim().ToUpperInvariant()
            .Replace(" ", "_")
            .Replace("/", "_")
            .Replace("-", "_");

        if (clean.Contains("C2") || clean.Contains("URLLC"))
            return C2Urllc;

        if (clean.Contains("IMAGE") || clean.Contains("EMBB"))
            return ImagerieEmbb;

        return clean;
    }
}
