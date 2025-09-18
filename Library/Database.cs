using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace Library;

public class Database
{
  string m_DbVersion = "1.0.7";
  SQLiteConnection m_Conn = null;

  public Database()
  {

  }

  static Database m_Instance;
  public static Database Instance
  {
    get { return m_Instance; }

    set { m_Instance = value; }
  }

  public string DbVersion
  {
    get { return m_DbVersion; }
  }

  public SQLiteConnection Connection
  {
    get { return m_Conn; }
  }

  public async Task<bool> Open(string dbName)
  {
    Close();
    m_Conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", dbName));
    await m_Conn.OpenAsync();
    using (SQLiteCommand statement = new SQLiteCommand("PRAGMA synchronous=OFF;", m_Conn))
      statement.ExecuteNonQuery();

    // Check database version:
    if (GetDbVersion() != DbVersion)
      DropDatabase();
    return true;
  }

  public void Close()
  {
    if (m_Conn != null) {
      m_Conn.Dispose();
      m_Conn = null;
    }
  }

  public bool IsEmpty()
  {
    string sql = "SELECT count(*) FROM db_tracks";
    using (SQLiteCommand statement = new SQLiteCommand(sql, m_Conn)) {
      int dbRes = (int)statement.ExecuteScalar();
      return dbRes == 0;
    }
  }

  public string GetDbVersion()
  {
    string sql = "SELECT DbVersion FROM db_system";
    try {
      using (SQLiteCommand statement = new SQLiteCommand(sql, m_Conn)) {
        return (string)statement.ExecuteScalar();
      }
    } catch (Exception) { }
    return string.Empty;
  }

  public bool BeginTransaction()
  {
    string sql = "BEGIN TRANSACTION";
    using (SQLiteCommand statement = new SQLiteCommand(sql, m_Conn)) {
      statement.ExecuteNonQuery();
    }
    return true;
  }

  public bool CommitTransaction()
  {
    string sql = "COMMIT TRANSACTION";
    using (SQLiteCommand statement = new SQLiteCommand(sql, m_Conn)) {
      statement.ExecuteNonQuery();
    }
    return true;
  }

  public bool RollbackTransaction()
  {
    string sql = "ROLLBACK TRANSACTION";
    using (SQLiteCommand statement = new SQLiteCommand(sql, m_Conn)) {
      statement.ExecuteNonQuery();
    }
    return true;
  }

  public bool Optimize()
  {
    string sql = "VACUUM;";
    try {
      using (SQLiteCommand statement = new SQLiteCommand(sql, m_Conn)) {
        statement.ExecuteNonQuery();
      }
    }catch (Exception ex) {
      System.Diagnostics.Debug.WriteLine(string.Format("Optimize: {0}", ex.Message));
      return false;
    }
    return true;
  }

  private bool DropDatabase()
  {
    List<string> stm = new List<string>();

    stm.Add("drop table if exists db_system");
    stm.Add("drop table if exists db_artists");
    stm.Add("drop table if exists db_albums");
    stm.Add("drop table if exists db_tracks");
    stm.Add("drop table if exists db_genres");
    stm.Add("drop table if exists db_track_genres");

    foreach (string sql in stm) {
      using (SQLiteCommand statement = new SQLiteCommand(sql, m_Conn))
        statement.ExecuteNonQuery();
    }
    return true;
  }

  public bool CreateDatabase()
  {
    List<string> stm = new List<string>();

    string currentDbVersion = GetDbVersion();
    if (string.IsNullOrEmpty(currentDbVersion)) {
      stm.Add(@"CREATE TABLE IF NOT EXISTS 
                  db_system (
                    Id          INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                    DbVersion   VARCHAR(50))
                ");
      stm.Add(string.Format("insert into db_system(DbVersion) values('{0}')", DbVersion));
    }

    stm.Add(@"CREATE TABLE IF NOT EXISTS 
                db_genres (
                  Id          INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                  Name        VARCHAR(128),
                  TracksCount INTEGER)
              ");
    stm.Add("CREATE unique index IF NOT EXISTS genres_idx_genre on db_genres(Name)");

    stm.Add(@"CREATE TABLE IF NOT EXISTS 
                db_track_genres (
                  TrackId    INTEGER,
                  GenreId    INTEGER)
              ");
    stm.Add("CREATE index IF NOT EXISTS track_genres_idx_genre on db_track_genres(GenreId)");
    stm.Add("CREATE index IF NOT EXISTS track_genres_idx_track on db_track_genres(TrackId)");

    stm.Add(@"CREATE TABLE IF NOT EXISTS 
                db_artists (
                  Id          INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                  ArtistArt   VARCHAR(256),
                  Name        VARCHAR(128),
                  AlbumsCount INTEGER,
                  TracksCount INTEGER)
              ");
    stm.Add("CREATE unique index IF NOT EXISTS artists_idx_artist on db_artists(Name)");

    stm.Add(@"CREATE TABLE IF NOT EXISTS 
                db_albums (
                  Id          INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                  ArtistId    INTEGER,
                  Title       VARCHAR(256),
                  AlbumArt    VARCHAR(256),
                  Year        INTEGER,
                  TracksCount INTEGER,
                  Duration    INTEGER)
              ");
    stm.Add("CREATE index IF NOT EXISTS albums_idx_artist on db_albums(ArtistId)");

    stm.Add(@"CREATE TABLE IF NOT EXISTS 
                db_tracks (
                  Id          INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                  FilePath    VARCHAR(512),
                  FolderPath  VARCHAR(512),
                  FileSize    INTEGER,
                  LastModifed INTEGER,
                  AlbumId     INTEGER,
                  ArtistId    INTEGER,
                  MimeType    VARCHAR(128),
                  Title       VARCHAR(256),
                  DiscNumber  INTEGER,
                  TrackNumber INTEGER,
                  Duration    INTEGER,
                  Year        INTEGER,
                  LastPlayed  INTEGER,
                  PlayedCount INTEGER,
                  Temp        INTEGER)
              ");
    stm.Add("CREATE index IF NOT EXISTS tracks_idx_artist on db_tracks(ArtistId)");
    stm.Add("CREATE index IF NOT EXISTS tracks_idx_album on db_tracks(AlbumId)");
    stm.Add("CREATE unique index IF NOT EXISTS tracks_idx_path on db_tracks(FilePath)");
    stm.Add("CREATE index IF NOT EXISTS tracks_idx_fpath on db_tracks(FolderPath)");

    foreach (string sql in stm) {
      using (SQLiteCommand statement = new SQLiteCommand(sql, m_Conn)) {
        statement.ExecuteNonQuery();
      }
    }

    return true;
  }
}