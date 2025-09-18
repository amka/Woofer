using System.Runtime.InteropServices;
using GObject;
using Gst;
using Microsoft.Extensions.Logging;
using Woofer.Models;

namespace Woofer.Services;

// Workaround while https://github.com/gircore/gir.core/issues/968 is not fixed
[StructLayout(LayoutKind.Explicit)]
public struct MessageData
{
    [FieldOffset(64)]
    public Gst.MessageType Type;
    [FieldOffset(72)]
    public ulong Timestamp;
    [FieldOffset(80)]
    public IntPtr Src;
    [FieldOffset(88)]
    public uint Seqnum;
}

[Subclass<GObject.Object>]
public partial class PlayerController
{
    private ILogger<PlayerController> _logger;

    private PlayerState _state = PlayerState.Stopped;
    private Track? _currentTrack;
    private readonly Element? _player;
    private readonly Bus? _bus;
    private double _volume = 0.8;

    public event Action<PlayerState>? OnStateChanged;
    public event Action<int>? OnPositionChanged;
    public event Action<double>? OnVolumeChanged;
    public event Action? OnEosReached;
    public event Action<Track>? OnTrackChanged;


    /// <summary>
    /// Возвращает текущее состояние воспроизведения.
    /// </summary>
    public PlayerState State => _state;

    /// <summary>
    /// Возвращает продолжительность текущего трека в секундах.
    /// </summary>
    public long Duration => _currentTrack?.Duration ?? 0;

    public PlayerController(GObject.Object? settingsManager) : this()
    {
        Gst.Module.Initialize();
        Gst.Application.Init();

        _player = ElementFactory.Make("playbin", "player");
        _bus = _player?.GetBus();
        if (_bus != null)
        {
            _bus!.AddSignalWatch();
            _bus!.OnMessage += OnBusMessage;
        }

        // Устанавливаем начальную громкость
        _player?.SetProperty("volume", new Value(_volume));

        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<PlayerController>();
        _logger.LogInformation("PlayerController created");

    }

    /// <summary>
    /// Обработка сообщений от GStreamer.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void OnBusMessage(Bus sender, Bus.MessageSignalArgs args)
    {
        var data = Marshal.PtrToStructure<MessageData>(args.Message.Handle.DangerousGetHandle());
        _logger.LogDebug("GStreamer Message: {Message}", data.ToString());
        switch (data.Type)
        {
            case MessageType.Eos:
                Stop();
                OnEosReached?.Invoke();
                break;
            case MessageType.Error:
                _logger.LogError("GStreamer Error: {Message}", args.Message.ToString());
                Stop();
                OnStateChanged?.Invoke(PlayerState.Error);
                break;
            case MessageType.Progress:
                // Обновляем позицию воспроизведения
                _logger.LogDebug("GStreamer Progress: {Message}", args.Message.ToString());
                var position = Position;
                OnPositionChanged?.Invoke((int)position);
                break;
            default:
                _logger.LogDebug("GStreamer Message: {Message}", args.Message.ToString());
                break;
        }
    }

    /// <summary>
    /// Начинает воспроизведение трека.
    /// </summary>
    /// <param name="track"></param>
    public void PlayTrack(Track track)
    {
        if (_currentTrack == track && _state == PlayerState.Playing) return;

        _currentTrack = track;
        var uri = new FileInfo(track.FilePath).FullName;
        if (!uri.StartsWith("file://"))
        {
            uri = "file://" + uri;
        }

        try
        {
            _player?.SetProperty("uri", new Value(uri));
            _player?.SetState(Gst.State.Playing);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error playing track {Track}: {Message}", track.Title, ex.Message);
        }
        _state = PlayerState.Playing;

        // Отправляем уведомление о новом треке
        OnStateChanged?.Invoke(_state);
        OnTrackChanged?.Invoke(track);
    }

    /// <summary>
    /// Продолжает воспроизведение.
    /// </summary>
    public void Play()
    {
        if (_state == PlayerState.Paused)
        {
            _player?.SetState(Gst.State.Playing);
            _state = PlayerState.Playing;
            OnStateChanged?.Invoke(_state);
        }
    }

    /// <summary>
    /// Продолжает воспроизведение.
    /// </summary>
    public void Pause()
    {
        if (_state == PlayerState.Playing)
        {
            _player?.SetState(Gst.State.Paused);
            _state = PlayerState.Paused;
            OnStateChanged?.Invoke(_state);
        }
    }

    /// <summary>
    /// Останавливает воспроизведение.
    /// </summary>
    public void Stop()
    {
        _player?.SetState(Gst.State.Null);
        _state = PlayerState.Stopped;
        OnStateChanged?.Invoke(_state);
    }

    public void TogglePlayPause()
    {
        switch (_state)
        {
            case PlayerState.Playing:
                Pause();
                break;
            default:
                Play();
                break;
        }
    }

    public double Volume
    {
        get => _volume;
        set
        {
            // Ограничиваем значение громкости от 0.0 до 1.0
            _volume = double.Max(0.0, double.Min(1.0, value));
            _player?.SetProperty("volume", new Value(_volume));
            OnVolumeChanged?.Invoke(_volume);
        }
    }

    /// <summary>
    /// Возвращает текущую позицию воспроизведения в наносекундах.
    /// </summary>
    public long Position
    {
        get
        {
            long cur = 0;
            var success = _player?.QueryPosition(Gst.Format.Time, out cur) ?? false;
            return success ? cur / Gst.Constants.SECOND : 0;
        }
    }

    /// <summary>
    /// Перематывает к указанной позиции (в секундах).
    /// </summary>
    /// <param name="position"></param>
    public void Seek(int position)
    {
        _player?.SeekSimple(Gst.Format.Time, Gst.SeekFlags.Flush | Gst.SeekFlags.KeyUnit, position * Gst.Constants.SECOND);
    }

}
