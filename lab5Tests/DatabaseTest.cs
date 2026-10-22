using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using lab5;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Npgsql;
using NUnit.Framework;

namespace lab5Tests;

[TestFixture]
[TestOf(typeof(Database))]
public class DatabaseTest
{
    private string connectionString = null!;
    private NpgsqlDataSource dataSource = null!;
    private Database database = null!;

    [OneTimeSetUp]
    public void ConfigureDatabase()
    {
        var builder = Host.CreateApplicationBuilder();
        var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        builder.Configuration.AddJsonFile(configPath, optional: false);

        connectionString = builder.Configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("В appsettings.json не задана ConnectionStrings:Postgres.");

        dataSource = NpgsqlDataSource.Create(connectionString);
        database = new Database(dataSource);
    }

    [SetUp]
    public async Task PrepareOrdersAsync()
    {
        await ClearOrdersAsync();

        await database.AddOrderAsync("+78005553535", "A1");
        await database.AddOrderAsync("ART-123456789", "");
        await database.AddOrderAsync("+78005553535", "B12");
    }

    [TearDown]
    public async Task CleanupAsync()
    {
        await ClearOrdersAsync();
    }

    [OneTimeTearDown]
    public async Task DisposeAsync()
    {
        if (dataSource is not null)
            await dataSource.DisposeAsync();
    }

    // телефон, артикул, повторный телефон.
    [Test]
    public async Task AddOrder_SavesExpectedFields()
    {
        var orders = await database.ListOrdersAsync();
        var first = orders.Single(o => o.Code == "PVZ-000001");
        var second = orders.Single(o => o.Code == "PVZ-000002");
        var third = orders.Single(o => o.Code == "PVZ-000003");

        Assert.Multiple(() =>
        {
            Assert.That(orders, Has.Count.EqualTo(3));

            Assert.That(first.Phone, Is.EqualTo("+78005553535"));
            Assert.That(first.Article, Is.Empty);
            Assert.That(first.Cell, Is.EqualTo("A1"));

            Assert.That(second.Phone, Is.Empty);
            Assert.That(second.Article, Is.EqualTo("ART-123456789"));
            Assert.That(second.Cell, Is.Empty);

            Assert.That(third.Phone, Is.EqualTo(first.Phone));
            Assert.That(third.Article, Is.Empty);
            Assert.That(third.Cell, Is.EqualTo("B12"));

            Assert.That(orders.Select(o => o.Status), Is.All.EqualTo(OrderStatuses.Ready));
        });
    }

    // короткий ввод не создаёт заказ.
    [Test]
    public async Task AddOrder_ShortContact_DoesNotCreateOrder()
    {
        Assert.ThrowsAsync<ArgumentException>(async () => await database.AddOrderAsync("abc", "A1"));

        var orders = await database.ListOrdersAsync();

        Assert.That(orders, Has.Count.EqualTo(3));
    }

    // Поиск по телефону, артикулу и коду.
    [Test]
    public async Task ListOrders_FindsByPhoneArticleAndCode()
    {
        var byPhone = await database.ListOrdersAsync("+7800");
        var byArticle = await database.ListOrdersAsync("art-");
        var byCode = await database.ListOrdersAsync("000002");

        Assert.Multiple(() =>
        {
            Assert.That(byPhone.Select(o => o.Code),
                Is.EquivalentTo([
                    "PVZ-000001",
                    "PVZ-000003"
                ]));

            Assert.That(byArticle, Has.Count.EqualTo(1));
            Assert.That(byArticle.Select(o => o.Code), Is.EqualTo(new[] { "PVZ-000002" }));

            Assert.That(byCode, Has.Count.EqualTo(1));
            Assert.That(byCode.Select(o => o.Code), Is.EqualTo(new[] { "PVZ-000002" }));
        });
    }

    //  фильтрация по статусу и получение всех заказов.
    [Test]
    public async Task ListByStatus_ReturnsExpectedOrders()
    {
        var ready = await database.ListByStatusAsync(OrderStatuses.Ready);

        var issued = await database.ListByStatusAsync(OrderStatuses.Issued);

        var cancelled = await database.ListByStatusAsync(OrderStatuses.Cancelled);

        var all = await database.ListByStatusAsync("");

        Assert.Multiple(() =>
        {
            Assert.That(ready, Has.Count.EqualTo(3));
            Assert.That(issued, Is.Empty);
            Assert.That(cancelled, Is.Empty);
            Assert.That(all, Has.Count.EqualTo(3));
        });
    }

