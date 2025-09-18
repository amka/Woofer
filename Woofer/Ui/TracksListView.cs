using GObject;
using Gtk;
using Woofer.Models;

namespace Woofer.UI;

public partial class TracksListView : ColumnView
{
    public event Action<Track>? OnTrackActivated;
    public event Action<Track>? OnTrackSelected;

    private TracksListView(Builder builder, string name) : base(handle: new Gtk.Internal.ColumnViewHandle(builder.GetPointer(name), false))
    {
        builder.Connect(this);

        var titleFactory = new SignalListItemFactory();
        titleFactory.OnSetup += OnSetupTitleColumn;
        titleFactory.OnBind += OnBindTitleColumn;

        var artistFactory = new SignalListItemFactory();
        artistFactory.OnSetup += OnSetupArtistColumn;
        artistFactory.OnBind += OnBindArtistColumn;

        var albumFactory = new SignalListItemFactory();
        albumFactory.OnSetup += OnSetupAlbumColumn;
        albumFactory.OnBind += OnBindAlbumColumn;

        var titleColumn = ColumnViewColumn.New("Title", titleFactory);
        titleColumn.Expand = true;

        var artistColumn = ColumnViewColumn.New("Artist", artistFactory);
        var albumColumn = ColumnViewColumn.New("Album", albumFactory);

        AppendColumn(titleColumn);
        AppendColumn(artistColumn);
        AppendColumn(albumColumn);

        OnActivate += OnActivateTrack;
    }

    /// <summary>
    /// Обработка двойного клика по треку.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void OnActivateTrack(ColumnView sender, ActivateSignalArgs args)
    {
        SingleSelection? model = sender.GetModel() as SingleSelection;
        if (model?.GetModel()?.GetObject(args.Position) is TrackRowData track)
        {
            OnTrackActivated?.Invoke(track.track);
        }
    }

    public TracksListView() : this(new Builder("TracksListView.ui"), "content")
    {

    }


    private void OnSetupAlbumColumn(SignalListItemFactory sender, SignalListItemFactory.SetupSignalArgs args)
    {
        if (args.Object is ListItem listItem)
        {
            var label = new Label()
            {
                Xalign = 0,
                Ellipsize = Pango.EllipsizeMode.End,
            };
            listItem.Child = label;
        }
    }

    private void OnBindAlbumColumn(SignalListItemFactory sender, SignalListItemFactory.BindSignalArgs args)
    {
        if (args.Object is ListItem listItem && listItem.Child is Label label)
        {
            label.Label_ = (listItem.Item as TrackRowData)?.Album;
        }
    }

    private void OnBindArtistColumn(SignalListItemFactory sender, SignalListItemFactory.BindSignalArgs args)
    {
        if (args.Object is ListItem listItem && listItem.Child is Label label)
        {
            label.Label_ = (listItem.Item as TrackRowData)?.Artist;
        }
    }

    private void OnSetupArtistColumn(SignalListItemFactory sender, SignalListItemFactory.SetupSignalArgs args)
    {
        if (args.Object is ListItem listItem)
        {
            var label = new Label()
            {
                Xalign = 0,
                Ellipsize = Pango.EllipsizeMode.End,
            };
            listItem.Child = label;
        }
    }

    private void OnBindTitleColumn(SignalListItemFactory sender, SignalListItemFactory.BindSignalArgs args)
    {
        if (args.Object is ListItem listItem && listItem.Child is Label label)
        {
            label.Label_ = (listItem.Item as TrackRowData)?.Title;
        }
    }

    private void OnSetupTitleColumn(SignalListItemFactory sender, SignalListItemFactory.SetupSignalArgs args)
    {
        if (args.Object is ListItem listItem)
        {
            var label = new Label()
            {
                Xalign = 0,
                Ellipsize = Pango.EllipsizeMode.End,
            };
            listItem.Child = label;
        }
    }
}