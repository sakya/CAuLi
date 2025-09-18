using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;
using Un4seen.Bass.AddOn.Mix;
using static Library.Library;

namespace CAuLi;

class Program
{
    class BackStackEntry
    {
        public Type Type { get; set; }
        public Utility.GenericConfig SaveState { get; set; }
    }

    public static string RootPath { get; private set; }

    static bool m_Running = true;
    static UI.Pages.PageBase m_CurrentPage = null;
    static List<BackStackEntry> m_BackStack = new List<BackStackEntry>();
    static int m_TracksAdded = 0;

    static void PrintInfo(string settingsPath = null)
    {
        Console.WriteLine("{0} v.{1}", Assembly.GetExecutingAssembly().GetName().Name, Assembly.GetExecutingAssembly().GetName().Version);
        Console.WriteLine("Copyright (c) 2016, 2021 Paolo Iommarini");
        Console.WriteLine();

        if (!string.IsNullOrEmpty(settingsPath))
            Console.WriteLine($"Settings path: {settingsPath}");

        Console.WriteLine("Using");
        Console.WriteLine("BASS v.{0}", Bass.BASS_GetVersion(3));
        Console.WriteLine("BASSMix v.{0}", BassMix.BASS_Mixer_GetVersion(3));
        Console.WriteLine("BASSFx v.{0}", BassFx.BASS_FX_GetVersion(3));

        Console.WriteLine();
    } // PrintInfo

    static void Main(string[] args)
    {
        Console.Title = string.Format("{0}", Assembly.GetExecutingAssembly().GetName().Name);
        AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
        if (OperatingSystem.IsLinux())
            Utility.Library.Load("libbass.so", Utility.Library.LoadFlags.RTLD_LAZY | Utility.Library.LoadFlags.RTLD_GLOBAL, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

        // Check portable settings:
        if (Directory.Exists("Data")) {
            RootPath = "Data";
        } else {
            RootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CAuLi");
            if (!Directory.Exists(RootPath))
                Directory.CreateDirectory(RootPath);
        }

        PrintInfo(RootPath);
        Console.WriteLine("Initializing...");

        // Check playlists folder:
        string playlistsFolder = Path.Combine(RootPath, "Playlists");
        if (!Directory.Exists(playlistsFolder))
            Directory.CreateDirectory(playlistsFolder);

        // Check equalizers folder:
        string eqFolder = Path.Combine(RootPath, "Equalizers");
        if (!Directory.Exists(eqFolder))
            Directory.CreateDirectory(eqFolder);

        // Init settings:
        AppSettings.Instance = new AppSettings(Path.Combine(RootPath, "settings.xml"));
        if (AppSettings.Instance.MusicFolders.Count == 0)
            AppSettings.Instance.MusicFolders = new List<string>() { Environment.GetFolderPath(Environment.SpecialFolder.MyMusic) };
        AppSettings.Instance.Save();

        // Init the player:
        Player.Instance = new Player();
        Player.Instance.Init(-1);
        Player.Instance.Volume = AppSettings.Instance.Volume;
        Player.Instance.Panning = AppSettings.Instance.Panning;
        Player.Instance.Repeat = (Player.RepeatType)AppSettings.Instance.Repeat;
        Player.Instance.Shuffle = AppSettings.Instance.Shuffle;
        Player.Instance.LoadStandardEqualizers();
        Player.Instance.OpenPlaylist(AppSettings.Instance.Playlist);

        // Init Screen
        if (!string.IsNullOrEmpty(AppSettings.Instance.ColorTheme)) {
            string fileName = Path.Combine(RootPath, "ColorThemes", AppSettings.Instance.ColorTheme);
            try {
                UI.ColorTheme.Instance = Utility.Serialization.Deserialize<UI.ColorTheme>(new FileStream(fileName, FileMode.Open));
                if (UI.ColorTheme.Instance == null)
                    UI.ColorTheme.Instance = new UI.ColorTheme();
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine(string.Format("Error loading color theme: {0}", ex.Message));
                UI.ColorTheme.Instance = new UI.ColorTheme();
            }
        }
        UI.Screen.Instance.Init(AppSettings.Instance.WindowWidth, AppSettings.Instance.WindowHeight, 80, 20);

        // Trap CTRL-C
        Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
        };

        // Init database:
        bool forceScan = false;
        string dbPath = Path.Combine(RootPath, "library.db");
        Library.Database.Instance = new Library.Database();
        Library.Database.Instance.Open(dbPath).Wait();
        if (string.IsNullOrEmpty(Library.Database.Instance.GetDbVersion())) {
            Library.Database.Instance.CreateDatabase();
            forceScan = true;
        }

        Library.Library.Instance.TrackAdded += (s, e) =>
        {
            m_TracksAdded++;
            if (m_TracksAdded % 100 == 0 && m_CurrentPage is UI.Pages.LibraryPage) {
                var lp = m_CurrentPage as UI.Pages.LibraryPage;
                lp.Refresh();
            }
        };

        Library.Library.Instance.ScanFinished += (s, e) =>
        {
            if (e.Result.TracksAdded + e.Result.TracksRemoved + e.Result.TracksUpdated > 0 && m_CurrentPage is UI.Pages.LibraryPage) {
                var lp = m_CurrentPage as UI.Pages.LibraryPage;
                lp.Refresh();
            }
        };

        // Library update
        if (forceScan || AppSettings.Instance.AutoUpdateLibrary)
            ThreadPool.QueueUserWorkItem(LibraryUpdateThread);

        // Input thread
        ThreadPool.QueueUserWorkItem(InputThread);

        m_CurrentPage = new UI.Pages.LibraryPage(UI.Screen.Instance);
        m_CurrentPage.Init(null, null);

        // Application run thread:
        ThreadPool.QueueUserWorkItem(ApplicationThread);

        while (m_Running)
            Thread.Sleep(100);
    }

