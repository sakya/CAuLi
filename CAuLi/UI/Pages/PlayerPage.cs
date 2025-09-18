using System;
using System.Threading;
using Library.Classes;
using Utility;
using static CAuLi.Player;

namespace CAuLi.UI.Pages
{
  class PlayerPage : PageBase
  {
    Timer m_Timer = null;
    Controls.MenuControl m_PLaylist = null;
    Controls.ProgressBar m_ProgressBar = null;
    string m_LastPos = string.Empty;

    public PlayerPage(Screen screen) :
      base(screen)
    {

    }

    #region Overrides
    public override void Init(object parameter, GenericConfig saveState)
    {
      base.Init(parameter, saveState);

      Player.Instance.EqualizerChanged += EqChanged;
      Player.Instance.TrackChanged += TrackChanged;
      Player.Instance.PlayingStatusChanged += StatusChanged;
      Player.Instance.ShuffleChanged += ShuffleChanged;
      Player.Instance.RepeatChanged += RepeatChanged;

      m_ProgressBar = new Controls.ProgressBar()
      {
        X = 0,
        Y = 6,
        Minimum = 0,
      };
      m_Screen.Controls.Add(m_ProgressBar);

      m_PLaylist = new Controls.MenuControl()
      {
        X = 0,
        Y = 9,
        Title = "Playlist"
      };
      m_Screen.Controls.Add(m_PLaylist);

      m_PLaylist.ItemTriggered += (s, eArgs) =>
      {
        Library.Classes.Track t = eArgs.Tag as Library.Classes.Track;
        int index = m_PLaylist.Items.IndexOf(eArgs);
        if (index >= 0)
          Player.Instance.Play(index);
      };

      OnSizeChanged(m_Screen, m_Screen.Width, m_Screen.Height);
    }

    public override void Dispose()
    {
      base.Dispose();

      Player.Instance.EqualizerChanged -= EqChanged;
      Player.Instance.TrackChanged -= TrackChanged;
      Player.Instance.PlayingStatusChanged -= StatusChanged;
      Player.Instance.ShuffleChanged -= ShuffleChanged;
      Player.Instance.RepeatChanged -= RepeatChanged;

      if (m_Timer != null) {
        m_Timer.Dispose();
        m_Timer = null;
      }
    }

    protected override void OnSizeChanged(Screen sender, int width, int height)
    {
      base.OnSizeChanged(sender, width, height);
      if (!m_Screen.ValidSize)
        return;

      m_ProgressBar.Width = m_Screen.Width;

      m_PLaylist.Width = m_Screen.Width;
      m_PLaylist.Height = m_Screen.Height - m_PLaylist.Y - 1;

      m_Screen.Clear();
      m_Screen.Draw();

      Update();
    }

