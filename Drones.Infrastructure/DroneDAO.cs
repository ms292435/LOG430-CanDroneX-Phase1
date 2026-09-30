using Npgsql;

namespace Drones.Infrastructure;

// Représentation plate des données en base (DTO d'infrastructure)
public sealed record DroneRecord(
    Guid Id,
    string ClientId,
    string Imsi,
    string Modele,
    string Statut,
    DateTime DateEnregistrement
);

public sealed class DroneDao
{
    private readonly string _connectionString;

    public DroneDao(string connectionString)
    {
        _connectionString = connectionString;
    }

    private async Task<NpgsqlConnection> OuvrirConnexionAsync(CancellationToken ct)
    {
        var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        return conn;
    }

    public async Task InsertAsync(DroneRecord record, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO drones.drones (id, client_id, imsi, modele, statut, date_enregistrement)
            VALUES (@id, @clientId, @imsi, @modele, @statut, @dateEnregistrement);
            """;

        await using var conn = await OuvrirConnexionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("id", record.Id);
        cmd.Parameters.AddWithValue("clientId", record.ClientId);
        cmd.Parameters.AddWithValue("imsi", record.Imsi);
        cmd.Parameters.AddWithValue("modele", record.Modele);
        cmd.Parameters.AddWithValue("statut", record.Statut);
        cmd.Parameters.AddWithValue("dateEnregistrement", record.DateEnregistrement);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<DroneRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, client_id, imsi, modele, statut, date_enregistrement
            FROM drones.drones
            WHERE id = @id;
            """;

        await using var conn = await OuvrirConnexionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        return MapperRecord(reader);
    }

    public async Task<DroneRecord?> GetByImsiAsync(string imsi, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, client_id, imsi, modele, statut, date_enregistrement
            FROM drones.drones
            WHERE imsi = @imsi;
            """;

        await using var conn = await OuvrirConnexionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("imsi", imsi);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        return MapperRecord(reader);
    }

    private static DroneRecord MapperRecord(NpgsqlDataReader reader) =>
        new(
            reader.GetGuid(reader.GetOrdinal("id")),
            reader.GetString(reader.GetOrdinal("client_id")),
            reader.GetString(reader.GetOrdinal("imsi")),
            reader.GetString(reader.GetOrdinal("modele")),
            reader.GetString(reader.GetOrdinal("statut")),
            reader.GetDateTime(reader.GetOrdinal("date_enregistrement"))
        );
}