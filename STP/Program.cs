using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace lab5;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        try
        {
            await using var dataSource = CreateDataSource(args);

            await using (var connection = await dataSource.OpenConnectionAsync())
            {
                Console.WriteLine(
                    $"Подключено: {connection.Database}, {connection.Host}:{connection.Port}");
            }

            var database = new Database(dataSource);
            await RunMenuAsync(database);
        }
        catch (PostgresException exception)
        {
            Console.Error.WriteLine($"Ошибка PostgreSQL: {exception.MessageText}");
            Environment.ExitCode = 1;
        }
        catch (NpgsqlException)
        {
            Console.Error.WriteLine("Не удалось подключиться к PostgreSQL. Проверь настройки и запуск Docker.");
            Environment.ExitCode = 1;
        }
        catch (Exception exception) when (
            exception is IOException or InvalidOperationException or FormatException or ArgumentException)
        {
            Console.Error.WriteLine($"Ошибка запуска: {exception.Message}");
            Environment.ExitCode = 1;
        }
    }

    private static NpgsqlDataSource CreateDataSource(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        builder.Configuration.AddJsonFile(configPath, optional: false);

        var connectionString = builder.Configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("В appsettings.json не задана ConnectionStrings:Postgres.");

        return NpgsqlDataSource.Create(connectionString);
    }

    private static async Task RunMenuAsync(Database database)
    {
        while (true)
        {
            Console.WriteLine("""

                              1. Все заказы / поиск
                              2. Принять заказ
                              3. Выдать заказ
                              4. Отменить заказ
                              5. Заказы по статусу
                              6. Сводка по статусам
                              7. Количество закрытых заказов
                              8. Очистить историю
                              0. Выход
                              """);

            Console.Write("Выбери действие: ");
            var choice = Console.ReadLine()?.Trim();
            if (choice is null or "0")
                return;

            try
            {
                switch (choice)
                {
                    case "1":
                        var filter = ReadInput("Поиск (Enter — все заказы): ", allowEmpty: true);
                        PrintOrders(await database.ListOrdersAsync(filter));
                        break;

                    case "2":
                        var contact = ReadInput("Телефон или артикул: ");
                        var cell = ReadInput("Ячейка (может быть пустой): ", allowEmpty: true);
                        var code = await database.AddOrderAsync(contact, cell);
                        Console.WriteLine($"Заказ принят. Код: {code}");
                        break;

                    case "3":
                        var issueCode = ReadInput("Код заказа для выдачи: ");
                        var issued = await database.IssueOrderAsync(issueCode);
                        Console.WriteLine(issued
                            ? "Заказ выдан."
                            : "Заказ не найден или его статус отличается от ready.");
                        break;

                    case "4":
                        var cancelCode = ReadInput("Код заказа для отмены: ");
                        var cancelled = await database.CancelOrderAsync(cancelCode);
                        Console.WriteLine(cancelled
                            ? "Заказ отменён."
                            : "Заказ не найден или его статус отличается от ready.");
                        break;

                    case "5":
                        var status = ReadInput(
                            "Статус (ready / issued / cancelled; Enter — все): ", allowEmpty: true);

                        if (status.Length > 0 && status != OrderStatuses.Ready &&
                            status != OrderStatuses.Issued && status != OrderStatuses.Cancelled)
                        {
                            Console.WriteLine("Неизвестный статус.");
                            break;
                        }

                        PrintOrders(await database.ListByStatusAsync(status));
                        break;

                    case "6":
                        await PrintReportAsync(database);
                        break;

                    case "7":
                        Console.WriteLine(
                            $"Закрытых заказов: {await database.CountClosedOrdersAsync()}");
                        break;

                    case "8":
                        await ClearHistoryAsync(database);
                        break;

                    default:
                        Console.WriteLine("Введи номер пункта меню.");
                        break;
                }
            }
            catch (EndOfStreamException)
            {
                return;
            }
            catch (PostgresException exception)
            {
                Console.WriteLine($"Ошибка PostgreSQL: {exception.MessageText}");
            }
            catch (NpgsqlException)
            {
                Console.WriteLine("Ошибка обращения к БД. Проверь, что контейнер PostgreSQL запущен.");
            }
            catch (Exception exception) when (
                exception is ArgumentException or InvalidOperationException or OverflowException)
            {
                Console.WriteLine($"Ошибка: {exception.Message}");
            }
        }
    }

    private static async Task PrintReportAsync(Database database)
    {
        var report = await database.BuildReportAsync();
        Console.WriteLine($"К выдаче (ready): {report.ReadyCount}");
        Console.WriteLine($"Выдано (issued): {report.IssuedCount}");
        Console.WriteLine($"Отменено (cancelled): {report.CancelledCount}");
    }

    private static async Task ClearHistoryAsync(Database database)
    {
        var count = await database.CountClosedOrdersAsync();
        if (count == 0)
        {
            Console.WriteLine("Закрытых заказов нет.");
            return;
        }

        var answer = ReadInput(
            $"Сейчас закрытых заказов: {count}. Удалить все выданные и отменённые? (да/нет): ");

        if (!string.Equals(answer, "да", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Удаление отменено.");
            return;
        }

        var deleted = await database.DeleteClosedOrdersAsync();
        Console.WriteLine($"Удалено заказов: {deleted}");
    }

    private static void PrintOrders(List<Order> orders)
    {
        if (orders.Count == 0)
        {
            Console.WriteLine("Заказы не найдены.");
            return;
        }

        Console.WriteLine($"Найдено заказов: {orders.Count}");

        foreach (var order in orders)
        {
            Console.WriteLine($"\n{order.Code} | id: {order.Id} | {order.Status} | {order.CreatedAt}");
            Console.WriteLine($"  Телефон: {Display(order.Phone)}");
            Console.WriteLine($"  Артикул: {Display(order.Article)}");
            Console.WriteLine($"  Ячейка:  {Display(order.Cell)}");
        }
    }

    private static string Display(string value) => string.IsNullOrEmpty(value) ? "—" : value;

    private static string ReadInput(string prompt, bool allowEmpty = false)
    {
        while (true)
        {
            Console.Write(prompt);
            var value = Console.ReadLine()
                        ?? throw new EndOfStreamException("Ввод завершён.");

            value = value.Trim();
            if (allowEmpty || value.Length > 0)
                return value;

            Console.WriteLine("Значение не должно быть пустым.");
        }
    }
}