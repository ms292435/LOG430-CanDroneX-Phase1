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
    public string Imsi { get; private set; }
    public string Modele { get; private set; }
    public StatutDrone Statut { get; private set; }
    public DateTime DateEnregistrement { get; private set; }

    // Constructeur complet interne
    private Drone(DroneId id, string imsi, string modele, StatutDrone statut, DateTime dateEnregistrement)
    {
        Id = id;
        Imsi = imsi;
        Modele = modele;
        Statut = statut;
        DateEnregistrement = dateEnregistrement;
    }

    // Point d'entrée métier pour l'enregistrement (UC-02)
    public static Drone Enregistrer(string imsi, string modele)
    {
        if (string.IsNullOrWhiteSpace(imsi))
            throw new DroneDomainException("L'IMSI est obligatoire.");

        var imsiNettoye = imsi.Trim();
        if (!EstImsiValide(imsiNettoye))
            throw new DroneDomainException("L'IMSI doit contenir entre 14 et 15 chiffres.");

        if (string.IsNullOrWhiteSpace(modele))
            throw new DroneDomainException("Le modèle du drone est obligatoire.");

        return new Drone(DroneId.Nouveau(), imsiNettoye, modele.Trim(), StatutDrone.Enregistre, DateTime.UtcNow);
    }

    // Reconstitution depuis la base de données par l'infrastructure
    public static Drone Reconstituer(DroneId id, string imsi, string modele, StatutDrone statut, DateTime dateEnregistrement) =>
        new(id, imsi, modele, statut, dateEnregistrement);

    private static bool EstImsiValide(string imsi) =>
        imsi.Length is >= 14 and <= 15 && imsi.All(char.IsDigit);
}