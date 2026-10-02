using Catalogue.Application;
using Catalogue.Domain;

namespace Catalogue.Infrastructure;

public sealed class CatalogueRepositorySql : ICatalogueRepository
{
    private readonly CatalogueDao _dao;

    public CatalogueRepositorySql(CatalogueDao dao)
    {
        _dao = dao;
    }

    public async Task<OffreDeService?> GetByTypeServiceAsync(string typeService, CancellationToken ct = default)
    {
        var record = await _dao.GetByTypeServiceAsync(typeService, ct);
        if (record is null)
            return null;

        return OffreDeService.Creer(record.TypeService, record.Libelle, record.ProfilReseau);
    }

    public async Task<IReadOnlyList<OffreDeService>> GetAllAsync(CancellationToken ct = default)
    {
        var records = await _dao.GetAllAsync(ct);
        return records
            .Select(r => OffreDeService.Creer(r.TypeService, r.Libelle, r.ProfilReseau))
            .ToList();
    }
}
