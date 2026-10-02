using Commandes.Domain;
using Npgsql;

namespace Commandes.Infrastructure;

public sealed record CommandeRecord(
    Guid Id,
    string ClientId,
    Guid DroneId,
    string CleIdempotence,
    string EmpreinteRequete,
    DateTime DateCreation
);

public sealed record ElementCommandeRecord(
    Guid Id,
    Guid CommandeId,
    string TypeService,
    string Etat,
    string? CauseEchec
);

public sealed class CommandeDao
{
    private readonly string _connectionString;

    public CommandeDao(string connectionString)
    {
        _connectionString = connectionString;
    }

    private async Task<NpgsqlConnection> OuvrirConnexionAsync(CancellationToken ct)
    {
        var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        return conn;
    }

    /// <summary>
    /// Insère la commande et tous ses éléments dans une transaction unique (§8.1 & Scénario Q4).
    /// Si une insertion échoue, tout est annulé (ROLLBACK).
    /// </summary>
    public async Task InsertTransactionnelAsync(
        CommandeRecord commande,
        IEnumerable<ElementCommandeRecord> elements,
        CancellationToken ct = default)
    {
        await using var conn = await OuvrirConnexionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            const string sqlCommande = """
                INSERT INTO commandes.commande (id, client_id, drone_id, cle_idempotence, empreinte_requete, date_creation)
                VALUES (@id, @clientId, @droneId, @cleIdempotence, @empreinteRequete, @dateCreation);
                """;

            await using (var cmd = new NpgsqlCommand(sqlCommande, conn, tx))
            {
                cmd.Parameters.AddWithValue("id", commande.Id);
                cmd.Parameters.AddWithValue("clientId", commande.ClientId);
                cmd.Parameters.AddWithValue("droneId", commande.DroneId);
                cmd.Parameters.AddWithValue("cleIdempotence", commande.CleIdempotence);
                cmd.Parameters.AddWithValue("empreinteRequete", commande.EmpreinteRequete);
                cmd.Parameters.AddWithValue("dateCreation", commande.DateCreation);

                await cmd.ExecuteNonQueryAsync(ct);
            }

            const string sqlElement = """
                INSERT INTO commandes.element_commande (id, commande_id, type_service, etat, cause_echec)
                VALUES (@id, @commandeId, @typeService, @etat, @causeEchec);
                """;

            foreach (var element in elements)
            {
                await using var cmd = new NpgsqlCommand(sqlElement, conn, tx);
                cmd.Parameters.AddWithValue("id", element.Id);
                cmd.Parameters.AddWithValue("commandeId", element.CommandeId);
                cmd.Parameters.AddWithValue("typeService", element.TypeService);
                cmd.Parameters.AddWithValue("etat", element.Etat);
                cmd.Parameters.AddWithValue("causeEchec", (object?)element.CauseEchec ?? DBNull.Value);

                await cmd.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await tx.RollbackAsync(ct);
            throw new IdempotenceConflitException(commande.CleIdempotence);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<(CommandeRecord? Commande, IReadOnlyList<ElementCommandeRecord> Elements)> GetByIdAndClientIdAsync(
        Guid id,
        string clientId,
        CancellationToken ct = default)
    {
        await using var conn = await OuvrirConnexionAsync(ct);

        const string sqlCmd = """
            SELECT id, client_id, drone_id, cle_idempotence, empreinte_requete, date_creation
            FROM commandes.commande
            WHERE id = @id AND client_id = @clientId;
            """;

        CommandeRecord? commande = null;
        await using (var cmd = new NpgsqlCommand(sqlCmd, conn))
        {
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("clientId", clientId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                commande = new CommandeRecord(
                    reader.GetGuid(reader.GetOrdinal("id")),
                    reader.GetString(reader.GetOrdinal("client_id")),
                    reader.GetGuid(reader.GetOrdinal("drone_id")),
                    reader.GetString(reader.GetOrdinal("cle_idempotence")),
                    reader.GetString(reader.GetOrdinal("empreinte_requete")),
                    reader.GetDateTime(reader.GetOrdinal("date_creation"))
                );
            }
        }

        if (commande is null)
            return (null, Array.Empty<ElementCommandeRecord>());

        var elements = await ObtenirElementsAsync(conn, id, ct);
        return (commande, elements);
    }

    public async Task<(CommandeRecord? Commande, IReadOnlyList<ElementCommandeRecord> Elements)> GetByCleIdempotenceAsync(
        string clientId,
        string cleIdempotence,
        CancellationToken ct = default)
    {
        await using var conn = await OuvrirConnexionAsync(ct);

        const string sqlCmd = """
            SELECT id, client_id, drone_id, cle_idempotence, empreinte_requete, date_creation
            FROM commandes.commande
            WHERE client_id = @clientId AND cle_idempotence = @cleIdempotence;
            """;

        CommandeRecord? commande = null;
        await using (var cmd = new NpgsqlCommand(sqlCmd, conn))
        {
            cmd.Parameters.AddWithValue("clientId", clientId);
            cmd.Parameters.AddWithValue("cleIdempotence", cleIdempotence);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                commande = new CommandeRecord(
                    reader.GetGuid(reader.GetOrdinal("id")),
                    reader.GetString(reader.GetOrdinal("client_id")),
                    reader.GetGuid(reader.GetOrdinal("drone_id")),
                    reader.GetString(reader.GetOrdinal("cle_idempotence")),
                    reader.GetString(reader.GetOrdinal("empreinte_requete")),
                    reader.GetDateTime(reader.GetOrdinal("date_creation"))
                );
            }
        }

        if (commande is null)
            return (null, Array.Empty<ElementCommandeRecord>());

        var elements = await ObtenirElementsAsync(conn, commande.Id, ct);
        return (commande, elements);
    }

    private static async Task<IReadOnlyList<ElementCommandeRecord>> ObtenirElementsAsync(
        NpgsqlConnection conn,
        Guid commandeId,
        CancellationToken ct)
    {
        const string sql = """
            SELECT id, commande_id, type_service, etat, cause_echec
            FROM commandes.element_commande
            WHERE commande_id = @commandeId
            ORDER BY type_service;
            """;

        var result = new List<ElementCommandeRecord>();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("commandeId", commandeId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            result.Add(new ElementCommandeRecord(
                reader.GetGuid(reader.GetOrdinal("id")),
                reader.GetGuid(reader.GetOrdinal("commande_id")),
                reader.GetString(reader.GetOrdinal("type_service")),
                reader.GetString(reader.GetOrdinal("etat")),
                reader.IsDBNull(reader.GetOrdinal("cause_echec")) ? null : reader.GetString(reader.GetOrdinal("cause_echec"))
            ));
        }

        return result;
    }
}
