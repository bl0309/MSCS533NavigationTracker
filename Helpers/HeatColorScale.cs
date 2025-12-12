using Microsoft.Maui.Graphics;

namespace NavigationTracker.Helpers;

public static class HeatColorScale
{
    private static readonly (double Offset, Color Color)[] Stops =
    [
        (0.0, Color.FromArgb("#A7F0FF")), // light aqua
        (0.5, Color.FromArgb("#4BB5FF")), // mid blue
        (1.0, Color.FromArgb("#2346C5"))  // deep blue
    ];

    public static Color Evaluate(double intensity)
    {
        var clamped = Math.Clamp(intensity, 0d, 1d);

        for (var index = 0; index < Stops.Length - 1; index++)
        {
            var current = Stops[index];
            var next = Stops[index + 1];

            if (clamped <= next.Offset)
            {
                var range = next.Offset - current.Offset;
                var normalized = range <= 0d ? 0d : (clamped - current.Offset) / range;
                return Lerp(current.Color, next.Color, normalized);
            }
        }

        return Stops[^1].Color;
    }

    private static Color Lerp(Color start, Color end, double amount)
    {
        var r = start.Red + (end.Red - start.Red) * amount;
        var g = start.Green + (end.Green - start.Green) * amount;
        var b = start.Blue + (end.Blue - start.Blue) * amount;
        var a = start.Alpha + (end.Alpha - start.Alpha) * amount;

        return Color.FromRgba(r, g, b, a);
    }
}
