using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using NavigationTracker.Data;
using NavigationTracker.Models;

namespace NavigationTracker.Services;

public class LocationTrackingService : ILocationTrackingService
{
    private readonly IGeolocation _geolocation;
    private readonly ILocationRepository _repository;
    private readonly ILogger<LocationTrackingService> _logger;
    private readonly TimeSpan _samplingInterval;
    private CancellationTokenSource? _cts;
    private Task? _trackingTask;

    public LocationTrackingService(
        IGeolocation geolocation,
        ILocationRepository repository,
        ILogger<LocationTrackingService> logger)
        : this(geolocation, repository, logger, TimeSpan.FromSeconds(5))
    {
    }

    public LocationTrackingService(
        IGeolocation geolocation,
        ILocationRepository repository,
        ILogger<LocationTrackingService> logger,
        TimeSpan samplingInterval)
    {
        _geolocation = geolocation;
        _repository = repository;
        _logger = logger;
        _samplingInterval = samplingInterval;
    }

    public event EventHandler<TrackPoint>? LocationRecorded;

    public event EventHandler<string>? TrackingError;

    public bool IsTracking => _cts is not null;

    public async Task StartAsync()
    {
        if (IsTracking)
        {
            return;
        }

        await _repository.InitializeAsync().ConfigureAwait(false);
        _cts = new CancellationTokenSource();
        _trackingTask = TrackAsync(_cts.Token);
    }

    public async Task StopAsync()
    {
        if (_cts is null)
        {
            return;
        }

        try
        {
            _cts.Cancel();
            if (_trackingTask is not null)
            {
                await _trackingTask.ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            _trackingTask = null;
        }
    }

    private async Task TrackAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
                var location = await _geolocation
                    .GetLocationAsync(request, cancellationToken)
                    .ConfigureAwait(false);

                if (location is not null)
                {
                    var point = new TrackPoint
                    {
                        Latitude = location.Latitude,
                        Longitude = location.Longitude,
                        Accuracy = location.Accuracy,
                        Speed = location.Speed,
                        RecordedAtUtc = location.Timestamp.UtcDateTime
                    };

                    await _repository.SaveAsync(point).ConfigureAwait(false);
                    LocationRecorded?.Invoke(this, point);
                }
            }
            catch (FeatureNotEnabledException ex)
            {
                HandleTrackingError("Location services are disabled. Enable them to continue.", ex);
                break;
            }
            catch (FeatureNotSupportedException ex)
            {
                HandleTrackingError("This device does not support location tracking.", ex);
                break;
            }
            catch (PermissionException ex)
            {
                HandleTrackingError("Location permission was denied.", ex);
                break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                HandleTrackingError("Unexpected error while tracking location.", ex);
            }

            try
            {
                await Task.Delay(_samplingInterval, cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private void HandleTrackingError(string message, Exception exception)
    {
        _logger.LogError(exception, message);
        TrackingError?.Invoke(this, message);
    }
}
