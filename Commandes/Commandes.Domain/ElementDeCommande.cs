namespace Commandes.Domain;

/// <summary>
/// Entité représentant un élément au sein d'une commande (§3.3 & §6.2).
/// Chaque élément porte son propre état et cycle de vie.
/// </summary>
public sealed class ElementDeCommande
{
    public Guid Id { get; private set; }
    public Guid CommandeId { get; internal set; }
    public string TypeService { get; private set; }
    public EtatElement Etat { get; private set; }
    public string? CauseEchec { get; private set; }

    private ElementDeCommande(Guid id, Guid commandeId, string typeService, EtatElement etat, string? causeEchec)
    {
        Id = id;
        CommandeId = commandeId;
        TypeService = typeService;
        Etat = etat;
        CauseEchec = causeEchec;
    }

    public static ElementDeCommande Creer(string typeService)
    {
        if (string.IsNullOrWhiteSpace(typeService))
            throw new CommandeDomainException("Le type de service de l'élément de commande est obligatoire.");

        return new ElementDeCommande(
            Guid.NewGuid(),
            Guid.Empty,
            typeService.Trim(),
            EtatElement.EN_ATTENTE,
            null
        );
    }

    public static ElementDeCommande Reconstituer(Guid id, Guid commandeId, string typeService, EtatElement etat, string? causeEchec) =>
        new(id, commandeId, typeService, etat, causeEchec);

    // Transitions autorisées du cycle de vie (§6.2)
    public void DemarrerActivation()
    {
        if (Etat != EtatElement.EN_ATTENTE)
            throw new CommandeDomainException($"Transition invalide : impossible de démarrer l'activation depuis l'état {Etat}.");

        Etat = EtatElement.EN_ACTIVATION;
        CauseEchec = null;
    }

    public void ConfirmerActivation()
    {
        if (Etat != EtatElement.EN_ACTIVATION)
            throw new CommandeDomainException($"Transition invalide : impossible de confirmer l'activation depuis l'état {Etat}.");

        Etat = EtatElement.ACTIF;
        CauseEchec = null;
    }

    public void SignalerEchec(string cause)
    {
        if (Etat != EtatElement.EN_ACTIVATION)
            throw new CommandeDomainException($"Transition invalide : impossible de signaler un échec depuis l'état {Etat}.");

        Etat = EtatElement.EN_ECHEC;
        CauseEchec = string.IsNullOrWhiteSpace(cause) ? "Échec d'activation non spécifié" : cause.Trim();
    }

    public void Reprendre()
    {
        if (Etat != EtatElement.EN_ECHEC)
            throw new CommandeDomainException($"Transition invalide : seule une activation en échec peut être reprise (état actuel : {Etat}).");

        Etat = EtatElement.EN_ACTIVATION;
        CauseEchec = null;
    }

    public void Annuler()
    {
        if (Etat == EtatElement.EN_ATTENTE)
        {
            // Annulé sans avoir été actif (§6.2, §12.1)
            Etat = EtatElement.ANNULE;
        }
        else if (Etat == EtatElement.ACTIF)
        {
            // Était actif, donc compensé / désactivé (§6.2, §12.1)
            Etat = EtatElement.COMPENSE;
        }
        else
        {
            throw new CommandeDomainException($"Transition invalide : impossible d'annuler un élément à l'état {Etat}.");
        }
    }
}
