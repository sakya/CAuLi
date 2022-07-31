using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Library.Classes
{
  public class Genre : ViewModels.ItemTypeBase
  {
    public Genre()
    {

    }

    static string m_UnknownText = string.Empty;
    public static string GetUnknownText()
    {
      if (string.IsNullOrEmpty(m_UnknownText)) {
        //Windows.ApplicationModel.Resources.ResourceLoader loader = new Windows.ApplicationModel.Resources.ResourceLoader();
        //m_UnknownText = loader.GetString("UnknownGenre");
        m_UnknownText = "Unknown genre";
      }
      return m_UnknownText;
    }

    static string m_DetailsText = string.Empty;
    public static string GetDetailsText()
    {
      if (string.IsNullOrEmpty(m_DetailsText)) {
        //Windows.ApplicationModel.Resources.ResourceLoader loader = new Windows.ApplicationModel.Resources.ResourceLoader();
        //m_DetailsText = loader.GetString("GenreDetails");
        m_DetailsText = "{0} tracks";
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

    public string Details
    {
      get
      {
        return string.Format(GetDetailsText(), TracksCount.ToString("###,###,##0", CultureInfo.CurrentCulture));
      }
    }

    public string Key
    {
      get { 
        return Utility.String.GetMD5Hash(!string.IsNullOrEmpty(Name) ? Name.ToLower() : "NoGenre");
      }
    }

    public long TracksCount
    {
      get;
      set;
    }

    public new string ToString()
    {
      return Name;
    }

  }
}
