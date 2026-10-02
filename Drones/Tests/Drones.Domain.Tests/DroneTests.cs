using Drones.Domain;
using Xunit;

namespace Drones.Domain.Tests;

public class DroneTests
{
    private const string ClientIdValide = "client-demo";
    private const string ImsiValide = "123456789012345"; // 15 chiffres
    private const TypeCarte TypeCarteValide = TypeCarte.Sim;
    private const string ModeleValide = "DJI Matrice 300";

    [Fact]
    public void Enregistrer_AvecDonneesValides_CreeLeDrone()
    {
        var drone = Drone.Enregistrer(ClientIdValide, ImsiValide, TypeCarteValide, ModeleValide);

        Assert.Equal(ClientIdValide, drone.ClientId);
        Assert.Equal(ImsiValide, drone.Imsi);
        Assert.Equal(TypeCarteValide, drone.TypeCarte);
        Assert.Equal(ModeleValide, drone.Modele);
        Assert.Equal(StatutDrone.Enregistre, drone.Statut);
        Assert.NotEqual(default, drone.Id);
        Assert.True(drone.DateEnregistrement <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Enregistrer_AvecClientIdAbsent_LeveException(string? clientId)
    {
        var exception = Assert.Throws<DroneDomainException>(
            () => Drone.Enregistrer(clientId!, ImsiValide, TypeCarteValide, ModeleValide));

        Assert.Contains("client", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Enregistrer_AvecImsiAbsent_LeveException(string? imsi)
    {
        var exception = Assert.Throws<DroneDomainException>(
            () => Drone.Enregistrer(ClientIdValide, imsi!, TypeCarteValide, ModeleValide));

        Assert.Contains("IMSI", exception.Message);
    }

    [Theory]
    [InlineData("123")]                // trop court
    [InlineData("1234567890123456")]   // trop long (16 chiffres)
    [InlineData("12345ABC90123")]      // contient des lettres
    public void Enregistrer_AvecImsiInvalide_LeveException(string imsiInvalide)
    {
        var exception = Assert.Throws<DroneDomainException>(
            () => Drone.Enregistrer(ClientIdValide, imsiInvalide, TypeCarteValide, ModeleValide));

        Assert.Contains("chiffres", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Enregistrer_AvecModeleAbsent_LeveException(string? modele)
    {
        var exception = Assert.Throws<DroneDomainException>(
            () => Drone.Enregistrer(ClientIdValide, ImsiValide, TypeCarteValide, modele!));

        Assert.Contains("modèle", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Enregistrer_AvecEspacesAutourDesChamps_NettoieLesValeurs()
    {
        var drone = Drone.Enregistrer($"  {ClientIdValide}  ", $"  {ImsiValide}  ", TypeCarteValide, $"  {ModeleValide}  ");

        Assert.Equal(ClientIdValide, drone.ClientId);
        Assert.Equal(ImsiValide, drone.Imsi);
        Assert.Equal(ModeleValide, drone.Modele);
    }

    [Fact]
    public void Reconstituer_DepuisPersistance_RestaureLIdentiteExacte()
    {
        var id = DroneId.Nouveau();
        var date = new DateTime(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc);
        var drone = Drone.Reconstituer(id, ClientIdValide, ImsiValide, TypeCarte.ESim, ModeleValide, StatutDrone.Actif, date);

        Assert.Equal(id, drone.Id);
        Assert.Equal(ClientIdValide, drone.ClientId);
        Assert.Equal(ImsiValide, drone.Imsi);
        Assert.Equal(TypeCarte.ESim, drone.TypeCarte);
        Assert.Equal(ModeleValide, drone.Modele);
        Assert.Equal(StatutDrone.Actif, drone.Statut);
        Assert.Equal(date, drone.DateEnregistrement);
    }

    [Theory]
    [InlineData("SIM", TypeCarte.Sim)]
    [InlineData("sim", TypeCarte.Sim)]
    [InlineData("eSIM", TypeCarte.ESim)]
    [InlineData("esim", TypeCarte.ESim)]
    public void TypeCarte_ParsingValide_Reussit(string texte, TypeCarte expected)
    {
        var reussi = TypeCarteExtensions.TryParseTypeCarte(texte, out var resultat);

        Assert.True(reussi);
        Assert.Equal(expected, resultat);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("inconnu")]
    [InlineData("4G")]
    public void TypeCarte_ParsingInvalide_Echoue(string? texte)
    {
        var reussi = TypeCarteExtensions.TryParseTypeCarte(texte, out _);

        Assert.False(reussi);
    }

    [Fact]
    public void ImsiDejaUtiliseException_ProprietesEtMessage_SontBienDefinis()
    {
        var ex = new ImsiDejaUtiliseException(ImsiValide);

        Assert.Equal(ImsiValide, ex.Imsi);
        Assert.IsAssignableFrom<DroneDomainException>(ex);
    }
}
