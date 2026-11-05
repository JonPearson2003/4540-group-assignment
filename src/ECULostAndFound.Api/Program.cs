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
    DateTime since,
    LostAndFoundDbContext database) =>
{
    var changes = await database.LostAndFoundItems
        .Where(item => item.LastUpdated > since)
        .OrderBy(item => item.LastUpdated)
        .ToListAsync();

    return Results.Ok(changes);
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

    var operation =
        change.OperationType.Trim().ToLowerInvariant();

    if (operation != "create" &&
        operation != "update" &&
        operation != "delete")
    {
        return Results.BadRequest("Invalid operation type.");
    }

    var existingItem = await database.LostAndFoundItems
        .FirstOrDefaultAsync(
            item => item.Id == change.RecordId);

    if (operation == "delete")
    {
        if (existingItem is null)
        {
            var deletedItem = new LostAndFoundItem
            {
                Id = change.RecordId,
                Status = "Closed",
                LastUpdated = change.LastModified,
                UserId = change.DeviceUserId,
                IsDeleted = true
            };

            await database.LostAndFoundItems.AddAsync(
                deletedItem);
        }
        else if (change.LastModified >= existingItem.LastUpdated)
        {
            existingItem.Status = "Closed";
            existingItem.LastUpdated = change.LastModified;
            existingItem.UserId = change.DeviceUserId;
            existingItem.IsDeleted = true;
        }

        await database.SaveChangesAsync();

        return Results.Ok(new
        {
            change.ChangeId,
            Operation = operation
        });
    }

    if (change.Item is null)
    {
        return Results.BadRequest(
            "Item data is required for create and update operations.");
    }

    if (existingItem is null)
    {
        var incomingItem = change.Item;

        incomingItem.Id = change.RecordId;
        incomingItem.LastUpdated = change.LastModified;
        incomingItem.UserId = change.DeviceUserId;
        incomingItem.IsDeleted = false;

        await database.LostAndFoundItems.AddAsync(
            incomingItem);
    }
    else if (change.LastModified >= existingItem.LastUpdated)
    {
        existingItem.Title = change.Item.Title;
        existingItem.Description = change.Item.Description;
        existingItem.Category = change.Item.Category;
        existingItem.Location = change.Item.Location;
        existingItem.ReportType = change.Item.ReportType;
        existingItem.Status = change.Item.Status;
        existingItem.DateReported = change.Item.DateReported;
        existingItem.LastUpdated = change.LastModified;
        existingItem.UserId = change.DeviceUserId;
        existingItem.IsClaimed = change.Item.IsClaimed;
        existingItem.IsDeleted = false;
    }

    await database.SaveChangesAsync();

    return Results.Ok(new
    {
        change.ChangeId,
        Operation = operation
    });
});

app.Run();