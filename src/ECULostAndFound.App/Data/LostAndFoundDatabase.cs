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

		initialization = database.CreateTableAsync<LostAndFoundItem>();
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
}