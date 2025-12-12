using System.Collections.Specialized;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Graphics;
using NavigationTracker.Helpers;
using NavigationTracker.ViewModels;
using MapDistance = Microsoft.Maui.Maps.Distance;
using MapSpanControl = Microsoft.Maui.Maps.MapSpan;

namespace NavigationTracker;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        _viewModel.TrackPoints.CollectionChanged += OnCollectionChanged;
        _viewModel.HeatPoints.CollectionChanged += OnCollectionChanged;
    }

    public MainPage()
        : this(ServiceHelper.GetRequiredService<MainViewModel>())
    {
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync().ConfigureAwait(false);
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            RenderMap();
            _ = CenterMapAsync();
        });
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        MainThread.BeginInvokeOnMainThread(RenderMap);

    private void RenderMap()
    {
        if (TrackingMap is null)
        {
            return;
        }

        TrackingMap.MapElements.Clear();

        foreach (var heatPoint in _viewModel.HeatPoints)
        {
            var circle = new Circle
            {
                Center = new Location(heatPoint.Latitude, heatPoint.Longitude),
                Radius = MapDistance.FromMeters(heatPoint.RadiusMeters),
                FillColor = heatPoint.Color.WithAlpha(0.35f),
                StrokeColor = heatPoint.Color.WithAlpha(0.75f),
                StrokeWidth = 1
            };

            TrackingMap.MapElements.Add(circle);
        }

        if (_viewModel.TrackPoints.Count > 1)
        {
            var path = new Polyline
            {
                StrokeColor = Colors.Red,
                StrokeWidth = 6
            };

            foreach (var point in _viewModel.TrackPoints)
            {
                path.Geopath.Add(new Location(point.Latitude, point.Longitude));
            }

            TrackingMap.MapElements.Add(path);
        }
    }

    private async void OnCenterRequested(object sender, EventArgs e) =>
        await CenterMapAsync().ConfigureAwait(false);

    private async Task CenterMapAsync()
    {
        if (TrackingMap is null)
        {
            return;
        }

        Location? targetLocation = null;

        if (_viewModel.TrackPoints.Count > 0)
        {
            var last = _viewModel.TrackPoints[^1];
            targetLocation = new Location(last.Latitude, last.Longitude);
        }
        else
        {
            try
            {
                targetLocation = await Geolocation.Default.GetLastKnownLocationAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                targetLocation = null;
            }
        }

        if (targetLocation is null)
        {
            return;
        }

        var span = MapSpanControl.FromCenterAndRadius(targetLocation, MapDistance.FromMeters(800));
        TrackingMap.MoveToRegion(span);
    }
}
