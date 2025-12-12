using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using NavigationTracker.Data;
using NavigationTracker.Helpers;
using NavigationTracker.Models;
using NavigationTracker.Services;

namespace NavigationTracker.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ILocationRepository _repository;
    private readonly ILocationTrackingService _trackingService;
    private readonly IHeatMapBuilder _heatMapBuilder;
    private readonly AsyncCommand _toggleTrackingCommand;
    private readonly AsyncCommand _clearHistoryCommand;
    private readonly AsyncCommand _refreshHeatMapCommand;
    private string _statusMessage = "Tap Start to begin tracking.";
    private bool _isInitialized;
    private bool _isTracking;
    private bool _isControlPanelExpanded = true;

    public MainViewModel(
        ILocationRepository repository,
        ILocationTrackingService trackingService,
        IHeatMapBuilder heatMapBuilder)
    {
        _repository = repository;
        _trackingService = trackingService;
        _heatMapBuilder = heatMapBuilder;

        _toggleTrackingCommand = new AsyncCommand(ToggleTrackingAsync);
        _clearHistoryCommand = new AsyncCommand(ClearAsync, () => TrackPoints.Count > 0 && !IsTracking);
        _refreshHeatMapCommand = new AsyncCommand(RefreshAsync, () => TrackPoints.Count > 0);
        ToggleControlPanelCommand = new Command(() => IsControlPanelExpanded = !IsControlPanelExpanded);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<TrackPoint> TrackPoints { get; } = new();

    public ObservableCollection<HeatMapPoint> HeatPoints { get; } = new();

    public ICommand ToggleTrackingCommand => _toggleTrackingCommand;

    public ICommand ClearHistoryCommand => _clearHistoryCommand;

    public ICommand RefreshHeatMapCommand => _refreshHeatMapCommand;

    public ICommand ToggleControlPanelCommand { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value)
            {
                return;
            }

            _statusMessage = value;
            OnPropertyChanged(nameof(StatusMessage));
        }
    }

    public bool IsTracking
    {
        get => _isTracking;
        private set
        {
            if (_isTracking == value)
            {
                return;
            }

            _isTracking = value;
            OnPropertyChanged(nameof(IsTracking));
            UpdateCommandStates();
        }
    }

    public bool IsControlPanelExpanded
    {
        get => _isControlPanelExpanded;
        private set
        {
            if (_isControlPanelExpanded == value)
            {
                return;
            }

            _isControlPanelExpanded = value;
            OnPropertyChanged(nameof(IsControlPanelExpanded));
            OnPropertyChanged(nameof(ControlPanelToggleText));
        }
    }

    public string ControlPanelToggleText =>
        IsControlPanelExpanded ? "Hide controls" : "Show controls";

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        await _repository.InitializeAsync().ConfigureAwait(false);
        _trackingService.LocationRecorded += OnLocationRecorded;
        _trackingService.TrackingError += OnTrackingError;
        await LoadPersistedTrackAsync().ConfigureAwait(false);

        _isInitialized = true;
    }

    private async Task LoadPersistedTrackAsync()
    {
        var history = await _repository.GetTrackAsync().ConfigureAwait(false);
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            TrackPoints.Clear();
            foreach (var point in history)
            {
                TrackPoints.Add(point);
            }

            RefreshHeatMap();
            UpdateCommandStates();
        });
    }

    private async Task ToggleTrackingAsync()
    {
        if (IsTracking)
        {
            await StopTrackingAsync().ConfigureAwait(false);
        }
        else
        {
            await StartTrackingAsync().ConfigureAwait(false);
        }
    }

    private async Task StartTrackingAsync()
    {
        if (IsTracking)
        {
            return;
        }

        StatusMessage = "Requesting permissions...";
        var permissionGranted = await EnsureLocationPermissionAsync().ConfigureAwait(false);
        if (!permissionGranted)
        {
            StatusMessage = "Location permission denied.";
            return;
        }

        StatusMessage = "Tracking location...";
        await _trackingService.StartAsync().ConfigureAwait(false);
        IsTracking = true;
    }

    private async Task StopTrackingAsync()
    {
        if (!IsTracking)
        {
            return;
        }

        await _trackingService.StopAsync().ConfigureAwait(false);
        IsTracking = false;
        StatusMessage = "Tracking paused.";
    }

    private async Task ClearAsync()
    {
        await _trackingService.StopAsync().ConfigureAwait(false);
        await _repository.DeleteAllAsync().ConfigureAwait(false);

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            TrackPoints.Clear();
            HeatPoints.Clear();
            UpdateCommandStates();
            StatusMessage = "History cleared.";
        });
    }

    private void OnLocationRecorded(object? sender, TrackPoint point)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            TrackPoints.Add(point);
            RefreshHeatMap();
            UpdateCommandStates();
        });
    }

    private void OnTrackingError(object? sender, string message)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            StatusMessage = message;
            await StopTrackingAsync().ConfigureAwait(false);
        });
    }

    private void RefreshHeatMap()
    {
        HeatPoints.Clear();
        foreach (var heatPoint in _heatMapBuilder.Build(TrackPoints))
        {
            HeatPoints.Add(heatPoint);
        }
        UpdateCommandStates();
    }

    private Task RefreshAsync()
    {
        return MainThread.InvokeOnMainThreadAsync(RefreshHeatMap);
    }

    private async Task<bool> EnsureLocationPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>().ConfigureAwait(false);
        if (status == PermissionStatus.Granted)
        {
            return true;
        }

        status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>().ConfigureAwait(false);
        if (status == PermissionStatus.Granted)
        {
            return true;
        }

#if ANDROID || IOS
        status = await Permissions.RequestAsync<Permissions.LocationAlways>().ConfigureAwait(false);
        if (status == PermissionStatus.Granted)
        {
            return true;
        }
#endif

        return false;
    }

    private void UpdateCommandStates()
    {
        _clearHistoryCommand.RaiseCanExecuteChanged();
        _toggleTrackingCommand.RaiseCanExecuteChanged();
        _refreshHeatMapCommand.RaiseCanExecuteChanged();
    }

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
