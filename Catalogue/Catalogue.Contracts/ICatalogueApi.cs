namespace Catalogue.Contracts;

/// <summary>
/// Contrat public offert par le module Catalogue aux autres modules (notamment Commandes pour UC-04).
/// </summary>
public interface ICatalogueApi
{
    Task<OffreInfo?> ObtenirOffreAsync(string typeService, CancellationToken ct = default);
    Task<IReadOnlyList<OffreInfo>> ObtenirToutesLesOffresAsync(CancellationToken ct = default);
}
