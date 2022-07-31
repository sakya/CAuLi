using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utility;

namespace CAuLi.UI.Pages
{
    class LibraryPage : PageBase
    {
        Controls.MenuControl m_Artists = null;
        Controls.MenuControl m_Albums = null;
        Controls.MenuControl m_Tracks = null;

        public LibraryPage(Screen screen) :
          base(screen)
        {

        }
        #region overrides
        public override void Init(object parameter, GenericConfig saveState)
        {
            base.Init(parameter, saveState);

            // Artists menu
            m_Artists = new Controls.MenuControl()
            {
                Title = "Artists",
                EmptyText = "No artists",
                HasFocus = true
            };
            foreach (Library.Classes.Artist a in Library.Library.Instance.GetArtists().Result)
                m_Artists.Items.Add(new Controls.MenuControl.MenuItem() { Id = a.Id.ToString(CultureInfo.InvariantCulture), Tag = a, Text = a.Name });
            Screen.Instance.Controls.Add(m_Artists);

            // Albums menu
            m_Albums = new Controls.MenuControl()
            {
                Title = "Albums",
                EmptyText = "No albums",
                HasFocus = false
            };
            Screen.Instance.Controls.Add(m_Albums);

            // Tracks menu
            m_Tracks = new Controls.MenuControl()
            {
                Title = "Tracks",
                EmptyText = "No tracks",
                HasFocus = false
            };
            Screen.Instance.Controls.Add(m_Tracks);

            // Changed events:
            string albumsRestoreSelection = string.Empty;
            string albumsRestorePosition = string.Empty;
            if (saveState != null) {
                albumsRestoreSelection = saveState.GetValue("m_Albums.SelectedItem", string.Empty);
                albumsRestorePosition = saveState.GetValue("m_Albums.FirstVisibleItem", string.Empty);
            }

            m_Artists.SelectedItemChanged += (s, eArgs) =>
            {
                // Update albums menu:
                Library.Classes.Artist art = eArgs.Tag as Library.Classes.Artist;
                m_Albums.Items.Clear();
                List<Library.Classes.Album> albums = null;
                if (art != null)
                    albums = Library.Library.Instance.GetAlbums(art.Id).Result;
                else
                    albums = Library.Library.Instance.GetAlbums().Result;

                if (AppSettings.Instance.SortAlbumsByDate)
                    albums = albums.OrderBy(o => o.Year).ThenBy(o => o.Title).ToList();
                foreach (Library.Classes.Album a in albums) {
                    Controls.MenuControl.MenuItem mi = new Controls.MenuControl.MenuItem()
                    {
                        Id = a.Id.ToString(CultureInfo.InvariantCulture),
                        Tag = a,
                        FormatText = new Controls.MenuControl.MenuItem.FormatTextHandler(FormatAlbum),
                        Text = a.Title
                    };
                    m_Albums.Items.Add(mi);
                }
                if (m_Albums.Items.Count > 1 || m_Albums.Items.Count == 0)
                    m_Albums.Items.Insert(0, new Controls.MenuControl.MenuItem() { Id = Guid.NewGuid().ToString("N"), Text = "All" });

                if (!string.IsNullOrEmpty(albumsRestoreSelection))
                    m_Albums.SelectMenuItem(albumsRestoreSelection);
                else
                    m_Albums.SelectedItem = m_Albums.Items.Count > 0 ? m_Albums.Items[0] : null;

                if (!string.IsNullOrEmpty(albumsRestorePosition))
                    m_Albums.ScrollToMenuItem(albumsRestorePosition);
                else
                    m_Albums.FirstVisibleItem = null;

                albumsRestoreSelection = string.Empty;
                albumsRestorePosition = string.Empty;
                m_Albums.Draw(Screen.Instance);
            };

            string tracksRestoreSelection = string.Empty;
            string tracksRestorePosition = string.Empty;
            if (saveState != null) {
                tracksRestoreSelection = saveState.GetValue("m_Tracks.SelectedItem", string.Empty);
                tracksRestorePosition = saveState.GetValue("m_Tracks.FirstVisibleItem", string.Empty);
            }

            m_Albums.SelectedItemChanged += (s, eArgs) =>
            {
                // Update tracks menu:
                if (eArgs == null)
                    m_Tracks.Items.Clear();
                else {
                    Library.Classes.Artist art = m_Artists.SelectedItem.Tag as Library.Classes.Artist;
                    Library.Classes.Album alb = eArgs.Tag as Library.Classes.Album;
                    m_Tracks.Items.Clear();
                    List<Library.Classes.Track> tracks = null;
                    if (alb != null)
                        tracks = Library.Library.Instance.GetTracks(alb.Id).Result;
                    else {
                        if (art != null)
                            tracks = Library.Library.Instance.GetArtistTracks(art.Id).Result;
                        else
                            tracks = Library.Library.Instance.GetTracks().Result;
                    }

                    foreach (Library.Classes.Track t in tracks) {
                        string text = string.Empty;
                        if (art == null)
                            text = string.Format("{0} - {1} - {2}", t.Title, t.Artist.Name, t.Album.Title);
                        else if (alb == null)
                            text = string.Format("{0} - {1}", t.Title, t.Album.Title);
                        else
                            text = string.Format("{0}. {1}", t.TrackNumber, t.Title);

                        Controls.MenuControl.MenuItem mi = new Controls.MenuControl.MenuItem()
                        {
                            Id = t.Id.ToString(CultureInfo.InvariantCulture),
                            Tag = t,
                            Text = text
                        };
                        mi.FormatText = new Controls.MenuControl.MenuItem.FormatTextHandler(FormatTrack);
                        m_Tracks.Items.Add(mi);
                    }
                }

                if (!string.IsNullOrEmpty(tracksRestoreSelection))
                    m_Tracks.SelectMenuItem(tracksRestoreSelection);
                else
                    m_Tracks.SelectedItem = m_Tracks.Items.Count > 0 ? m_Tracks.Items[0] : null;

                if (!string.IsNullOrEmpty(tracksRestorePosition))
                    m_Tracks.ScrollToMenuItem(tracksRestorePosition);
                else
                    m_Tracks.FirstVisibleItem = null;

                tracksRestoreSelection = string.Empty;
                tracksRestorePosition = string.Empty;
                m_Tracks.Draw(Screen.Instance);
            };

            // Triggered events:
            m_Artists.ItemTriggered += (s, eArgs) =>
            {
                // Build the playlist:
                Library.Classes.PlayerPlayList pl = new Library.Classes.PlayerPlayList();
                Library.Classes.Artist a = eArgs.Tag as Library.Classes.Artist;

                List<Library.Classes.Track> tracks = null;
                if (a != null)
                    tracks = Library.Library.Instance.GetArtistTracks(a.Id).Result;
                else
                    tracks = Library.Library.Instance.GetTracks().Result;
                foreach (Library.Classes.Track t in tracks)
                    pl.TrackFiles.Add(t.FilePath);
                pl.CurrentIndex = 0;
                if (Player.Instance.OpenPlaylist(pl)) {
                    Player.Instance.Play();
                    ReturnValue = new PageReturnValue() { NavigateTo = typeof(PlayerPage) };
                }
            };

            m_Albums.ItemTriggered += (s, eArgs) =>
            {
                // Build the playlist:
                Library.Classes.PlayerPlayList pl = new Library.Classes.PlayerPlayList();
                Library.Classes.Album album = eArgs.Tag as Library.Classes.Album;
                List<Library.Classes.Track> tracks = null;
                if (album != null)
                    tracks = Library.Library.Instance.GetTracks(album.Id).Result;
                else {
                    Library.Classes.Artist art = m_Artists.SelectedItem.Tag as Library.Classes.Artist;
                    if (art != null)
                        tracks = Library.Library.Instance.GetArtistTracks(art.Id).Result;
                    else
                        tracks = Library.Library.Instance.GetTracks().Result;
                }

                foreach (Library.Classes.Track t in tracks)
                    pl.TrackFiles.Add(t.FilePath);
                pl.CurrentIndex = 0;
                if (Player.Instance.OpenPlaylist(pl)) {
                    Player.Instance.Play();
                    ReturnValue = new PageReturnValue() { NavigateTo = typeof(PlayerPage) };
                }
            };

            m_Tracks.ItemTriggered += (s, eArgs) =>
            {
                // Build the playlist:
                Library.Classes.PlayerPlayList pl = new Library.Classes.PlayerPlayList();
                foreach (Controls.MenuControl.MenuItem i in m_Tracks.Items) {
                    pl.TrackFiles.Add((i.Tag as Library.Classes.Track).FilePath);
                }
                pl.CurrentIndex = pl.TrackFiles.IndexOf((eArgs.Tag as Library.Classes.Track).FilePath);
                if (Player.Instance.OpenPlaylist(pl)) {
                    Player.Instance.Play();
                    ReturnValue = new PageReturnValue() { NavigateTo = typeof(PlayerPage) };
                }
            };

            if (saveState != null) {
                string id = saveState.GetValue("m_Artists.SelectedItem", string.Empty);
                if (!string.IsNullOrEmpty(id))
                    m_Artists.SelectMenuItem(id);
                id = saveState.GetValue("m_Artists.FirstVisibleItem", string.Empty);
                if (!string.IsNullOrEmpty(id))
                    m_Artists.ScrollToMenuItem(id);
            }

            OnSizeChanged(m_Screen, m_Screen.Width, m_Screen.Height);
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
            } else if ((int)keyPress.Key == AppSettings.Instance.KeyCodePlayer && (keyPress.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control) {
                // CTRL-P: Player
                PlayerStatus status = Player.Instance.GetStatus();
                if (!string.IsNullOrEmpty(status.Track))
                    ReturnValue = new PageReturnValue() { NavigateTo = typeof(PlayerPage) };
                return true;
            }

            return false;
        }

