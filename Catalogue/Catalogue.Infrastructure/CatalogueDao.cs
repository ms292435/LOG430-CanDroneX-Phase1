using Npgsql;

namespace Catalogue.Infrastructure;

public sealed record OffreServiceRecord(
    string TypeService,
    string Libelle,
    string ProfilReseau
);

public sealed class CatalogueDao
{
    private readonly string _connectionString;

    public CatalogueDao(string connectionString)
    {
        _connectionString = connectionString;
    }

    private async Task<NpgsqlConnection> OuvrirConnexionAsync(CancellationToken ct)
    {
        var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        return conn;
    }

    public async Task<OffreServiceRecord?> GetByTypeServiceAsync(string typeService, CancellationToken ct = default)
    {
        const string sql = """
            SELECT type_service, libelle, profil_reseau
            FROM catalogue.offre_service
            WHERE type_service = @typeService;
            """;

        await using var conn = await OuvrirConnexionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("typeService", typeService);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        return MapperRecord(reader);
    }

    public async Task<IReadOnlyList<OffreServiceRecord>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT type_service, libelle, profil_reseau
            FROM catalogue.offre_service
            ORDER BY type_service;
            """;

        var resultats = new List<OffreServiceRecord>();

        await using var conn = await OuvrirConnexionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            resultats.Add(MapperRecord(reader));
        }

        return resultats;
    }

    private static OffreServiceRecord MapperRecord(NpgsqlDataReader reader) =>
        new(
            reader.GetString(reader.GetOrdinal("type_service")),
            reader.GetString(reader.GetOrdinal("libelle")),
            reader.GetString(reader.GetOrdinal("profil_reseau"))
        );
}
