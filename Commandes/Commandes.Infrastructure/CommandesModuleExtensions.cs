using Commandes.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commandes.Infrastructure;

public static class CommandesModuleExtensions
{
    public static IServiceCollection AddCommandesModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CanDroneX")
            ?? throw new InvalidOperationException("La chaîne de connexion 'CanDroneX' est manquante.");

        services.AddSingleton(new CommandeDao(connectionString));
        services.AddScoped<ICommandeRepository, CommandeRepositorySql>();
        services.AddScoped<ICommandePort, CommandeUseCase>();

        return services;
    }
}
