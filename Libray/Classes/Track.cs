using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Library.Classes
{
  public class Track : ViewModels.ItemTypeBase
  {
    static string m_UnknownText = string.Empty;
    Album m_Album = null;
    Artist m_Artist = null;
    List<Genre> m_Genres = null;

    public Track()
    {
      Id = 0;
      IsTemp = false;
      PlayedCount = 0;
    }

    public static string GetUnknownText()
    {
      if (string.IsNullOrEmpty(m_UnknownText)) {
        //Windows.ApplicationModel.Resources.ResourceLoader loader = new Windows.ApplicationModel.Resources.ResourceLoader();
        //m_UnknownText = loader.GetString("UnknownTrack");
        m_UnknownText = "Unknown title";
      }
      return m_UnknownText;
    }

    public bool IsTemp
    {
      get;
      set;
    }

    /// <summary>
    /// OneDrive File Id
    /// </summary>
    public string FileId
    {
      get;
      set;
    }

    /// <summary>
    /// Full file path
    /// </summary>
    public string FilePath
    {
      get;
      set;
    }

    public string FolderPath
    {
      get;
      set;
    }

    public long FileSize
    {
      get;
      set;
    }

    public DateTime? LastModifed
    {
      get;
      set;
    }

    public string MimeType
    {
      get;
      set;
    }

    public Album Album
    {
      get {
        if (m_Album != null && m_Album.IsLazy) { 
          m_Album = Library.Instance.GetAlbum(m_Album.Id).Result;
          if (m_Album == null)
            m_Album = new Album();
        }
        return m_Album; 
      }
      set { m_Album = value; }
    }

    public Artist Artist
    {
      get {
        if (m_Artist != null && m_Artist.IsLazy) { 
          m_Artist = Library.Instance.GetArtist(m_Artist.Id).Result;
          if (m_Artist == null)
            m_Artist = new Artist();
        }
        return m_Artist; 
      }
      set { m_Artist = value; }
    }
    public List<Genre> Genres
    {
      get {  
        if (m_Genres == null) {
          var g = ViewModels.TracksViewModel.Instance.LoadGenres(this).Result;
        }
        return m_Genres; 
      }
      set { m_Genres = value; }
    }

    public string Title
    {
      get;
      set;
    }

    public string DisplayValue
    {
      get
      {
        if (!string.IsNullOrEmpty(Title)) {
#if DEBUG
          return Title;
          //return string.Format("[{1}]{0}", Title, PlayedCount);
#else
          return Title;
#endif
        }
        return GetUnknownText();
      }
    }

    public uint DiscNumber
    {
      get;
      set;
    }

    public uint TrackNumber
    {
      get;
      set;
    }

    public TimeSpan Duration
    {
      get;
      set;
    }

    public uint Year
    {
      get;
      set;
    }

    public uint Rating 
    { 
      get; 
      set; 
    }

    public DateTime? LastPlayed
    {
      get;
      set;
    }

    public long PlayedCount
    {
      get;
      set;
    }

    public bool IsOneDriveFile
    {
      get
      {
        return !string.IsNullOrEmpty(FileId);
      }
    }

    //public string OneDriveFilePath
    //{
    //  get
    //  {
    //    if (IsOneDriveFile)
    //      return string.Format("{0}{1}", OneDriveRest.OneDriveAPI.UriScheme, FileId);
    //    return string.Empty;
    //  }
    //}

    public string AlbumArtFileName
    {
      get
      {
        if (Album != null) { 
          if (!string.IsNullOrEmpty(Album.AlbumArt))
            return Album.AlbumArt;
          return string.Format("{0}.jpg", Album.Key);
        }
        return string.Empty;
      }
    }

    public string Details
    {
      get
      {
        if (Album != null && Artist != null)
          return string.Format("{0} - {1}", Artist.DisplayValue, Album.DisplayValue);
        if (Album != null)
          return Album.DisplayValue;
        return string.Empty;
      }
    }

    public bool IsFromFile
    {
      get
      {
        return Id == 0;
      }
    }

    public new string ToString()
    {
      return string.Format("{0} [{1}]", Title, Album.Title);
    }

  } // Track
}
