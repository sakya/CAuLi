using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagLib;
using System.Threading;

namespace Library
{
    public class Library
    {
        public class UpdateResult
        {
            public UpdateResult()
            {
                Result = true;
                TracksAdded = 0;
                TracksUpdated = 0;
                TracksRemoved = 0;
                Exception = null;
            }

            public bool Result { get; set; }
            public int TracksAdded { get; set; }
            public int TracksUpdated { get; set; }
            public int TracksRemoved { get; set; }
            public Exception Exception { get; set; }

            public bool LibraryModified
            {
                get
                {
                    return TracksAdded + TracksUpdated + TracksRemoved > 0;
                }
            }
        }

        public class LibraryStats
        {
            public long GenresCount { get; set; }
            public long ArtistsCount { get; set; }
            public long AlbumsCount { get; set; }
            public long TracksCount { get; set; }
        }

        private static volatile Library m_Instance;

        private bool m_AbortRequest = false;

        SemaphoreSlim m_AlbumsSemaphore = null;
        Dictionary<string, Classes.Album> m_AlbumsCache = new Dictionary<string, Classes.Album>();

        SemaphoreSlim m_ArtistsSemaphore = null;
        Dictionary<string, Classes.Artist> m_ArtistsCache = new Dictionary<string, Classes.Artist>();

        SemaphoreSlim m_GenresSemaphore = null;
        Dictionary<string, Classes.Genre> m_GenresCache = new Dictionary<string, Classes.Genre>();

        // Events:
        public event EventHandler ScanStarted;
        public class ScanFinishedArgs : EventArgs
        {
            public ScanFinishedArgs(UpdateResult result)
            {
                Result = result;
            }
            public UpdateResult Result { get; set; }
        }
        public delegate void ScanFinishedHandler(object sender, ScanFinishedArgs e);
        public event ScanFinishedHandler ScanFinished;

        public class ArtistAddedArgs : EventArgs
        {
            public Classes.Artist Artist { get; set; }
        }
        public delegate void ArtistAddedHandler(object sender, ArtistAddedArgs e);
        public event ArtistAddedHandler ArtistAdded;

        public class AlbumAddedArgs : EventArgs
        {
            public Classes.Album Album { get; set; }
        }
        public delegate void AlbumAddedHandler(object sender, AlbumAddedArgs e);
        public event AlbumAddedHandler AlbumAdded;

        public class TrackAddedArgs : EventArgs
        {
            public Classes.Track Track { get; set; }
        }
        public delegate void TrackAddedHandler(object sender, TrackAddedArgs e);
        public event TrackAddedHandler TrackAdded;

        public event EventHandler UpdatingData;
        public event EventHandler OptimizingData;

        public Library()
        {
            bool portable = Directory.Exists("Data");

            string root = portable ? "Data" : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "cauli");
            AlbumsFolder = Path.Combine(root, "albums");
            ArtistsFolder = Path.Combine(root, "artists");
            PlaylistsFolder = Path.Combine(root, "playlists");
        }

        public static Library Instance
        {
            get
            {
                lock (typeof(Library)) {
                    if (m_Instance == null)
                        m_Instance = new Library();
                }

                return m_Instance;
            }
        }

        public string AlbumsFolder
        {
            get;
            private set;
        }

        public string ArtistsFolder
        {
            get;
            private set;
        }

        public string PlaylistsFolder
        {
            get;
            private set;
        }

        public bool IsUpdating
        {
            get;
            private set;
        }

        public async Task<List<Classes.Genre>> GetGenres()
        {
            return await GetGenres(string.Empty);
        }

        public async Task<List<Classes.Genre>> GetGenres(string filter)
        {
            List<Classes.Genre> res = await ViewModels.GenresViewModel.Instance.GetGenres(filter);
            return res;

        }
        public async Task<Classes.Genre> GetGenre(long id)
        {
            return await ViewModels.GenresViewModel.Instance.GetItem(id);
        }

        public async Task<List<Classes.Artist>> GetArtists(string filter)
        {
            List<Classes.Artist> res = await ViewModels.ArtistsViewModel.Instance.GetArtists(filter);
            return res;
        }

