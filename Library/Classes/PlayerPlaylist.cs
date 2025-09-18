using System;
using System.Collections.Generic;
using System.Globalization;

namespace Library.Classes;

/// <summary>
/// Playlist data used by the BackgroundPlayer
/// </summary>
public class PlayerPlayList
{
  public enum PlaylistType
  {
    User,
    LastAdded,
    Favorites
  }

  private List<string> m_OriginalTracks = null;

  public PlayerPlayList()
  {
    Name = string.Empty;
    Type = PlaylistType.User;
    TrackFiles = new List<string>();
  }

  public static PlayerPlayList Create(Classes.PlayList playlist)
  {
    if (playlist == null)
      return null;

    PlayerPlayList res = new PlayerPlayList();
    res.Name = playlist.Name;
    res.FileName = playlist.FileName;
    foreach (Classes.Track t in playlist.Tracks)
      res.TrackFiles.Add(t.FilePath);
    return res;
  }

  public string Name
  { get; set; }

  public PlaylistType Type
  { get; set; }

  /// <summary>
  /// Full paths of the files
  /// </summary>
  public List<string> TrackFiles
  { get; set; }

  public int CurrentIndex
  { get; set; }

  public bool IsShuffled
  {
    get { return m_OriginalTracks != null && TrackFiles != null; }
  }

  public int TracksCount
  {
    get { return TrackFiles != null ? TrackFiles.Count : 0; }
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
      return string.Format(GetDetailsText(), TrackFiles != null ? TrackFiles.Count.ToString("###,###,##0", CultureInfo.CurrentCulture) : "0");
    }
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

  public void RemoveTrack(string filePath)
  {
    if (TrackFiles != null) {
      int idx = TrackFiles.IndexOf(filePath);
      if (idx >= 0)
        TrackFiles.RemoveAt(idx);
    }
  }

  public void RemoveTrack(int index)
  {
    if (TrackFiles != null && TracksCount > index)
      TrackFiles.RemoveAt(index);
  }

  public void SetShuffle(bool shuffle)
  {
    SetShuffle(shuffle, false);
  }

  public void SetShuffle(bool shuffle, bool isPlaying)
  {
    if (TrackFiles != null) {
      if (shuffle) {
        if (!IsShuffled) {
          m_OriginalTracks = new List<string>(TrackFiles);
          string[] tempTracks = new string[m_OriginalTracks.Count];
          if (isPlaying)
            tempTracks[0] = m_OriginalTracks[CurrentIndex];

          Random rnd = new Random();
          for (int i = 0; i < m_OriginalTracks.Count; i++) {
            if (isPlaying && i == CurrentIndex)
              continue;

            while (true) {
              int pos = rnd.Next(0, TracksCount);
              if (string.IsNullOrEmpty(tempTracks[pos])) {
                tempTracks[pos] = m_OriginalTracks[i];
                break;
              }
            }
          }
          TrackFiles = new List<string>(tempTracks);
          if (isPlaying)
            CurrentIndex = 0;
        }
      } else {
        if (m_OriginalTracks != null) {
          if (isPlaying)
            CurrentIndex = m_OriginalTracks.IndexOf(TrackFiles[CurrentIndex]);
          TrackFiles = new List<string>(m_OriginalTracks);
          m_OriginalTracks = null;
        }
      }
    }
  }
} // PlayerPlayList