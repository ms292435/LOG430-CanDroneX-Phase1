using Drones.Domain;
using Xunit;

namespace Drones.Application.Tests;

public class DroneUseCaseTests
{
    private const string ImsiValide = "123456789012345";
    private const string ModeleValide = "DJI Matrice 300";

    [Fact]
    public async Task EnregistrerDroneAsync_AvecImsiNonExistant_EnregistreEtRetourneUnId()
    {
        var repository = new FakeDroneRepository();
        var useCase = new DroneUseCase(repository);
        var commande = new EnregistrerDroneCommand(ImsiValide, ModeleValide);

        var id = await useCase.EnregistrerDroneAsync(commande);

        Assert.NotEqual(default, id);
        Assert.Equal(1, repository.NombreDeDronesEnregistres);
    }

    [Fact]
    public async Task EnregistrerDroneAsync_AvecImsiDejaEnregistre_LeveException()
    {
        var repository = new FakeDroneRepository();
        var useCase = new DroneUseCase(repository);
        var commande = new EnregistrerDroneCommand(ImsiValide, ModeleValide);
        await useCase.EnregistrerDroneAsync(commande); // premier enregistrement, réussit

        var exception = await Assert.ThrowsAsync<DroneDomainException>(
            () => useCase.EnregistrerDroneAsync(commande));

        Assert.Contains("déjà enregistré", exception.Message);
        Assert.Equal(1, repository.NombreDeDronesEnregistres); // pas de doublon créé
    }

    [Fact]
    public async Task EnregistrerDroneAsync_AvecImsiInvalide_LeveExceptionEtNeSauvegardeRien()
    {
        var repository = new FakeDroneRepository();
        var useCase = new DroneUseCase(repository);
        var commande = new EnregistrerDroneCommand("123", ModeleValide); // trop court

        await Assert.ThrowsAsync<DroneDomainException>(
            () => useCase.EnregistrerDroneAsync(commande));

        Assert.Equal(0, repository.NombreDeDronesEnregistres);
    }
}
