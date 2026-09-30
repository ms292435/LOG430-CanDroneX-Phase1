# Consultez https://aka.ms/customizecontainer pour savoir comment personnaliser votre conteneur de débogage et comment Visual Studio utilise ce Dockerfile pour générer vos images afin d’accélérer le débogage.

# Cet index est utilisé lors de l’exécution à partir de VS en mode rapide (par défaut pour la configuration de débogage)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# Cette phase est utilisée pour générer le projet de service
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["CanDroneX.Host/CanDroneX.Host.csproj", "CanDroneX.Host/"]
COPY ["Drones.Infrastructure/Drones.Infrastructure.csproj", "Drones.Infrastructure/"]
COPY ["Drones.Application/Drones.Application.csproj", "Drones.Application/"]
COPY ["Drones.Domain/Drones.Domain.csproj", "Drones.Domain/"]
COPY ["Drones.Api/Drones.Api.csproj", "Drones.Api/"]
COPY ["Drones.Contracts/Drones.Contracts.csproj", "Drones.Contracts/"]
RUN dotnet restore "./CanDroneX.Host/CanDroneX.Host.csproj"
COPY . .
WORKDIR "/src/CanDroneX.Host"	
RUN dotnet build "./CanDroneX.Host.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Cette étape permet de publier le projet de service à copier dans la phase finale
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CanDroneX.Host.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Cette phase est utilisée en production ou lors de l’exécution à partir de VS en mode normal (par défaut quand la configuration de débogage n’est pas utilisée)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CanDroneX.Host.dll"]