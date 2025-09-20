using GObject;

namespace Woofer.Models;

[Subclass<GObject.Object>]
public partial class TrackRowData
{
    public Track Track { get; set; }

    public string Title => Track.Title;
    public string Artist => Track.Artist;
    public string Album => Track.Album;
    public string Duration => Track.DurationFormatted;
    public bool IsCurrent { get; set; } = false;

    public TrackRowData(Track track) : this()
    {
        this.Track = track;
    }

    /// <summary>
    /// Возвращает Pixbuf обложки заданного размера.
    /// </summary>
    /// <param name="size">Размер в пикселях</param>
    /// <returns></returns>
    public GdkPixbuf.Pixbuf? GetCoverPixbuf(int size = 48)
    {
        if (Track.CoverPath != null && File.Exists(Track.CoverPath))
        {
            return GdkPixbuf.Pixbuf.NewFromFileAtSize(Track.CoverPath, size, size);
        }

        return null;
    }
}