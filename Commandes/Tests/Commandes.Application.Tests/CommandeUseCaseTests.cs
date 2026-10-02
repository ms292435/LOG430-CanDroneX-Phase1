using Catalogue.Contracts;
using Commandes.Application;
using Commandes.Domain;
using Drones.Contracts;
using Xunit;

namespace Commandes.Application.Tests;

internal sealed class FakeCommandeRepository : ICommandeRepository
{
    private readonly Dictionary<Guid, Commande> _commandes = new();

    public Task SaveAsync(Commande commande, CancellationToken ct = default)
    {
        _commandes[commande.Id] = commande;
        return Task.CompletedTask;
    }

    public Task<Commande?> GetByIdAndClientIdAsync(Guid id, string clientId, CancellationToken ct = default)
    {
        _commandes.TryGetValue(id, out var cmd);
        if (cmd is not null && cmd.ClientId == clientId)
            return Task.FromResult<Commande?>(cmd);

        return Task.FromResult<Commande?>(null);
    }

    public Task<Commande?> GetByCleIdempotenceAsync(string clientId, string cleIdempotence, CancellationToken ct = default)
    {
        var cmd = _commandes.Values.FirstOrDefault(c =>
            c.ClientId == clientId && c.CleIdempotence == cleIdempotence);

        return Task.FromResult(cmd);
    }

    public int Count => _commandes.Count;
}

internal sealed class FakeDronesApiForCommandes : IDronesApi
{
    private readonly Dictionary<(Guid DroneId, string ClientId), DroneInfo> _drones = new();

    public void EnregistrerDrone(Guid droneId, string clientId)
    {
        _drones[(droneId, clientId)] = new DroneInfo(
            droneId,
            clientId,
            "123456789012345",
            "Sim",
            "DJI Matrice 300",
            "Enregistre",
            DateTime.UtcNow
        );
    }

    public Task<DroneInfo?> ObtenirDroneDuClientAsync(Guid droneId, string clientId, CancellationToken ct = default)
    {
        _drones.TryGetValue((droneId, clientId), out var drone);
        return Task.FromResult(drone);
    }
}

internal sealed class FakeCatalogueApiForCommandes : ICatalogueApi
{
    private readonly HashSet<string> _servicesValides = new(StringComparer.OrdinalIgnoreCase)
    {
        "C2_URLLC",
        "IMAGERIE_EMBB"
    };

    public Task<OffreInfo?> ObtenirOffreAsync(string typeService, CancellationToken ct = default)
    {
        if (_servicesValides.Contains(typeService))
        {
            return Task.FromResult<OffreInfo?>(new OffreInfo(typeService, "Service Démo", "URLLC/eMBB"));
        }
        return Task.FromResult<OffreInfo?>(null);
    }

    public Task<IReadOnlyList<OffreInfo>> ObtenirToutesLesOffresAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<OffreInfo>>(
            _servicesValides.Select(s => new OffreInfo(s, s, "Profil")).ToList()
        );
}

public class CommandeUseCaseTests
{
    private const string ClientIdValide = "client-demo";
    private const string AutreClientId = "autre-client";
    private static readonly Guid DroneIdValide = Guid.NewGuid();
    private const string CleIdempotenceValide = "cle-uuid-001";
    private static readonly string[] ServicesValides = new[] { "C2_URLLC", "IMAGERIE_EMBB" };

    private readonly FakeCommandeRepository _repository;
    private readonly FakeDronesApiForCommandes _dronesApi;
    private readonly FakeCatalogueApiForCommandes _catalogueApi;
    private readonly CommandeUseCase _useCase;

    public CommandeUseCaseTests()
    {
        _repository = new FakeCommandeRepository();
        _dronesApi = new FakeDronesApiForCommandes();
        _catalogueApi = new FakeCatalogueApiForCommandes();

        // Enregistre un drone pour notre client valide
        _dronesApi.EnregistrerDrone(DroneIdValide, ClientIdValide);

        _useCase = new CommandeUseCase(_repository, _dronesApi, _catalogueApi);
    }

    [Fact]
    public async Task CreerCommandeAsync_AvecDonneesValides_CreeEtPersisteLaCommande()
    {
        var cmd = new CreerCommandeCommand(ClientIdValide, DroneIdValide, CleIdempotenceValide, ServicesValides);

        var (commande, estRejeu) = await _useCase.CreerCommandeAsync(cmd);

        Assert.False(estRejeu);
        Assert.NotNull(commande);
        Assert.Equal(ClientIdValide, commande.ClientId);
        Assert.Equal(DroneIdValide, commande.DroneId);
        Assert.Equal(2, commande.Elements.Count);
        Assert.Equal(EtatCommande.RECUE, commande.Etat);
        Assert.Equal(1, _repository.Count);
    }

