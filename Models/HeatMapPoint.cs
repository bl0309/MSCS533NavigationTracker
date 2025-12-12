using Microsoft.Maui.Graphics;

namespace NavigationTracker.Models;

public class HeatMapPoint
{
    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public double RadiusMeters { get; init; }

    public Color Color { get; init; } = Colors.Blue;

    public double Intensity { get; init; }
}
