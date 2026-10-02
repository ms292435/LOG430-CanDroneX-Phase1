using Drones.Application;
using Drones.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Drones.Infrastructure;

public static class DroneModuleExtensions
{
    public static IServiceCollection AddDroneModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CanDroneX")
            ?? throw new InvalidOperationException("La chaîne de connexion 'CanDroneX' est manquante.");

        services.AddSingleton(new DroneDao(connectionString));
        services.AddScoped<IDroneRepository, DroneRepositorySql>();
        services.AddScoped<IDronePort, DroneUseCase>();
        services.AddScoped<IDronesApi, DronesApi>();

        return services;
    }
}