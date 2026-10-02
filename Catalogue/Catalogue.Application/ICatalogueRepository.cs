using Catalogue.Domain;

namespace Catalogue.Application;

public interface ICatalogueRepository
{
    Task<OffreDeService?> GetByTypeServiceAsync(string typeService, CancellationToken ct = default);
    Task<IReadOnlyList<OffreDeService>> GetAllAsync(CancellationToken ct = default);
}
