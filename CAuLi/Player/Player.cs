using System;
using System.Collections.Generic;
using System.Threading;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Mix;

namespace CAuLi;

internal class PlayerStatus
{
    public string Track { get; set; }
    public Library.Classes.PlayerPlayList PlayList { get; set; }
    public Player.PlayingStatus Status { get; set; }
    public Player.RepeatType Repeat { get; set; }
    public double TrackPosition { get; set; }
    public bool EqualizerEnabled { get; set; }
    public string EqualizerName { get; set; }
}

internal partial class Player
{
    public enum PlayingStatus
    {
        Stop,
        Pause,
        Play
    }

    public enum RepeatType
    {
        None,
        One,
        All
    }

    private readonly Mutex _mutex = new();
    private PlayingStatus _status = PlayingStatus.Stop;
    private RepeatType _repeat = RepeatType.None;
    private bool _shuffle;

    private float _volume = -1;
    private float _pan;
    private bool _ignoreEnd;

    private int _bassMixerStreamHandle;
    private int _bassStreamHandle;
    private Library.Classes.PlayerPlayList _playlist;

    #region delegates
    public delegate void TrackChangedHandler(Player sender, string fileName, int playlystCount, int playlistCurrentIndex);
    public TrackChangedHandler TrackChanged;
    public delegate void PlayingStatusChangedHandler(Player sender, PlayingStatus status);
    public PlayingStatusChangedHandler PlayingStatusChanged;
    public delegate void RepeatChangedHandler(Player sender, RepeatType type);
    public RepeatChangedHandler RepeatChanged;
    public delegate void VolumeChangedHandler(Player sender, float volume);
    public VolumeChangedHandler VolumeChanged;
    public delegate void PanningChangedHandler(Player sender, float panning);
    public PanningChangedHandler PanningChanged;
    public delegate void ShuffleChangedHandler(Player sender, bool shuffle);
    public ShuffleChangedHandler ShuffleChanged;
    #endregion

    private readonly SYNCPROC _syncProcEndMix;
    private readonly SYNCPROC _syncProcEnd;

    public Player()
    {
        _syncProcEndMix = OnStreamEndMix;
        _syncProcEnd = OnStreamEnd;
    }

    public static Player Instance
    {
        get;
        set;
    }

    public static List<BASS_DEVICEINFO> DevicesInfo
    {
        get
        {
            List<BASS_DEVICEINFO> res = new List<BASS_DEVICEINFO>();

            int idx = 0;
            BASS_DEVICEINFO info = Bass.BASS_GetDeviceInfo(idx);
            while (info != null) {
                res.Add(info);
                info = Bass.BASS_GetDeviceInfo(++idx);
            }
            return res;
        }
    }

    public BASS_DEVICEINFO DeviceInfo
    {
        get
        {
            int dev = Bass.BASS_GetDevice();
            if (dev >= 0)
                return Bass.BASS_GetDeviceInfo(dev);
            return null;
        }
    }

    public BASS_CHANNELINFO ChannelInfo
    {
        get
        {
            if (_bassMixerStreamHandle != 0)
                return Bass.BASS_ChannelGetInfo(_bassMixerStreamHandle);
            return null;
        }
    }

    public bool IsPlaying
    {
        get
        {
            if (_bassMixerStreamHandle != 0)
                return Bass.BASS_ChannelIsActive(_bassMixerStreamHandle) == BASSActive.BASS_ACTIVE_PLAYING;
            return false;
        }
    }

    public double Length
    {
        get
        {
            if (_bassStreamHandle != 0) {
                long pos = Bass.BASS_ChannelGetLength(_bassStreamHandle);
                return Bass.BASS_ChannelBytes2Seconds(_bassStreamHandle, pos);
            }
            return 0;
        }
    }

    public double TrackPosition
    {
        get
        {
            if (_bassStreamHandle != 0) {
                long pos = Bass.BASS_ChannelGetPosition(_bassStreamHandle);
                return Bass.BASS_ChannelBytes2Seconds(_bassStreamHandle, pos);
            }
            return 0;
        }

        set
        {
            if (_bassStreamHandle != 0)
                Bass.BASS_ChannelSetPosition(_bassStreamHandle, value);
        }
    }

    public Library.Classes.PlayerPlayList Playlist
    {
        get { return _playlist; }
    }