    private static async void LibraryUpdateThread(object state)
    {
        DateTime time = DateTime.UtcNow;
        m_TracksAdded = 0;
        List<string> folders = AppSettings.Instance.MusicFolders;
        UpdateResult res = await Library.Library.Instance.Update(AppSettings.Instance.MusicFolders, AppSettings.Instance.LastMusicFolders, AppSettings.Instance.LastLibraryUpdate);
        if (res != null && res.Result) {
            AppSettings.Instance.LastLibraryUpdate = time;
            AppSettings.Instance.LastMusicFolders = folders;
            AppSettings.Instance.Save();
        }
    } // LibraryUpdateThread

    private static void ApplicationThread(object state)
    {
        // Main loop:
        while (m_CurrentPage != null) {
            UI.Pages.PageReturnValue res = m_CurrentPage.Run();
            if (res.NavigateTo != null) {
                // Forward
                UI.Pages.PageBase newPage = (UI.Pages.PageBase)Activator.CreateInstance(res.NavigateTo, new object[] { UI.Screen.Instance });
                if (newPage != null) {
                    newPage.Init(res.Parameter, null);
                    m_BackStack.Add(new BackStackEntry() { Type = m_CurrentPage.GetType(), SaveState = m_CurrentPage.GetSaveState() });
                    m_CurrentPage.Dispose();
                    m_CurrentPage = newPage;
                }
            } else {
                // Back
                m_CurrentPage.Dispose();
                m_CurrentPage = null;
                if (m_BackStack.Count > 0) {
                    BackStackEntry entry = m_BackStack.Last();
                    m_CurrentPage = (UI.Pages.PageBase)Activator.CreateInstance(entry.Type, new object[] { UI.Screen.Instance });
                    m_CurrentPage.Init(res.Parameter, entry.SaveState);
                    m_BackStack.RemoveAt(m_BackStack.Count - 1);
                }
            }
        }
    } // ApplicationThread

