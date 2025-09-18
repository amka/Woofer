using Microsoft.Extensions.Logging;
using Woofer.Models;

namespace Woofer.Services;

public class MusicScanner
{
    private readonly ILogger _logger;
    private readonly CoverExtractor _coverExtractor;
    private readonly Dictionary<string, string> _albumCovers = new Dictionary<string, string>();
    private readonly HashSet<string> _scannedDirectories = new HashSet<string>();
    public event EventHandler<Track>? OnTrackFound;

    public MusicScanner(ILogger? logger = null, CoverExtractor? coverExtractor = null)
    {
        _logger = logger ?? LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<MusicScanner>();
        _coverExtractor = new CoverExtractor();
    }

    public async Task<List<Track>> ScanDirectoryAsync(string directoryPath)
    {
        var tracks = await Task.Run(() => ScanDirectory(directoryPath));
        return tracks;
    }

    public List<Track> ScanDirectory(string directoryPath)
    {
        _logger.LogInformation("Scanning directory: {DirectoryPath}", directoryPath);
        var tracks = new List<Track>();

        if (!Directory.Exists(directoryPath))
        {
            _logger.LogWarning("Directory does not exist: {DirectoryPath}", directoryPath);
            return tracks;
        }

        try
        {
            // First scan external covers for optimization
            ScanExternalCovers(directoryPath);

            // Then scan tracks recursively
            var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var extension = Path.GetExtension(file).ToLower();
                if (SUPPORTED_FORMATS.Contains(extension))
                {
                    var track = ReadTrackWithCover(file);
                    if (track != null)
                    {
                        tracks.Add(track);
                        OnTrackFound?.Invoke(this, track);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning directory: {DirectoryPath}", directoryPath);
        }

        _logger.LogInformation("Found {Count} tracks in {DirectoryPath}", tracks.Count, directoryPath);
        return tracks;
    }

    private void ScanExternalCovers(string directoryPath)
    {
        _logger.LogInformation("Scanning external covers in {DirectoryPath}", directoryPath);
        var directories = Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories);

        foreach (var dir in directories)
        {
            var dirPath = Path.GetFullPath(dir);
            var albumKey = dirPath; // Use full path as key

            if (_scannedDirectories.Contains(albumKey))
            {
                continue;
            }

            _scannedDirectories.Add(albumKey);

            foreach (var coverFilename in COVER_FILENAMES)
            {
                var coverPath = Path.Combine(dirPath, coverFilename);
                if (File.Exists(coverPath))
                {
                    _albumCovers[albumKey] = coverPath;
                    break;
                }
            }
        }
    }

    private Track? ReadTrackWithCover(string filePath)
    {
        var track = TagReader.ReadTrackInfo(filePath);
        if (track == null)
        {
            return null;
        }

        var albumKey = $"{track.Artist} - {track.Album}";

        string? path = Path.GetDirectoryName(filePath);
        if (path == null)
        {
            return null;
        }
        var dirKey = Path.GetFullPath(path);

        // Check album cache first
        if (_albumCovers.TryGetValue(albumKey, out var coverPath))
        {
            track.CoverPath = coverPath;
        }
        // Then check directory cache
        else if (_albumCovers.TryGetValue(dirKey, out coverPath))
        {
            track.CoverPath = coverPath;
        }
        else
        {
            // Extract cover if not found in cache
            coverPath = _coverExtractor.GetCoverForTrack(filePath);
            if (!string.IsNullOrEmpty(coverPath))
            {
                track.CoverPath = coverPath;
                // Cache for other tracks in same album/directory
                _albumCovers[albumKey] = coverPath;
                _albumCovers[dirKey] = coverPath;
            }
        }

        return track;
    }
}
