using CAuLi.UI.Controls;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace CAuLi.UI;

class Screen : IDisposable
{
  static Screen m_Instance = null;
  Timer m_ResizeTimer = null;
  Timer m_MusicTimer = null;
  Mutex m_Mutex = new Mutex();
  bool m_ChangingSize = false;

  string m_LastMusicTime = string.Empty;

  public delegate void SizeChangedHandler(Screen sender, int width, int height);
  public SizeChangedHandler SizeChanged;

  public enum WindowStates
  {
    Maximized,
    Minimized,
    Normal
  }
  public delegate void WindowStateChangedHandler(Screen sender, WindowStates newState);

  public Screen()
  {
    Controls = new List<ControlBase>();
  }

  public void Dispose()
  {
    Clear();
    if (m_ResizeTimer != null) {
      m_ResizeTimer.Dispose();
      m_ResizeTimer = null;
    }
    Player.Instance.TrackChanged -= OnTrackChanged;
    Player.Instance.PlayingStatusChanged -= OnPlayingStatusChanged;
    Player.Instance.RepeatChanged -= OnRepeatChanged;
    Player.Instance.ShuffleChanged -= OnShuffleChanged;
    Player.Instance.VolumeChanged -= OnVolumeChanged;
    Player.Instance.PanningChanged -= OnPanningChanged;

    Library.Library.Instance.ScanStarted -= OnLibraryScanStart;
    Library.Library.Instance.ScanFinished -= OnLibraryScanFinish;

    m_Mutex.Dispose();
  }

  public static Screen Instance
  {
    get
    {
      lock (typeof(Screen)) {
        if (m_Instance == null) {
          m_Instance = new Screen();
          //m_Instance.Init();
        }
        return m_Instance;
      }
    }
  }

  public int MinimumWidth
  {
    get; private set;
  }

  public int MinimumHeight
  {
    get; private set;
  }

  public int Width
  {
    get; private set;
  }

  public int Height
  {
    get; private set;
  }

  public bool ValidSize
  {
    get { return Width >= MinimumWidth && Height >= MinimumHeight; }
  }

  public List<ControlBase> Controls
  {
    get; set;
  }

  public void Init(int width, int height, int minimumWidth, int minimumHeight)
  {
    if (OperatingSystem.IsWindows() && width > 0 && height > 0) {
      Console.SetWindowSize(width, height);
      Console.SetBufferSize(width, height);
    }
    Width = Console.WindowWidth;
    Height = Console.WindowHeight;
    MinimumWidth = minimumWidth;
    MinimumHeight = minimumHeight;
    Clear();

    if (m_ResizeTimer == null)
      m_ResizeTimer = new Timer(ResizeTimerCallback, null, 0, 200);

    Console.CursorVisible = false;

    if (Player.Instance != null) {
      Player.Instance.TrackChanged += OnTrackChanged;
      Player.Instance.PlayingStatusChanged += OnPlayingStatusChanged;
      Player.Instance.RepeatChanged += OnRepeatChanged;
      Player.Instance.ShuffleChanged += OnShuffleChanged;
      Player.Instance.VolumeChanged += OnVolumeChanged;
      Player.Instance.PanningChanged += OnPanningChanged;

      Library.Library.Instance.ScanStarted += OnLibraryScanStart;
      Library.Library.Instance.ScanFinished += OnLibraryScanFinish;
    }
  } // Init

  private void OnLibraryScanFinish(object sender, EventArgs e)
  {
    DrawTitleBar();
  }

  private void OnLibraryScanStart(object sender, EventArgs e)
  {
    DrawTitleBar();
  }

  private void OnTrackChanged(Player sender, string fileName, int playlystCount, int playlistCurrentIndex)
  {
    DrawStatusBar();
  }

  private void OnRepeatChanged(Player sender, Player.RepeatType type)
  {
    DrawTitleBar();
  }

  private void OnPlayingStatusChanged(Player sender, Player.PlayingStatus status)
  {
    DrawStatusBar();
    if (status == Player.PlayingStatus.Play) {
      if (m_MusicTimer == null) {
        m_LastMusicTime = string.Empty;
        m_MusicTimer = new Timer(MusicTimerCallback, null, 0, 200);
      }
    } else {
      if (m_MusicTimer != null) {
        m_MusicTimer.Dispose();
        m_MusicTimer = null;
      }
    }
  }

  private void OnShuffleChanged(Player sender, bool shuffle)
  {
    DrawTitleBar();
  }

  private void OnVolumeChanged(Player sender, float volume)
  {
    DrawTitleBar();
  }

  private void OnPanningChanged(Player sender, float panning)
  {
    DrawTitleBar();
  }

  public void Clear(int x, int y, int width, int height)
  {
    Clear(x, y, width, height, ColorTheme.Instance.Background);
  }

  public void Clear(int x, int y, int width, int height, ConsoleColor color)
  {
    var line = new string(' ', width);
    for (int row = y; row < y + height; row++) {
      WriteString(x, row, width, color, ColorTheme.Instance.Foreground, line);
    }
  }

