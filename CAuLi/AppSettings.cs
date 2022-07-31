using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CAuLi
{
    class AppSettings
    {
        string m_FileName = string.Empty;
        Utility.GenericConfig m_Config = null;
        public AppSettings(string fileName)
        {
            m_FileName = fileName;
            if (File.Exists(fileName))
                m_Config = Utility.Serialization.Deserialize<Utility.GenericConfig>(new FileStream(fileName, FileMode.Open));
            else {
                m_Config = new Utility.GenericConfig();
                AutoUpdateLibrary = true;
            }

            if (m_Config.Values == null)
                m_Config.Values = new List<Utility.GenericConfigValue>();
        }

        public static AppSettings Instance
        {
            get;
            set;
        }

        #region Settings
        public string ColorTheme
        {
            get { return m_Config.GetValue("ColorTheme", "Default.xml"); }
            set { m_Config.SetValue("ColorTheme", value); }
        }

        public List<string> MusicFolders
        {
            get
            {
                string val = m_Config.GetValue("MusicFolders", string.Empty);
                if (!string.IsNullOrEmpty(val))
                    return val.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                return new List<string>();
            }

            set
            {
                string val = string.Join(";", value);
                m_Config.SetValue("MusicFolders", val);
            }
        }

        public int WindowWidth
        {
            get { return int.Parse(m_Config.GetValue("WindowWidth", "120")); }
            set { m_Config.SetValue("WindowWidth", value.ToString(CultureInfo.InvariantCulture)); }
        }

        public int WindowHeight
        {
            get { return int.Parse(m_Config.GetValue("WindowHeight", "30")); }
            set { m_Config.SetValue("WindowHeight", value.ToString(CultureInfo.InvariantCulture)); }
        }

        public int Repeat
        {
            get { return int.Parse(m_Config.GetValue("Repeat", "0"), CultureInfo.InvariantCulture); }
            set { m_Config.SetValue("Repeat", value.ToString(CultureInfo.InvariantCulture)); }
        }

        public bool Shuffle
        {
            get { return int.Parse(m_Config.GetValue("Shuffle", "0"), CultureInfo.InvariantCulture) == 1; }
            set { m_Config.SetValue("Shuffle", value ? "1" : "0"); }
        }

        public float Volume
        {
            get { return float.Parse(m_Config.GetValue("Volume", "1.0"), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture); }
            set { m_Config.SetValue("Volume", value.ToString(CultureInfo.InvariantCulture)); }
        }

        public float Panning
        {
            get { return float.Parse(m_Config.GetValue("Panning", "0.0"), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture); }
            set { m_Config.SetValue("Panning", value.ToString(CultureInfo.InvariantCulture)); }
        }

        public bool SortAlbumsByDate
        {
            get { return int.Parse(m_Config.GetValue("SortAlbumsByDate", "0")) == 1; }
            set { m_Config.SetValue("SortAlbumsByDate", value ? "1" : "0"); }
        }

        public bool AutoUpdateLibrary
        {
            get { return int.Parse(m_Config.GetValue("AutoUpdateLibrary", "1")) == 1; }
            set { m_Config.SetValue("AutoUpdateLibrary", value ? "1" : "0"); }
        }

        public DateTime LastLibraryUpdate
        {
            get { return GetDateTime("LastLibraryUpdate"); }
            set { m_Config.SetValue("LastLibraryUpdate", value.ToString("yyyyMMddTHHmmss")); }
        }

        public List<string> LastMusicFolders
        {
            get
            {
                string val = m_Config.GetValue("LastMusicFolders", string.Empty);
                if (!string.IsNullOrEmpty(val))
                    return val.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                return new List<string>();
            }

            set
            {
                string val = string.Join(";", value);
                m_Config.SetValue("LastMusicFolders", val);
            }
        }

        public string EqualizerName
        {
            get { return m_Config.GetValue("EqualizerName", string.Empty); }
            set { m_Config.SetValue("EqualizerName", value); }
        }

        public Library.Classes.PlayerPlayList Playlist
        {
            get { return Utility.Serialization.Deserialize<Library.Classes.PlayerPlayList>(m_Config.GetValue("Playlist", string.Empty)); }
            set { m_Config.SetValue("Playlist", Utility.Serialization.Serialize(value)); }
        }

        /// <summary>
        /// Key code for the Quit command (default X)
        /// </summary>
        public int KeyCodeQuit
        {
            get { return int.Parse(m_Config.GetValue("KeyCodeQuit", "88"), CultureInfo.InvariantCulture); }
            set { m_Config.SetValue("KeyCodeQuit", value.ToString(CultureInfo.InvariantCulture)); }
        }

        /// <summary>
        /// Key code for the Player command (default P)
        /// </summary>
        public int KeyCodePlayer
        {
            get { return int.Parse(m_Config.GetValue("KeyCodePlayer", "80"), CultureInfo.InvariantCulture); }
            set { m_Config.SetValue("KeyCodePlayer", value.ToString(CultureInfo.InvariantCulture)); }
        }

        /// <summary>
        /// Key code for the Lyrics command (default L)
        /// </summary>
        public int KeyCodeLyrics
        {
            get { return int.Parse(m_Config.GetValue("KeyCodeLyrics", "76"), CultureInfo.InvariantCulture); }
            set { m_Config.SetValue("KeyCodeLyrics", value.ToString(CultureInfo.InvariantCulture)); }
        }

        /// <summary>
        /// Key code for the Repeat toggle command (default R)
        /// </summary>
        public int KeyCodeRepeat
        {
            get { return int.Parse(m_Config.GetValue("KeyCodeRepeat", "82"), CultureInfo.InvariantCulture); }
            set { m_Config.SetValue("KeyCodeRepeat", value.ToString(CultureInfo.InvariantCulture)); }
        }

        /// <summary>
        /// Key code for the Shuffle toggle command (default Z)
        /// </summary>
        public int KeyCodeShuffle
        {
            get { return int.Parse(m_Config.GetValue("KeyCodeShuffle", "90"), CultureInfo.InvariantCulture); }
            set { m_Config.SetValue("KeyCodeShuffle", value.ToString(CultureInfo.InvariantCulture)); }
        }

        /// <summary>
        /// Key code for the Equalizer toggle command (default E)
        /// </summary>
        public int KeyCodeToggleEq
        {
            get { return int.Parse(m_Config.GetValue("KeyCodeToggleEq", "69"), CultureInfo.InvariantCulture); }
            set { m_Config.SetValue("KeyCodeToggleEq", value.ToString(CultureInfo.InvariantCulture)); }
        }

        /// <summary>
        /// Key code for the next equalizer command (default N)
        /// </summary>
        public int KeyCodeNextEq
        {
            get { return int.Parse(m_Config.GetValue("KeyCodeNextEq", "78"), CultureInfo.InvariantCulture); }
            set { m_Config.SetValue("KeyCodeNextEq", value.ToString(CultureInfo.InvariantCulture)); }
        }

        /// <summary>
        /// Key code for the balance right (default H)
        /// </summary>
        public int KeyCodePanRight
        {
            get { return int.Parse(m_Config.GetValue("KeyCodePanRight", "72"), CultureInfo.InvariantCulture); }
            set { m_Config.SetValue("KeyCodePanRight", value.ToString(CultureInfo.InvariantCulture)); }
        }

        /// <summary>
        /// Key code for the balance left (default G)
        /// </summary>
        public int KeyCodePanLeft
        {
            get { return int.Parse(m_Config.GetValue("KeyCodePanLeft", "71"), CultureInfo.InvariantCulture); }
            set { m_Config.SetValue("KeyCodePanLeft", value.ToString(CultureInfo.InvariantCulture)); }
        }
        #endregion

        #region Public operations
        public bool Save()
        {
            string ser = Utility.Serialization.Serialize(m_Config);
            using (Stream stream = new FileStream(m_FileName, FileMode.Create)) {
                byte[] bytes = Encoding.UTF8.GetBytes(ser);
                stream.Write(bytes, 0, bytes.Length);
            }
            return true;
        }
        #endregion

        #region Private operations
        private DateTime GetDateTime(string name)
        {
            string v = m_Config.GetValue(name, string.Empty);
            if (string.IsNullOrEmpty(v))
                return DateTime.MinValue;

            DateTime res = DateTime.MinValue;
            if (DateTime.TryParseExact(v, "yyyyMMddTHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out res))
                return res;
            return DateTime.MinValue;
        }
        #endregion
    }
}
