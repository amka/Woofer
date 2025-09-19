using GObject;
using Gst.Internal;
using Gtk;
using Woofer.Models;

namespace Woofer.UI;

public partial class TracksGridView : GridView
{
    public event Action<Track>? OnTrackActivated;

    private TracksGridView(Builder builder, string name) : base(handle: new Gtk.Internal.GridViewHandle(builder.GetPointer(name), false))
    {
        builder.Connect(this);

        var itemFactory = new SignalListItemFactory();
        itemFactory.OnSetup += OnSetupGridItem;
        itemFactory.OnBind += OnBindGridItem;

        SetFactory(itemFactory);

        OnActivate += OnActivateTrack;
    }

    /// <summary>
    /// Обработка двойного клика по треку.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void OnActivateTrack(GridView sender, ActivateSignalArgs args)
    {
        SingleSelection? model = sender.GetModel() as SingleSelection;
        if (model?.GetModel()?.GetObject(args.Position) is TrackRowData track)
        {
            OnTrackActivated?.Invoke(track.track);
        }
    }

    private void OnSetupGridItem(SignalListItemFactory sender, SignalListItemFactory.SetupSignalArgs args)
    {
        // Создаем карточку для трека
        if (args.Object is ListItem listItem)
        {
            listItem.Child = new TrackGridItem(track: null);
        }
    }

    private void OnBindGridItem(SignalListItemFactory sender, SignalListItemFactory.BindSignalArgs args)
    {
        if (args.Object is ListItem listItem)
        {
            var card = listItem.Child as TrackGridItem;
            card!.SetTrackData(listItem.Item as TrackRowData);
        }
    }

    public TracksGridView() : this(new Builder("TracksGridView.ui"), "content")
    {
    }


}

[Subclass<Box>]
public partial class TrackGridItem
{
    private Picture cover;
    private Label title;
    private Label artist;

    public TrackGridItem(Track? track) : this()
    {
        // Основной контейнер
        var mainBox = new Box()
        {
            MarginBottom = 4,
            MarginEnd = 4,
            MarginStart = 4,
            MarginTop = 4,
            Spacing = 6,
            Hexpand = true
        };
        mainBox.SetOrientation(Orientation.Vertical);

        // Обложка

        cover = new Picture()
        {
            WidthRequest = 150,
            HeightRequest = 150,
            ContentFit = ContentFit.Cover,
            CssClasses = ["rounded"]
        };

        // Название трека
        title = new Label()
        {
            Lines = 2,
            Xalign = 0,
            MarginTop = 8,
            MarginBottom = 4,
            Ellipsize = Pango.EllipsizeMode.End,
            CssClasses = ["heading"]
        };

        // Исполнитель
        artist = new Label()
        {
            Xalign = 0,
            Ellipsize = Pango.EllipsizeMode.End,
            CssClasses = ["caption", "dim-label"]
        };

        // Добавляем все в карточку
        mainBox.Append(cover);
        mainBox.Append(title);
        mainBox.Append(artist);

        Append(mainBox);
    }

    public void SetTrackData(TrackRowData? trackData)
    {
        if (trackData == null)
        {
            return;
        }

        // cover.SetPixbuf(track.);
        title.Label_ = trackData.Title;
        artist.Label_ = trackData.Artist;

        var pixbuf = trackData.GetCoverPixbuf() switch
        {
            // null => GdkPixbuf.Pixbuf.New(GdkPixbuf.Colorspace.Rgb, false, 8, 150, 150),
            null => GdkPixbuf.Pixbuf.NewFromResource("/com/tenderowl/woofer/icons/scalable/actions/music-note-symbolic.svg"),
            _ => trackData.GetCoverPixbuf(size: 150)
        };

        cover.SetPixbuf(pixbuf);
    }
}