  public void Clear()
  {
    m_Mutex.WaitOne();
    Console.ForegroundColor = ColorTheme.Instance.Foreground;
    Console.BackgroundColor = ColorTheme.Instance.Background;

    var line = new string(' ', Width);
    for (int row = 0; row < Height; row++) {
      Console.SetCursorPosition(0, row);
      Console.Write(line);
    }
    Console.SetCursorPosition(0, 0);
    m_LastMusicTime = string.Empty;
    m_Mutex.ReleaseMutex();
  }

  public void Draw()
  {
    DrawTitleBar();

    foreach (ControlBase c in Controls)
      c.Draw(this);

    DrawStatusBar();
  } // Draw

  private void DrawTitleBar()
  {
    WriteString(0, 0, Width, ColorTheme.Instance.AccentColor, ColorTheme.Instance.OverAccentColor, new string(' ', Width));
    WriteString(0, 0, Width, ColorTheme.Instance.AccentColor, ColorTheme.Instance.OverAccentColor, string.Format("CAuLi v.{0}", Assembly.GetEntryAssembly().GetName().Version.ToString()));
    WriteString(Width / 2 - 3, 0, Width, ColorTheme.Instance.AccentColor, ColorTheme.Instance.OverAccentColor, "F1=Help");

    // Update panning:
    WriteString(Width - 18, 0, 10, ColorTheme.Instance.AccentColor, ColorTheme.Instance.OverAccentColor, string.Format("Bal.:{0}", (Math.Round(Player.Instance.Panning, 1)).ToString("+0.0;-0.0; 0.0").PadLeft(3)).PadRight(8));

    // Update volume:
    WriteString(Width - 8, 0, 8, ColorTheme.Instance.AccentColor, ColorTheme.Instance.OverAccentColor, string.Format("Vol:{0}%", (Math.Round(Player.Instance.Volume * 100.0)).ToString().PadLeft(3)).PadRight(8));

    // Library scan indicator:
    if (Library.Library.Instance.IsUpdating)
      WriteString(Width - 20, 0, 1, ColorTheme.Instance.AccentColor, ConsoleColor.Yellow, "U");
    else
      WriteString(Width - 20, 0, 1, ColorTheme.Instance.AccentColor, ConsoleColor.Yellow, " ");

    // Repeat indicator:
    if (Player.Instance.Repeat == Player.RepeatType.One)
      WriteString(Width - 23, 0, 2, ColorTheme.Instance.AccentColor, ConsoleColor.Yellow, "RO");
    else if (Player.Instance.Repeat == Player.RepeatType.All)
      WriteString(Width - 23, 0, 2, ColorTheme.Instance.AccentColor, ConsoleColor.Yellow, "RA");

    // Shuffle indicator:
    if (Player.Instance.Shuffle)
      WriteString(Width - 25, 0, 1, ColorTheme.Instance.AccentColor, ConsoleColor.Yellow, "S");
  } // DrawTitleBar

  private void DrawStatusBar()
  {
    WriteString(0, Height - 1, Width, ColorTheme.Instance.AccentColor, ColorTheme.Instance.OverAccentColor, new string(' ', Width));

    if (Player.Instance != null) {
      PlayerStatus status = Player.Instance.GetStatus();

      string sStatus = string.Empty;
      if (status.Status == Player.PlayingStatus.Play)
        sStatus = " PLAY";
      else if (status.Status == Player.PlayingStatus.Pause)
        sStatus = "PAUSE";
      else
        sStatus = "     ";

      int length = Width - 18;
      Library.Classes.Track track = Library.Library.Instance.GetTrack(status.Track).Result;
      if (track != null) {
        WriteString(0, Height - 1, length, ColorTheme.Instance.AccentColor, ColorTheme.Instance.OverAccentColor,
          new string(' ', length));
        WriteString(0, Height - 1, length, ColorTheme.Instance.AccentColor, ColorTheme.Instance.OverAccentColor,
          string.Format("[{0}/{1}]{2} - {3}", status.PlayList.CurrentIndex + 1, status.PlayList.TracksCount, track.Artist.Name, track.Title));

        WriteString(Width - 5, Height - 1, 5, ColorTheme.Instance.AccentColor, ColorTheme.Instance.OverAccentColor, sStatus);

        m_LastMusicTime = string.Empty;
        MusicTimerCallback(null);
      }

      // Window's title:
      if (track != null)
        Console.Title = string.Format("{0} [{1} - {2}]", Assembly.GetExecutingAssembly().GetName().Name, track.Title, track.Artist.Name);
      else
        Console.Title = string.Format("{0}", Assembly.GetExecutingAssembly().GetName().Name);
    }
  } // DrawStatusBar

