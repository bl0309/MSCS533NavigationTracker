using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using NavigationTracker.Data;
using NavigationTracker.Helpers;
using NavigationTracker.Services;
using NavigationTracker.ViewModels;

namespace NavigationTracker;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiMaps()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "locations.db3");

        builder.Services.AddSingleton<IGeolocation>(Geolocation.Default);
        builder.Services.AddSingleton<ILocationRepository>(_ => new LocationRepository(databasePath));
        builder.Services.AddSingleton<IHeatMapBuilder, HeatMapBuilder>();
        builder.Services.AddSingleton<ILocationTrackingService, LocationTrackingService>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        ServiceHelper.Initialize(app.Services);
        return app;
    }
}
