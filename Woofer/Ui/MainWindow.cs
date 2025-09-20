using GdkPixbuf;
using Gio;
using Gtk;
using Microsoft.Extensions.Logging;
using Woofer.Extensions;
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
    [Connect("current_track_title")] public readonly Label currentTrackTitle = null!;
    [Connect("current_track_artist")] public readonly Label currentTrackArtist = null!;
    [Connect("current_time_label")] public readonly Label currentTimeLabel = null!;
    [Connect("total_time_label")] public readonly Label totalTimeLabel = null!;
    [Connect("current_track_artwork")] public readonly Picture currentTrackArtwork = null!;
    [Connect(widgetName: "volume_scale")] public readonly Scale volumeScale = null!;
    [Connect(widgetName: "volume_button")] public readonly Button volumeButton = null!;
    [Connect(widgetName: "repeat_button")] public readonly ToggleButton repeatButton = null!;
    [Connect(widgetName: "shuffle_button")] public readonly ToggleButton shuffleButton = null!;

    private readonly MusicLibrary musicLibrary;
    private readonly Gio.ListStore TracksModel;
    private readonly SelectionModel SelectionModel;
    private readonly PlaylistManager playlistManager;
    public readonly PlayerController playerController;
    private TracksListView? tracksListView;
    private TracksGridView? tracksGridView;
    private uint _positionUpdateId;
    private double volumeLevel = 0.8;
    private RepeatMode repeatMode = RepeatMode.None;
    private List<int> playbackOrder;
    private bool shuffleMode = false;
    private int currentPlaybackIndex;
    private Playlist? currentPlaylist;

    private MainWindow(Builder builder, string name) : base(new Adw.Internal.ApplicationWindowHandle(builder.GetPointer(name), false))
    {
        builder.Connect(this);

        // Do any initialization, or connect signals here.
        //  Инициализируем менеджер плейлистов
        playlistManager = new PlaylistManager();

        // Инициализируем контроллер плеера
        playerController = new PlayerController(settingsManager: null);
        SetupPlayer();

        TracksModel = Gio.ListStore.New(TrackRowData.GetGType());
        SelectionModel = SingleSelection.New(TracksModel);

        // Состояния воспроизведения
        shuffleMode = false;
        repeatMode = RepeatMode.None;
        // Порядок воспроизведения при shuffle
        playbackOrder = [];
        // Индекс текущего трека в порядке воспроизведения
        currentPlaybackIndex = -1;

        SetupTrackViews();
        SetupControls();
        SetupUiUpdates();
        SetupActions();

        SidebarList.OnRowActivated += OnPlaylistRowActivated;

        musicLibrary = new MusicLibrary();
        ScanMusicLibrary();
    }

    private void SetupPlayer()
    {
        // Подключаем сигналы от плеера
        playerController.OnStateChanged += OnPlayerStateChanged;
        playerController.OnPositionChanged += OnPositionChanged;
        playerController.OnEosReached += OnPlayerEosReached;
        playerController.OnTrackChanged += OnPlayerTrackChanged;
    }

    private void OnPlayerTrackChanged(Track track)
    {
        UpdateCurrentTrackInfo(track);
    }

    private void OnPlayerEosReached()
    {
        _logger.LogInformation("End of stream reached");
        PlayNextTrack();
    }

    private void PlayNextTrack()
    {
        var model = (SelectionModel as SingleSelection)?.GetModel();
        var totalTracks = model?.GetNItems() ?? 0;
        if (totalTracks == 0) return;

        // Определяем следующий трек в зависимости от режима воспроизведения
        if (repeatMode == RepeatMode.One)
        {
            var currentTrack = playerController.CurrentTrack;
            if (currentTrack != null)
            {
                playerController.PlayTrack(currentTrack);
            }
        }

        var nextIndex = -1;

        // Shuffle режим
        if (shuffleMode && playbackOrder.Count > 0)
        {
            var currentOrderIndex = currentPlaybackIndex;
            if (currentOrderIndex >= 0 && currentOrderIndex < playbackOrder.Count - 1)
            {
                var nextOrderIndex = playbackOrder[currentOrderIndex + 1];
                currentPlaybackIndex = nextOrderIndex;
            }
            else if (repeatMode == RepeatMode.All)
            {
                // Повтор всей очереди
                // Создаем новую очередь
                UpdateShuffleOrder();
                nextIndex = playbackOrder[0];
                currentPlaybackIndex = 0;
            }
        }
        // Нормальный режим
        else
        {
            var currentIndex = GetCurrentTrackIndex();
            if (currentIndex >= 0 && currentIndex < totalTracks - 1)
            {
                nextIndex = currentIndex + 1;
                currentPlaybackIndex = nextIndex;
            }
            else if (repeatMode == RepeatMode.All)
            {
                // Повтор всей очереди
                nextIndex = 0;
                currentPlaybackIndex = 0;
            }
        }

        if (nextIndex >= 0 && nextIndex < totalTracks)
        {
            PlayTrackAtIndex((uint)nextIndex);
        }

        // Останавливаем воспроизведение
        if (nextIndex == -1)
        {
            playerController.Stop();
            currentPlaybackIndex = -1;
        }
    }

    /// <summary>
    /// Начинает воспроизведение трека по указанной позиции.
    /// </summary>
    /// <param name="nextIndex">Позиция трека в модели.</param>
    private void PlayTrackAtIndex(uint nextIndex)
    {
        var model = (SelectionModel as SingleSelection)?.GetModel();
        if (nextIndex >= 0 && nextIndex < model?.GetNItems())
        {
            var trackData = model.GetObject(nextIndex) as TrackRowData;
            if (trackData != null)
            {
                currentPlaybackIndex = (int)nextIndex;
                trackData.IsCurrent = true;
                // Обновляем выделение в списке
                if (currentPlaylist != null)
                {
                    (SelectionModel as SingleSelection)?.SelectItem(nextIndex, true);
                }

                PlayTrack(trackData.Track);
                return;
            }
        }
    }

    /// <summary>
    /// Начинает воспроизведение трека.
    /// </summary>
    /// <param name="track">Трек для воспроизведения.</param>
    private void PlayTrack(Track track)
    {
        playerController.Stop();
        playerController.PlayTrack(track);
        UpdateCurrentTrackInfo(track);
    }

    /// <summary>
    /// Обновляет информацию о текущем треке в интерфейсе.
    /// </summary>
    /// <param name="track">Трек для обновления информации.</param>
    /// <exception cref="NotImplementedException"></exception>
    private void UpdateCurrentTrackInfo(Track track)
    {
        currentTrackTitle.Label_ = track.Title;
        currentTrackArtist.Label_ = track.Artist;
        totalTimeLabel.Label_ = $"{track.Duration / 60:00}:{track.Duration % 60:00}";

        // Обновляем обложку текущего трека
        if (track.CoverPath != null && System.IO.File.Exists(track.CoverPath))
        {
            try
            {
                var pixbuf = Pixbuf.NewFromFileAtSize(track.CoverPath, 48, 48);
                currentTrackArtwork.SetPixbuf(pixbuf);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set cover for track {track}", track.Title);
                currentTrackArtwork.SetPixbuf(null);
            }
        }
        else
        {
            currentTrackArtwork.SetPixbuf(null);
        }
    }

    private void SetupActions()
    {
        // Добавляем действие для переключения громкости
        var muteAction = SimpleAction.New("mute-toggle", null);
        muteAction.OnActivate += OnMuteToggle;
        AddAction(muteAction);

        var repeatAction = SimpleAction.New("repeat-toggle", null);
        repeatAction.OnActivate += OnRepeatToggle;
        AddAction(repeatAction);

        var shuffleAction = SimpleAction.New("shuffle-toggle", null);
        shuffleAction.OnActivate += OnShuffleToggle;
        AddAction(shuffleAction);
    }

    private void OnShuffleToggle(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        shuffleMode = !shuffleMode;
        _logger.LogInformation("Shuffle mode changed to {mode}", shuffleMode);

        shuffleButton.SetActive(shuffleMode);
        if (shuffleMode)
        {
            UpdateShuffleOrder();
        }
        else
        {
            currentPlaybackIndex = GetCurrentTrackIndex();
        }
    }

    private int GetCurrentTrackIndex()
    {
        if (currentPlaylist != null)
        {
            // Если отображается плейлист, возвращаем индекс из модели плейлиста
            // TODO: Реализовать получение выделенного элемента из модели плейлиста
            return currentPlaybackIndex;
        }
        else
        {
            // Если отображается все треки, возвращаем индекс из основной модели
            var selected = (SelectionModel as SingleSelection)?.GetSelected();
            if (selected != Gtk.Constants.INVALID_LIST_POSITION)
            {
                return (int)selected!;
            }
        }
        return -1;
    }

    /// <summary>
    /// Обновляет порядок воспроизведения при shuffle режиме.
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private void UpdateShuffleOrder()
    {
        var totalTracks = TracksModel.GetNItems();
        if (totalTracks > 0)
        {
            playbackOrder = [.. Enumerable.Range(0, (int)totalTracks).OrderBy(x => Guid.NewGuid())];
            playbackOrder.Shuffle();
            // Найдем текущий трек в новом порядке
            var currentIndex = GetCurrentTrackIndex();
            if (currentIndex >= 0)
            {
                // Переместим текущий трек в начало
                playbackOrder.Remove(currentIndex);
                playbackOrder.Insert(0, currentIndex);
            }
            currentPlaybackIndex = 0;
            _logger.LogDebug("New shuffle order: {order}", string.Join(", ", playbackOrder));
        }
    }

    private void OnRepeatToggle(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var newMode = repeatMode switch
        {
            RepeatMode.None => RepeatMode.All,
            RepeatMode.All => RepeatMode.One,
            RepeatMode.One => RepeatMode.None,
            _ => RepeatMode.None
        };
        repeatMode = newMode;

        _logger.LogInformation("Repeat mode changed to {mode}", newMode);

        UpdateRepeatUi(newMode);
        repeatButton.SetActive(newMode != RepeatMode.None);
    }

    /// <summary>
    /// Переключение режима беззвучного воспроизведения.
    /// <br/>
    /// Если громкость больше 0, запоминаем уровень и ставим громкость в 0.
    /// Если громкость 0, восстанавливаем запомненный уровень.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
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
            0 => "speaker-x-symbolic",
            > 0 and < 0.5 => "speaker-low-symbolic",
            >= 0.7 => "speaker-high-symbolic",
            _ => "speaker-high-symbolic"
        };

        GLib.Functions.IdleAdd(GLib.Constants.PRIORITY_DEFAULT, () =>
            {
                _logger.LogDebug("Update volume icon to {icon}", volumeIcon);
                volumeButton.SetIconName(volumeIcon);
                return false;
            });
    }

    /// <summary>
    /// Обновляет иконку режима повтора в UI.
    /// </summary>
    /// <param name="mode"></param>
    private void UpdateRepeatUi(RepeatMode mode)
    {
        var repeatIcon = mode switch
        {
            RepeatMode.One => "repeat-once-symbolic",
            _ => "repeat-symbolic"
        };

        GLib.Functions.IdleAdd(GLib.Constants.PRIORITY_DEFAULT, () =>
        {
            _logger.LogDebug("Update repeat icon to {icon}", repeatIcon);
            repeatButton.SetIconName(repeatIcon);
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
