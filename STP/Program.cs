using Lab7.Application.Exceptions;
using Lab7.Application.Interfaces;
using Lab7.Application.Services;
using Lab7.Infrastructure;
using Lab7.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var connectionString = "Host=localhost;Port=5433;Database=lab7;Username=postgres;Password=123";

var services = new ServiceCollection();

services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

services.AddScoped<IAssetRepository, AssetRepository>();
services.AddScoped<IEmployeeRepository, EmployeeRepository>();
services.AddScoped<IAssignmentRepository, AssignmentRepository>();
services.AddScoped<IIssuanceRepository, IssuanceRepository>();

services.AddScoped<AssetService>();
services.AddScoped<EmployeeService>();
services.AddScoped<AssignmentService>();

await using var provider = services.BuildServiceProvider(
    new ServiceProviderOptions
    {
        ValidateScopes = true,
        ValidateOnBuild = true
    });

var suffix = Guid.NewGuid().ToString("N")[..8];
var email = $"ivan-{suffix}@example.com";
var secondEmail = $"anna-{suffix}@example.com";
var inventoryNumber = $"CAM-{suffix}";

var employeeId = Guid.Empty;
var secondEmployeeId = Guid.Empty;
var assetId = Guid.Empty;

await Test("Добавление фотографов и техники", async sp =>
{
    var employees = sp.GetRequiredService<EmployeeService>();
    var assets = sp.GetRequiredService<AssetService>();
    var db = sp.GetRequiredService<AppDbContext>();

    await employees.AddEmployeeAsync("Иван Иванов", email);
    await employees.AddEmployeeAsync("Анна Петрова", secondEmail);
    await assets.AddAssetAsync(inventoryNumber, "Canon R6", "Камера");

    employeeId = await db.Employees
        .Where(x => x.Email == email)
        .Select(x => x.Id)
        .SingleAsync();

    secondEmployeeId = await db.Employees
        .Where(x => x.Email == secondEmail)
        .Select(x => x.Id)
        .SingleAsync();

    var asset = await db.Assets.AsNoTracking()
        .SingleAsync(x => x.InventoryNumber == inventoryNumber);

    assetId = asset.Id;

    Check(!asset.Assigned, "Новая техника должна быть в стойке");
});

await Test("Запрет повторного email", sp =>
    sp.GetRequiredService<EmployeeService>()
        .AddEmployeeAsync("Дубль", email),
    $"Фотограф с email {email} уже существует");

await Test("Запрет повторного инвентарного номера", sp =>
    sp.GetRequiredService<AssetService>()
        .AddAssetAsync(inventoryNumber, "Другая камера", "Камера"),
    $"Инвентарный номер {inventoryNumber} уже существует");

await Test("Редактирование фотографа", async sp =>
{
    await sp.GetRequiredService<EmployeeService>()
        .UpdateEmployeeAsync(
            employeeId, "Иван Сидоров", email, true);

    var employee = await sp.GetRequiredService<AppDbContext>()
        .Employees.AsNoTracking()
        .SingleAsync(x => x.Id == employeeId);

    Check(employee.FullName == "Иван Сидоров", "ФИО не обновлено");
});

await Test("Запрет чужого email при редактировании", sp =>
    sp.GetRequiredService<EmployeeService>()
        .UpdateEmployeeAsync(
            employeeId, "Ошибочное имя", secondEmail, false),
    $"Фотограф с email {secondEmail} уже существует");

await Test("Проверка отсутствия частичного обновления", async sp =>
{
    var employee = await sp.GetRequiredService<AppDbContext>()
        .Employees.AsNoTracking()
        .SingleAsync(x => x.Id == employeeId);

    Check(
        employee.FullName == "Иван Сидоров"
        && employee.Email == email
        && employee.Active,
        "После ошибки изменились данные фотографа");
});

await Test("Выдача техники", async sp =>
{
    await sp.GetRequiredService<AssignmentService>()
        .IssueAsync(assetId, employeeId);

    var db = sp.GetRequiredService<AppDbContext>();

    Check(
        await db.Assets.AnyAsync(x => x.Id == assetId && x.Assigned),
        "Статус техники не изменился");

    Check(
        await db.Assignments.AnyAsync(x =>
            x.AssetId == assetId
            && x.EmployeeId == employeeId
            && x.ReturnedAt == null),
        "Активная выдача не создана");
});