    private static void InputThread(object state)
    {
        while (true) {
            ConsoleKeyInfo key = Console.ReadKey(true);
            Debug.WriteLine("Key pressed: {0} {1}", key.Key, key.Modifiers);

            // Page key:
            if (m_CurrentPage != null && m_CurrentPage.KeyPress(key))
                continue;

            // General keys
            if (key.Key == ConsoleKey.Spacebar) {
                // SPACE: Play/Pause
                if (Player.Instance.IsPlaying)
                    Player.Instance.Pause();
                else
                    Player.Instance.Play();
                continue;
            } else if (key.Key == ConsoleKey.LeftArrow) {
                // LEFT: previous track
                if (Player.Instance.TrackPosition >= 10)
                    Player.Instance.TrackPosition = 0;
                else
                    Player.Instance.Previous();
                continue;
            } else if (key.Key == ConsoleKey.RightArrow) {
                // RIGHT: next track
                Player.Instance.Next();
                continue;
            } else if (key.Key == ConsoleKey.OemPlus || key.Key == ConsoleKey.Add) {
                // PLUS: volume up:
                if (Player.Instance.Volume < 1)
                    Player.Instance.Volume = Player.Instance.Volume + 0.01F;
                continue;
            } else if (key.Key == ConsoleKey.OemMinus || key.Key == ConsoleKey.Subtract) {
                // MINUS volume down:
                if (Player.Instance.Volume > 0)
                    Player.Instance.Volume = Player.Instance.Volume - 0.01F;
                continue;
            } else if ((int)key.Key == AppSettings.Instance.KeyCodePanRight && (key.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control) {
                // CTRL-H: Balance right
                if (Player.Instance.Panning < 1.0f)
                    Player.Instance.Panning = (float)Math.Round(Player.Instance.Panning + 0.1f, 1);
                continue;
            } else if ((int)key.Key == AppSettings.Instance.KeyCodePanLeft && (key.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control) {
                // CTRL-G: Balance left
                if (Player.Instance.Panning > -1.0f)
                    Player.Instance.Panning = (float)Math.Round(Player.Instance.Panning - 0.1f, 1);
                continue;
            } else if (key.Key == ConsoleKey.Tab) {
                // TAB changes focus:
                if ((key.Modifiers & ConsoleModifiers.Shift) == ConsoleModifiers.Shift)
                    UI.Screen.Instance.FocusPreviousControl();
                else
                    UI.Screen.Instance.FocusNextControl();

                foreach (UI.Controls.ControlBase c in UI.Screen.Instance.Controls) {
                    if (c.NeedsRedraw)
                        c.Draw(UI.Screen.Instance);
                }
                continue;
            } else if (key.Key == ConsoleKey.MediaPlay) {
                // Play/pause
                if (Player.Instance.IsPlaying)
                    Player.Instance.Pause();
                else
                    Player.Instance.Play();
                continue;
            } else if (key.Key == ConsoleKey.MediaStop) {
                // Stop
                Player.Instance.Stop();
                continue;
            } else if (key.Key == ConsoleKey.MediaPrevious) {
                // Previous track
                if (Player.Instance.TrackPosition >= 10)
                    Player.Instance.TrackPosition = 0;
                else
                    Player.Instance.Previous();
                continue;
            } else if (key.Key == ConsoleKey.MediaNext) {
                // Next track
                Player.Instance.Next();
                continue;
            } else if ((int)key.Key == AppSettings.Instance.KeyCodeRepeat && (key.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control) {
                // CTRL-R: Repeat toggle
                if (Player.Instance.Repeat == Player.RepeatType.None)
                    Player.Instance.Repeat = Player.RepeatType.One;
                else if (Player.Instance.Repeat == Player.RepeatType.One)
                    Player.Instance.Repeat = Player.RepeatType.All;
                else
                    Player.Instance.Repeat = Player.RepeatType.None;
                continue;
            } else if ((int)key.Key == AppSettings.Instance.KeyCodeShuffle && (key.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control) {
                // CTRL-Z: Shuffle toggle
                Player.Instance.Shuffle = !Player.Instance.Shuffle;
                continue;
            } else if ((int)key.Key == AppSettings.Instance.KeyCodeToggleEq && (key.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control) {
                // CTRL-E: Enable/disable equalizer
                if (Player.Instance.EqualizerEnabled) {
                    Player.Instance.RemoveEqualizer();
                    AppSettings.Instance.EqualizerName = string.Empty;
                    AppSettings.Instance.Save();
                } else {
                    Equalizer eq = Player.Instance.Equalizers != null && Player.Instance.Equalizers.Count > 0 ? Player.Instance.Equalizers[0] : null;
                    if (eq != null)
                        Player.Instance.SetEqualizer(eq);
                }
                continue;
            } else if ((int)key.Key == AppSettings.Instance.KeyCodeNextEq && (key.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control) {
                // CTRL-N: Next equalizer
                if (Player.Instance.EqualizerEnabled) {
                    if (Player.Instance.Equalizers != null && Player.Instance.Equalizers.Count > 1) {
                        for (int i = 0; i < Player.Instance.Equalizers.Count; i++) {
                            Equalizer eq = Player.Instance.Equalizers[i];
                            if (string.Compare(eq.Name, Player.Instance.EqualizerName, true) == 0) {
                                Equalizer newEq = null;
                                if (i + 1 < Player.Instance.Equalizers.Count)
                                    newEq = Player.Instance.Equalizers[i + 1];
                                else
                                    newEq = Player.Instance.Equalizers[0];
                                if (newEq != null) {
                                    Player.Instance.SetEqualizer(newEq);
                                    break;
                                }
                            }
                        }
                    }
                }
                continue;
            } else if ((int)key.Key == AppSettings.Instance.KeyCodeQuit && (key.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control) {
                // CTRL-X: Quit
                Player.Instance.Stop();
                UI.Screen.Instance.Dispose();

                AppSettings.Instance.Playlist = Player.Instance.Playlist;
                AppSettings.Instance.Repeat = (int)Player.Instance.Repeat;
                AppSettings.Instance.Shuffle = Player.Instance.Shuffle;
                AppSettings.Instance.Volume = Player.Instance.Volume;
                AppSettings.Instance.Panning = Player.Instance.Panning;
                AppSettings.Instance.WindowWidth = UI.Screen.Instance.Width;
                AppSettings.Instance.WindowHeight = UI.Screen.Instance.Height;
                AppSettings.Instance.Save();

                Console.ResetColor();
                Console.Clear();
                PrintInfo(RootPath);
                Console.WriteLine("Goodbye...");
                Console.CursorVisible = true;
                m_Running = false;
                Environment.Exit(0);
            }

            // Focused control key
            foreach (UI.Controls.ControlBase c in UI.Screen.Instance.Controls) {
                if (c.HasFocus) {
                    if (c.KeyPress(key)) {
                        if (c.NeedsRedraw)
                            c.Draw(UI.Screen.Instance);
                        break;
                    }
                }
            }
        }
    } // InputThread

    static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
    {
        Console.Clear();
        Console.ResetColor();
        var dumpPath = Path.Combine(RootPath, "exception.txt");
        Console.WriteLine("Sadly {0} crashed.", Assembly.GetExecutingAssembly().GetName().Name);
        Console.WriteLine($"Dumping crash informations to {dumpPath}");

        // Log the exception:
        using (StreamWriter sw = new StreamWriter(dumpPath)) {
            sw.WriteLine(string.Format("{0} {1}", DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString()));
            Exception ex = e.ExceptionObject as Exception;
            while (ex != null) {
                sw.WriteLine(ex.Message);
                if (!string.IsNullOrEmpty(ex.StackTrace))
                    sw.WriteLine(ex.StackTrace);

                ex = ex.InnerException;
            }
        }
        Environment.Exit(1);
    } // UnhandledExceptionTrapper
}