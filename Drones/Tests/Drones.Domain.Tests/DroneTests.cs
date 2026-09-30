using Drones.Domain;
using Xunit;

namespace Drones.Domain.Tests;

public class DroneTests
{
    private const string ImsiValide = "123456789012345"; // 15 chiffres
    private const string ModeleValide = "DJI Matrice 300";

    [Fact]
    public void Enregistrer_AvecImsiEtModeleValides_CreeLeDrone()
    {
        var drone = Drone.Enregistrer(ImsiValide, ModeleValide);

        Assert.Equal(ImsiValide, drone.Imsi);
        Assert.Equal(ModeleValide, drone.Modele);
        Assert.Equal(StatutDrone.Enregistre, drone.Statut);
        Assert.NotEqual(default, drone.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Enregistrer_AvecImsiAbsent_LeveException(string? imsi)
    {
        var exception = Assert.Throws<DroneDomainException>(
            () => Drone.Enregistrer(imsi!, ModeleValide));

        Assert.Contains("IMSI", exception.Message);
    }

    [Theory]
    [InlineData("123")]                // trop court
    [InlineData("1234567890123456")]   // trop long (16 chiffres)
    [InlineData("12345ABC90123")]      // contient des lettres
    public void Enregistrer_AvecImsiInvalide_LeveException(string imsiInvalide)
    {
        var exception = Assert.Throws<DroneDomainException>(
            () => Drone.Enregistrer(imsiInvalide, ModeleValide));

        Assert.Contains("chiffres", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Enregistrer_AvecModeleAbsent_LeveException(string? modele)
    {
        var exception = Assert.Throws<DroneDomainException>(
            () => Drone.Enregistrer(ImsiValide, modele!));

        Assert.Contains("modèle", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Enregistrer_AvecEspacesAutourDeLImsi_NettoieLaValeur()
    {
        var drone = Drone.Enregistrer($"  {ImsiValide}  ", $"  {ModeleValide}  ");

        Assert.Equal(ImsiValide, drone.Imsi);
        Assert.Equal(ModeleValide, drone.Modele);
    }
}
