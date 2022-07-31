using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Library.Classes
{
  public class Album : ViewModels.ItemTypeBase
  {
    static string m_UnknownText = string.Empty;
    private string m_AlbumArt = string.Empty;
    Artist m_Artist = null;

    public Album()
    {
      Title = string.Empty;
      IsLazy = false;
    }

    public static string GetUnknownText()
    {
      if (string.IsNullOrEmpty(m_UnknownText)) {
        //Windows.ApplicationModel.Resources.ResourceLoader loader = new Windows.ApplicationModel.Resources.ResourceLoader();
        //m_UnknownText = loader.GetString("UnknownAlbum");
        m_UnknownText = "Unknown album";
      }
      return m_UnknownText;
    }

    static string m_DetailsText = string.Empty;
    public static string GetDetailsText()
    {
      if (string.IsNullOrEmpty(m_DetailsText)) {
        //Windows.ApplicationModel.Resources.ResourceLoader loader = new Windows.ApplicationModel.Resources.ResourceLoader();
        //m_DetailsText = loader.GetString("AlbumDetails");
        m_DetailsText = "{0} tracks";
      }
      return m_DetailsText;
    }

    static string m_DetailsYearText = string.Empty;
    public static string GetDetailsYearText()
    {
      if (string.IsNullOrEmpty(m_DetailsYearText)) {
        //Windows.ApplicationModel.Resources.ResourceLoader loader = new Windows.ApplicationModel.Resources.ResourceLoader();
        //m_DetailsYearText = loader.GetString("AlbumDetailsYear");
        m_DetailsYearText = "{0}, {1} tracks";
      }
      return m_DetailsYearText;
    }

    public string Key
    {
      get
      {
        return Utility.String.GetMD5Hash(string.Format("{0}_{1}", Artist != null && Artist.Name != null ? Artist.Name.ToLower() : "NoArtist", !string.IsNullOrEmpty(Title) ? Title.ToLower() : "NoTitle"));
      }
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

    public string Title
    {
      get;
      set;
    }

    public string DisplayValue
    {
      get
      {
        if (!string.IsNullOrEmpty(Title))
          return Title;
        return GetUnknownText();
      }
    }

    public string AlbumArt
    {
      get;
      set;
    }

    public long TracksCount
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

    public string Details
    {
      get
      {
        if (Year > 0)
          return string.Format(GetDetailsYearText(), Year, TracksCount);
        return string.Format(GetDetailsText(), TracksCount.ToString("###,###,##0", CultureInfo.CurrentCulture));
      }
    }

    public new string ToString()
    {
      return string.Format("{0} [{1}]", Title, Artist.Name);
    }
  }
}
