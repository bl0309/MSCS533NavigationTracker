using NavigationTracker.Models;

namespace NavigationTracker.Data;

public interface ILocationRepository
{
    Task InitializeAsync();

    Task SaveAsync(TrackPoint point);

    Task<IReadOnlyList<TrackPoint>> GetTrackAsync();

    Task DeleteAllAsync();
}