    // выдача и запрет повторной выдачи.
    [Test]
    public async Task IssueOrder_IssuesOnceAndPreservesCell()
    {
        var firstResult = await database.IssueOrderAsync("PVZ-000001");

        var afterFirst = await ReadStateAsync("PVZ-000001");

        var secondResult = await database.IssueOrderAsync("PVZ-000001");

        var afterSecond = await ReadStateAsync("PVZ-000001");

        Assert.Multiple(() =>
        {
            Assert.That(firstResult, Is.True);
            Assert.That(afterFirst.Status, Is.EqualTo(OrderStatuses.Issued));
            Assert.That(afterFirst.IssuedAt, Is.Not.Null);
            Assert.That(afterFirst.Cell, Is.EqualTo("A1"));

            Assert.That(secondResult, Is.False);
            Assert.That(afterSecond, Is.EqualTo(afterFirst));
        });
    }

    //  отмена ready и запрет отмены issued.
    [Test]
    public async Task CancelOrder_CancelsReadyButKeepsIssued()
    {
        var cancelled = await database.CancelOrderAsync("PVZ-000002");

        var cancelledState = await ReadStateAsync("PVZ-000002");

        Assert.That(await database.IssueOrderAsync("PVZ-000001"), Is.True);

        var before = await ReadStateAsync("PVZ-000001");

        var issuedCancellation = await database.CancelOrderAsync("PVZ-000001");

        var after = await ReadStateAsync("PVZ-000001");

        Assert.Multiple(() =>
        {
            Assert.That(cancelled, Is.True);
            Assert.That(cancelledState.Status, Is.EqualTo(OrderStatuses.Cancelled));
            Assert.That(cancelledState.IssuedAt, Is.Null);

            Assert.That(issuedCancellation, Is.False);
            Assert.That(after, Is.EqualTo(before));
        });
    }

    // сводка и списки для каждого статуса.
    [Test]
    public async Task BuildReport_CountsCurrentStatuses()
    {
        Assert.That(await database.IssueOrderAsync("PVZ-000001"), Is.True);

        Assert.That(await database.CancelOrderAsync("PVZ-000002"), Is.True);

        var report = await database.BuildReportAsync();

        var ready = await database.ListByStatusAsync(OrderStatuses.Ready);

        var issued = await database.ListByStatusAsync(OrderStatuses.Issued);

        var cancelled = await database.ListByStatusAsync(OrderStatuses.Cancelled);

        Assert.Multiple(() =>
        {
            Assert.That(report.ReadyCount, Is.EqualTo(1));
            Assert.That(report.IssuedCount, Is.EqualTo(1));
            Assert.That(report.CancelledCount, Is.EqualTo(1));

            Assert.That(ready.Select(o => o.Code), Is.EqualTo(new[] { "PVZ-000003" }));

            Assert.That(issued.Select(o => o.Code), Is.EqualTo(new[] { "PVZ-000001" }));

            Assert.That(cancelled.Select(o => o.Code), Is.EqualTo(new[] { "PVZ-000002" }));
        });
    }

    // подсчёт и удаление только закрытых заказов.
    [Test]
    public async Task DeleteClosedOrders_KeepsReadyOrder()
    {
        Assert.That(await database.IssueOrderAsync("PVZ-000001"), Is.True);

        Assert.That(await database.CancelOrderAsync("PVZ-000002"), Is.True);

        var countBefore = await database.CountClosedOrdersAsync();
        var deleted = await database.DeleteClosedOrdersAsync();
        var countAfter = await database.CountClosedOrdersAsync();
        var remaining = await database.ListOrdersAsync();

        Assert.Multiple(() =>
        {
            Assert.That(countBefore, Is.EqualTo(2));
            Assert.That(deleted, Is.EqualTo(2));
            Assert.That(countAfter, Is.Zero);

            Assert.That(remaining, Has.Count.EqualTo(1));
            Assert.That(remaining.Select(o => o.Code), Is.EqualTo(new[] { "PVZ-000003" }));
        });
    }

    // SQL-инъекция не превращается в условие запроса.
    [Test]
    public async Task Orders_PersistAndSearchRejectsSqlInjection()
    {
        await using var secondSource = NpgsqlDataSource.Create(connectionString);

        var secondDatabase = new Database(secondSource);

        var orders = await secondDatabase.ListOrdersAsync();

        var searchResult = await secondDatabase.ListOrdersAsync("' OR '1'='1");

        var afterSearch = await secondDatabase.ListOrdersAsync();

        Assert.Multiple(() =>
        {
            Assert.That(orders, Has.Count.EqualTo(3));
            Assert.That(searchResult, Is.Empty);
            Assert.That(afterSearch, Has.Count.EqualTo(3));
        });
    }

    private async Task ClearOrdersAsync()
    {
        const string sql = "truncate table orders restart identity;";

        await using var command = dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<(string Status, string Cell, DateTime? IssuedAt)> ReadStateAsync(string code)
    {
        const string sql = "select status, coalesce(cell, ''), issued_at from orders where order_code = @code;";

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("code", code);

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            throw new InvalidOperationException($"Заказ {code} не найден.");

        return (
            reader.GetString(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetDateTime(2));
    }
}