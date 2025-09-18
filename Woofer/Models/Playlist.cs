using System.Text;
using GObject;
using Woofer.Services;

namespace Woofer.Models;

[Subclass<GObject.Object>]
public partial class Playlist
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? Cover { get; set; }
    public string? FilePath { get; set; }
    public List<Track> Tracks { get; set; } = [];

    public Playlist(string name, string? filePath = null) : this()
    {
        FilePath = filePath;
        Name = name;
    }

    /// <summary>
    /// Добавляет трек в плейлист.
    /// </summary>
    /// <param name="track"></param>
    public void AddTrack(Track track)
    {
        Tracks.Add(track);
    }

    /// <summary>
    /// Удаляет трек из плейлиста.
    /// </summary>
    /// <param name="track"></param>
    public void RemoveTrack(Track track)
    {
        Tracks.Remove(track);
    }

    /// <summary>
    /// даляет трек по индексу.
    /// </summary>
    /// <param name="index"></param>
    public void RemoveTrackAt(int index)
    {
        Tracks.RemoveAt(index);
    }

    /// <summary>
    /// Очищает плейлист.
    /// </summary>
    public void Clear()
    {
        Tracks.Clear();
    }

    /// <summary>
    /// Сохраняет плейлист в формате M3U
    /// </summary>
    /// <param name="filePath"></param>
    public void SaveToM3u(string filePath)
    {
        try
        {
            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.WriteLine($"#EXTM3U");
                foreach (var track in Tracks)
                {
                    writer.WriteLine($"#EXTINF:{track.Duration},{track.Artist} - {track.Title}");
                    writer.WriteLine($"{track.FilePath}");
                }
            }
            FilePath = filePath;

            Console.WriteLine($"Playlist saved to {filePath}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error saving playlist to {filePath}: {e}");
            throw;
        }
    }

    /// <summary>
    /// Загружает плейлист из файла M3U.
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static Playlist LoadFromM3u(string filePath, string? name = null)
    {
        name ??= Path.GetFileNameWithoutExtension(filePath);
        var playlist = new Playlist(name, filePath);
        try
        {

            using (var reader = new StreamReader(filePath, Encoding.UTF8))
            {
                string? _line;
                string? line;
                while ((_line = reader.ReadLine()) != null)
                {
                    line = _line.Trim();
                    if (line.StartsWith("#EXTINF:"))
                    {
                        // Парсим метаданные
                        // Следующая строка - путь к файлу
                        var filePathStr = reader.ReadLine();
                        if (filePathStr == null) continue;

                        // Загружаем метаданные трека
                        var track = TagReader.ReadTrackInfo(filePathStr);
                        if (track == null) continue;

                        playlist.AddTrack(track);
                    }
                    else
                    {
                        // Просто путь к файлу (без метаданных)
                        var track = TagReader.ReadTrackInfo(line);
                        if (track == null) continue;

                        playlist.AddTrack(track);
                    }
                    line = null;
                }
            }

            Console.WriteLine($"Playlist loaded from {filePath}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error loading playlist from {filePath}: {e}");
            throw;
        }
        return playlist;
    }
}