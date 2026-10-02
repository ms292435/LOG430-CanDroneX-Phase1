namespace Commandes.Domain;

public enum EtatElement
{
    EN_ATTENTE,
    EN_ACTIVATION,
    ACTIF,
    EN_ECHEC,
    ANNULE,
    COMPENSE
}

public enum EtatCommande
{
    RECUE,
    EN_COURS,
    COMPLETEE,
    PARTIELLE,
    ECHOUEE,
    ANNULEE
}
