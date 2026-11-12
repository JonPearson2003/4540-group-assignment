using System.Text.Json;
using ECULostAndFound.Api.Data;
using ECULostAndFound.Api.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<LostAndFoundDbContext>(options =>
{
    var connectionString =
        builder.Configuration.GetConnectionString("DefaultConnection");

    options.UseSqlite(connectionString);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

await using (var scope = app.Services.CreateAsyncScope())
{
    var database =
        scope.ServiceProvider.GetRequiredService<LostAndFoundDbContext>();

    await database.Database.MigrateAsync();

    var unversionedItems = await database.LostAndFoundItems
        .Where(item => item.ServerVersion == 0)
        .OrderBy(item => item.Id)
        .ToListAsync();

    if (unversionedItems.Count > 0)
    {
        await using var transaction =
            await database.Database.BeginTransactionAsync();

        var baselineEntries = unversionedItems
            .Select(item => new ProcessedChange
            {
                ChangeId = $"baseline-{item.Id}",
                RecordId = item.Id,
                OperationType = "snapshot",
                ProcessedAtUtc = DateTime.UtcNow
            })
            .ToList();

        database.ProcessedChanges.AddRange(baselineEntries);
        await database.SaveChangesAsync();

        for (var index = 0; index < unversionedItems.Count; index++)
        {
            var item = unversionedItems[index];
            var entry = baselineEntries[index];

            item.ServerVersion = entry.Id;
            entry.ItemJson = JsonSerializer.Serialize(item);
        }

        await database.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}

app.MapGet("/", () =>
{
    return Results.Ok("ECU Lost and Found API is running.");
});

app.MapGet("/items", async (LostAndFoundDbContext database) =>
{
    var items = await database.LostAndFoundItems
        .OrderBy(item => item.Id)
        .ToListAsync();

    return Results.Ok(items);
});

app.MapGet("/changes", async (
    long afterVersion,
    LostAndFoundDbContext database) =>
{
    if (afterVersion < 0)
    {
        return Results.BadRequest("afterVersion cannot be negative.");
    }

    var entries = await database.ProcessedChanges
        .Where(change => change.Id > afterVersion)
        .OrderBy(change => change.Id)
        .ToListAsync();

    var changes = entries.Select(entry => new
    {
        Version = entry.Id,
        Item = JsonSerializer.Deserialize<LostAndFoundItem>(
            entry.ItemJson)
            ?? throw new InvalidOperationException(
                $"Processed change {entry.Id} has an invalid item snapshot.")
    });

    return Results.Ok(changes);
});

app.MapGet("/audit", async (LostAndFoundDbContext database) =>
{
    var entries = await database.AuditEntries
        .OrderByDescending(entry => entry.Id)
        .ToListAsync();

    return Results.Ok(entries);
});

app.MapPost("/audit", async (
    AuditEntryRequest request,
    LostAndFoundDbContext database) =>
{
    if (string.IsNullOrWhiteSpace(request.ChangeId))
    {
        return Results.BadRequest("ChangeId is required.");
    }

    if (request.ItemId <= 0 ||
        string.IsNullOrWhiteSpace(request.ChangeType) ||
        string.IsNullOrWhiteSpace(request.ActorId))
    {
        return Results.BadRequest(
            "ItemId, ChangeType, and ActorId are required.");
    }

    var existingEntry = await database.AuditEntries
        .SingleOrDefaultAsync(
            entry => entry.ChangeId == request.ChangeId);

    if (existingEntry is not null)
    {
        return Results.Ok(new { existingEntry.ChangeId });
    }

    var entry = new AuditEntry
    {
        ChangeId = request.ChangeId,
        ItemId = request.ItemId,
        ChangeType = request.ChangeType,
        ColumnName = request.ColumnName,
        OldValue = request.OldValue,
        NewValue = request.NewValue,
        ChangedAtUtc = request.ChangedAtUtc,
        ActorId = request.ActorId,
        Source = request.Source,
        ReceivedAtUtc = DateTime.UtcNow
    };

    database.AuditEntries.Add(entry);
    await database.SaveChangesAsync();

    return Results.Created(
        $"/audit/{entry.Id}",
        new { entry.ChangeId, entry.Id });
});

app.MapPost("/pushChanges", async (
    PushChangeRequest change,
    LostAndFoundDbContext database) =>
{
    if (string.IsNullOrWhiteSpace(change.ChangeId))
    {
        return Results.BadRequest("ChangeId is required.");
    }

    if (change.RecordId <= 0)
    {
        return Results.BadRequest("RecordId must be greater than zero.");
    }

    var operation = change.OperationType?.Trim().ToLowerInvariant();

    if (operation is not ("create" or "update" or "delete"))
    {
        return Results.BadRequest("Invalid operation type.");
    }

    if (operation != "delete" && change.Item is null)
    {
        return Results.BadRequest("Item data is required.");
    }

    var previous = await database.ProcessedChanges
        .SingleOrDefaultAsync(entry => entry.ChangeId == change.ChangeId);

    if (previous is not null)
    {
        return Results.Ok(new
        {
            change.ChangeId,
            ServerVersion = previous.Id
        });
    }

    await using var transaction =
        await database.Database.BeginTransactionAsync();

    var item = await database.LostAndFoundItems
        .SingleOrDefaultAsync(existing => existing.Id == change.RecordId);

    if (operation == "delete")
    {
        item ??= new LostAndFoundItem { Id = change.RecordId };
        item.IsDeleted = true;
        item.Status = "Closed";
        item.UserId = change.DeviceUserId;
        item.LastUpdated = DateTime.UtcNow;
    }
    else
    {
        var incomingItem = change.Item!;
        item ??= new LostAndFoundItem { Id = change.RecordId };

        item.Title = incomingItem.Title;
        item.Description = incomingItem.Description;
        item.Category = incomingItem.Category;
        item.Location = incomingItem.Location;
        item.ReportType = incomingItem.ReportType;
        item.Status = incomingItem.Status;
        item.DateReported = incomingItem.DateReported;
        item.IsClaimed = incomingItem.IsClaimed;
        item.IsDeleted = false;
        item.UserId = change.DeviceUserId;
        item.LastUpdated = DateTime.UtcNow;
    }

    if (database.Entry(item).State == EntityState.Detached)
    {
        database.LostAndFoundItems.Add(item);
    }

    var logEntry = new ProcessedChange
    {
        ChangeId = change.ChangeId,
        RecordId = change.RecordId,
        OperationType = operation,
        ProcessedAtUtc = DateTime.UtcNow
    };

    database.ProcessedChanges.Add(logEntry);
    await database.SaveChangesAsync();

    item.ServerVersion = logEntry.Id;
    logEntry.ItemJson = JsonSerializer.Serialize(item);

    await database.SaveChangesAsync();
    await transaction.CommitAsync();

    return Results.Ok(new
    {
        change.ChangeId,
        ServerVersion = logEntry.Id
    });
});

app.Run();