        public async Task<List<Classes.Artist>> GetArtists()
        {
            List<Classes.Artist> res = await ViewModels.ArtistsViewModel.Instance.GetArtists();
            return res;
        }

        public async Task<bool> UpdateArtist(Classes.Artist artist)
        {
            return await ViewModels.ArtistsViewModel.Instance.UpdateItem(artist.Id, artist);
        }

        public async Task<Classes.Track> GetTrack(long id)
        {
            return await ViewModels.TracksViewModel.Instance.GetItem(id);
        }

        public async Task<Classes.Track> GetTrack(string filePath)
        {
            return await ViewModels.TracksViewModel.Instance.GetTrackFromPath(filePath);
        }

        public async Task<List<Classes.Track>> GetTracks(List<string> filePaths)
        {
            return await ViewModels.TracksViewModel.Instance.GetTracks(filePaths);
        }

        public async Task<List<Classes.Track>> GetTracks(List<long> ids)
        {
            return await ViewModels.TracksViewModel.Instance.GetTracks(ids);
        }

        public async Task<List<Classes.Track>> GetTracks()
        {
            return await GetTracks(string.Empty);
        }

        public async Task<List<Classes.Track>> GetTracks(string filter)
        {
            List<Classes.Track> res = new List<Classes.Track>(await ViewModels.TracksViewModel.Instance.GetTracks(filter));
            return res;
        }

        public async Task<List<Classes.Track>> GetTracks(long albumId)
        {
            List<Classes.Track> res = new List<Classes.Track>(await ViewModels.TracksViewModel.Instance.GetItems("AlbumId=@AlbumId",
                                                                                                           new Dictionary<string, object>() { { "@AlbumId", albumId } }));
            return res;
        }

        public async Task<List<Classes.Track>> GetGenreTracks(long genreId)
        {
            List<Classes.Track> res = new List<Classes.Track>(await ViewModels.TracksViewModel.Instance.GetItems(
                                                      string.Format("exists(select 1 from db_track_genres where TrackId={0}.Id and GenreId=@GenreId)", ViewModels.TracksViewModel.Instance.GetTableName()),
                                                      "Title",
                                                      new Dictionary<string, object>() { { "@GenreId", genreId } }));
            return res;
        }

        public async Task<Classes.Album> GetAlbum(long albumId)
        {
            Classes.Album res = await ViewModels.AlbumsViewModel.Instance.GetItem(albumId);
            return res;
        }

        public async Task<List<Classes.Track>> GetArtistTracks(long artistId)
        {
            List<Classes.Track> res = new List<Classes.Track>(await ViewModels.TracksViewModel.Instance.GetItems("ArtistId=@ArtistId", "title",
                                                                                                           new Dictionary<string, object>() { { "@ArtistId", artistId } }));
            return res;
        }

        public async Task<bool> UpdateAlbum(Classes.Album album)
        {
            return await ViewModels.AlbumsViewModel.Instance.UpdateItem(album.Id, album);
        }

        public async Task<List<Classes.Album>> GetAlbums(long artistId)
        {
            List<Classes.Album> res = await ViewModels.AlbumsViewModel.Instance.GetArtistAlbums(artistId);
            return res;
        }

        public async Task<List<Classes.Album>> GetAlbums()
        {
            return await GetAlbums(string.Empty);
        }

        public async Task<List<Classes.Album>> GetAlbums(string filter)
        {
            List<Classes.Album> res = await ViewModels.AlbumsViewModel.Instance.GetAlbums(filter);
            return res;
        }

        public async Task<Classes.Artist> GetArtist(long artistId)
        {
            return await ViewModels.ArtistsViewModel.Instance.GetItem(artistId);
        }

        public async Task<LibraryStats> GetStats()
        {
            LibraryStats res = new LibraryStats();

            res.TracksCount = await ViewModels.TracksViewModel.Instance.ItemsCount();
            res.ArtistsCount = await ViewModels.ArtistsViewModel.Instance.ItemsCount();
            res.GenresCount = await ViewModels.GenresViewModel.Instance.ItemsCount();
            res.AlbumsCount = await ViewModels.AlbumsViewModel.Instance.ItemsCount();

            return res;
        }

