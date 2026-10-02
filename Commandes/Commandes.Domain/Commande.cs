using System.Security.Cryptography;
using System.Text;

namespace Commandes.Domain;

/// <summary>
/// Racine d'agrégat représentant une commande de connectivité pour un drone (§3.3 & §6.2).
/// Son état global est calculé dynamiquement à partir de celui de ses éléments.
/// </summary>
public sealed class Commande
{
    private readonly List<ElementDeCommande> _elements = new();

    public Guid Id { get; private set; }
    public string ClientId { get; private set; }
    public Guid DroneId { get; private set; }
    public string CleIdempotence { get; private set; }
    public string EmpreinteRequete { get; private set; }
    public DateTime DateCreation { get; private set; }

    public IReadOnlyList<ElementDeCommande> Elements => _elements.AsReadOnly();

    /// <summary>
    /// État global de la commande, calculé dynamiquement selon les règles du §6.2.
    /// Non persisté en base pour éviter toute contradiction avec les éléments (§8.1).
    /// </summary>
    public EtatCommande Etat
    {
        get
        {
            if (_elements.Count == 0)
                return EtatCommande.RECUE;

            // Règle 1 : Tous les éléments sont EN_ATTENTE -> RECUE
            if (_elements.All(e => e.Etat == EtatElement.EN_ATTENTE))
                return EtatCommande.RECUE;

            // Règle 6 : Tous les éléments sont annulés ou compensés -> ANNULEE
            if (_elements.All(e => e.Etat is EtatElement.ANNULE or EtatElement.COMPENSE))
                return EtatCommande.ANNULEE;

            var aDesElementsEnCours = _elements.Any(e => e.Etat is EtatElement.EN_ATTENTE or EtatElement.EN_ACTIVATION);
            var aDesActifs = _elements.Any(e => e.Etat == EtatElement.ACTIF);
            var aDesEchecs = _elements.Any(e => e.Etat == EtatElement.EN_ECHEC);

            // S'il reste des éléments non terminaux et ce n'est pas le cas RECUE -> EN_COURS
            if (aDesElementsEnCours)
                return EtatCommande.EN_COURS;

            // Tous les éléments sont dans un état terminal (ACTIF, EN_ECHEC, ANNULE, COMPENSE)
            // Règle 4 : Au moins un ACTIF et au moins un EN_ECHEC -> PARTIELLE
            if (aDesActifs && aDesEchecs)
                return EtatCommande.PARTIELLE;

            // Règle 5 : Aucun élément ACTIF, au moins un EN_ECHEC -> ECHOUEE
            if (!aDesActifs && aDesEchecs)
                return EtatCommande.ECHOUEE;

            // Règle 3 : Tous les éléments non annulés sont ACTIF -> COMPLETEE
            var elementsNonAnnules = _elements.Where(e => e.Etat is not (EtatElement.ANNULE or EtatElement.COMPENSE)).ToList();
            if (elementsNonAnnules.Count > 0 && elementsNonAnnules.All(e => e.Etat == EtatElement.ACTIF))
                return EtatCommande.COMPLETEE;

            return EtatCommande.EN_COURS;
        }
    }

    private Commande(
        Guid id,
        string clientId,
        Guid droneId,
        string cleIdempotence,
        string empreinteRequete,
        DateTime dateCreation,
        IEnumerable<ElementDeCommande> elements)
    {
        Id = id;
        ClientId = clientId;
        DroneId = droneId;
        CleIdempotence = cleIdempotence;
        EmpreinteRequete = empreinteRequete;
        DateCreation = dateCreation;

        foreach (var element in elements)
        {
            element.CommandeId = id;
            _elements.Add(element);
        }
    }

    // Point d'entrée métier pour la création (UC-04)
    public static Commande Creer(
        string clientId,
        Guid droneId,
        string cleIdempotence,
        IEnumerable<string> servicesDemandes)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new CommandeDomainException("L'identifiant du client est obligatoire.");

        if (droneId == Guid.Empty)
            throw new CommandeDomainException("L'identifiant du drone est obligatoire.");

        if (string.IsNullOrWhiteSpace(cleIdempotence))
            throw new CommandeDomainException("La clé d'idempotence est obligatoire.");

        if (servicesDemandes is null)
            throw new CommandeDomainException("La liste des services demandés est obligatoire.");

        var listeServices = servicesDemandes
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .ToList();

        // Règle A4 : Commande sans élément -> 400
        if (listeServices.Count == 0)
            throw new CommandeDomainException("Une commande doit contenir au moins un élément de service.");

        // Règle A4 : Même service demandé deux fois pour le même drone -> 400
        var doublons = listeServices
            .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (doublons.Count > 0)
            throw new CommandeDomainException($"Un même service ne peut pas être commandé deux fois dans une même commande ({string.Join(", ", doublons)}).");

        var commandeId = Guid.NewGuid();
        var empreinte = CalculerEmpreinteRequete(droneId, listeServices);

        var elements = listeServices.Select(ElementDeCommande.Creer).ToList();

        return new Commande(
            commandeId,
            clientId.Trim(),
            droneId,
            cleIdempotence.Trim(),
            empreinte,
            DateTime.UtcNow,
            elements
        );
    }

    // Reconstitution depuis la base de données
    public static Commande Reconstituer(
        Guid id,
        string clientId,
        Guid droneId,
        string cleIdempotence,
        string empreinteRequete,
        DateTime dateCreation,
        IEnumerable<ElementDeCommande> elements) =>
        new(id, clientId, droneId, cleIdempotence, empreinteRequete, dateCreation, elements);

    /// <summary>
    /// Calcule un hachage SHA-256 de l'identifiant du drone et de la liste triée des services (§8.2).
    /// </summary>
    public static string CalculerEmpreinteRequete(Guid droneId, IEnumerable<string> services)
    {
        var servicesTries = services
            .Select(s => s.Trim().ToUpperInvariant())
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();

        var chargeUtile = $"{droneId:N}:{string.Join(";", servicesTries)}";
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(chargeUtile));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
