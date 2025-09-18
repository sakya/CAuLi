using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CAuLi;

class AppSettings
{
    private readonly string _fileName;
    private readonly Utility.GenericConfig _config;
    public AppSettings(string fileName)
    {
        _fileName = fileName;
        if (File.Exists(fileName))
            _config = Utility.Serialization.Deserialize<Utility.GenericConfig>(new FileStream(fileName, FileMode.Open));
        else {
            _config = new Utility.GenericConfig();
            AutoUpdateLibrary = true;
        }

        if (_config.Values == null)
            _config.Values = new List<Utility.GenericConfigValue>();
    }

    public static AppSettings Instance
    {
        get;
        set;
    }

    #region Settings
    public string ColorTheme
    {
        get { return _config.GetValue("ColorTheme", "Default.xml"); }
        set { _config.SetValue("ColorTheme", value); }
    }

    public List<string> MusicFolders
    {
        get
        {
            string val = _config.GetValue("MusicFolders", string.Empty);
            if (!string.IsNullOrEmpty(val))
                return val.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            return new List<string>();
        }

        set
        {
            string val = string.Join(";", value);
            _config.SetValue("MusicFolders", val);
        }
    }

    public int WindowWidth
    {
        get { return int.Parse(_config.GetValue("WindowWidth", "120")); }
        set { _config.SetValue("WindowWidth", value.ToString(CultureInfo.InvariantCulture)); }
    }

    public int WindowHeight
    {
        get { return int.Parse(_config.GetValue("WindowHeight", "30")); }
        set { _config.SetValue("WindowHeight", value.ToString(CultureInfo.InvariantCulture)); }
    }

    public int Repeat
    {
        get { return int.Parse(_config.GetValue("Repeat", "0"), CultureInfo.InvariantCulture); }
        set { _config.SetValue("Repeat", value.ToString(CultureInfo.InvariantCulture)); }
    }

    public bool Shuffle
    {
        get { return int.Parse(_config.GetValue("Shuffle", "0"), CultureInfo.InvariantCulture) == 1; }
        set { _config.SetValue("Shuffle", value ? "1" : "0"); }
    }

    public float Volume
    {
        get { return float.Parse(_config.GetValue("Volume", "1.0"), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture); }
        set { _config.SetValue("Volume", value.ToString(CultureInfo.InvariantCulture)); }
    }

    public float Panning
    {
        get { return float.Parse(_config.GetValue("Panning", "0.0"), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture); }
        set { _config.SetValue("Panning", value.ToString(CultureInfo.InvariantCulture)); }
    }

    public bool SortAlbumsByDate
    {
        get { return int.Parse(_config.GetValue("SortAlbumsByDate", "0")) == 1; }
        set { _config.SetValue("SortAlbumsByDate", value ? "1" : "0"); }
    }

    public bool AutoUpdateLibrary
    {
        get { return int.Parse(_config.GetValue("AutoUpdateLibrary", "1")) == 1; }
        set { _config.SetValue("AutoUpdateLibrary", value ? "1" : "0"); }
    }

    public DateTime LastLibraryUpdate
    {
        get { return GetDateTime("LastLibraryUpdate"); }
        set { _config.SetValue("LastLibraryUpdate", value.ToString("yyyyMMddTHHmmss")); }
    }

    public List<string> LastMusicFolders
    {
        get
        {
            string val = _config.GetValue("LastMusicFolders", string.Empty);
            if (!string.IsNullOrEmpty(val))
                return val.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            return new List<string>();
        }

        set
        {
            string val = string.Join(";", value);
            _config.SetValue("LastMusicFolders", val);
        }
    }

    public string EqualizerName
    {
        get { return _config.GetValue("EqualizerName", string.Empty); }
        set { _config.SetValue("EqualizerName", value); }
    }

    public Library.Classes.PlayerPlayList Playlist
    {
        get { return Utility.Serialization.Deserialize<Library.Classes.PlayerPlayList>(_config.GetValue("Playlist", string.Empty)); }
        set { _config.SetValue("Playlist", Utility.Serialization.Serialize(value)); }
    }

