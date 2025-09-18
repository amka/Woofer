// using System;
// using System.Collections.Concurrent;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using System.Threading.Tasks;
// using NAudio.Wave;
// using NAudio.Utils;
// using Microsoft.Extensions.Logging;
// using Woofer.Models;
// using GObject;

// namespace Woofer.Services;

// /// <summary>
// /// Manages music scanning and playback operations with efficient cover management and state tracking.
// /// </summary>
// [Subclass<GObject.Object>]
// public partial class PlayerController : IDisposable
// {
//     private readonly ILogger _logger;
//     private readonly ITrackInfoProvider _trackInfoProvider;
//     private readonly ICoverManager _coverManager;
//     private readonly ConcurrentDictionary<string, Path> _albumCoversCache = new ConcurrentDictionary<string, Path>();
//     private readonly HashSet<string> _scannedDirectories = new HashSet<string>();
//     private readonly object _lock = new object();
//     private readonly List<Track> _foundTracks = new List<Track>();
//     private readonly Func<Track, Task>? _onTrackFound;

//     public MusicScanner(
//         ILogger logger,
//         ITrackInfoProvider trackInfoProvider,
//         ICoverManager coverManager,
//         Func<Track, Task>? onTrackFound = null)
//     {
//         _logger = logger;
//         _trackInfoProvider = trackInfoProvider;
//         _coverManager = coverManager;
//         _onTrackFound = onTrackFound;
//     }

//     /// <summary>
//     /// Recursively scans the specified directory for music files and processes tracks.
//     /// </summary>
//     /// <param name="directory">The directory to scan (must be a valid directory path)</param>
//     /// <returns>List of found tracks</returns>
//     public async Task<List<Track>> ScanDirectoryAsync(string directory)
//     {
//         if (!Directory.Exists(directory))
//         {
//             _logger.LogWarning("Directory does not exist: {Directory}", directory);
//             return new List<Track>();
//         }

//         _logger.LogInformation("Starting scan of directory: {Directory}", directory);
//         var tracks = new List<Track>();
        
//         try
//         {
//             // First scan for album covers
//             await _coverManager.ScanExternalCoversAsync(directory);
            
//             // Then scan for tracks
//             var directoryPath = Path.GetFullPath(directory);
//             foreach (var file in Directory.GetFiles(directory, "*.*", SearchOption.TopDirectoryOnly))
//             {
//                 var fileExtension = Path.GetExtension(file).ToLower();
//                 if (IsSupportedFormat(fileExtension))
//                 {
//                     var track = await ProcessTrackAsync(file);
//                     if (track != null)
//                     {
//                         tracks.Add(track);
//                         await HandleTrackFoundAsync(track);
//                     }
//                 }
//             }
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error scanning directory: {Directory}", directory);
//         }

//         _logger.LogInformation("Found {Count} tracks in directory: {Directory}", tracks.Count, directory);
//         return tracks;
//     }

//     private bool IsSupportedFormat(string extension) => 
//         _trackInfoProvider.GetSupportedExtensions().Contains(extension);

//     private async Task<Track?> ProcessTrackAsync(string filePath)
//     {
//         var track = await _trackInfoProvider.GetTrackInfoAsync(filePath);
//         if (track == null) return null;

//         var albumKey = $"{track.Artist} - {track.Album}";
//         var dirKey = Path.GetFullPath(filePath).Replace('\\', '/');

//         // Check cache for album covers
//         if (_albumCoversCache.TryGetValue(albumKey, out var albumCover))
//         {
//             track.CoverPath = albumCover;
//         }
//         // Check cache for directory covers
//         else if (_albumCoversCache.TryGetValue(dirKey, out var dirCover))
//         {
//             track.CoverPath = dirCover;
//         }
//         else
//         {
//             var coverPath = await _coverManager.GetCoverForTrackAsync(filePath);
//             if (coverPath != null)
//             {
//                 track.CoverPath = coverPath;
//                 _albumCoversCache[albumKey] = coverPath;
//                 _albumCoversCache[dirKey] = coverPath;
//             }
//         }

//         return track;
//     }

//     private async Task HandleTrackFoundAsync(Track track)
//     {
//         if (_onTrackFound != null)
//         {
//             await _onTrackFound(track);
//         }
//     }

//     /// <summary>
//     /// Scans for external album covers in the directory structure
//     /// </summary>
//     private async Task ScanExternalCoversAsync(string directory)
//     {
//         var directoryPath = Path.GetFullPath(directory);
        
//         foreach (var dir in Directory.GetDirectories(directoryPath, "*", SearchOption.TopDirectoryOnly))
//         {
//             var dirKey = Path.GetFullPath(dir).Replace('\\', '/');

//             if (!_scannedDirectories.Add(dirKey))
//                 continue;

//             foreach (var coverFile in _coverManager.GetCoverFileNames())
//             {
//                 var coverPath = Path.Combine(dir, coverFile);
//                 if (File.Exists(coverPath))
//                 {
//                     _albumCoversCache[coverPath] = coverPath;
//                     break;
//                 }
//             }
//         }
//     }

//     // Required for Dispose pattern
//     public void Dispose()
//     {
//         _coverManager?.Dispose();
//     }
// }

// public interface ITrackInfoProvider
// {
//     IEnumerable<string> GetSupportedExtensions();
//     Task<Track?> GetTrackInfoAsync(string filePath);
// }

// public interface ICoverManager
// {
//     IEnumerable<string> GetCoverFileNames();
//     Task<string> GetCoverForTrackAsync(string filePath);
//     Task ScanExternalCoversAsync(string directory);
//     void Dispose();
// }
