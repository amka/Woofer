using GObject;

namespace Woofer.Models;

[Subclass<GObject.Object>]
public partial class Track
{
    public string FilePath { get; set; }
    public string Title { get; set; }
    public string Artist { get; set; }
    public string Album { get; set; }
    public int Duration { get; set; }
    public uint? Year { get; set; }
    public string? Genre { get; set; }
    public uint? TrackNumber { get; set; }
    public uint? DiscNumber { get; set; }
    public string? CoverPath { get; set; }

    public Track(string filePath, string title, string artist, string album, int duration, uint? year = null, string? genre = null, uint? trackNumber = null, uint? discNumber = null, string? coverPath = null) : this()
    {
        FilePath = filePath;
        Title = title;
        Artist = artist;
        Album = album;
        Duration = duration;
        Year = year;
        Genre = genre;
        TrackNumber = trackNumber;
        DiscNumber = discNumber;
        CoverPath = coverPath;
    }

    /// <summary>
    /// Возвращает продолжительность в формате M:SS.
    /// </summary>
    public string DurationFormatted => $"{Duration / 60}:{Duration % 60}";

    public override string ToString()
    {
        return $"{Artist} - {Title}";
    }
}