    /// <summary>
    /// Key code for the Quit command (default X)
    /// </summary>
    public int KeyCodeQuit
    {
        get { return int.Parse(_config.GetValue("KeyCodeQuit", "88"), CultureInfo.InvariantCulture); }
        set { _config.SetValue("KeyCodeQuit", value.ToString(CultureInfo.InvariantCulture)); }
    }

    /// <summary>
    /// Key code for the Player command (default P)
    /// </summary>
    public int KeyCodePlayer
    {
        get { return int.Parse(_config.GetValue("KeyCodePlayer", "80"), CultureInfo.InvariantCulture); }
        set { _config.SetValue("KeyCodePlayer", value.ToString(CultureInfo.InvariantCulture)); }
    }

    /// <summary>
    /// Key code for the Lyrics command (default L)
    /// </summary>
    public int KeyCodeLyrics
    {
        get { return int.Parse(_config.GetValue("KeyCodeLyrics", "76"), CultureInfo.InvariantCulture); }
        set { _config.SetValue("KeyCodeLyrics", value.ToString(CultureInfo.InvariantCulture)); }
    }

    /// <summary>
    /// Key code for the Repeat toggle command (default R)
    /// </summary>
    public int KeyCodeRepeat
    {
        get { return int.Parse(_config.GetValue("KeyCodeRepeat", "82"), CultureInfo.InvariantCulture); }
        set { _config.SetValue("KeyCodeRepeat", value.ToString(CultureInfo.InvariantCulture)); }
    }

    /// <summary>
    /// Key code for the Shuffle toggle command (default Z)
    /// </summary>
    public int KeyCodeShuffle
    {
        get { return int.Parse(_config.GetValue("KeyCodeShuffle", "90"), CultureInfo.InvariantCulture); }
        set { _config.SetValue("KeyCodeShuffle", value.ToString(CultureInfo.InvariantCulture)); }
    }

    /// <summary>
    /// Key code for the Equalizer toggle command (default E)
    /// </summary>
    public int KeyCodeToggleEq
    {
        get { return int.Parse(_config.GetValue("KeyCodeToggleEq", "69"), CultureInfo.InvariantCulture); }
        set { _config.SetValue("KeyCodeToggleEq", value.ToString(CultureInfo.InvariantCulture)); }
    }

    /// <summary>
    /// Key code for the next equalizer command (default N)
    /// </summary>
    public int KeyCodeNextEq
    {
        get { return int.Parse(_config.GetValue("KeyCodeNextEq", "78"), CultureInfo.InvariantCulture); }
        set { _config.SetValue("KeyCodeNextEq", value.ToString(CultureInfo.InvariantCulture)); }
    }

    /// <summary>
    /// Key code for the balance right (default H)
    /// </summary>
    public int KeyCodePanRight
    {
        get { return int.Parse(_config.GetValue("KeyCodePanRight", "72"), CultureInfo.InvariantCulture); }
        set { _config.SetValue("KeyCodePanRight", value.ToString(CultureInfo.InvariantCulture)); }
    }

    /// <summary>
    /// Key code for the balance left (default G)
    /// </summary>
    public int KeyCodePanLeft
    {
        get { return int.Parse(_config.GetValue("KeyCodePanLeft", "71"), CultureInfo.InvariantCulture); }
        set { _config.SetValue("KeyCodePanLeft", value.ToString(CultureInfo.InvariantCulture)); }
    }
    #endregion

    #region Public operations
    public bool Save()
    {
        string ser = Utility.Serialization.Serialize(_config);
        using (Stream stream = new FileStream(_fileName, FileMode.Create)) {
            byte[] bytes = Encoding.UTF8.GetBytes(ser);
            stream.Write(bytes, 0, bytes.Length);
        }
        return true;
    }
    #endregion

    #region Private operations
    private DateTime GetDateTime(string name)
    {
        string v = _config.GetValue(name, string.Empty);
        if (string.IsNullOrEmpty(v))
            return DateTime.MinValue;

        DateTime res = DateTime.MinValue;
        if (DateTime.TryParseExact(v, "yyyyMMddTHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out res))
            return res;
        return DateTime.MinValue;
    }
    #endregion
}