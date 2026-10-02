namespace Drones.Domain;

public enum StatutDrone
{
    Enregistre,
    Actif,
    Desactive
}

public sealed class Drone
{
    public DroneId Id { get; private set; }
    public string ClientId { get; private set; }
    public IdentiteReseau IdentiteReseau { get; private set; }
    public string Modele { get; private set; }
    public StatutDrone Statut { get; private set; }
    public DateTime DateEnregistrement { get; private set; }

    // Accesseurs pratiques
    public string Imsi => IdentiteReseau.Imsi.Valeur;
    public TypeCarte TypeCarte => IdentiteReseau.TypeCarte;

    // Constructeur complet interne
    private Drone(
        DroneId id,
        string clientId,
        IdentiteReseau identiteReseau,
        string modele,
        StatutDrone statut,
        DateTime dateEnregistrement)
    {
        Id = id;
        ClientId = clientId;
        IdentiteReseau = identiteReseau;
        Modele = modele;
        Statut = statut;
        DateEnregistrement = dateEnregistrement;
    }

    // Point d'entrée métier pour l'enregistrement (UC-02)
    public static Drone Enregistrer(string clientId, string imsi, TypeCarte typeCarte, string modele)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new DroneDomainException("L'identifiant du client est obligatoire.");

        if (string.IsNullOrWhiteSpace(modele))
            throw new DroneDomainException("Le modèle du drone est obligatoire.");

        var identiteReseau = new IdentiteReseau(imsi, typeCarte);

        return new Drone(
            DroneId.Nouveau(),
            clientId.Trim(),
            identiteReseau,
            modele.Trim(),
            StatutDrone.Enregistre,
            DateTime.UtcNow
        );
    }

    // Reconstitution depuis la persistance (DAO)
    public static Drone Reconstituer(
        DroneId id,
        string clientId,
        IdentiteReseau identiteReseau,
        string modele,
        StatutDrone statut,
        DateTime dateEnregistrement) =>
        new(id, clientId, identiteReseau, modele, statut, dateEnregistrement);

    public static Drone Reconstituer(
        DroneId id,
        string clientId,
        string imsi,
        TypeCarte typeCarte,
        string modele,
        StatutDrone statut,
        DateTime dateEnregistrement) =>
        new(id, clientId, new IdentiteReseau(imsi, typeCarte), modele, statut, dateEnregistrement);
}