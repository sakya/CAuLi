using System;
using System.Collections.Generic;
using System.Threading;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Mix;

namespace CAuLi;

class PlayerStatus
{
    public string Track { get; set; }
    public Library.Classes.PlayerPlayList PlayList { get; set; }
    public Player.PlayingStatus Status { get; set; }
    public Player.RepeatType Repeat { get; set; }
    public double TrackPosition { get; set; }
    public bool EqualizerEnabled { get; set; }
    public string EqualizerName { get; set; }
}

partial class Player
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

    private Mutex m_Mutex = new Mutex();
    PlayingStatus m_Status = PlayingStatus.Stop;
    RepeatType m_Repeat = RepeatType.None;
    bool m_Shuffle = false;

    float m_Volume = -1;
    float m_Pan = 0;
    bool m_IgnoreEnd = false;

    int m_BassMixerStreamHandle = 0;
    int m_BassStreamHandle = 0;
    Library.Classes.PlayerPlayList m_Playlist = null;

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

    SYNCPROC m_SyncProcEndMix = null;
    SYNCPROC m_SyncProcEnd = null;

    public Player()
    {
        m_SyncProcEndMix = new SYNCPROC(OnStreamEndMix);
        m_SyncProcEnd = new SYNCPROC(OnStreamEnd);
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
            if (m_BassMixerStreamHandle != 0)
                return Bass.BASS_ChannelGetInfo(m_BassMixerStreamHandle);
            return null;
        }
    }

    public bool IsPlaying
    {
        get
        {
            if (m_BassMixerStreamHandle != 0)
                return Bass.BASS_ChannelIsActive(m_BassMixerStreamHandle) == BASSActive.BASS_ACTIVE_PLAYING;
            return false;
        }
    }

    public double Length
    {
        get
        {
            if (m_BassStreamHandle != 0) {
                long pos = Bass.BASS_ChannelGetLength(m_BassStreamHandle);
                return Bass.BASS_ChannelBytes2Seconds(m_BassStreamHandle, pos);
            }
            return 0;
        }
    }

    public double TrackPosition
    {
        get
        {
            if (m_BassStreamHandle != 0) {
                long pos = Bass.BASS_ChannelGetPosition(m_BassStreamHandle);
                return Bass.BASS_ChannelBytes2Seconds(m_BassStreamHandle, pos);
            }
            return 0;
        }

        set
        {
            if (m_BassStreamHandle != 0)
                Bass.BASS_ChannelSetPosition(m_BassStreamHandle, value);
        }
    }

    public Library.Classes.PlayerPlayList Playlist
    {
        get { return m_Playlist; }
    }

    public float Volume
    {
        get
        {
            if (m_BassMixerStreamHandle != 0) {
                float vol = 0;
                if (Bass.BASS_ChannelGetAttribute(m_BassMixerStreamHandle, BASSAttribute.BASS_ATTRIB_VOL, ref vol)) {
                    m_Volume = vol;
                    return vol;
                }
            }
            return m_Volume >= 0 ? m_Volume : 0;
        }

        set
        {
            if (m_BassMixerStreamHandle != 0)
                Bass.BASS_ChannelSetAttribute(m_BassMixerStreamHandle, BASSAttribute.BASS_ATTRIB_VOL, value);
            if (m_Volume != value) {
                m_Volume = value;
                VolumeChanged?.Invoke(this, m_Volume);
            }
        }
    }

    public float Panning
    {
        get
        {
            if (m_BassMixerStreamHandle != 0) {
                float pan = 0;
                if (Bass.BASS_ChannelGetAttribute(m_BassMixerStreamHandle, BASSAttribute.BASS_ATTRIB_PAN, ref pan)) {
                    m_Pan = pan;
                    return pan;
                }
            }
            return m_Pan >= -1.0f && m_Pan <= 1.0f ? m_Pan : 0;
        }

        set
        {
            if (value > 1.0f)
                value = 1.0f;
            else if (value < -1.0f)
                value = -1.0f;
            if (m_BassMixerStreamHandle != 0)
                Bass.BASS_ChannelSetAttribute(m_BassMixerStreamHandle, BASSAttribute.BASS_ATTRIB_PAN, value);
            if (m_Pan != value) {
                m_Pan = value;
                PanningChanged?.Invoke(this, m_Pan);
            }
        }
    }

    public RepeatType Repeat
    {
        get { return m_Repeat; }
        set
        {
            m_Repeat = value;
            RepeatChanged?.Invoke(this, value);
        }
    }

    public bool Shuffle
    {
        get { return m_Shuffle; }
        set
        {
            m_Shuffle = value;
            if (m_Playlist != null) {
                string track = m_Playlist.TrackFiles[m_Playlist.CurrentIndex];
                m_Playlist.SetShuffle(m_Shuffle);
                m_Playlist.CurrentIndex = m_Playlist.TrackFiles.IndexOf(track);
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
            Track = m_Playlist != null ? m_Playlist.TrackFiles[m_Playlist.CurrentIndex] : string.Empty,
            TrackPosition = TrackPosition,
            PlayList = m_Playlist,
            Status = m_Status,
            Repeat = m_Repeat,
            EqualizerEnabled = EqualizerEnabled,
            EqualizerName = m_EqualizerName,
        };
        return res;
    }

    public bool OpenPlaylist(Library.Classes.PlayerPlayList playlist)
    {
        m_Playlist = playlist;
        if (m_Playlist != null && m_Playlist.TrackFiles != null && m_Playlist.TrackFiles.Count > 0) {
            m_Playlist.SetShuffle(Shuffle);
            FreeMixer();
            m_BassMixerStreamHandle = BassMix.BASS_Mixer_StreamCreate(44100, 2, BASSFlag.BASS_MIXER_END);
            if (!string.IsNullOrEmpty(AppSettings.Instance.EqualizerName))
                SetEqualizer(AppSettings.Instance.EqualizerName);
            if (m_Volume >= 0)
                Volume = m_Volume;
            if (m_Pan >= -1.0f && m_Pan <= 1.0f)
                Panning = m_Pan;

            return SetTrack();
        }
        return false;
    } // OpenPlaylist

    public void Play()
    {
        if (m_BassMixerStreamHandle != 0) {
            m_IgnoreEnd = true;
            Bass.BASS_ChannelPlay(m_BassMixerStreamHandle, false);
            m_IgnoreEnd = false;
            m_Status = PlayingStatus.Play;
            PlayingStatusChanged?.Invoke(this, m_Status);
        }
    }

    public void Pause()
    {
        if (m_BassMixerStreamHandle != 0) {
            Bass.BASS_ChannelPause(m_BassMixerStreamHandle);
            m_Status = PlayingStatus.Pause;
            PlayingStatusChanged?.Invoke(this, m_Status);
        }
    }

    public void Stop()
    {
        if (m_BassMixerStreamHandle != 0) {
            FreeMixer();
            m_Status = PlayingStatus.Stop;
            PlayingStatusChanged?.Invoke(this, m_Status);
        }
    }

    public void Play(int index)
    {
        if (m_Playlist != null) {
            m_IgnoreEnd = true;
            if (index < m_Playlist.TracksCount)
                m_Playlist.CurrentIndex = index;
            else
                m_Playlist.CurrentIndex = 0;

            FreeMixer();
            m_BassMixerStreamHandle = BassMix.BASS_Mixer_StreamCreate(44100, 2, BASSFlag.BASS_MIXER_END);
            if (m_Volume >= 0)
                Volume = m_Volume;

            if (SetTrack())
                Play();
        }
    }

    public void Previous()
    {
        if (m_Playlist != null) {
            m_IgnoreEnd = true;
            if (m_Playlist.CurrentIndex > 0)
                m_Playlist.CurrentIndex--;
            else
                m_Playlist.CurrentIndex = m_Playlist.TracksCount - 1;

            FreeMixer();
            m_BassMixerStreamHandle = BassMix.BASS_Mixer_StreamCreate(44100, 2, BASSFlag.BASS_MIXER_END);
            if (m_Volume >= 0)
                Volume = m_Volume;

            if (SetTrack())
                Play();
        }
    }

    public void Next()
    {
        if (m_Playlist != null) {
            m_IgnoreEnd = true;
            if (m_Playlist.CurrentIndex < m_Playlist.TracksCount - 1)
                m_Playlist.CurrentIndex++;
            else
                m_Playlist.CurrentIndex = 0;

            FreeMixer();
            m_BassMixerStreamHandle = BassMix.BASS_Mixer_StreamCreate(44100, 2, BASSFlag.BASS_MIXER_END);
            if (m_Volume >= 0)
                Volume = m_Volume;

            if (SetTrack())
                Play();
        }
    }
    #endregion

    #region Private operations
    private bool SetTrack()
    {
        FreeMixer();
        m_BassMixerStreamHandle = BassMix.BASS_Mixer_StreamCreate(44100, 2, BASSFlag.BASS_MIXER_END);
        if (m_Volume >= 0)
            Volume = m_Volume;
        m_BassStreamHandle = Bass.BASS_StreamCreateFile(m_Playlist.TrackFiles[m_Playlist.CurrentIndex], 0, 0, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_ASYNCFILE | BASSFlag.BASS_SAMPLE_FLOAT);
        if (m_BassStreamHandle != 0) {
            Bass.BASS_ChannelSetSync(m_BassMixerStreamHandle, BASSSync.BASS_SYNC_MIXTIME | BASSSync.BASS_SYNC_END, 0, m_SyncProcEndMix, IntPtr.Zero);
            Bass.BASS_ChannelSetSync(m_BassMixerStreamHandle, BASSSync.BASS_SYNC_END, 0, m_SyncProcEnd, IntPtr.Zero);
            BassMix.BASS_Mixer_StreamAddChannel(m_BassMixerStreamHandle, m_BassStreamHandle, BASSFlag.BASS_STREAM_AUTOFREE);
            Bass.BASS_ChannelSetPosition(m_BassMixerStreamHandle, 0);
            TrackChanged?.Invoke(this, m_Playlist.TrackFiles[m_Playlist.CurrentIndex], m_Playlist.TracksCount, m_Playlist.CurrentIndex);
        }
        return m_BassStreamHandle != 0;
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
        if (m_IgnoreEnd) {
            m_IgnoreEnd = false;
            return;
        }

        m_Mutex.WaitOne();
        if (m_Playlist != null) {
            var playedTrack = await Library.Library.Instance.GetTrack(m_Playlist.TrackFiles[m_Playlist.CurrentIndex]);
            if (playedTrack != null)
                Library.Library.Instance.TrackPlayed(playedTrack);

            if (Repeat == RepeatType.One) {
                // Repeat track
                m_BassStreamHandle = Bass.BASS_StreamCreateFile(m_Playlist.TrackFiles[m_Playlist.CurrentIndex], 0, 0, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_ASYNCFILE | BASSFlag.BASS_SAMPLE_FLOAT);
                if (m_BassStreamHandle != 0) {
                    BassMix.BASS_Mixer_StreamAddChannel(m_BassMixerStreamHandle, m_BassStreamHandle, BASSFlag.BASS_STREAM_AUTOFREE);
                    Bass.BASS_ChannelSetPosition(m_BassMixerStreamHandle, 0);
                }
            } else {
                if (m_Playlist.CurrentIndex + 1 < m_Playlist.TrackFiles.Count) {
                    // Next track
                    m_BassStreamHandle = Bass.BASS_StreamCreateFile(m_Playlist.TrackFiles[++m_Playlist.CurrentIndex], 0, 0, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_ASYNCFILE | BASSFlag.BASS_SAMPLE_FLOAT);
                    if (m_BassStreamHandle != 0) {
                        BassMix.BASS_Mixer_StreamAddChannel(m_BassMixerStreamHandle, m_BassStreamHandle, BASSFlag.BASS_STREAM_AUTOFREE);
                        Bass.BASS_ChannelSetPosition(m_BassMixerStreamHandle, 0);
                        TrackChanged?.Invoke(this, m_Playlist.TrackFiles[m_Playlist.CurrentIndex], m_Playlist.TracksCount, m_Playlist.CurrentIndex);
                    }
                } else {
                    // Playlist end
                    if (Repeat == RepeatType.All) {
                        // Repeat all:
                        m_Playlist.CurrentIndex = 0;
                        m_BassStreamHandle = Bass.BASS_StreamCreateFile(m_Playlist.TrackFiles[m_Playlist.CurrentIndex], 0, 0, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_ASYNCFILE | BASSFlag.BASS_SAMPLE_FLOAT);
                        if (m_BassStreamHandle != 0) {
                            BassMix.BASS_Mixer_StreamAddChannel(m_BassMixerStreamHandle, m_BassStreamHandle, BASSFlag.BASS_STREAM_AUTOFREE);
                            Bass.BASS_ChannelSetPosition(m_BassMixerStreamHandle, 0);
                            TrackChanged?.Invoke(this, m_Playlist.TrackFiles[m_Playlist.CurrentIndex], m_Playlist.TracksCount, m_Playlist.CurrentIndex);
                        }
                    } else {
                        // Finished
                        m_Playlist.CurrentIndex = -1;
                    }
                }
            }

            if (playedTrack != null)
                Library.Library.Instance.TrackPlayed(playedTrack);
        }
        m_Mutex.ReleaseMutex();
    } // OnStreamEndMix

    private void OnStreamEnd(int handle, int channel, int data, IntPtr user)
    {
        m_Mutex.WaitOne();
        if (m_Playlist != null) {
            if (m_Playlist.CurrentIndex == -1) {
                FreeMixer();
                m_Playlist.CurrentIndex = 0;
                m_Status = PlayingStatus.Stop;
                PlayingStatusChanged?.Invoke(this, m_Status);
                TrackChanged?.Invoke(this, m_Playlist.TrackFiles[m_Playlist.CurrentIndex], m_Playlist.TracksCount, m_Playlist.CurrentIndex);
            }
        }
        m_Mutex.ReleaseMutex();
    } // OnStreamEnd

    private void FreeMixer()
    {
        if (m_BassMixerStreamHandle != 0) {
            RemoveEqualizer();
            Bass.BASS_ChannelStop(m_BassMixerStreamHandle);
            if (!Bass.BASS_StreamFree(m_BassMixerStreamHandle)) {
                BASSError err = Bass.BASS_ErrorGetCode();
            }
        }
        m_BassStreamHandle = 0;
        m_BassMixerStreamHandle = 0;
    } // FreeMixer
    #endregion
}