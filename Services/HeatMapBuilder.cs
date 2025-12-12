using Microsoft.Maui.Graphics;
using NavigationTracker.Helpers;
using NavigationTracker.Models;

namespace NavigationTracker.Services;

public class HeatMapBuilder : IHeatMapBuilder
{
    private const int CoordinatePrecision = 3;

    public IEnumerable<HeatMapPoint> Build(IEnumerable<TrackPoint> points)
    {
        if (points is null)
        {
            yield break;
        }

        var grouped = points
            .GroupBy(point => (
                Math.Round(point.Latitude, CoordinatePrecision),
                Math.Round(point.Longitude, CoordinatePrecision)))
            .ToList();

        if (grouped.Count == 0)
        {
            yield break;
        }

        var maxDensity = grouped.Max(group => group.Count());

        foreach (var group in grouped)
        {
            var intensity = (double)group.Count() / maxDensity;
            intensity = Math.Clamp(intensity, 0.15d, 1d);

            yield return new HeatMapPoint
            {
                Latitude = group.Average(point => point.Latitude),
                Longitude = group.Average(point => point.Longitude),
                RadiusMeters = 100d + 180d * intensity,
                Color = HeatColorScale.Evaluate(intensity),
                Intensity = intensity
            };
        }
    }
}
