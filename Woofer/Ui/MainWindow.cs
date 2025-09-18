using System.Threading.Tasks;
using GLib;
using Gtk;
using Pango;
using Woofer.Models;
using Woofer.Services;

namespace Woofer.UI;

public class MainWindow : Adw.ApplicationWindow
{
    [Connect(widgetName: "sidebar_list")] public readonly ListBox SidebarList = null!;
    [Connect(widgetName: "track_list_container")] public readonly ScrolledWindow trackListContainer = null!;
    [Connect(widgetName: "track_grid_container")] public readonly Gtk.ScrolledWindow trackGridContainer = null!;
    [Connect(widgetName: "view_toggle")] public readonly Adw.ToggleGroup viewToggle = null!;
    [Connect(widgetName: "view_stack")] public readonly Stack viewStack = null!;

    private readonly MusicLibrary musicLibrary;
    private readonly Gio.ListStore TracksModel;
    private readonly SelectionModel SelectionModel;
    private readonly PlaylistManager playlistManager;
    private TracksListView tracksListView;
    private TracksGridView tracksGridView;

    private MainWindow(Builder builder, string name) : base(new Adw.Internal.ApplicationWindowHandle(builder.GetPointer(name), false))
    {
        builder.Connect(this);

        // Do any initialization, or connect signals here.
        //  Инициализируем менеджер плейлистов
        playlistManager = new PlaylistManager();

        TracksModel = Gio.ListStore.New(TrackRowData.GetGType());
        SelectionModel = SingleSelection.New(TracksModel);

        SetupTrackViews();
        SetupControls();

        SidebarList.OnRowActivated += OnPlaylistRowActivated;

        musicLibrary = new MusicLibrary();
        ScanMusicLibrary();
    }

    private void SetupControls()
    {
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
}
