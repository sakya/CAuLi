using System;
using System.Threading;
using Utility;

namespace CAuLi.UI.Pages
{
  class LyricsPage : PageBase
  {
    Library.Classes.Track m_Track = null;
    Controls.TextBlock m_TextBlock = null;
    public LyricsPage(Screen screen) :
      base(screen)
    {

    }

    #region Overrides
    public override void Init(object parameter, GenericConfig saveState)
    {
      base.Init(parameter, saveState);
      if (parameter == null && saveState != null)
        parameter = saveState.GetValue("m_Track.FilePath", string.Empty);

      Player.Instance.TrackChanged += TrackChanged;

      m_TextBlock = new Controls.TextBlock()
      {
        X = 0,
        Y = 3,
        Text = "Getting lyrics...",
        HasFocus = true
      };

      m_Screen.Controls.Add(m_TextBlock);

      OnSizeChanged(m_Screen, m_Screen.Width, m_Screen.Height);

      if (parameter != null) {
        m_Track = Library.Library.Instance.GetTrack(parameter as string).Result;
        ThreadPool.QueueUserWorkItem(GetLyricsThread);
      }
      DrawTitle();
    }

    public override GenericConfig GetSaveState()
    {
      GenericConfig ss = new GenericConfig();
      ss.SetValue("m_Track.FilePath", m_Track != null ? m_Track.FilePath : string.Empty);
      return ss;
    } // GetSaveState

    public override void Dispose()
    {
      base.Dispose();

      Player.Instance.TrackChanged -= TrackChanged;
    }

    public override bool KeyPress(ConsoleKeyInfo keyPress)
    {
      if (base.KeyPress(keyPress))
        return true;

      if (keyPress.Key == ConsoleKey.Escape) {
        // ESCAPE: go back
        ReturnValue = new PageReturnValue();
        return true;
      }

      return false;
    }

    protected override void OnSizeChanged(Screen sender, int width, int height)
    {
      base.OnSizeChanged(sender, width, height);
      if (!m_Screen.ValidSize)
        return;

      m_TextBlock.Width = m_Screen.Width;
      m_TextBlock.Height = m_Screen.Height - m_TextBlock.Y - 1;

      m_Screen.Clear();
      m_Screen.Draw();
      DrawTitle();
    }
    #endregion

    #region Private operations
    private void TrackChanged(Player sender, string fileName, int playlystCount, int playlistCurrentIndex)
    {
      m_Track = Library.Library.Instance.GetTrack(fileName).Result;
      ThreadPool.QueueUserWorkItem(GetLyricsThread);
      DrawTitle();
    }

    private void DrawTitle()
    {
      // Title
      m_Screen.WriteString(0, 1, m_Screen.Width, ColorTheme.Instance.BackgroundTitle, ColorTheme.Instance.ForegroundTitle, m_Track != null ? m_Track.Title.PadRight(m_Screen.Width) : "No title".PadRight(m_Screen.Width));
      m_Screen.WriteString(0, 2, m_Screen.Width, ColorTheme.Instance.BackgroundTitle, ColorTheme.Instance.Foreground, m_Track != null ? m_Track.Artist.Name.PadRight(m_Screen.Width) : "No artist".PadRight(m_Screen.Width));
    }

    private async void GetLyricsThread(object state)
    {
      if (m_Track != null) {
        string lyrics = Library.Library.Instance.GetLyrics(m_Track.FilePath);
        if (string.IsNullOrEmpty(lyrics))
          lyrics = await Utility.Lyrics.GetLyrics(m_Track.Artist.Name, m_Track.Title);
        if (!string.IsNullOrEmpty(lyrics)) {
          // Remove starting newlines:
          while (lyrics.StartsWith("\r") || lyrics.StartsWith("\n"))
            lyrics = lyrics.Remove(0, 1);
          while (lyrics.EndsWith("\r") || lyrics.EndsWith("\n"))
            lyrics = lyrics.Remove(lyrics.Length - 1, 1);
          m_TextBlock.Text = lyrics;
        }else {
          m_TextBlock.Text = "No lyrics found";
        }
        m_TextBlock.FirstLine = 0;
        m_TextBlock.NeedsRedraw = true;
      }else {
        m_TextBlock.Text = "No lyrics found";
        m_TextBlock.NeedsRedraw = true;
      }
      m_Screen.Draw();
      DrawTitle();
    }
    #endregion
  }
}