        protected override void OnSizeChanged(Screen sender, int width, int height)
        {
            base.OnSizeChanged(sender, width, height);
            if (!m_Screen.ValidSize)
                return;

            m_Artists.Y = 1;
            m_Artists.Width = m_Screen.Width / 2 - 1;
            m_Artists.Height = m_Screen.Height / 2 - 1;

            m_Albums.Y = 1;
            m_Albums.Width = m_Screen.Width - m_Artists.Width - 1;
            m_Albums.X = m_Screen.Width / 2;
            m_Albums.Height = m_Screen.Height / 2 - 1;

            m_Tracks.Width = m_Screen.Width;
            m_Tracks.Y = m_Screen.Height / 2;
            m_Tracks.Height = m_Screen.Height - m_Artists.Height - 2;

            m_Screen.Clear();
            m_Screen.Draw();

            // Lines:
            for (int y = m_Artists.Y; y < m_Artists.Y + m_Artists.Height; y++) {
                m_Screen.WriteString(m_Artists.X + m_Artists.Width, y, 1,
                                     y == m_Artists.Y ? ColorTheme.Instance.BackgroundTitle : UI.ColorTheme.Instance.Background,
                                     ColorTheme.Instance.AccentColor, "│");
            }
        }

        public override GenericConfig GetSaveState()
        {
            GenericConfig ss = new GenericConfig();

            ss.SetValue("m_Artists.SelectedItem", m_Artists.SelectedItem != null ? m_Artists.SelectedItem.Id : string.Empty);
            ss.SetValue("m_Artists.FirstVisibleItem", m_Artists.FirstVisibleItem != null ? m_Artists.FirstVisibleItem.Id : string.Empty);

            ss.SetValue("m_Albums.SelectedItem", m_Albums.SelectedItem != null ? m_Albums.SelectedItem.Id : string.Empty);
            ss.SetValue("m_Albums.FirstVisibleItem", m_Albums.FirstVisibleItem != null ? m_Albums.FirstVisibleItem.Id : string.Empty);

            ss.SetValue("m_Tracks.SelectedItem", m_Tracks.SelectedItem != null ? m_Tracks.SelectedItem.Id : string.Empty);
            ss.SetValue("m_Tracks.FirstVisibleItem", m_Tracks.FirstVisibleItem != null ? m_Tracks.FirstVisibleItem.Id : string.Empty);

            return ss;
        }

