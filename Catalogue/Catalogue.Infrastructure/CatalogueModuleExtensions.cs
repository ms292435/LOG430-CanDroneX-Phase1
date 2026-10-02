using Catalogue.Application;
using Catalogue.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Catalogue.Infrastructure;

public static class CatalogueModuleExtensions
{
    public static IServiceCollection AddCatalogueModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CanDroneX")
            ?? throw new InvalidOperationException("La chaîne de connexion 'CanDroneX' est manquante.");

        services.AddSingleton(new CatalogueDao(connectionString));
        services.AddScoped<ICatalogueRepository, CatalogueRepositorySql>();
        services.AddScoped<ICatalogueApi, CatalogueApi>();

        return services;
    }
}
