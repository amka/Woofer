using System.Text;
using GObject;
using Microsoft.Extensions.Logging;
using Woofer.Models;

namespace Woofer.Services;

[Subclass<GObject.Object>]
public partial class PlaylistManager
{
    private List<Playlist> _playlists = [];
    private string _playlistsDir;

    public event Action<Playlist>? OnPlaylistAdded;
    public event Action<Playlist>? OnPlaylistRemoved;
    public event Action<Playlist>? OnPlaylistChanged;

    public PlaylistManager(string? playlistsDir) : this()
    {
        _playlistsDir = playlistsDir ?? Path.Combine(
            GLib.Functions.GetUserDataDir(),
            "playlists"
        );
        Directory.CreateDirectory(_playlistsDir);
        LoadPlaylists();
    }

    /// <summary>
    /// Loads all playlist files with the .m3u extension from the specified playlists directory.
    /// Each playlist is loaded and added to the internal playlists collection.
    /// Errors encountered during the loading of individual playlists or the directory are caught and logged to the console.
    /// </summary>
    private void LoadPlaylists()
    {
        try
        {
            var directory = new DirectoryInfo(_playlistsDir);
            foreach (var file in directory.GetFiles("*.m3u"))
            {
                try
                {
                    var playlist = Playlist.LoadFromM3u(file.FullName);
                    _playlists.Add(playlist);
                }
                catch (Exception e)
                {
                    // In production, we'd log here using your logger
                    Console.WriteLine($"Error loading playlist {file.Name}: {e.Message}");
                }
            }
        }
        catch (Exception e)
        {
            // In production, we'd log here using your logger
            Console.WriteLine($"Error loading playlists: {e.Message}");
        }
    }

    public void SavePlaylists()
    {
        foreach (var playlist in _playlists)
        {
            if (string.IsNullOrEmpty(playlist.FilePath))
            {
                // Generate safe name
                var safeName = new string([.. playlist.Name
                        .Where(c => char.IsLetterOrDigit(c)
                            || c == ' ' || c == '-' || c == '_')]
                );
                safeName = safeName.Replace(" ", "_");
                if (string.IsNullOrEmpty(safeName))
                    safeName = "playlist";
                playlist.FilePath = Path.Combine(_playlistsDir, $"{safeName}.m3u");
            }
            playlist.SaveToM3u(playlist.FilePath);
        }
    }

    public Playlist CreatePlaylist(string name)
    {
        if (GetPlaylistByName(name) != null)
            throw new ArgumentException($"Playlist '{name}' already exists");

        var playlist = new Playlist(name);
        _playlists.Add(playlist);
        OnPlaylistAdded?.Invoke(playlist);
        return playlist;
    }

    public void DeletePlaylist(Playlist playlist)
    {
        if (_playlists.Remove(playlist))
        {
            OnPlaylistRemoved?.Invoke(playlist);
            if (!string.IsNullOrEmpty(playlist.FilePath) && File.Exists(playlist.FilePath))
            {
                try
                {
                    File.Delete(playlist.FilePath);
                }
                catch (Exception e)
                {
                    // In production, we'd log here using your logger
                    Console.WriteLine($"Error deleting playlist file {playlist.FilePath}: {e.Message}");
                }
            }
        }
    }

    public Playlist? GetPlaylistByName(string name)
    {
        return _playlists.FirstOrDefault(p => p.Name == name);
    }

    public bool AddTrackToPlaylist(Playlist playlist, Track track)
    {
        if (_playlists.Contains(playlist))
        {
            playlist.AddTrack(track);
            OnPlaylistChanged?.Invoke(playlist);
            return true;
        }
        return false;
    }

    public bool RemoveTrackFromPlaylist(Playlist playlist, Track track)
    {
        if (_playlists.Contains(playlist))
        {
            playlist.RemoveTrack(track);
            OnPlaylistChanged?.Invoke(playlist);
            return true;
        }
        return false;
    }

    public List<Playlist> GetAllPlaylists()
    {
        return _playlists.ToList();
    }

    public bool PlaylistExists(string name)
    {
        return GetPlaylistByName(name) != null;
    }
}