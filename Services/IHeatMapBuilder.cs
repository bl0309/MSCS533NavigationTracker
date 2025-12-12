using NavigationTracker.Models;

namespace NavigationTracker.Services;

public interface IHeatMapBuilder
{
    IEnumerable<HeatMapPoint> Build(IEnumerable<TrackPoint> points);
}
