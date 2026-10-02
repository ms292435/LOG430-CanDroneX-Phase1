using Commandes.Domain;
using Xunit;

namespace Commandes.Domain.Tests;

public class CommandeDomainTests
{
    private const string ClientIdValide = "client-demo";
    private static readonly Guid DroneIdValide = Guid.NewGuid();
    private const string CleIdempotenceValide = "cle-12345";
    private static readonly string[] ServicesValides = new[] { "C2_URLLC", "IMAGERIE_EMBB" };

    [Fact]
    public void Creer_AvecParametresValides_CreeCommandeAEtatRecueEtElementsEnAttente()
    {
        var commande = Commande.Creer(ClientIdValide, DroneIdValide, CleIdempotenceValide, ServicesValides);

        Assert.NotEqual(Guid.Empty, commande.Id);
        Assert.Equal(ClientIdValide, commande.ClientId);
        Assert.Equal(DroneIdValide, commande.DroneId);
        Assert.Equal(CleIdempotenceValide, commande.CleIdempotence);
        Assert.NotEmpty(commande.EmpreinteRequete);
        Assert.Equal(2, commande.Elements.Count);
        Assert.All(commande.Elements, e => Assert.Equal(EtatElement.EN_ATTENTE, e.Etat));
        Assert.Equal(EtatCommande.RECUE, commande.Etat);
    }

