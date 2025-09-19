using Gio;
using Gtk;
using Microsoft.Extensions.Logging;
using Woofer.Models;
using Woofer.Services;

namespace Woofer.UI;

public class MainWindow : Adw.ApplicationWindow, IDisposable
{
    private ILogger<MainWindow> _logger;

    [Connect(widgetName: "sidebar_list")] public readonly ListBox SidebarList = null!;
    [Connect(widgetName: "track_list_container")] public readonly ScrolledWindow trackListContainer = null!;
    [Connect(widgetName: "track_grid_container")] public readonly Gtk.ScrolledWindow trackGridContainer = null!;
    [Connect(widgetName: "view_toggle")] public readonly Adw.ToggleGroup viewToggle = null!;
    [Connect(widgetName: "view_stack")] public readonly Stack viewStack = null!;
    [Connect(widgetName: "play_pause_button")] public readonly Button playPauseButton = null!;
    [Connect(widgetName: "progress_scale")] public readonly Scale progressScale = null!;
    [Connect("current_time_label")] public readonly Label currentTimeLabel = null!;
    [Connect(widgetName: "volume_scale")] public readonly Scale volumeScale = null!;
    [Connect(widgetName: "volume_button")] public readonly Button volumeButton = null!;
    [Connect(widgetName: "repeat_button")] public readonly Button repeatButton = null!;
    [Connect(widgetName: "shuffle_button")] public readonly Button shuffleButton = null!;

    private readonly MusicLibrary musicLibrary;
    private readonly Gio.ListStore TracksModel;
    private readonly SelectionModel SelectionModel;
    private readonly PlaylistManager playlistManager;
    public readonly PlayerController playerController;
    private TracksListView? tracksListView;
    private TracksGridView? tracksGridView;
    private uint _positionUpdateId;
    private double volumeLevel = 0.8;

    private MainWindow(Builder builder, string name) : base(new Adw.Internal.ApplicationWindowHandle(builder.GetPointer(name), false))
    {
        builder.Connect(this);

        // Do any initialization, or connect signals here.
        //  Инициализируем менеджер плейлистов
        playlistManager = new PlaylistManager();

        playerController = new PlayerController(settingsManager: null);

        // Подключаем сигналы от плеера
        playerController.OnStateChanged += OnPlayerStateChanged;
        playerController.OnPositionChanged += OnPositionChanged;

        TracksModel = Gio.ListStore.New(TrackRowData.GetGType());
        SelectionModel = SingleSelection.New(TracksModel);

        SetupTrackViews();
        SetupControls();
        SetupUiUpdates();
        SetupActions();

        SidebarList.OnRowActivated += OnPlaylistRowActivated;

        musicLibrary = new MusicLibrary();
        ScanMusicLibrary();
    }

    private void SetupActions()
    {
        var action = SimpleAction.New("toggle-mute", null);
        action.OnActivate += OnMuteToggle;
        AddAction(action);
    }

    private void OnMuteToggle(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        _logger.LogInformation("Toggle mute");
        if (volumeScale.GetValue() > 0)
        {
            volumeLevel = volumeScale.GetValue();
            volumeScale.SetValue(0);
        }
        else
        {
            volumeScale.SetValue(volumeLevel);
        }
        playerController.Volume = volumeScale.GetValue() / 100;
        UpdateVolumeUi(volumeScale.GetValue());
    }

    /// <summary>
    /// Настройка периодического обновления UI.
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private void SetupUiUpdates()
    {
    }

    private void OnPositionChanged(int obj)
    {
        UpdatePositionUi();
    }

    /// <summary>
    /// Обновляет UI с текущей позицией воспроизведения.
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private void UpdatePositionUi()
    {
        var position = playerController.Position;
        var duration = playerController.Duration;

        if (duration > 0)
        {
            var progress = (double)position / duration * 100;
            progressScale.SetValue(progress);
        }
        else
        {
            progressScale.SetValue(0);
        }

        // Обновляем текущее время
        Math.DivRem(position, 60, out var seconds);
        var minutes = position / 60;
        currentTimeLabel.SetLabel($"{minutes:00}:{seconds:00}");

    }

    private void OnPlayerStateChanged(PlayerState state)
    {
        if (state == PlayerState.Playing)
        {
            playPauseButton.SetIconName("media-playback-pause-symbolic");
        }
        else
        {
            playPauseButton.SetIconName("media-playback-start-symbolic");
        }
    }

    private void SetupControls()
    {
        // Настройка шкалы громкости
        volumeScale.OnChangeValue += OnVolumeChanged;

        // Подключаем кнопку воспроизведения/паузы
        playPauseButton.OnClicked += OnPlayPauseButtonClicked;

        // Подключаем переключение режимов отображения
        viewToggle.OnNotify += (sender, args) =>
        {
            if (args.Pspec.GetName() == "active-name")
            {
                switch (viewToggle.GetActiveName())
                {
                    case "list":
                        ShowListView();
                        break;
                    case "grid":
                        ShowGridView();
                        break;
                }
            }
        };

        // Настройка шкалы прогресса
        progressScale.OnChangeValue += OnProgressChanged;
        progressScale.SetRange(0, 100);
    }