    [Fact]
    public async Task CreerCommandeAsync_RejeuAvecMemeCleEtMemeContenu_RetourneCommandeExistanteSansDoublon_ScenarioQ3()
    {
        // Scénario Q3 du document d'architecture (§10.2) :
        // "La même requête de création de commande est soumise deux fois avec la même clé d'idempotence.
        // Une seule commande est créée ; chaque réponse retourne le même identifiant de commande."
        var cmd = new CreerCommandeCommand(ClientIdValide, DroneIdValide, CleIdempotenceValide, ServicesValides);
        var (premiere, _) = await _useCase.CreerCommandeAsync(cmd);

        // Rejeu
        var (deuxieme, estRejeu) = await _useCase.CreerCommandeAsync(cmd);

        Assert.True(estRejeu);
        Assert.Equal(premiere.Id, deuxieme.Id);
        Assert.Equal(1, _repository.Count); // Aucun doublon créé
    }

    [Fact]
    public async Task CreerCommandeAsync_RejeuAvecMemeCleMaisContenuDifferent_LeveIdempotenceConflitException()
    {
        var cmd1 = new CreerCommandeCommand(ClientIdValide, DroneIdValide, CleIdempotenceValide, new[] { "C2_URLLC" });
        await _useCase.CreerCommandeAsync(cmd1);

        // Deuxième requête avec même clé mais services différents
        var cmd2 = new CreerCommandeCommand(ClientIdValide, DroneIdValide, CleIdempotenceValide, new[] { "IMAGERIE_EMBB" });

        var ex = await Assert.ThrowsAsync<IdempotenceConflitException>(
            () => _useCase.CreerCommandeAsync(cmd2));

        Assert.Equal(CleIdempotenceValide, ex.CleIdempotence);
        Assert.Equal(1, _repository.Count);
    }

    [Fact]
    public async Task CreerCommandeAsync_DroneInconnuOuAutreClient_LeveDroneIntrouvableException_ScenarioA2EtQ2()
    {
        // Le drone n'appartient pas à AutreClientId
        var cmd = new CreerCommandeCommand(AutreClientId, DroneIdValide, CleIdempotenceValide, ServicesValides);

        var ex = await Assert.ThrowsAsync<DroneIntrouvableException>(
            () => _useCase.CreerCommandeAsync(cmd));

        Assert.Equal(DroneIdValide, ex.DroneId);
        Assert.Equal(0, _repository.Count); // Rien n'est écrit
    }

    [Fact]
    public async Task CreerCommandeAsync_ServiceAbsentDuCatalogue_LeveServiceInconnuAuCatalogueException_ScenarioA3()
    {
        var servicesInconnus = new[] { "C2_URLLC", "SERVICE_INEXISTANT" };
        var cmd = new CreerCommandeCommand(ClientIdValide, DroneIdValide, CleIdempotenceValide, servicesInconnus);

        var ex = await Assert.ThrowsAsync<ServiceInconnuAuCatalogueException>(
            () => _useCase.CreerCommandeAsync(cmd));

        Assert.Equal("SERVICE_INEXISTANT", ex.TypeService);
        Assert.Equal(0, _repository.Count); // Rien n'est écrit
    }

    [Fact]
    public async Task ObtenirCommandeAsync_PourClientProprietaire_RetourneLaCommande()
    {
        var cmd = new CreerCommandeCommand(ClientIdValide, DroneIdValide, CleIdempotenceValide, ServicesValides);
        var (creee, _) = await _useCase.CreerCommandeAsync(cmd);

        var obtenue = await _useCase.ObtenirCommandeAsync(creee.Id, ClientIdValide);

        Assert.NotNull(obtenue);
        Assert.Equal(creee.Id, obtenue.Id);
    }

    [Fact]
    public async Task ObtenirCommandeAsync_PourAutreClient_RetourneNull_ScenarioIsolation()
    {
        var cmd = new CreerCommandeCommand(ClientIdValide, DroneIdValide, CleIdempotenceValide, ServicesValides);
        var (creee, _) = await _useCase.CreerCommandeAsync(cmd);

        // Tentative d'accès par un autre client
        var obtenue = await _useCase.ObtenirCommandeAsync(creee.Id, AutreClientId);

        Assert.Null(obtenue);
    }
}
