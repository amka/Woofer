using GObject;

namespace Woofer.Models;

[Subclass<GObject.Object>]
public partial class TrackRowData
{
    public Track track { get; set; }

    public string Title => track.Title;
    public string Artist => track.Artist;
    public string Album => track.Album;
    public string Duration => track.DurationFormatted;

    public TrackRowData(Track track) : this()
    {
        this.track = track;
    }

    /// <summary>
    /// Возвращает Pixbuf обложки заданного размера.
    /// </summary>
    /// <param name="size">Размер в пикселях</param>
    /// <returns></returns>
    public GdkPixbuf.Pixbuf? GetCoverPixbuf(int size = 48)
    {
        if (track.CoverPath != null && File.Exists(track.CoverPath))
        {
            return GdkPixbuf.Pixbuf.NewFromFileAtSize(track.CoverPath, size, size);
        }

        return null;
    }
}