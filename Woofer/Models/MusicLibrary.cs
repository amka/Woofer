namespace Woofer.Models;

public class MusicLibrary
{
    public List<Track> Tracks { get; set; } = [];
    public Dictionary<string, List<Track>> Artists { get; set; } = [];
    public Dictionary<string, List<Track>> Albums { get; set; } = [];

    /// <summary>
    /// Добавляет трек в библиотеку и обновляет индексы.
    /// </summary>
    /// <param name="track">Аудио трек</param>
    public void AddTrack(Track track)
    {
        Tracks.Add(track);

        // Добавляем трек в исполнителей
        if (!Artists.TryGetValue(track.Artist, out List<Track>? value))
        {
            value = [];
            Artists.Add(track.Artist, value);
        }
        value.Add(track);

        // Добавляем трек в альбомы
        if (!Albums.TryGetValue(track.Album, out _))
        {
            value = [];
            Albums.Add(track.Album, value);
        }
        value.Add(track);
    }

    /// <summary>
    /// Удаляет трек из библиотеки и обновляет индексы.
    /// </summary>
    /// <param name="track">Аудио трек</param>
    public void RemoveTrack(Track track)
    {
        Tracks.Remove(track);
        Artists[track.Artist].Remove(track);
        Albums[track.Album].Remove(track);
    }

    /// <summary>
    /// Возвращает список треков по исполнителю.
    /// </summary>
    /// <param name="artist"></param>
    /// <returns></returns>
    public List<Track> GetTracksByArtist(string artist)
    {
        return Artists[artist];
    }

    /// <summary>
    /// Возвращает список треков по альбому.
    /// </summary>
    /// <param name="album"></param>
    /// <returns></returns>
    public List<Track> GetTracksByAlbum(string album)
    {
        return Albums[album];
    }

    /// <summary>
    /// Возвращает список всех треков.
    /// </summary>
    /// <returns></returns>
    public List<Track> GetAllTracks()
    {
        return Tracks;
    }

    /// <summary>
    /// Очищает библиотеку.
    /// </summary>
    public void Clear()
    {
        Tracks.Clear();
        Artists.Clear();
        Albums.Clear();
    }
}