        public void TrackPlayed(Classes.Track track)
        {
            track.LastPlayed = DateTime.UtcNow;
            track.PlayedCount++;
            ViewModels.TracksViewModel.Instance.TrackPlayed(track.Id, track.PlayedCount, track.LastPlayed.Value);
        }

        #region Playlists
        public async Task<List<Classes.PlayerPlayList>> GetPlaylists(string filter, bool includeRecent, bool includePopular)
        {
            List<Classes.PlayerPlayList> res = new List<Classes.PlayerPlayList>();

            // Add LastAdded and Favorites playlist:
            Classes.PlayerPlayList tp = null;
            if (includeRecent) {
                tp = new Classes.PlayerPlayList() { Type = Classes.PlayerPlayList.PlaylistType.LastAdded };
                foreach (Classes.Track t in await ViewModels.TracksViewModel.Instance.GetLastAddedTracks(30))
                    tp.TrackFiles.Add(t.FilePath);
                if (tp.TracksCount > 0)
                    res.Add(tp);
            }

            if (includePopular) {
                tp = new Classes.PlayerPlayList() { Type = Classes.PlayerPlayList.PlaylistType.Favorites };
                foreach (Classes.Track t in await ViewModels.TracksViewModel.Instance.GetPopularTracks(30))
                    tp.TrackFiles.Add(t.FilePath);
                if (tp.TracksCount > 0)
                    res.Add(tp);
                tp = null;
            }

            return res.OrderBy(o => o.Name).ToList();
        } // GetPlaylists

        #endregion

        #region Scanner
        private static TagLib.Tag GetTag(TagLib.File tagLibFile)
        {
            TagLib.Tag tags = null;
            List<TagTypes> types = new List<TagTypes>() { TagTypes.Id3v2, TagTypes.FlacMetadata, TagTypes.Xiph, TagTypes.Asf, TagTypes.Apple, TagTypes.Ape, TagTypes.Id3v1 };
            foreach (TagTypes type in types) {
                tags = tagLibFile.GetTag(type);
                if (tags != null)
                    break;
            }
            return tags;
        } // GetTag

        public static Classes.Track ParseFileMinimum(string fileName)
        {
            Classes.Track res = new Classes.Track();
            FileInfo fi = new FileInfo(fileName);
            TagLib.File tagFile = null;
            TagLib.Tag tags = null;
            try {
                tagFile = TagLib.File.Create(new TagLib.File.LocalFileAbstraction(fileName));
                tags = GetTag(tagFile);
                tagFile.Dispose();

                res.Title = tags != null && !string.IsNullOrEmpty(tags.Title) ? tags.Title.Trim() : string.Empty;

                string artist = tags != null && tags.Performers != null && tags.Performers.Length > 0 ? tags.Performers[0].Trim() : string.Empty;
                if (string.IsNullOrEmpty(artist))
                    artist = tags != null && tags.AlbumArtists != null && tags.AlbumArtists.Length > 0 ? tags.AlbumArtists[0].Trim() : string.Empty;

                res.Artist = new Classes.Artist() { Name = artist };

                string albumTitle = tags != null && !string.IsNullOrEmpty(tags.Album) ? tags.Album.Trim() : string.Empty;
                string albumArtist = tags != null && tags.AlbumArtists != null && tags.AlbumArtists.Length > 0 ? tags.AlbumArtists[0].Trim() : string.Empty;
                if (string.IsNullOrEmpty(albumArtist))
                    albumArtist = artist;

                res.Album = new Classes.Album()
                {
                    Title = albumTitle,
                    Artist = new Classes.Artist() { Name = albumArtist }
                };

                res.FilePath = fi.FullName;
                res.FileSize = (long)fi.Length;
                res.LastModifed = fi.LastWriteTime;
                res.MimeType = tagFile.MimeType;
                res.TrackNumber = tags != null ? tags.Track : 0;
                res.DiscNumber = tags != null ? tags.Disc : 0;
                res.Duration = tagFile.Properties.Duration;
                res.Year = tags != null ? tags.Year : 0;
                if (tags != null && tags.Genres != null) {
                    foreach (string g in tags.Genres) {
                        Classes.Genre genre = new Classes.Genre();
                        genre.Name = g.Trim();
                        res.Genres.Add(genre);
                    }
                }

            } catch (Exception) {

            }
            return res;
        } // ParseFileMinimum

