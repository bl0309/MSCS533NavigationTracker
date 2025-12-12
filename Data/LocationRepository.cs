using System.Linq;
using NavigationTracker.Models;
using SQLite;

namespace NavigationTracker.Data;

public class LocationRepository : ILocationRepository
{
    private readonly SQLiteAsyncConnection _connection;
    private Task? _initializationTask;

    public LocationRepository(string databasePath)
    {
        _connection = new SQLiteAsyncConnection(databasePath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
    }

    public Task InitializeAsync() => EnsureInitializedAsync();

    public async Task SaveAsync(TrackPoint point)
    {
        if (point == null)
        {
            return;
        }

        await EnsureInitializedAsync().ConfigureAwait(false);
        await _connection.InsertAsync(point).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TrackPoint>> GetTrackAsync()
    {
        await EnsureInitializedAsync().ConfigureAwait(false);
        var items = await _connection
            .Table<TrackPoint>()
            .OrderBy(point => point.RecordedAtUtc)
            .ToListAsync()
            .ConfigureAwait(false);

        return items;
    }

    public async Task DeleteAllAsync()
    {
        await EnsureInitializedAsync().ConfigureAwait(false);
        await _connection.DeleteAllAsync<TrackPoint>().ConfigureAwait(false);
    }

    private Task EnsureInitializedAsync()
    {
        return _initializationTask ??= _connection.CreateTableAsync<TrackPoint>();
    }
}
