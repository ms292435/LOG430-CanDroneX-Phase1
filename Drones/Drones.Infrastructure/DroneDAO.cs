using Drones.Domain;
using Npgsql;

namespace Drones.Infrastructure;

// Représentation plate des données en base (DTO d'infrastructure)
public sealed record DroneRecord(
    Guid Id,
    string ClientId,
    string Imsi,
    string TypeCarte,
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
            INSERT INTO drones.drone (id, client_id, imsi, type_carte, modele, statut, date_enregistrement)
            VALUES (@id, @clientId, @imsi, @typeCarte, @modele, @statut, @dateEnregistrement);
            """;

        try
        {
            await using var conn = await OuvrirConnexionAsync(ct);
            await using var cmd = new NpgsqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("id", record.Id);
            cmd.Parameters.AddWithValue("clientId", record.ClientId);
            cmd.Parameters.AddWithValue("imsi", record.Imsi);
            cmd.Parameters.AddWithValue("typeCarte", record.TypeCarte);
            cmd.Parameters.AddWithValue("modele", record.Modele);
            cmd.Parameters.AddWithValue("statut", record.Statut);
            cmd.Parameters.AddWithValue("dateEnregistrement", record.DateEnregistrement);

            await cmd.ExecuteNonQueryAsync(ct);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            // Traduction de la violation d'unicité (code 23505) en exception de domaine (§8.1)
            throw new ImsiDejaUtiliseException(record.Imsi);
        }
    }

    public async Task<DroneRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, client_id, imsi, type_carte, modele, statut, date_enregistrement
            FROM drones.drone
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

    public async Task<DroneRecord?> GetByIdAndClientIdAsync(Guid id, string clientId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, client_id, imsi, type_carte, modele, statut, date_enregistrement
            FROM drones.drone
            WHERE id = @id AND client_id = @clientId;
            """;

        await using var conn = await OuvrirConnexionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("clientId", clientId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        return MapperRecord(reader);
    }

    public async Task<DroneRecord?> GetByImsiAsync(string imsi, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, client_id, imsi, type_carte, modele, statut, date_enregistrement
            FROM drones.drone
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
            reader.GetString(reader.GetOrdinal("type_carte")),
            reader.GetString(reader.GetOrdinal("modele")),
            reader.GetString(reader.GetOrdinal("statut")),
            reader.GetDateTime(reader.GetOrdinal("date_enregistrement"))
        );
}