        private async Task<Classes.Artist> GetArtistFromCache(string name)
        {
            Classes.Artist res = null;
            if (m_ArtistsCache.TryGetValue(name.ToLower(), out res))
                return res;

            res = await ViewModels.ArtistsViewModel.Instance.GetItem(name);
            if (res != null)
                m_ArtistsCache[res.Name.ToLower()] = res;
            return res;
        }

        private async Task<bool> AddArtist(Classes.Artist artist)
        {
            await m_ArtistsSemaphore.WaitAsync();
            Classes.Artist a = await GetArtistFromCache(artist.Name);
            if (artist.Id == 0 && a == null) {
                m_ArtistsCache[artist.Name.ToLower()] = artist;

                await ViewModels.ArtistsViewModel.Instance.InsertItem(artist);
                if (ArtistAdded != null)
                    ArtistAdded(this, new ArtistAddedArgs() { Artist = artist });
            } else if (a != null) {
                artist.Id = a.Id;
                artist.ArtistArt = a.ArtistArt;
            }

            m_ArtistsSemaphore.Release();
            return true;
        }

        private async Task<Classes.Album> GetAlbumFromCache(Classes.Album album)
        {
            string key = album.Key;
            Classes.Album res = null;
            if (m_AlbumsCache.TryGetValue(key, out res))
                return res;

            if (album.Artist != null && album.Artist.Id > 0) {
                res = await ViewModels.AlbumsViewModel.Instance.GetItem(album.Artist.Id, album.Title);
                if (res != null)
                    m_AlbumsCache[res.Key] = res;
            }
            return res;
        }

        private async Task<bool> AddAlbum(Classes.Album album)
        {
            await m_AlbumsSemaphore.WaitAsync();
            Classes.Album a = await GetAlbumFromCache(album);
            if (a == null) {
                m_AlbumsCache[album.Key] = album;

                await ViewModels.AlbumsViewModel.Instance.InsertItem(album);
                if (AlbumAdded != null)
                    AlbumAdded(this, new AlbumAddedArgs() { Album = album });
            } else {
                album.Id = a.Id;
                album.AlbumArt = a.AlbumArt;
            }

            m_AlbumsSemaphore.Release();
            return true;
        }

        private async Task<Classes.Genre> GetGenreFromCache(string name)
        {
            Classes.Genre res = null;
            if (m_GenresCache.TryGetValue(name.ToLower(), out res))
                return res;

            res = await ViewModels.GenresViewModel.Instance.GetItem(name);
            if (res != null)
                m_GenresCache[res.Name.ToLower()] = res;
            return res;
        }

        private async Task<bool> AddGenre(Classes.Genre genre)
        {
            await m_GenresSemaphore.WaitAsync();
            Classes.Genre g = await GetGenreFromCache(genre.Name);
            if (g == null) {
                m_GenresCache[genre.Key] = genre;
                await ViewModels.GenresViewModel.Instance.InsertItem(genre);
            } else {
                genre.Id = g.Id;
            }

            m_GenresSemaphore.Release();
            return true;
        }

        public void AbortScan()
        {
            m_AbortRequest = true;
        }