    [Fact]
    public void Creer_SansElements_LeveCommandeDomainException()
    {
        var ex = Assert.Throws<CommandeDomainException>(
            () => Commande.Creer(ClientIdValide, DroneIdValide, CleIdempotenceValide, Array.Empty<string>()));

        Assert.Contains("au moins un élément", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Creer_AvecMemeServiceDemandeDeuxFois_LeveCommandeDomainException()
    {
        var servicesAvecDoublon = new[] { "C2_URLLC", "C2_URLLC" };

        var ex = Assert.Throws<CommandeDomainException>(
            () => Commande.Creer(ClientIdValide, DroneIdValide, CleIdempotenceValide, servicesAvecDoublon));

        Assert.Contains("deux fois", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Creer_AvecClientIdVide_LeveCommandeDomainException(string? clientId)
    {
        var ex = Assert.Throws<CommandeDomainException>(
            () => Commande.Creer(clientId!, DroneIdValide, CleIdempotenceValide, ServicesValides));

        Assert.Contains("client", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Creer_AvecDroneIdVide_LeveCommandeDomainException()
    {
        var ex = Assert.Throws<CommandeDomainException>(
            () => Commande.Creer(ClientIdValide, Guid.Empty, CleIdempotenceValide, ServicesValides));

        Assert.Contains("drone", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Creer_AvecCleIdempotenceVide_LeveCommandeDomainException(string? cle)
    {
        var ex = Assert.Throws<CommandeDomainException>(
            () => Commande.Creer(ClientIdValide, DroneIdValide, cle!, ServicesValides));

        Assert.Contains("idempotence", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CalculerEmpreinteRequete_AvecOrdreDifferentDeServices_RetourneLaMemeEmpreinte()
    {
        var empreinte1 = Commande.CalculerEmpreinteRequete(DroneIdValide, new[] { "C2_URLLC", "IMAGERIE_EMBB" });
        var empreinte2 = Commande.CalculerEmpreinteRequete(DroneIdValide, new[] { "IMAGERIE_EMBB", "C2_URLLC" });

        Assert.Equal(empreinte1, empreinte2);
    }

    [Fact]
    public void CalculerEmpreinteRequete_AvecServicesDifferents_RetourneEmpreinteDifferente()
    {
        var empreinte1 = Commande.CalculerEmpreinteRequete(DroneIdValide, new[] { "C2_URLLC" });
        var empreinte2 = Commande.CalculerEmpreinteRequete(DroneIdValide, new[] { "IMAGERIE_EMBB" });

        Assert.NotEqual(empreinte1, empreinte2);
    }

    // ========================================================================
    // Tests du cycle de vie et calcul de l'état global (§6.2 du document)
    // ========================================================================

    [Fact]
    public void CycleDeVie_UnElementEnCoursDActivation_EtatGlobalDevientEnCours()
    {
        var commande = Commande.Creer(ClientIdValide, DroneIdValide, CleIdempotenceValide, ServicesValides);
        var c2 = commande.Elements.First(e => e.TypeService == "C2_URLLC");

        c2.DemarrerActivation();

        Assert.Equal(EtatElement.EN_ACTIVATION, c2.Etat);
        Assert.Equal(EtatCommande.EN_COURS, commande.Etat);
    }

    [Fact]
    public void CycleDeVie_TousElementsActifs_EtatGlobalDevientCompletee()
    {
        var commande = Commande.Creer(ClientIdValide, DroneIdValide, CleIdempotenceValide, ServicesValides);
        foreach (var element in commande.Elements)
        {
            element.DemarrerActivation();
            element.ConfirmerActivation();
        }

        Assert.All(commande.Elements, e => Assert.Equal(EtatElement.ACTIF, e.Etat));
        Assert.Equal(EtatCommande.COMPLETEE, commande.Etat);
    }

    [Fact]
    public void CycleDeVie_C2ActifEtImagerieEnEchec_EtatGlobalDevientPartielle_ScenarioCritique()
    {
        // Spécification explicite §6.2 et §2.2 (BVLOS) :
        // C2 ACTIF, Imagerie EN_ECHEC -> PARTIELLE, pas ECHOUEE, pour ne pas masquer que le C2 fonctionne.
        var commande = Commande.Creer(ClientIdValide, DroneIdValide, CleIdempotenceValide, ServicesValides);
        var c2 = commande.Elements.First(e => e.TypeService == "C2_URLLC");
        var imagerie = commande.Elements.First(e => e.TypeService == "IMAGERIE_EMBB");

        c2.DemarrerActivation();
        c2.ConfirmerActivation();

        imagerie.DemarrerActivation();
        imagerie.SignalerEchec("Délai de réponse réseau 5G dépassé");

        Assert.Equal(EtatElement.ACTIF, c2.Etat);
        Assert.Equal(EtatElement.EN_ECHEC, imagerie.Etat);
        Assert.Equal("Délai de réponse réseau 5G dépassé", imagerie.CauseEchec);
        Assert.Equal(EtatCommande.PARTIELLE, commande.Etat);
    }

    [Fact]
    public void CycleDeVie_C2ActifEtImagerieAnnulee_EtatGlobalDevientCompletee()
    {
        // Spécification §6.2 : C2 ACTIF, Imagerie ANNULE -> COMPLETEE.
        var commande = Commande.Creer(ClientIdValide, DroneIdValide, CleIdempotenceValide, ServicesValides);
        var c2 = commande.Elements.First(e => e.TypeService == "C2_URLLC");
        var imagerie = commande.Elements.First(e => e.TypeService == "IMAGERIE_EMBB");

        c2.DemarrerActivation();
        c2.ConfirmerActivation();

        imagerie.Annuler(); // Annulation sans avoir été actif -> ANNULE

        Assert.Equal(EtatElement.ACTIF, c2.Etat);
        Assert.Equal(EtatElement.ANNULE, imagerie.Etat);
        Assert.Equal(EtatCommande.COMPLETEE, commande.Etat);
    }

    [Fact]
    public void CycleDeVie_TousElementsEnEchec_EtatGlobalDevientEchouee()
    {
        var commande = Commande.Creer(ClientIdValide, DroneIdValide, CleIdempotenceValide, ServicesValides);
        foreach (var element in commande.Elements)
        {
            element.DemarrerActivation();
            element.SignalerEchec("Réseau indisponible");
        }

        Assert.Equal(EtatCommande.ECHOUEE, commande.Etat);
    }

    [Fact]
    public void CycleDeVie_C2CompenseEtImagerieAnnule_EtatGlobalDevientAnnulee()
    {
        // Spécification §6.2 : C2 COMPENSE, Imagerie ANNULE -> ANNULEE.
        var commande = Commande.Creer(ClientIdValide, DroneIdValide, CleIdempotenceValide, ServicesValides);
        var c2 = commande.Elements.First(e => e.TypeService == "C2_URLLC");
        var imagerie = commande.Elements.First(e => e.TypeService == "IMAGERIE_EMBB");

        c2.DemarrerActivation();
        c2.ConfirmerActivation();
        c2.Annuler(); // Était actif -> COMPENSE

        imagerie.Annuler(); // Était en attente -> ANNULE

        Assert.Equal(EtatElement.COMPENSE, c2.Etat);
        Assert.Equal(EtatElement.ANNULE, imagerie.Etat);
        Assert.Equal(EtatCommande.ANNULEE, commande.Etat);
    }

    [Fact]
    public void CycleDeVie_RepriseDUnElementEnEchec_RepasseEnActivationEtCommandeDevientEnCours()
    {
        // Spécification §6.2 (UC-10 reprise simulée) :
        var commande = Commande.Creer(ClientIdValide, DroneIdValide, CleIdempotenceValide, ServicesValides);
        var c2 = commande.Elements.First(e => e.TypeService == "C2_URLLC");
        var imagerie = commande.Elements.First(e => e.TypeService == "IMAGERIE_EMBB");

        c2.DemarrerActivation();
        c2.ConfirmerActivation();

        imagerie.DemarrerActivation();
        imagerie.SignalerEchec("Erreur temporaire");
        Assert.Equal(EtatCommande.PARTIELLE, commande.Etat);

        // Reprise
        imagerie.Reprendre();
        Assert.Equal(EtatElement.EN_ACTIVATION, imagerie.Etat);
        Assert.Null(imagerie.CauseEchec);
        Assert.Equal(EtatCommande.EN_COURS, commande.Etat);
    }

    [Fact]
    public void TransitionsInvalides_LeventCommandeDomainException()
    {
        var element = ElementDeCommande.Creer("C2_URLLC");

        // Ne peut pas confirmer avant de démarrer
        Assert.Throws<CommandeDomainException>(() => element.ConfirmerActivation());

        // Ne peut pas reprendre un élément qui n'est pas en échec
        Assert.Throws<CommandeDomainException>(() => element.Reprendre());

        // Ne peut pas signaler un échec s'il n'est pas en cours d'activation
        Assert.Throws<CommandeDomainException>(() => element.SignalerEchec("erreur"));
    }
}