    public float Volume
    {
        get
        {
            if (_bassMixerStreamHandle != 0) {
                float vol = 0;
                if (Bass.BASS_ChannelGetAttribute(_bassMixerStreamHandle, BASSAttribute.BASS_ATTRIB_VOL, ref vol)) {
                    _volume = vol;
                    return vol;
                }
            }
            return _volume >= 0 ? _volume : 0;
        }

        set
        {
            if (_bassMixerStreamHandle != 0)
                Bass.BASS_ChannelSetAttribute(_bassMixerStreamHandle, BASSAttribute.BASS_ATTRIB_VOL, value);
            if (_volume != value) {
                _volume = value;
                VolumeChanged?.Invoke(this, _volume);
            }
        }
    }

    public float Panning
    {
        get
        {
            if (_bassMixerStreamHandle != 0) {
                float pan = 0;
                if (Bass.BASS_ChannelGetAttribute(_bassMixerStreamHandle, BASSAttribute.BASS_ATTRIB_PAN, ref pan)) {
                    _pan = pan;
                    return pan;
                }
            }
            return _pan >= -1.0f && _pan <= 1.0f ? _pan : 0;
        }

        set
        {
            if (value > 1.0f)
                value = 1.0f;
            else if (value < -1.0f)
                value = -1.0f;
            if (_bassMixerStreamHandle != 0)
                Bass.BASS_ChannelSetAttribute(_bassMixerStreamHandle, BASSAttribute.BASS_ATTRIB_PAN, value);
            if (_pan != value) {
                _pan = value;
                PanningChanged?.Invoke(this, _pan);
            }
        }
    }

    public RepeatType Repeat
    {
        get { return _repeat; }
        set
        {
            _repeat = value;
            RepeatChanged?.Invoke(this, value);
        }
    }

    public bool Shuffle
    {
        get { return _shuffle; }
        set
        {
            _shuffle = value;
            if (_playlist != null) {
                string track = _playlist.TrackFiles[_playlist.CurrentIndex];
                _playlist.SetShuffle(_shuffle);
                _playlist.CurrentIndex = _playlist.TrackFiles.IndexOf(track);
            }
            ShuffleChanged?.Invoke(this, value);
        }
    }
    #region Public operations
    public bool Init(int device)
    {
        BassNet.Registration("sakya_tg@yahoo.it", "2X283151734318");
        if (!Bass.BASS_Init(device, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero)) {
            BASSError err = Bass.BASS_ErrorGetCode();
            return false;
        }
        Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_VISTA_SPEAKERS, true);
        if (!Un4seen.Bass.Bass.BASS_Start()) {
            BASSError err = Bass.BASS_ErrorGetCode();
            return false;
        }

        List<string> plugins = null;
        if (OperatingSystem.IsWindows()) {
            plugins = new List<string>() { "bass_aac.dll", "bass_ac3.dll", "bass_ape.dll", "bass_mpc.dll",
                "bassalac.dll", "bassflac.dll", "basswma.dll" };
        } else if (OperatingSystem.IsLinux()) {
            plugins = new List<string>() { "libbass_aac.so", "libbass_ac3.so", "libbass_ape.so", "libbass_mpc.so",
                "libbassalac.so", "libbassflac.so" };
        }
        LoadPlugins(plugins);