    public override bool KeyPress(ConsoleKeyInfo keyPress)
    {
      if (base.KeyPress(keyPress))
        return true;

      if ((int)keyPress.Key == AppSettings.Instance.KeyCodeLyrics && (keyPress.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control) {
        // CTRL-L: Lyrics
        PlayerStatus status = Player.Instance.GetStatus();
        if (!string.IsNullOrEmpty(status.Track))
          ReturnValue = new PageReturnValue() { NavigateTo = typeof(LyricsPage), Parameter = status.Track };
        return true;
      } else if (keyPress.Key == ConsoleKey.RightArrow && (keyPress.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control) {
        // FFWD
        Player.Instance.TrackPosition = Player.Instance.TrackPosition + 3;
        return true;
      } else if (keyPress.Key == ConsoleKey.LeftArrow && (keyPress.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control) {
        // REW
        Player.Instance.TrackPosition = Player.Instance.TrackPosition - 4;
        return true;
      } else if (keyPress.Key == ConsoleKey.Escape) {
        // ESCAPE: go back
        ReturnValue = new PageReturnValue();
        return true;
      }

      return false;
    }
    #endregion

    #region Private operations
    private string FormatTrack(UI.Controls.MenuControl menu, UI.Controls.MenuControl.MenuItem item)
    {
      if (item.Tag is Track track) {
        var time = track.Duration.ToString(@"mm\:ss");
        var res = UI.Screen.ElideString(item.Text, menu.Width - 6);
        return string.Format("{0}{1}{2}", res, new string(' ', menu.Width - res.Length - time.Length), time);
      }
      return item.Text;
    }

    private void EqChanged(Player sender, string eqName)
    {
      UpdateEq(eqName);
    }

    private void UpdateEq(string eqName)
    {
      m_Screen.WriteString(m_Screen.Width - 20, 1, m_Screen.Width, UI.ColorTheme.Instance.Background, ColorTheme.Instance.AccentColor, "Eq     :");
      if (string.IsNullOrEmpty(eqName))
        m_Screen.WriteString(m_Screen.Width - 11, 1, 11, UI.ColorTheme.Instance.Background, ColorTheme.Instance.Foreground, "None".PadRight(11));
      else
        m_Screen.WriteString(m_Screen.Width - 11, 1, 11, UI.ColorTheme.Instance.Background, ColorTheme.Instance.Foreground, eqName.PadRight(11));
    }

    private void TrackChanged(Player sender, string fileName, int playlystCount, int playlistCurrentIndex)
    {
      Update();
    }

    private void Update()
    {
      PlayerStatus status = Player.Instance.GetStatus();

      StatusChanged(Player.Instance, status.Status);
      Library.Classes.Track track = Library.Library.Instance.GetTrack(status.Track).Result;
      m_Screen.WriteString(0, 1, m_Screen.Width, ColorTheme.Instance.Background, ColorTheme.Instance.AccentColor, "Track :");
      m_Screen.WriteString(0, 2, m_Screen.Width, ColorTheme.Instance.Background, ColorTheme.Instance.AccentColor, "Title :");
      m_Screen.WriteString(0, 3, m_Screen.Width, ColorTheme.Instance.Background, ColorTheme.Instance.AccentColor, "Album :");
      m_Screen.WriteString(0, 4, m_Screen.Width, ColorTheme.Instance.Background, ColorTheme.Instance.AccentColor, "Artist:");

      UpdateEq(status.EqualizerEnabled ? status.EqualizerName : "None");

      // Shuffle:
      m_Screen.WriteString(m_Screen.Width - 20, 2, m_Screen.Width, ColorTheme.Instance.Background, ColorTheme.Instance.AccentColor, "Shuffle:");
      m_Screen.WriteString(m_Screen.Width - 11, 2, 11, ColorTheme.Instance.Background, ColorTheme.Instance.Foreground, Player.Instance.Shuffle ? "ON " : "OFF");

      // Repeat:
      m_Screen.WriteString(m_Screen.Width - 20, 3, m_Screen.Width, ColorTheme.Instance.Background, ColorTheme.Instance.AccentColor, "Repeat :");
      if (Player.Instance.Repeat == RepeatType.None)
        m_Screen.WriteString(m_Screen.Width - 11, 3, 11, ColorTheme.Instance.Background, ColorTheme.Instance.Foreground, "OFF");
      else if (Player.Instance.Repeat == RepeatType.One)
        m_Screen.WriteString(m_Screen.Width - 11, 3, 11, ColorTheme.Instance.Background, ColorTheme.Instance.Foreground, "One");
      else if (Player.Instance.Repeat == RepeatType.All)
        m_Screen.WriteString(m_Screen.Width - 11, 3, 11, ColorTheme.Instance.Background, ColorTheme.Instance.Foreground, "All");

      // Track info:
      if (track != null) {
        m_ProgressBar.Maximum = track.Duration.TotalSeconds;

        if (status.PlayList != null)
          m_Screen.WriteString(8, 1, m_Screen.Width - 8 - 21, ColorTheme.Instance.Background, ColorTheme.Instance.Foreground,
            string.Format("{0}/{1}", status.PlayList.CurrentIndex + 1, status.PlayList.TracksCount).PadRight(m_Screen.Width - 8 - 21));
        else
          m_Screen.WriteString(8, 1, m_Screen.Width - 8 - 21, ColorTheme.Instance.Background, ColorTheme.Instance.ForegroundHighlight, "-".PadRight(m_Screen.Width - 8 - 21));
        m_Screen.WriteString(8, 2, m_Screen.Width - 8 - 21, ColorTheme.Instance.Background, ColorTheme.Instance.ForegroundHighlight, track.Title.PadRight(m_Screen.Width - 8 - 21));
        if (track.Album.Year > 0)
          m_Screen.WriteString(8, 3, m_Screen.Width - 8 - 21, ColorTheme.Instance.Background, ColorTheme.Instance.Foreground, string.Format("{0} ({1})", track.Album.Title, track.Album.Year).PadRight(m_Screen.Width - 8 - 21));
        else
          m_Screen.WriteString(8, 3, m_Screen.Width - 8 - 21, ColorTheme.Instance.Background, ColorTheme.Instance.Foreground, track.Album.Title.PadRight(m_Screen.Width - 8 - 21));
        m_Screen.WriteString(8, 4, m_Screen.Width - 8, ColorTheme.Instance.Background, ColorTheme.Instance.Foreground, track.Artist.Name.PadRight(m_Screen.Width - 8));
      }else{
        m_Screen.WriteString(8, 1, m_Screen.Width - 8 - 21, ColorTheme.Instance.Background, ColorTheme.Instance.Foreground, "".PadRight(m_Screen.Width - 8 - 21));
        m_Screen.WriteString(8, 2, m_Screen.Width - 8 - 21, ColorTheme.Instance.Background, ColorTheme.Instance.Foreground, "".PadRight(m_Screen.Width - 8 - 21));
        m_Screen.WriteString(8, 3, m_Screen.Width - 8 - 21, ColorTheme.Instance.Background, ColorTheme.Instance.Foreground, "".PadRight(m_Screen.Width - 8 - 21));
        m_Screen.WriteString(8, 4, m_Screen.Width - 8, ColorTheme.Instance.Background, ColorTheme.Instance.Foreground, "".PadRight(m_Screen.Width - 8));
      }

      if (status.PlayList != null) {
        Library.Classes.PlayList pl = Library.Classes.PlayList.Create(status.PlayList);

        m_PLaylist.Items.Clear();
        int idx = 0;
        foreach (Library.Classes.Track t in pl.Tracks) {
          Controls.MenuControl.MenuItem mi = new Controls.MenuControl.MenuItem()
          {
            Tag = t,
            Text = string.Format("{0}{1}. {2} - {3}",
              t.Id == track.Id ? "»" : " ",
              idx + 1,
              t.Artist.Name,
              t.Title)
          };

          mi.FormatText = new Controls.MenuControl.MenuItem.FormatTextHandler(FormatTrack);
          m_PLaylist.Items.Add(mi);
          idx++;
        }
        m_PLaylist.NeedsRedraw = true;
      }

      m_LastPos = string.Empty;

      if (status.Status != PlayingStatus.Play)
        TimerCallback(null);

      m_Screen.Draw();
    }

    private void TimerCallback(Object state)
    {
      TimeSpan pos = TimeSpan.FromSeconds(Player.Instance.TrackPosition);
      TimeSpan length = TimeSpan.FromSeconds(Player.Instance.Length);
      TimeSpan left = length - pos;

      m_ProgressBar.Value = pos.TotalSeconds;
      if (m_ProgressBar.NeedsRedraw)
        m_ProgressBar.Draw(m_Screen);

      string sPos = pos.ToString(@"mm\:ss");
      if (string.Compare(sPos, m_LastPos) != 0) {
        m_LastPos = sPos;
        m_Screen.WriteString(0, 7, 5, ColorTheme.Instance.Background, ColorTheme.Instance.Foreground, sPos);
        m_Screen.WriteString(m_Screen.Width - 5, 7, 5, ColorTheme.Instance.Background, ColorTheme.Instance.Foreground, left.ToString(@"mm\:ss"));
      }
    }

    private void StatusChanged(Player sender, PlayingStatus status)
    {
      if (status == Player.PlayingStatus.Play) {
        if (m_Timer == null)
          m_Timer = new Timer(TimerCallback, null, 0, 200);
      } else {
        if (m_Timer != null) {
          m_Timer.Dispose();
          m_Timer = null;
        }
      }
    }

    private void RepeatChanged(Player sender, Player.RepeatType repeat)
    {
      Update();
    }

    private void ShuffleChanged(Player sender, bool shuffle)
    {
      Update();
    }
    #endregion
  }
}
