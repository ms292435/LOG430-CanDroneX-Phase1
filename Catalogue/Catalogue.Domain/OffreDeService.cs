namespace Catalogue.Domain;

/// <summary>
/// Racine d'agrégat représentant une offre de service au catalogue (§3.3).
/// </summary>
public sealed class OffreDeService
{
    public string TypeService { get; private set; }
    public string Libelle { get; private set; }
    public string ProfilReseau { get; private set; }

    private OffreDeService(string typeService, string libelle, string profilReseau)
    {
        TypeService = typeService;
        Libelle = libelle;
        ProfilReseau = profilReseau;
    }

    public static OffreDeService Creer(string typeService, string libelle, string profilReseau)
    {
        if (string.IsNullOrWhiteSpace(typeService))
            throw new CatalogueDomainException("Le type de service est obligatoire.");

        if (string.IsNullOrWhiteSpace(libelle))
            throw new CatalogueDomainException("Le libellé de l'offre est obligatoire.");

        if (string.IsNullOrWhiteSpace(profilReseau))
            throw new CatalogueDomainException("Le profil réseau visé est obligatoire.");

        var typeNormalise = TypeServiceCatalogue.Normaliser(typeService);

        return new OffreDeService(typeNormalise, libelle.Trim(), profilReseau.Trim());
    }
}