        public async Task<UpdateResult> Update(List<string> folders, List<string> lastFolders, DateTime lastUpdateTime)
        {
            ScanStarted?.Invoke(this, new EventArgs());

            IsUpdating = true;
            m_AlbumsSemaphore = new SemaphoreSlim(1);
            m_ArtistsSemaphore = new SemaphoreSlim(1);
            m_GenresSemaphore = new SemaphoreSlim(1);

            Database.Instance.BeginTransaction();
            m_AbortRequest = false;

            ViewModels.TracksViewModel.Instance.SetTracksTemp();

            UpdatingData?.Invoke(this, new EventArgs());

            UpdateResult res = new UpdateResult();
            try {
                foreach (string folder in folders) {
                    if (lastFolders.Contains(folder))
                        res.Result = await ScanFolder(res, folder, lastUpdateTime, true);
                    else
                        res.Result = await ScanFolder(res, folder, DateTime.MinValue, true);

                    if (!res.Result)
                        break;
                }

                res.TracksRemoved = await ViewModels.TracksViewModel.Instance.DeleteTempTracks();
                OptimizingData?.Invoke(this, new EventArgs());

                if (res.TracksAdded + res.TracksRemoved + res.TracksUpdated > 0) {
                    FixVariousArtists();
                    if (!m_AbortRequest)
                        await OptimizeData();
                }
            } catch (Exception) {
                m_AbortRequest = true;
            }

            if (m_AbortRequest) {
                Database.Instance.RollbackTransaction();
                res = new UpdateResult();
                res.Result = false;
            } else {
                Database.Instance.CommitTransaction();
                Database.Instance.Optimize();
            }
            m_AlbumsCache.Clear();
            m_ArtistsCache.Clear();
            m_GenresCache.Clear();

            m_AlbumsSemaphore.Dispose();
            m_AlbumsSemaphore = null;
            m_ArtistsSemaphore.Dispose();
            m_ArtistsSemaphore = null;
            m_GenresSemaphore.Dispose();
            m_GenresSemaphore = null;

            IsUpdating = false;

            ScanFinished?.Invoke(this, new ScanFinishedArgs(res));
            return res;
        }

        public async Task<bool> ScanFolder(UpdateResult result, string path, DateTime lastUpdateTime, bool recursive)
        {
            bool scanFiles = true;
            if (scanFiles) {
                string[] files = Directory.GetFiles(path);

                uint parallelScan = 4;
                List<Task<bool>> tasks = new List<Task<bool>>();
                foreach (string file in files) {
                    if (m_AbortRequest)
                        return false;

                    FileInfo fi = new FileInfo(file);
                    string ext = fi.Extension;
                    if (string.Compare(ext, ".jpeg", StringComparison.CurrentCultureIgnoreCase) == 0 ||
                        string.Compare(ext, ".jpg", StringComparison.CurrentCultureIgnoreCase) == 0 ||
                        string.Compare(ext, ".pamp", StringComparison.CurrentCultureIgnoreCase) == 0)
                        continue;

                    if (tasks.Count >= parallelScan) {
                        Task.WaitAll(tasks.ToArray());
                        tasks.Clear();
                    }

                    Classes.Track track = await ViewModels.TracksViewModel.Instance.GetTrackFromPath(fi.FullName);
                    if (track == null) {
                        tasks.Add(ScanFile(fi.FullName, fi));
                        result.TracksAdded++;
                    } else {
                        if ((long)fi.Length != track.FileSize || fi.LastWriteTime > track.LastModifed) {
                            await ViewModels.TracksViewModel.Instance.DeleteItem(track.Id);
                            tasks.Add(ScanFile(fi.FullName, fi));
                            result.TracksUpdated++;
                        } else {
                            track.IsTemp = false;
                            await ViewModels.TracksViewModel.Instance.UpdateItem(track.Id, track);
                        }
                    }
                }

                if (tasks.Count > 0) {
                    Task.WaitAll(tasks.ToArray());
                    tasks.Clear();
                }
            }

            if (recursive) {
                string[] folders = null;
                try {
                    folders = Directory.GetDirectories(path);
                } catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine("ScanFolder: {0}", ex.Message);
                }

                if (folders != null) {
                    foreach (string f in folders) {
                        if (m_AbortRequest)
                            return false;
                        await ScanFolder(result, f, lastUpdateTime, recursive);
                    }
                }
            }

