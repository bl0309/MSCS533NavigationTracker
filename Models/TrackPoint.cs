using SQLite;

namespace NavigationTracker.Models;

public class TrackPoint
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public double? Accuracy { get; set; }

    public double? Speed { get; set; }

    public DateTime RecordedAtUtc { get; set; }
}