        return true;
    }

    public PlayerStatus GetStatus()
    {
        PlayerStatus res = new PlayerStatus()
        {
            Track = _playlist != null ? _playlist.TrackFiles[_playlist.CurrentIndex] : string.Empty,
            TrackPosition = TrackPosition,
            PlayList = _playlist,
            Status = _status,
            Repeat = _repeat,
            EqualizerEnabled = EqualizerEnabled,
            EqualizerName = _equalizerName,
        };
        return res;
    }

    public bool OpenPlaylist(Library.Classes.PlayerPlayList playlist)
    {
        _playlist = playlist;
        if (_playlist != null && _playlist.TrackFiles != null && _playlist.TrackFiles.Count > 0) {
            _playlist.SetShuffle(Shuffle);
            FreeMixer();
            _bassMixerStreamHandle = BassMix.BASS_Mixer_StreamCreate(44100, 2, BASSFlag.BASS_MIXER_END);
            if (!string.IsNullOrEmpty(AppSettings.Instance.EqualizerName))
                SetEqualizer(AppSettings.Instance.EqualizerName);
            if (_volume >= 0)
                Volume = _volume;
            if (_pan >= -1.0f && _pan <= 1.0f)
                Panning = _pan;

            return SetTrack();
        }
        return false;
    } // OpenPlaylist

    public void Play()
    {
        if (_bassMixerStreamHandle != 0) {
            _ignoreEnd = true;
            Bass.BASS_ChannelPlay(_bassMixerStreamHandle, false);
            _ignoreEnd = false;
            _status = PlayingStatus.Play;
            PlayingStatusChanged?.Invoke(this, _status);
        }
    }

    public void Pause()
    {
        if (_bassMixerStreamHandle != 0) {
            Bass.BASS_ChannelPause(_bassMixerStreamHandle);
            _status = PlayingStatus.Pause;
            PlayingStatusChanged?.Invoke(this, _status);
        }
    }

    public void Stop()
    {
        if (_bassMixerStreamHandle != 0) {
            FreeMixer();
            _status = PlayingStatus.Stop;
            PlayingStatusChanged?.Invoke(this, _status);
        }
    }

    public void Play(int index)
    {
        if (_playlist != null) {
            _ignoreEnd = true;
            if (index < _playlist.TracksCount)
                _playlist.CurrentIndex = index;
            else
                _playlist.CurrentIndex = 0;

            FreeMixer();
            _bassMixerStreamHandle = BassMix.BASS_Mixer_StreamCreate(44100, 2, BASSFlag.BASS_MIXER_END);
            if (_volume >= 0)
                Volume = _volume;

            if (SetTrack())
                Play();
        }
    }

    public void Previous()
    {
        if (_playlist != null) {
            _ignoreEnd = true;
            if (_playlist.CurrentIndex > 0)
                _playlist.CurrentIndex--;
            else
                _playlist.CurrentIndex = _playlist.TracksCount - 1;

            FreeMixer();
            _bassMixerStreamHandle = BassMix.BASS_Mixer_StreamCreate(44100, 2, BASSFlag.BASS_MIXER_END);
            if (_volume >= 0)
                Volume = _volume;

            if (SetTrack())
                Play();
        }
    }

    public void Next()
    {
        if (_playlist != null) {
            _ignoreEnd = true;
            if (_playlist.CurrentIndex < _playlist.TracksCount - 1)
                _playlist.CurrentIndex++;
            else
                _playlist.CurrentIndex = 0;

            FreeMixer();
            _bassMixerStreamHandle = BassMix.BASS_Mixer_StreamCreate(44100, 2, BASSFlag.BASS_MIXER_END);
            if (_volume >= 0)
                Volume = _volume;

            if (SetTrack())
                Play();
        }
    }
    #endregion

    #region Private operations
    private bool SetTrack()
    {
        FreeMixer();
        _bassMixerStreamHandle = BassMix.BASS_Mixer_StreamCreate(44100, 2, BASSFlag.BASS_MIXER_END);
        if (_volume >= 0)
            Volume = _volume;
        _bassStreamHandle = Bass.BASS_StreamCreateFile(_playlist.TrackFiles[_playlist.CurrentIndex], 0, 0, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_ASYNCFILE | BASSFlag.BASS_SAMPLE_FLOAT);
        if (_bassStreamHandle != 0) {
            Bass.BASS_ChannelSetSync(_bassMixerStreamHandle, BASSSync.BASS_SYNC_MIXTIME | BASSSync.BASS_SYNC_END, 0, _syncProcEndMix, IntPtr.Zero);
            Bass.BASS_ChannelSetSync(_bassMixerStreamHandle, BASSSync.BASS_SYNC_END, 0, _syncProcEnd, IntPtr.Zero);
            BassMix.BASS_Mixer_StreamAddChannel(_bassMixerStreamHandle, _bassStreamHandle, BASSFlag.BASS_STREAM_AUTOFREE);
            Bass.BASS_ChannelSetPosition(_bassMixerStreamHandle, 0);
            TrackChanged?.Invoke(this, _playlist.TrackFiles[_playlist.CurrentIndex], _playlist.TracksCount, _playlist.CurrentIndex);
        }
        return _bassStreamHandle != 0;
    } // SetTrack

    private void LoadPlugins(List<string> plugins)
    {
        var libPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        foreach (string plugin in plugins) {
            if (Bass.BASS_PluginLoad(System.IO.Path.Combine(libPath, plugin)) == 0) {
                BASSError err = Bass.BASS_ErrorGetCode();
                System.Diagnostics.Debug.WriteLine("Failed to load plugin {0}: {1}", plugin, err);
            }
        }
    }

    private async void OnStreamEndMix(int handle, int channel, int data, IntPtr user)
    {
        if (_ignoreEnd) {
            _ignoreEnd = false;
            return;
        }

        _mutex.WaitOne();
        if (_playlist != null) {
            var playedTrack = await Library.Library.Instance.GetTrack(_playlist.TrackFiles[_playlist.CurrentIndex]);
            if (playedTrack != null)
                Library.Library.Instance.TrackPlayed(playedTrack);

            if (Repeat == RepeatType.One) {
                // Repeat track
                _bassStreamHandle = Bass.BASS_StreamCreateFile(_playlist.TrackFiles[_playlist.CurrentIndex], 0, 0, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_ASYNCFILE | BASSFlag.BASS_SAMPLE_FLOAT);
                if (_bassStreamHandle != 0) {
                    BassMix.BASS_Mixer_StreamAddChannel(_bassMixerStreamHandle, _bassStreamHandle, BASSFlag.BASS_STREAM_AUTOFREE);
                    Bass.BASS_ChannelSetPosition(_bassMixerStreamHandle, 0);
                }
            } else {
                if (_playlist.CurrentIndex + 1 < _playlist.TrackFiles.Count) {
                    // Next track
                    _bassStreamHandle = Bass.BASS_StreamCreateFile(_playlist.TrackFiles[++_playlist.CurrentIndex], 0, 0, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_ASYNCFILE | BASSFlag.BASS_SAMPLE_FLOAT);
                    if (_bassStreamHandle != 0) {
                        BassMix.BASS_Mixer_StreamAddChannel(_bassMixerStreamHandle, _bassStreamHandle, BASSFlag.BASS_STREAM_AUTOFREE);
                        Bass.BASS_ChannelSetPosition(_bassMixerStreamHandle, 0);
                        TrackChanged?.Invoke(this, _playlist.TrackFiles[_playlist.CurrentIndex], _playlist.TracksCount, _playlist.CurrentIndex);
                    }
                } else {
                    // Playlist end
                    if (Repeat == RepeatType.All) {
                        // Repeat all:
                        _playlist.CurrentIndex = 0;
                        _bassStreamHandle = Bass.BASS_StreamCreateFile(_playlist.TrackFiles[_playlist.CurrentIndex], 0, 0, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_ASYNCFILE | BASSFlag.BASS_SAMPLE_FLOAT);
                        if (_bassStreamHandle != 0) {
                            BassMix.BASS_Mixer_StreamAddChannel(_bassMixerStreamHandle, _bassStreamHandle, BASSFlag.BASS_STREAM_AUTOFREE);
                            Bass.BASS_ChannelSetPosition(_bassMixerStreamHandle, 0);
                            TrackChanged?.Invoke(this, _playlist.TrackFiles[_playlist.CurrentIndex], _playlist.TracksCount, _playlist.CurrentIndex);
                        }
                    } else {
                        // Finished
                        _playlist.CurrentIndex = -1;
                    }
                }
            }

            if (playedTrack != null)
                Library.Library.Instance.TrackPlayed(playedTrack);
        }
        _mutex.ReleaseMutex();
    } // OnStreamEndMix

    private void OnStreamEnd(int handle, int channel, int data, IntPtr user)
    {
        _mutex.WaitOne();
        if (_playlist != null) {
            if (_playlist.CurrentIndex == -1) {
                FreeMixer();
                _playlist.CurrentIndex = 0;
                _status = PlayingStatus.Stop;
                PlayingStatusChanged?.Invoke(this, _status);
                TrackChanged?.Invoke(this, _playlist.TrackFiles[_playlist.CurrentIndex], _playlist.TracksCount, _playlist.CurrentIndex);
            }
        }
        _mutex.ReleaseMutex();
    } // OnStreamEnd

    private void FreeMixer()
    {
        if (_bassMixerStreamHandle != 0) {
            RemoveEqualizer();
            Bass.BASS_ChannelStop(_bassMixerStreamHandle);
            if (!Bass.BASS_StreamFree(_bassMixerStreamHandle)) {
                BASSError err = Bass.BASS_ErrorGetCode();
            }
        }
        _bassStreamHandle = 0;
        _bassMixerStreamHandle = 0;
    } // FreeMixer
    #endregion
}