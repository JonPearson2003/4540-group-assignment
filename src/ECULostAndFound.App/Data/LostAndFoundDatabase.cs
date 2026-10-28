using ECULostAndFound.App.Models;
using Microsoft.Maui.Storage;
using SQLite;

namespace ECULostAndFound.App.Data;

public sealed class LostAndFoundDatabase
{
	private readonly SQLiteAsyncConnection database;
	private readonly Task initialization;

	public LostAndFoundDatabase()
	{
		var databasePath = Path.Combine(FileSystem.AppDataDirectory, "lostandfound.db3");

		database = new SQLiteAsyncConnection(
			databasePath,
			SQLiteOpenFlags.ReadWrite |
			SQLiteOpenFlags.Create |
			SQLiteOpenFlags.SharedCache);

		initialization = InitializeAsync();

	}
	private async Task InitializeAsync()
{
    await database.CreateTableAsync<LostAndFoundItem>();
    await database.CreateTableAsync<ChangesTable>();
	await database.CreateTableAsync<PendingSyncChange>();
}	

	public async Task<List<LostAndFoundItem>> GetLostAndFoundItemsAsync()
	{
		await initialization;
		return await database.Table<LostAndFoundItem>()
			.OrderByDescending(item => item.DateReported)
			.ToListAsync();
	}

	public async Task<int> SaveLostAndFoundItemAsync(LostAndFoundItem item)
	{
		await initialization;

		if (item.Id == 0)
		{
			var now = DateTime.UtcNow;
			item.DateReported = now;
			item.LastUpdated = now;
			return await database.InsertAsync(item);
		}

		item.LastUpdated = DateTime.UtcNow;
		return await database.UpdateAsync(item);
	}

	public async Task<int> DeleteLostAndFoundItemAsync(LostAndFoundItem item)
	{
		await initialization;
		return await database.DeleteAsync(item);
	}

	public async Task<int> SaveChangeAsync(ChangesTable change)
	{
    	await initialization;
    	return await database.InsertAsync(change);
	}

	public async Task<int> QueuePendingChangeAsync(PendingSyncChange change)
	{
		await initialization;

		if (string.IsNullOrWhiteSpace(change.ChangeId))
			change.ChangeId = Guid.NewGuid().ToString();

		change.IsSynced = false;
		return await database.InsertAsync(change);
	}

	public async Task<List<PendingSyncChange>> GetPendingChangesAsync()
	{
		await initialization;

		return await database.Table<PendingSyncChange>()
			.Where(change => !change.IsSynced)
			.OrderBy(change => change.Id)
			.ToListAsync();
	}

	public async Task<int> MarkChangeSyncedAsync(int id)
	{
		await initialization;

		var change = await database.Table<PendingSyncChange>()
			.Where(change => change.Id == id)
			.FirstOrDefaultAsync();

		if (change is null)
			return 0;

		change.IsSynced = true;
		return await database.UpdateAsync(change);
	}
}