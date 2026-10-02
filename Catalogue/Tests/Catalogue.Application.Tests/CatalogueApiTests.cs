using Catalogue.Application;
using Catalogue.Domain;
using Xunit;

namespace Catalogue.Application.Tests;

internal sealed class FakeCatalogueRepository : ICatalogueRepository
{
    private readonly Dictionary<string, OffreDeService> _offres = new(StringComparer.OrdinalIgnoreCase);

    public FakeCatalogueRepository()
    {
        // Données de démonstration du cahier des charges (§3.1, §7.2)
        Ajouter(OffreDeService.Creer("C2_URLLC", "Commande et Contrôle (C2)", "URLLC - Faible latence, haute fiabilité, priorité élevée"));
        Ajouter(OffreDeService.Creer("IMAGERIE_EMBB", "Flux vidéo et imagerie mission", "eMBB - Haut débit montant, latence moins critique"));
    }

    public void Ajouter(OffreDeService offre)
    {
        _offres[offre.TypeService] = offre;
    }

    public Task<OffreDeService?> GetByTypeServiceAsync(string typeService, CancellationToken ct = default)
    {
        var clean = TypeServiceCatalogue.Normaliser(typeService);
        _offres.TryGetValue(clean, out var offre);
        return Task.FromResult(offre);
    }

    public Task<IReadOnlyList<OffreDeService>> GetAllAsync(CancellationToken ct = default)
    {
        IReadOnlyList<OffreDeService> liste = _offres.Values.ToList();
        return Task.FromResult(liste);
    }
}

public class CatalogueApiTests
{
    private readonly FakeCatalogueRepository _repository;
    private readonly CatalogueApi _api;

    public CatalogueApiTests()
    {
        _repository = new FakeCatalogueRepository();
        _api = new CatalogueApi(_repository);
    }

    [Fact]
    public async Task ObtenirOffreAsync_PourOffreExistanteC2_RetourneOffreInfo()
    {
        var offre = await _api.ObtenirOffreAsync("C2_URLLC");

        Assert.NotNull(offre);
        Assert.Equal("C2_URLLC", offre.TypeService);
        Assert.Equal("Commande et Contrôle (C2)", offre.Libelle);
        Assert.Contains("URLLC", offre.ProfilReseau);
    }

    [Fact]
    public async Task ObtenirOffreAsync_PourOffreExistanteImagerie_RetourneOffreInfo()
    {
        var offre = await _api.ObtenirOffreAsync("IMAGERIE_EMBB");

        Assert.NotNull(offre);
        Assert.Equal("IMAGERIE_EMBB", offre.TypeService);
        Assert.Equal("Flux vidéo et imagerie mission", offre.Libelle);
        Assert.Contains("eMBB", offre.ProfilReseau);
    }

    [Theory]
    [InlineData("c2/urllc")]
    [InlineData("c2-urllc")]
    [InlineData("imagerie/embb")]
    public async Task ObtenirOffreAsync_AvecVariantesDeNom_Reussit(string input)
    {
        var offre = await _api.ObtenirOffreAsync(input);

        Assert.NotNull(offre);
    }

    [Fact]
    public async Task ObtenirOffreAsync_PourOffreInconnue_RetourneNull_SansLeverException()
    {
        // Règle inter-modules ADR-003 : un cas attendu (service inconnu) retourne null, pas d'exception
        var offre = await _api.ObtenirOffreAsync("SERVICE_INEXISTANT");

        Assert.Null(offre);
    }

    [Fact]
    public async Task ObtenirToutesLesOffresAsync_RetourneLesDeuxOffresDeDemonstration()
    {
        var offres = await _api.ObtenirToutesLesOffresAsync();

        Assert.Equal(2, offres.Count);
        Assert.Contains(offres, o => o.TypeService == "C2_URLLC");
        Assert.Contains(offres, o => o.TypeService == "IMAGERIE_EMBB");
    }
}