        public void Refresh()
        {
            var selected = m_Artists.SelectedItem?.Id;

            m_Artists.IsEnabled = false;
            m_Artists.Items.Clear();
            foreach (Library.Classes.Artist a in Library.Library.Instance.GetArtists().Result)
                m_Artists.Items.Add(new Controls.MenuControl.MenuItem() { Id = a.Id.ToString(CultureInfo.InvariantCulture), Tag = a, Text = a.Name });

            if (selected != null) {
                m_Artists.FirstVisibleItem = m_Artists.Items.Where(i => i.Id == selected).FirstOrDefault();
                m_Artists.SelectedItem = m_Artists.Items.Where(i => i.Id == selected).FirstOrDefault();
            }
            m_Artists.Draw(m_Screen);
            m_Artists.IsEnabled = true;
        } // Refresh
        #endregion

        #region Private operations
        private string FormatAlbum(UI.Controls.MenuControl menu, UI.Controls.MenuControl.MenuItem item)
        {
            Library.Classes.Album album = item.Tag as Library.Classes.Album;
            if (album != null) {
                string year = album.Year > 0 ? album.Year.ToString(CultureInfo.InvariantCulture) : string.Empty;
                string res = UI.Screen.ElideString(item.Text, menu.Width - 5);
                return string.Format("{0}{1}{2}", res, new string(' ', menu.Width - res.Length - year.Length), year);
            }
            return item.Text;
        }

        private string FormatTrack(UI.Controls.MenuControl menu, UI.Controls.MenuControl.MenuItem item)
        {
            Library.Classes.Track track = item.Tag as Library.Classes.Track;
            if (track != null) {
                string time = track.Duration.ToString(@"mm\:ss");
                string res = UI.Screen.ElideString(item.Text, menu.Width - 6);
                return string.Format("{0}{1}{2}", res, new string(' ', menu.Width - res.Length - time.Length), time);
            }
            return item.Text;
        }
        #endregion
    }
}
