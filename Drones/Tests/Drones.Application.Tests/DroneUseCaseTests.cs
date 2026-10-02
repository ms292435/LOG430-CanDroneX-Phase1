using Drones.Application;
using Drones.Domain;
using Xunit;

namespace Drones.Application.Tests;

public class DroneUseCaseTests
{
    private const string ClientIdValide = "client-demo";
    private const string AutreClientId = "autre-client";
    private const string ImsiValide = "123456789012345";
    private const TypeCarte TypeCarteValide = TypeCarte.Sim;
    private const string ModeleValide = "DJI Matrice 300";

    [Fact]
    public async Task EnregistrerDroneAsync_AvecDonneesValides_EnregistreEtRetourneUnId()
    {
        var repository = new FakeDroneRepository();
        var useCase = new DroneUseCase(repository);
        var commande = new EnregistrerDroneCommand(ClientIdValide, ImsiValide, TypeCarteValide, ModeleValide);

        var id = await useCase.EnregistrerDroneAsync(commande);

        Assert.NotEqual(default, id);
        Assert.Equal(1, repository.NombreDeDronesEnregistres);
    }

    [Fact]
    public async Task EnregistrerDroneAsync_AvecImsiDejaEnregistre_LeveImsiDejaUtiliseException()
    {
        var repository = new FakeDroneRepository();
        var useCase = new DroneUseCase(repository);
        var commande = new EnregistrerDroneCommand(ClientIdValide, ImsiValide, TypeCarteValide, ModeleValide);
        await useCase.EnregistrerDroneAsync(commande); // premier enregistrement, réussit

        var exception = await Assert.ThrowsAsync<ImsiDejaUtiliseException>(
            () => useCase.EnregistrerDroneAsync(commande));

        Assert.Equal(ImsiValide, exception.Imsi);
        Assert.Equal(1, repository.NombreDeDronesEnregistres); // pas de doublon créé
    }

    [Fact]
    public async Task EnregistrerDroneAsync_AvecImsiInvalide_LeveExceptionEtNeSauvegardeRien()
    {
        var repository = new FakeDroneRepository();
        var useCase = new DroneUseCase(repository);
        var commande = new EnregistrerDroneCommand(ClientIdValide, "123", TypeCarteValide, ModeleValide); // trop court

        await Assert.ThrowsAsync<DroneDomainException>(
            () => useCase.EnregistrerDroneAsync(commande));

        Assert.Equal(0, repository.NombreDeDronesEnregistres);
    }

    [Fact]
    public async Task EnregistrerDroneAsync_AvecModeleAbsent_LeveExceptionEtNeSauvegardeRien()
    {
        var repository = new FakeDroneRepository();
        var useCase = new DroneUseCase(repository);
        var commande = new EnregistrerDroneCommand(ClientIdValide, ImsiValide, TypeCarteValide, "");

        await Assert.ThrowsAsync<DroneDomainException>(
            () => useCase.EnregistrerDroneAsync(commande));

        Assert.Equal(0, repository.NombreDeDronesEnregistres);
    }

    [Fact]
    public async Task EnregistrerDroneAsync_AvecClientIdAbsent_LeveExceptionEtNeSauvegardeRien()
    {
        var repository = new FakeDroneRepository();
        var useCase = new DroneUseCase(repository);
        var commande = new EnregistrerDroneCommand("", ImsiValide, TypeCarteValide, ModeleValide);

        await Assert.ThrowsAsync<DroneDomainException>(
            () => useCase.EnregistrerDroneAsync(commande));

        Assert.Equal(0, repository.NombreDeDronesEnregistres);
    }

    [Fact]
    public async Task ObtenirDroneAsync_PourDroneDuClient_RetourneLeDrone()
    {
        var repository = new FakeDroneRepository();
        var useCase = new DroneUseCase(repository);
        var commande = new EnregistrerDroneCommand(ClientIdValide, ImsiValide, TypeCarteValide, ModeleValide);
        var id = await useCase.EnregistrerDroneAsync(commande);

        var drone = await useCase.ObtenirDroneAsync(id, ClientIdValide);

        Assert.NotNull(drone);
        Assert.Equal(id, drone.Id);
        Assert.Equal(ClientIdValide, drone.ClientId);
        Assert.Equal(ImsiValide, drone.Imsi);
    }

    [Fact]
    public async Task ObtenirDroneAsync_PourDroneDUnAutreClient_RetourneNull()
    {
        var repository = new FakeDroneRepository();
        var useCase = new DroneUseCase(repository);
        var commande = new EnregistrerDroneCommand(ClientIdValide, ImsiValide, TypeCarteValide, ModeleValide);
        var id = await useCase.EnregistrerDroneAsync(commande);

        // Tentative d'accès par un autre client B2B (Q2 / §8.5)
        var drone = await useCase.ObtenirDroneAsync(id, AutreClientId);

        Assert.Null(drone);
    }

    [Fact]
    public async Task ObtenirDroneAsync_PourDroneInexistant_RetourneNull()
    {
        var repository = new FakeDroneRepository();
        var useCase = new DroneUseCase(repository);

        var drone = await useCase.ObtenirDroneAsync(DroneId.Nouveau(), ClientIdValide);

        Assert.Null(drone);
    }

    [Fact]
    public async Task DronesApi_ObtenirDroneDuClientAsync_PourDroneDuClient_RetourneDroneInfo()
    {
        var repository = new FakeDroneRepository();
        var useCase = new DroneUseCase(repository);
        var commande = new EnregistrerDroneCommand(ClientIdValide, ImsiValide, TypeCarteValide, ModeleValide);
        var id = await useCase.EnregistrerDroneAsync(commande);

        var dronesApi = new DronesApi(repository);
        var droneInfo = await dronesApi.ObtenirDroneDuClientAsync(id.Value, ClientIdValide);

        Assert.NotNull(droneInfo);
        Assert.Equal(id.Value, droneInfo.Id);
        Assert.Equal(ClientIdValide, droneInfo.ClientId);
        Assert.Equal(ImsiValide, droneInfo.Imsi);
        Assert.Equal(TypeCarteValide.ToString(), droneInfo.TypeCarte);
        Assert.Equal(ModeleValide, droneInfo.Modele);
        Assert.Equal(StatutDrone.Enregistre.ToString(), droneInfo.Statut);
    }

    [Fact]
    public async Task DronesApi_ObtenirDroneDuClientAsync_PourAutreClient_RetourneNull()
    {
        var repository = new FakeDroneRepository();
        var useCase = new DroneUseCase(repository);
        var commande = new EnregistrerDroneCommand(ClientIdValide, ImsiValide, TypeCarteValide, ModeleValide);
        var id = await useCase.EnregistrerDroneAsync(commande);

        var dronesApi = new DronesApi(repository);
        var droneInfo = await dronesApi.ObtenirDroneDuClientAsync(id.Value, AutreClientId);

        Assert.Null(droneInfo);
    }
}
