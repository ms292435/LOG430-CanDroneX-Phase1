using Catalogue.Contracts;
using Commandes.Domain;
using Drones.Contracts;

namespace Commandes.Application;

public sealed class CommandeUseCase : ICommandePort
{
    private readonly ICommandeRepository _repository;
    private readonly IDronesApi _dronesApi;
    private readonly ICatalogueApi _catalogueApi;

    public CommandeUseCase(
        ICommandeRepository repository,
        IDronesApi dronesApi,
        ICatalogueApi catalogueApi)
    {
        _repository = repository;
        _dronesApi = dronesApi;
        _catalogueApi = catalogueApi;
    }

    public async Task<(Commande Commande, bool EstRejeu)> CreerCommandeAsync(
        CreerCommandeCommand commande,
        CancellationToken ct = default)
    {
        // 1. Vérification préalable d'idempotence (§8.2, ADR-004)
        var existante = await _repository.GetByCleIdempotenceAsync(commande.ClientId, commande.CleIdempotence, ct);
        if (existante is not null)
        {
            var empreinteNouvelle = Commande.CalculerEmpreinteRequete(commande.DroneId, commande.Services);
            if (existante.EmpreinteRequete == empreinteNouvelle)
            {
                // Rejeu idempotent avec même contenu : retourne la commande existante sans rien recréer
                return (existante, EstRejeu: true);
            }

            // Même clé réutilisée avec contenu différent : conflit
            throw new IdempotenceConflitException(commande.CleIdempotence);
        }

        // 2. Vérification auprès du module Drones (existence et appartenance au client - §6.1 étape 2)
        var droneInfo = await _dronesApi.ObtenirDroneDuClientAsync(commande.DroneId, commande.ClientId, ct);
        if (droneInfo is null)
        {
            // Drone inconnu ou appartenant à un autre client (A2 / Q2 -> 404 sans distinction)
            throw new DroneIntrouvableException(commande.DroneId);
        }

        // 3. Vérification auprès du module Catalogue (chaque service demandé existe - §6.1 étape 3)
        foreach (var service in commande.Services)
        {
            var offre = await _catalogueApi.ObtenirOffreAsync(service, ct);
            if (offre is null)
            {
                // Service absent du catalogue (A3 -> 400 Bad Request)
                throw new ServiceInconnuAuCatalogueException(service);
            }
        }

        // 4. Création de l'agrégat du domaine (valide doublons et présence d'éléments - A4 -> 400)
        var nouvelleCommande = Commande.Creer(
            commande.ClientId,
            commande.DroneId,
            commande.CleIdempotence,
            commande.Services
        );

        // 5. Persistance transactionnelle (Scénario Q4 & §8.1)
        try
        {
            await _repository.SaveAsync(nouvelleCommande, ct);
        }
        catch (IdempotenceConflitException)
        {
            // En cas d'insertion concurrente avec la même clé, relire et appliquer la règle d'idempotence
            var commandeEnregistree = await _repository.GetByCleIdempotenceAsync(commande.ClientId, commande.CleIdempotence, ct);
            if (commandeEnregistree is not null && commandeEnregistree.EmpreinteRequete == nouvelleCommande.EmpreinteRequete)
            {
                return (commandeEnregistree, EstRejeu: true);
            }
            throw;
        }

        return (nouvelleCommande, EstRejeu: false);
    }

    public async Task<Commande?> ObtenirCommandeAsync(Guid commandeId, string clientId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            return null;

        return await _repository.GetByIdAndClientIdAsync(commandeId, clientId.Trim(), ct);
    }
}
