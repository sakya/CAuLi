using System.Globalization;

namespace Library.Classes;

public class Artist : ViewModels.ItemTypeBase
{
  public Artist()
  {
    IsLazy = false;
  }

  static string m_UnknownText = string.Empty;
  public static string GetUnknownText()
  {
    if (string.IsNullOrEmpty(m_UnknownText)) {
      //Windows.ApplicationModel.Resources.ResourceLoader loader = new Windows.ApplicationModel.Resources.ResourceLoader();
      //m_UnknownText = loader.GetString("UnknownArtist");
      m_UnknownText = "Unknown artist";
    }
    return m_UnknownText;
  }

  static string m_DetailsText = string.Empty;
  public static string GetDetailsText()
  {
    if (string.IsNullOrEmpty(m_DetailsText)) {
      //Windows.ApplicationModel.Resources.ResourceLoader loader = new Windows.ApplicationModel.Resources.ResourceLoader();
      //m_DetailsText = loader.GetString("ArtistDetails");
      m_DetailsText = "{0} albums, {1} tracks";
    }
    return m_DetailsText;
  }

  public string Name
  {
    get;
    set;
  }

  public string DisplayValue
  {
    get
    {
      if (!string.IsNullOrEmpty(Name))
        return Name;
      return GetUnknownText();
    }
  }

  public string ArtistArt
  {
    get;
    set;
  }

  public string Key
  {
    get {
      return Utility.String.GetMD5Hash(!string.IsNullOrEmpty(Name) ? Name.ToLower() : "NoArtist");
    }
  }

  public long TracksCount
  {
    get;
    set;
  }

  public long AlbumsCount
  {
    get;
    set;
  }

  public string Details
  {
    get
    {
      return string.Format(GetDetailsText(), AlbumsCount, TracksCount.ToString("###,###,##0", CultureInfo.CurrentCulture));
    }
  }

  public new string ToString()
  {
    return Name;
  }
}