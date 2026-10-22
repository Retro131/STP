using System.Globalization;
using Npgsql;

namespace lab5;

public sealed class Database
{
    private const string SelectOrdersSql = """
                                           SELECT id,
                                                  order_code,
                                                  COALESCE(phone, ''),
                                                  COALESCE(parcel_article, ''),
                                                  COALESCE(cell, ''),
                                                  status,
                                                  TO_CHAR(created_at, 'YYYY-MM-DD HH24:MI')
                                           FROM orders
                                           """;

    private const string OrderByNewestSql = "\nORDER BY created_at DESC, id DESC;";

    private readonly NpgsqlDataSource dataSource;

    public Database(NpgsqlDataSource dataSource)
    {
        ArgumentNullException.ThrowIfNull(dataSource);
        this.dataSource = dataSource;
    }

    public async Task<List<Order>> ListOrdersAsync(string filter = "", CancellationToken cancellationToken = default)
    {
        var sql = SelectOrdersSql;
        var hasFilter = !string.IsNullOrEmpty(filter);

        if (hasFilter)
        {
            sql += """

                   WHERE order_code ILIKE @filter
                      OR phone ILIKE @filter
                      OR parcel_article ILIKE @filter
                   """;
        }

        sql += OrderByNewestSql;

        await using var command = dataSource.CreateCommand(sql);

        if (hasFilter)
        {
            command.Parameters.AddWithValue("filter", $"%{filter}%");
        }

        return await ReadOrdersAsync(command, cancellationToken);
    }

    public async Task<List<Order>> ListByStatusAsync(string? status, CancellationToken cancellationToken = default)
    {
        var sql = SelectOrdersSql;
        var hasStatus = !string.IsNullOrEmpty(status);

        if (hasStatus)
            sql += "\nWHERE status = @status";

        sql += OrderByNewestSql;

        await using var command = dataSource.CreateCommand(sql);

        if (hasStatus)
            command.Parameters.AddWithValue("status", status!);

        return await ReadOrdersAsync(command, cancellationToken);
    }

    public async Task<string> AddOrderAsync(string contact, string cell, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contact);
        ArgumentNullException.ThrowIfNull(cell);

        if (string.IsNullOrWhiteSpace(contact) || contact.Length < 5)
        {
            throw new ArgumentException(
                "Телефон или артикул должен содержать минимум 5 символов.",
                nameof(contact));
        }

        if (cell.Length > 30)
        {
            throw new ArgumentException(
                "Код ячейки не должен превышать 30 символов.",
                nameof(cell));
        }

        var isPhone = LooksLikePhone(contact);
        var temporaryCode = Guid.NewGuid().ToString("N")[..30];

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string insertSql = """
                                 INSERT INTO orders (order_code, phone, parcel_article, cell)
                                 VALUES (@code, @phone, @article, @cell)
                                 RETURNING id;
                                 """;

        await using var insertCommand = new NpgsqlCommand(insertSql, connection, transaction);

        insertCommand.Parameters.AddWithValue("code", temporaryCode);
        insertCommand.Parameters.AddWithValue("phone", isPhone ? contact : "");
        insertCommand.Parameters.AddWithValue("article", isPhone ? "" : contact);
        insertCommand.Parameters.AddWithValue("cell", cell);

        var result = await insertCommand.ExecuteScalarAsync(cancellationToken);

        if (result is not int id)
        {
            throw new InvalidOperationException(
                "База данных не вернула идентификатор нового заказа.");
        }

        var code = "PVZ-" + id.ToString("D6", CultureInfo.InvariantCulture);

        const string updateSql = "UPDATE orders SET order_code = @code WHERE id = @id;";

        await using var updateCommand = new NpgsqlCommand(updateSql, connection, transaction);

        updateCommand.Parameters.AddWithValue("code", code);
        updateCommand.Parameters.AddWithValue("id", id);

        var affectedRows = await updateCommand.ExecuteNonQueryAsync(cancellationToken);

        if (affectedRows != 1)
        {
            throw new InvalidOperationException(
                "Не удалось присвоить код новому заказу.");
        }

        await transaction.CommitAsync(cancellationToken);

        return code;
    }

    public Task<bool> IssueOrderAsync(string code, CancellationToken cancellationToken = default)
    {
        return ChangeReadyOrderStatusAsync(
            code, OrderStatuses.Issued, cancellationToken);
    }

    public Task<bool> CancelOrderAsync(string code, CancellationToken cancellationToken = default)
    {
        return ChangeReadyOrderStatusAsync(code, OrderStatuses.Cancelled, cancellationToken);
    }

    public async Task<Report> BuildReportAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT status, COUNT(*) FROM orders GROUP BY status;";

        await using var command = dataSource.CreateCommand(sql);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var report = new Report();

        while (await reader.ReadAsync(cancellationToken))
        {
            var status = reader.GetString(0);
            var count = checked((int)reader.GetInt64(1));

            switch (status)
            {
                case OrderStatuses.Ready:
                    report.ReadyCount = count;
                    break;

                case OrderStatuses.Issued:
                    report.IssuedCount = count;
                    break;

                case OrderStatuses.Cancelled:
                    report.CancelledCount = count;
                    break;
            }
        }

        return report;
    }

    public async Task<int> CountClosedOrdersAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(*) FROM orders WHERE status IN ('issued', 'cancelled');";

        await using var command = dataSource.CreateCommand(sql);

        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (result is not long count)
        {
            throw new InvalidOperationException(
                "База данных не вернула количество закрытых заказов.");
        }

        return checked((int)count);
    }

    public async Task<int> DeleteClosedOrdersAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM orders WHERE status IN ('issued', 'cancelled');";

        await using var command = dataSource.CreateCommand(sql);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<bool> ChangeReadyOrderStatusAsync(string code, string newStatus,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var status = await GetOrderStatusForUpdateAsync(code, connection, transaction, cancellationToken);

        if (status != OrderStatuses.Ready)
            return false;

        const string sql = """
                           UPDATE orders
                           SET status = @status,
                               issued_at = CASE
                                   WHEN @status = 'issued' THEN now()
                                   ELSE issued_at
                               END
                           WHERE order_code = @code;
                           """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);

        command.Parameters.AddWithValue("code", code);
        command.Parameters.AddWithValue("status", newStatus);

        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);

        if (affectedRows != 1)
        {
            throw new InvalidOperationException(
                "Не удалось изменить статус заблокированного заказа.");
        }

        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    private static async Task<string?> GetOrderStatusForUpdateAsync(string code, NpgsqlConnection connection,
        NpgsqlTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = "SELECT status FROM orders WHERE order_code = @code FOR UPDATE;";

        await using var command = new NpgsqlCommand(sql, connection, transaction);

        command.Parameters.AddWithValue("code", code);

        return await command.ExecuteScalarAsync(cancellationToken)
            as string;
    }

    private static async Task<List<Order>> ReadOrdersAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var orders = new List<Order>();

        while (await reader.ReadAsync(cancellationToken))
        {
            orders.Add(new Order
            {
                Id = reader.GetInt32(0),
                Code = reader.GetString(1),
                Phone = reader.GetString(2),
                Article = reader.GetString(3),
                Cell = reader.GetString(4),
                Status = reader.GetString(5),
                CreatedAt = reader.GetString(6)
            });
        }

        return orders;
    }


    private static bool LooksLikePhone(string value)
    {
        var digits = 0;

        foreach (var character in value)
        {
            if (character is >= '0' and <= '9')
            {
                digits++;
                continue;
            }

            if (character is not ('+' or '-' or ' ' or '(' or ')'))
                return false;
        }

        return digits >= 10;
    }
}