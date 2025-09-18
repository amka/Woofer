using System.Diagnostics;
using System.Runtime.InteropServices;
using GObject;
using Gst;
using NAudio.Wave;
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
        Console.WriteLine("GStreamer Message: " + data.ToString());
        switch (data.Type)
        {
            case MessageType.Eos:
                Stop();
                OnEosReached?.Invoke();
                break;
            case MessageType.Error:
                Console.WriteLine("GStreamer Error: " + args.Message.ToString());
                Stop();
                OnStateChanged?.Invoke(PlayerState.Error);
                break;
            default:
                Console.WriteLine("GStreamer Message: " + args.Message.ToString());
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
            Console.WriteLine(ex.Message);
        }
        _state = PlayerState.Playing;

        // Отправляем уведомление о новом треке
        OnStateChanged?.Invoke(_state);
        OnTrackChanged?.Invoke(track);
    }

    /// <summary>
    /// Продолжает воспроизведение.
    /// </summary>
    /// <param name="filePath"></param>
    public void Play(string filePath)
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

    public PlayerState State => _state;
}