await Test("Запрет повторной выдачи", sp =>
    sp.GetRequiredService<AssignmentService>()
        .IssueAsync(assetId, secondEmployeeId),
    "Техника уже выдана");

await Test("Запрет увольнения с техникой", sp =>
    sp.GetRequiredService<EmployeeService>()
        .UpdateEmployeeAsync(
            employeeId, "Иван Сидоров", email, false),
    "У фотографа на руках техника. Для начала верните технику");

await Test("Возврат техники", async sp =>
{
    await sp.GetRequiredService<AssignmentService>()
        .GiveBackAsync(assetId);

    var db = sp.GetRequiredService<AppDbContext>();

    Check(
        await db.Assets.AnyAsync(x => x.Id == assetId && !x.Assigned),
        "Техника не вернулась в стойку");

    var assignment = await db.Assignments.AsNoTracking()
        .SingleAsync(x => x.AssetId == assetId);

    Check(assignment.ReturnedAt != null, "Дата возврата не заполнена");

    Check(
        assignment.ReturnedAt >= assignment.IssuedAt,
        "Возврат оказался раньше выдачи");
});

await Test("Запрет повторного возврата", sp =>
    sp.GetRequiredService<AssignmentService>()
        .GiveBackAsync(assetId),
    "Нет активной выдачи для этой техники");

await Test("Увольнение после возврата", async sp =>
{
    await sp.GetRequiredService<EmployeeService>()
        .UpdateEmployeeAsync(
            employeeId, "Иван Сидоров", email, false);

    Check(
        await sp.GetRequiredService<AppDbContext>()
            .Employees.AnyAsync(x => x.Id == employeeId && !x.Active),
        "Фотограф не уволен");
});

await Test("Запрет выдачи уволенному", sp =>
    sp.GetRequiredService<AssignmentService>()
        .IssueAsync(assetId, employeeId),
    "Нельзя выдать уволенному фотографу");

await Test("Повторная выдача другому фотографу", async sp =>
{
    await sp.GetRequiredService<AssignmentService>()
        .IssueAsync(assetId, secondEmployeeId);

    var db = sp.GetRequiredService<AppDbContext>();

    Check(
        await db.Assignments.CountAsync(x => x.AssetId == assetId) == 2,
        "История выдач не сохранилась");

    Check(
        await db.Assignments.CountAsync(x =>
            x.AssetId == assetId && x.ReturnedAt == null) == 1,
        "Должна существовать одна активная выдача");
});

await Test("Список техники с держателем", async sp =>
{
    var rows = await sp.GetRequiredService<AssetService>()
        .GetAllWithHoldersAsync();

    var row = rows.Single(x => x.asset.Id == assetId);

    Check(row.asset.Assigned, "Неверный статус в списке");
    Check(row.holder == "Анна Петрова", "Неверный держатель");
});

await Test("Держатель очищается после возврата", async sp =>
{
    await sp.GetRequiredService<AssignmentService>()
        .GiveBackAsync(assetId);

    var rows = await sp.GetRequiredService<AssetService>()
        .GetAllWithHoldersAsync();

    var row = rows.Single(x => x.asset.Id == assetId);

    Check(!row.asset.Assigned && row.holder == "",
        "После возврата статус или держатель не сброшены");
});

Console.WriteLine("\nВсе проверки пройдены.");

async Task Test(
    string name,
    Func<IServiceProvider, Task> action,
    string? expectedError = null)
{
    await using var scope = provider.CreateAsyncScope();

    try
    {
        await action(scope.ServiceProvider);
    }
    catch (DomainException exception) when (expectedError != null)
    {
        Check(
            exception.Message == expectedError,
            $"Ожидалось: {expectedError}\nПолучено: {exception.Message}");

        Console.WriteLine($"OK: {name}");
        return;
    }

    Check(expectedError == null, $"Не возникла ожидаемая ошибка: {name}");

    Console.WriteLine($"OK: {name}");
}

static void Check(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}