    /// <summary>
    /// Обновляет иконку громкости в UI.
    /// </summary>
    /// <param name="volume"></param>
    private void UpdateVolumeUi(double volume)
    {
        var volumeIcon = volume switch
        {
            0 => "audio-volume-muted-symbolic",
            > 0 and < 0.5 => "audio-volume-low-symbolic",
            >= 0.5 and < 0.8 => "audio-volume-medium-symbolic",
            _ => "audio-volume-high-symbolic"
        };

        GLib.Functions.IdleAdd(GLib.Constants.PRIORITY_DEFAULT, () =>
            {
                _logger.LogDebug("Update volume icon to {icon}", volumeIcon);
                volumeButton.SetIconName(volumeIcon);
                return false;
            });

    }

    /// <summary>
    /// Обработка изменения громкости.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private bool OnVolumeChanged(Gtk.Range sender, Gtk.Range.ChangeValueSignalArgs args)
    {
        var volume = args.Value / 100;
        playerController.Volume = volume;

        UpdateVolumeUi(volume);

        return false;
    }

    /// <summary>
    /// Обработка изменения шкалы прогресса.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private bool OnProgressChanged(Gtk.Range sender, Gtk.Range.ChangeValueSignalArgs args)
    {
        if (playerController.State != PlayerState.Stopped)
        {
            var duration = playerController.Duration;
            if (duration > 0)
            {
                var position = (int)(args.Value / 100 * duration);
                GLib.Functions.IdleAdd(GLib.Constants.PRIORITY_DEFAULT_IDLE, () => { playerController.Seek(position); return false; });
            }
        }
        return true;
    }

    private void OnPlayPauseButtonClicked(Button sender, EventArgs args)
    {
        switch (playerController.State)
        {
            case PlayerState.Stopped:
                playerController.Play();
                break;
            default:
                playerController.TogglePlayPause();
                break;
        }
    }

    private void ShowListView()
    {
        viewStack.VisibleChildName = "list";
    }

    private void ShowGridView()
    {
        viewStack.VisibleChildName = "grid";
    }

    public MainWindow(Adw.Application application) : this(new Builder("MainWindow.ui"), "main_window")
    {
        Application = application;

        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<MainWindow>();
        _logger.LogInformation("MainWindow created");
    }

    private void SetupTrackViews()
    {
        tracksListView = new TracksListView
        {
            Model = SelectionModel
        };
        trackListContainer.Child = tracksListView;
        tracksGridView = new TracksGridView
        {
            Model = SelectionModel
        };
        trackGridContainer.Child = tracksGridView;

        tracksListView.OnTrackActivated += OnTrackActivated;
        tracksGridView.OnTrackActivated += OnTrackActivated;
    }

    private void OnTrackActivated(Track track)
    {
        playerController.Stop();
        playerController.PlayTrack(track);

        // Таймер для обновления позиции воспроизведения
        _positionUpdateId = GLib.Functions.TimeoutAdd(GLib.Constants.PRIORITY_LOW, 1000, () =>
        {
            UpdatePositionUi();
            return true; // Возвращаем true, чтобы таймер продолжал работать
        });
    }

    public void OnPlaylistRowActivated(object sender, ListBox.RowActivatedSignalArgs args)
    {
        switch (args.Row.Name)
        {
            case "music-row":
                Console.WriteLine("Music row activated");
                break;
            default:
                Console.WriteLine("Other row activated");
                break;
        }
    }

    private void ScanMusicLibrary()
    {
        var musicDirPath = GLib.Functions.GetUserSpecialDir(GLib.UserDirectory.DirectoryMusic);
        if (musicDirPath == null)
        {
            Console.WriteLine("Music directory not found");
            return;
        }
        var musicDir = new DirectoryInfo(musicDirPath);

        var scanner = new MusicScanner();
        scanner.OnTrackFound += OnTrackFound;

        var thread = new System.Threading.Thread(() => scanner.ScanDirectory(musicDir.FullName));
        thread.Start();
    }

    private void OnTrackFound(object? sender, Track track)
    {
        Console.WriteLine($"Track found: {track.Title}");

        GLib.Functions.IdleAdd(100, () =>
        {
            AddTrackToModel(track);
            return false;
        });
    }

    private void AddTrackToModel(Track track)
    {
        var trackRowData = new TrackRowData(track);
        TracksModel.Append(trackRowData);
    }

    public override void Dispose()
    {
        if (_positionUpdateId != 0)
        { GLib.Functions.SourceRemove(_positionUpdateId); }

        playerController?.Dispose();

        GC.SuppressFinalize(this);
        base.Dispose();
    }
}
