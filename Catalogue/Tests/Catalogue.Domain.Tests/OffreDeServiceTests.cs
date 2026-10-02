using Catalogue.Domain;
using Xunit;

namespace Catalogue.Domain.Tests;

public class OffreDeServiceTests
{
    [Fact]
    public void Creer_AvecParametresValides_CreeOffreDeService()
    {
        var offre = OffreDeService.Creer(
            "C2_URLLC",
            "Commande et Contrôle (C2)",
            "URLLC - Faible latence, haute fiabilité"
        );

        Assert.Equal("C2_URLLC", offre.TypeService);
        Assert.Equal("Commande et Contrôle (C2)", offre.Libelle);
        Assert.Equal("URLLC - Faible latence, haute fiabilité", offre.ProfilReseau);
    }

    [Theory]
    [InlineData("c2_urllc", "C2_URLLC")]
    [InlineData("c2/urllc", "C2_URLLC")]
    [InlineData("c2-urllc", "C2_URLLC")]
    [InlineData("imagerie_embb", "IMAGERIE_EMBB")]
    [InlineData("imagerie/embb", "IMAGERIE_EMBB")]
    public void Creer_NormaliseLeNomDeService(string input, string attendu)
    {
        var offre = OffreDeService.Creer(input, "Libellé", "Profil");

        Assert.Equal(attendu, offre.TypeService);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Creer_AvecTypeServiceVide_LeveException(string? typeService)
    {
        var ex = Assert.Throws<CatalogueDomainException>(
            () => OffreDeService.Creer(typeService!, "Libellé", "Profil"));

        Assert.Contains("type de service", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Creer_AvecLibelleVide_LeveException(string? libelle)
    {
        var ex = Assert.Throws<CatalogueDomainException>(
            () => OffreDeService.Creer("C2_URLLC", libelle!, "Profil"));

        Assert.Contains("libellé", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Creer_AvecProfilReseauVide_LeveException(string? profilReseau)
    {
        var ex = Assert.Throws<CatalogueDomainException>(
            () => OffreDeService.Creer("C2_URLLC", "Libellé", profilReseau!));

        Assert.Contains("profil réseau", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
