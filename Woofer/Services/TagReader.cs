using Woofer.Models;

namespace Woofer.Services;

public class TagReader
{
    /// <summary>
    /// Читает метаданные из аудиофайла и возвращает объект Track.
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static Track? ReadTrackInfo(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        if (!SUPPORTED_FORMATS.Contains(fileInfo.Extension))
        {
            Console.WriteLine($"Unsupported file format: {fileInfo.Extension}");
            return null;
        }

        if (!fileInfo.Exists)
        {
            Console.WriteLine($"Could not read file: {filePath}");
            return null;
        }

        try
        {
            var audioFile = TagLib.File.Create(filePath);
            var track = new Track
            {
                FilePath = filePath,
                Title = audioFile.Tag.Title,
                Artist = audioFile.Tag.FirstAlbumArtist,
                Album = audioFile.Tag.Album,
                Year = audioFile.Tag.Year,
                TrackNumber = audioFile.Tag.Track,
                Genre = audioFile.Tag.FirstGenre,
                DiscNumber = audioFile.Tag.Disc,
                Duration = (long)audioFile.Properties.Duration.TotalSeconds,
            };
            Console.WriteLine($"Read track info: {track.Duration}");
            return track;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error reading tags from {filePath}: {e}");
            return null;
        }
    }
}