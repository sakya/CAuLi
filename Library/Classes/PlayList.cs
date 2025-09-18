using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Library.Classes;

public class PlayList
{
  public PlayList()
  {
    Tracks = new List<Track>();
    Name = string.Empty;
  }

  public static PlayList Create(PlayerPlayList playlist)
  {
    if (playlist == null)
      return null;
    PlayList res = new PlayList();
    res.Name = playlist.Name;
    res.FileName = playlist.FileName;
    var aRes = res.AddTracks(playlist.TrackFiles).Result;
    return res;
  }

  static string m_DetailsText = string.Empty;
  public static string GetDetailsText()
  {
    if (string.IsNullOrEmpty(m_DetailsText)) {
      m_DetailsText = "{0} tracks, running time {1}";
    }
    return m_DetailsText;
  }

  public string Name
  {
    get;
    set;
  }

  public int StartIndex
  {
    get;
    set;
  }

  public List<Track> Tracks
  {
    get;
    set;
  }

  public string FileName
  {
    get;
    set;
  }

  public string Details
  {
    get
    {
      return string.Format(GetDetailsText(), Tracks != null ? Tracks.Count.ToString("###,###,##0", CultureInfo.CurrentCulture) : "0",
        Duration);
    }
  }

  public async Task<bool> AddTracks(List<string> filePaths)
  {
    if (filePaths != null) {
      foreach (string path in filePaths) {
        Classes.Track track = await Library.Instance.GetTrack(path);
        if (track == null) {
          try {
            track = Library.ParseFileMinimum(path);
          } catch (Exception) {

          }
        }
        if (track != null)
          Tracks.Add(track);
      }
    }
    return true;
  }

  public TimeSpan Duration
  {
    get
    {
      TimeSpan res = new TimeSpan();
      if (Tracks != null) {
        foreach (Track t in Tracks)
          res = res.Add(t.Duration);
      }
      return res;
    }
  }

  public int GetTrackIndex(Classes.Track track)
  {
    int i = 0;
    foreach (Classes.Track t in Tracks) {
      if (t.Id == track.Id)
        return i;
      i++;
    }

    return -1;
  }

  public void RemoveTrack(Classes.Track track)
  {
    Classes.Track tr = null;
    foreach (Classes.Track t in Tracks) {
      if (t.Id == track.Id) {
        tr = t;
        break;
      }
    }

    if (tr != null)
      Tracks.Remove(tr);
  }

  public void SetRandomStart()
  {
    if (Tracks != null && Tracks.Count > 0)
      StartIndex = new Random().Next(0, Tracks.Count);
  }

  //public string Serialize()
  //{
  //  string res = string.Empty;
  //  using (MemoryStream stream = new MemoryStream()) {
  //    DataContractJsonSerializer jsonSer = new DataContractJsonSerializer(typeof(PlayList));
  //    jsonSer.WriteObject(stream, this);
  //    stream.Position = 0;

  //    using (StreamReader sr = new StreamReader(stream))
  //      res = sr.ReadToEnd();
  //  }
  //  return res;
  //} // Serialize

  //public static PlayList Deserialize(string value)
  //{
  //  DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(PlayList));
  //  byte[] bytes = Encoding.UTF8.GetBytes(value);

  //  PlayList notifyData;
  //  using (MemoryStream memoryStream = new MemoryStream(bytes)) {
  //    try {
  //      notifyData = (PlayList)serializer.ReadObject(memoryStream);
  //    } catch (Exception ex) {
  //      System.Diagnostics.Debug.WriteLine(string.Format("Error deserializing: {0}", ex.Message));
  //      notifyData = default(PlayList);
  //    }
  //  }
  //  return notifyData;
  //} // Deserialize
}