            return true;
        } // AddFolder

        private async Task<bool> ScanFile(string fileName, FileInfo properties)
        {
            TagLib.File tagFile = null;
            TagLib.Tag tags = null;
            try {
                tagFile = TagLib.File.Create(new TagLib.File.LocalFileAbstraction(fileName));
                if (tagFile.Properties.Duration.TotalSeconds == 0) {
                    System.Diagnostics.Debug.WriteLine(string.Format("File with duration 0 {0}", fileName));
                    return false;
                }

                tags = GetTag(tagFile);
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine(string.Format("Error parsing file {0}:\r\n{1}", fileName, ex.Message));
                return false;
            }

            Classes.Artist trackArtist = new Classes.Artist();
            trackArtist.Name = tags != null && tags.Performers != null && tags.Performers.Length > 0 ? tags.Performers[0].Trim() : string.Empty;
            if (string.IsNullOrEmpty(trackArtist.Name))
                trackArtist.Name = tags != null && tags.AlbumArtists != null && tags.AlbumArtists.Length > 0 ? tags.AlbumArtists[0].Trim() : string.Empty;
            await AddArtist(trackArtist);

            Classes.Artist albumArtist = new Classes.Artist();
            albumArtist.Name = tags != null && tags.AlbumArtists != null && tags.AlbumArtists.Length > 0 ? tags.AlbumArtists[0].Trim() : string.Empty;
            if (string.IsNullOrEmpty(albumArtist.Name) && !string.IsNullOrEmpty(trackArtist.Name))
                albumArtist.Name = trackArtist.Name;
            await AddArtist(albumArtist);

            Classes.Album album = new Classes.Album();
            album.Title = tags != null && !string.IsNullOrEmpty(tags.Album) ? tags.Album.Trim() : string.Empty;
            album.Artist = albumArtist;
            if (await GetAlbumFromCache(album) == null)
                await AddAlbum(album);
            else
                album = await GetAlbumFromCache(album);

            Classes.Track track = new Classes.Track();
            track.FilePath = fileName;
            track.FileSize = properties.Length;
            track.LastModifed = properties.LastWriteTime;
            track.MimeType = tagFile.MimeType;
            track.Album = album;
            track.Artist = trackArtist;
            track.Title = tags != null && !string.IsNullOrEmpty(tags.Title) ? tags.Title.Trim() : string.Empty;
            if (string.IsNullOrEmpty(track.Title))
                track.Title = properties.Name;
            track.TrackNumber = tags != null ? tags.Track : 0;
            track.DiscNumber = tags != null ? tags.Disc : 0;
            track.Duration = tagFile.Properties.Duration;
            track.Year = tags != null ? tags.Year : 0;
            if (tags != null && tags.Genres != null) {
                foreach (string g in tags.Genres) {
                    Classes.Genre genre = new Classes.Genre();
                    genre.Name = g.Trim();
                    await AddGenre(genre);
                    track.Genres.Add(genre);
                }
            }

            tagFile.Dispose();
            await ViewModels.TracksViewModel.Instance.InsertItem(track);
            if (TrackAdded != null)
                TrackAdded(this, new TrackAddedArgs() { Track = track });
            return true;
        } // ScanFile

        public string GetLyrics(string fileName)
        {
            TagLib.Tag tags = null;
            try {
                using (TagLib.File tagFile = TagLib.File.Create(new TagLib.File.LocalFileAbstraction(fileName))) {
                    tags = GetTag(tagFile);
                    if (tags != null) {
                        if (tags is TagLib.Ogg.XiphComment) {
                            TagLib.Ogg.XiphComment xt = tags as TagLib.Ogg.XiphComment;
                            string res = xt.GetFirstField("LYRICS");
                            if (res != null)
                                return res;
                            res = xt.GetFirstField("UNSYNCEDLYRICS");
                            if (res != null)
                                return res;
                        } else if (tags is TagLib.Id3v2.Tag) {
                            TagLib.Id3v2.Tag id3v2 = tags as TagLib.Id3v2.Tag;
                            return id3v2.Lyrics;
                        } else if (tags is TagLib.Flac.Metadata) {
                            TagLib.Flac.Metadata fm = tags as TagLib.Flac.Metadata;
                            return fm.Lyrics;
                        }
                        return tags.Lyrics;
                    }
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine(string.Format("Error parsing file {0}:\r\n{1}", fileName, ex.Message));
                return string.Empty;
            }

            return string.Empty;
        } // GetLyrics

        private void FixVariousArtists()
        {

        } // FixVariousArtists

        private async Task<bool> OptimizeData()
        {
            await ViewModels.AlbumsViewModel.Instance.OptimizeData();
            await ViewModels.ArtistsViewModel.Instance.OptimizeData();
            await ViewModels.GenresViewModel.Instance.OptimizeData();
            return true;
        } // OptimizeData

        #endregion
    }
}
