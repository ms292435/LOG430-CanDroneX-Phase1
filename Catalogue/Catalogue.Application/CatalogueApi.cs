using Catalogue.Contracts;
using Catalogue.Domain;

namespace Catalogue.Application;

public sealed class CatalogueApi : ICatalogueApi
{
    private readonly ICatalogueRepository _repository;

    public CatalogueApi(ICatalogueRepository repository)
    {
        _repository = repository;
    }

    public async Task<OffreInfo?> ObtenirOffreAsync(string typeService, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(typeService))
            return null;

        var normalise = TypeServiceCatalogue.Normaliser(typeService);
        var offre = await _repository.GetByTypeServiceAsync(normalise, ct);
        if (offre is null)
            return null;

        return new OffreInfo(
            offre.TypeService,
            offre.Libelle,
            offre.ProfilReseau
        );
    }

    public async Task<IReadOnlyList<OffreInfo>> ObtenirToutesLesOffresAsync(CancellationToken ct = default)
    {
        var offres = await _repository.GetAllAsync(ct);
        return offres.Select(o => new OffreInfo(o.TypeService, o.Libelle, o.ProfilReseau)).ToList();
    }
}
