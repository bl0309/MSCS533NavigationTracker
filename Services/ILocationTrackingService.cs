using NavigationTracker.Models;

namespace NavigationTracker.Services;

public interface ILocationTrackingService
{
    event EventHandler<TrackPoint>? LocationRecorded;

    event EventHandler<string>? TrackingError;

    bool IsTracking { get; }

    Task StartAsync();

    Task StopAsync();
}