  public void WriteString(int x, int y, int width, ConsoleColor background, ConsoleColor foreground, string str)
  {
    if (x < 0 || y < 0 || x >= Width || y >= Height || string.IsNullOrEmpty(str))
      return;

    m_Mutex.WaitOne();

    str = ElideString(str, width);
    try {
      Console.SetCursorPosition(x, y);
      Console.BackgroundColor = background;
      Console.ForegroundColor = foreground;
      if (y == Height - 1 && x + str.Length == Width && OperatingSystem.IsWindows()) {
        // Workaround for the last character:
        Console.Write(str[str.Length - 1]);
        Console.MoveBufferArea(x, y, 1, 1, Width - 1, y);
        Console.SetCursorPosition(x, y);
        Console.Write(str.Substring(0, str.Length - 1));
      } else
        Console.Write(str);
    }catch(Exception ex) {
      System.Diagnostics.Debug.WriteLine("WriteString: {0}", ex.Message);
    }
    Console.ResetColor();

    m_Mutex.ReleaseMutex();
  } // WriteString

  public static string ElideString(string str, int width)
  {
    string res = str;
    if (width > 0 && str.Length > width)
      res = string.Format("{0}...", str.Substring(0, width - 3));
    return res;
  } // ElideString

  public void FocusPreviousControl()
  {
    // Get focusable controls:
    List<ControlBase> fCtrls = new List<ControlBase>();
    if (Controls != null) {
      foreach (ControlBase c in Controls) {
        if (c.Focusable)
          fCtrls.Add(c);
      }
    }

    if (fCtrls.Count == 0)
      return;

    ControlBase focused = null;
    foreach (ControlBase c in fCtrls) {
      if (c.HasFocus) {
        focused = c;
        break;
      }
    }

    if (focused == null) {
      fCtrls[0].HasFocus = true;
      fCtrls[0].NeedsRedraw = true;
      return;
    }

    int idx = fCtrls.IndexOf(focused);
    if (idx > 0) {
      fCtrls[idx - 1].HasFocus = true;
      fCtrls[idx - 1].NeedsRedraw = true;

      focused.NeedsRedraw = true;
      focused.HasFocus = false;
    } else {
      fCtrls[0].HasFocus = true;
      fCtrls[0].NeedsRedraw = true;

      if (focused != fCtrls[0]) {
        focused.NeedsRedraw = true;
        focused.HasFocus = false;
      }
    }
  } // FocusPreviousControl

  public void FocusNextControl()
  {
    // Get focusable controls:
    List<ControlBase> fCtrls = new List<ControlBase>();
    if (Controls != null) {
      foreach (ControlBase c in Controls) {
        if (c.Focusable)
          fCtrls.Add(c);
      }
    }

    if (fCtrls.Count == 0)
      return;

    ControlBase focused = null;
    foreach (ControlBase c in fCtrls) {
      if (c.HasFocus) {
        focused = c;
        break;
      }
    }

    if (focused == null) {
      fCtrls[0].HasFocus = true;
      fCtrls[0].NeedsRedraw = true;
      return;
    }

    int idx = fCtrls.IndexOf(focused);
    if (idx < fCtrls.Count - 1) {
      fCtrls[idx + 1].HasFocus = true;
      fCtrls[idx + 1].NeedsRedraw = true;

      focused.NeedsRedraw = true;
      focused.HasFocus = false;
    } else {
      fCtrls[0].HasFocus = true;
      fCtrls[0].NeedsRedraw = true;

      if (focused != fCtrls[0]) {
        focused.NeedsRedraw = true;
        focused.HasFocus = false;
      }
    }
  } // FocusNextControl

  private void ResizeTimerCallback(Object state)
  {
    int newWidth = Console.WindowWidth;
    int newHeight = Console.WindowHeight;
    if (newWidth != Width || newHeight != Height) {
      if (m_ChangingSize)
        return;
      m_ChangingSize = true;
      int oldWidth = Width;
      int oldHeight = Height;
      bool changed = false;

      Width = newWidth;
      Height = newHeight;

      m_Mutex.WaitOne();
      try {
        Console.SetCursorPosition(0, 0);
        if (OperatingSystem.IsWindows())
          Console.SetBufferSize(Width, Height);
        changed = true;
      } catch(Exception ex) {
        System.Diagnostics.Debug.WriteLine("TimerCallback: {0}", ex.Message);

        Width = oldWidth;
        Height = oldHeight;
      }
      m_Mutex.ReleaseMutex();

      if (changed) {
        SizeChanged?.Invoke(this, Width, Height);
      }
      m_ChangingSize = false;
    }
  } // ResizeTimerCallback

  private void MusicTimerCallback(Object state)
  {
    if (!ValidSize)
      return;

    TimeSpan pos = TimeSpan.FromSeconds(Player.Instance.TrackPosition);
    TimeSpan length = TimeSpan.FromSeconds(Player.Instance.Length);

    string str = string.Format("{0}/{1}", pos.ToString(@"mm\:ss"), length.ToString(@"mm\:ss"));
    if (string.Compare(str, m_LastMusicTime) != 0) {
      m_LastMusicTime = str;
      WriteString(Width - 17, Height - 1, 11, ColorTheme.Instance.AccentColor, ColorTheme.Instance.OverAccentColor, str);
    }
  }

}