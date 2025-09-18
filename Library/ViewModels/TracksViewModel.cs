using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Data.SQLite;
using System.Text;
using System.Threading.Tasks;

namespace Library.ViewModels;

class TracksViewModel : TableViewModelBase<Classes.Track, long>
{
  public TracksViewModel(SQLiteConnection connection) :
    base(connection)
  {

  }

  static TracksViewModel m_Instance;
  public static TracksViewModel Instance
  {
    get {
      lock (typeof(TracksViewModel)) {
        if (m_Instance == null)
          m_Instance = new TracksViewModel(Database.Instance.Connection);
      }

      return m_Instance;
    }
  }

  public override string GetTableName()
  {
    return "db_tracks";
  }

  protected override List<string> GetCreateTable()
  {
    return null;
  }

  protected override string GetAllColumns()
  {
    return "Id, FilePath, AlbumId, MimeType, Title, DiscNumber, TrackNumber, Duration, Year, FileSize, LastModifed, LastPlayed, PlayedCount, ArtistId, Temp, FolderPath";
  }

  protected override string GetDefaultOrderBy()
  {
    return "DiscNumber, TrackNumber";
  }

  protected override void FillSelectAllStatement(SQLiteCommand statement)
  {
    // nothing to do
  }

  protected override Classes.Track CreateItem(DbDataReader reader)
  {
    Classes.Track t = new Classes.Track();
    t.Id = (long)reader.GetValue(0);
    t.FilePath = (string)reader.GetValue(1);
    t.Album = new Classes.Album() { Id = (long)reader.GetValue(2), IsLazy = true };
    t.MimeType = (string)reader.GetValue(3);
    t.Title = (string)reader.GetValue(4);
    t.DiscNumber = (uint)(long)reader.GetValue(5);
    t.TrackNumber = (uint)(long)reader.GetValue(6);
    t.Duration = TimeSpan.FromSeconds((Int64)reader.GetValue(7));
    t.Year = (uint)(long)reader.GetValue(8);
    t.FileSize = (long)reader.GetValue(9);
    t.LastModifed = new DateTime((long)reader.GetValue(10));
    if (t.LastModifed.Value == DateTime.MinValue)
      t.LastModifed = null;
    t.LastPlayed = new DateTime((long)reader.GetValue(11));
    if (t.LastPlayed.Value == DateTime.MinValue)
      t.LastPlayed = null;
    t.PlayedCount = (long)reader.GetValue(12);
    t.Artist = new Classes.Artist() { Id = (long)reader.GetValue(13), IsLazy = true };
    t.IsTemp = (long)reader.GetValue(14) > 0;
    t.FolderPath = (string)reader.GetValue(15);

    //LoadGenres(t);
    return t;
  }

  protected override string GetSelectItemSql()
  {
    return string.Format("{0} WHERE Id=@Id", GetSelectAllSql(false));
  }

  protected override void FillSelectItemStatement(SQLiteCommand statement, long key)
  {
    statement.Parameters.AddWithValue("@Id", key);
  }

  protected override string GetInsertItemSql()
  {
    return string.Format(@"INSERT INTO {0} ({1}) 
                                    VALUES (NULL, @FilePath, @AlbumId, @MimeType, @Title, @DiscNumber, @TrackNumber, @Duration, @Year,
                                            @FileSize, @LastModifed, @LastPlayed, @PlayedCount, @ArtistId, @Temp, @FolderPath)",
      GetTableName(), GetAllColumns());
  }

  protected override void FillInsertStatement(SQLiteCommand statement, Classes.Track item)
  {
    statement.Parameters.AddWithValue("@FilePath", item.FilePath);
    statement.Parameters.AddWithValue("@FolderPath", GetFolderPath(item.FilePath));
    statement.Parameters.AddWithValue("@AlbumId", item.Album != null ? item.Album.Id : 0);
    statement.Parameters.AddWithValue("@MimeType", item.MimeType == null ? string.Empty : item.MimeType);
    statement.Parameters.AddWithValue("@Title", item.Title == null ? string.Empty : item.Title);
    statement.Parameters.AddWithValue("@DiscNumber", (int)item.DiscNumber);
    statement.Parameters.AddWithValue("@TrackNumber", (int)item.TrackNumber);
    statement.Parameters.AddWithValue("@Duration", (int)item.Duration.TotalSeconds);
    statement.Parameters.AddWithValue("@Year", (int)item.Year);
    statement.Parameters.AddWithValue("@FileSize", item.FileSize);
    statement.Parameters.AddWithValue("@LastModifed", item.LastModifed.HasValue ? item.LastModifed.Value.Ticks : 0);
    statement.Parameters.AddWithValue("@LastPlayed", item.LastPlayed.HasValue ? item.LastPlayed.Value.Ticks : 0);
    statement.Parameters.AddWithValue("@PlayedCount", item.PlayedCount);
    statement.Parameters.AddWithValue("@ArtistId", item.Artist != null ? item.Artist.Id : 0);
    statement.Parameters.AddWithValue("@Temp", item.IsTemp ? 1 : 0);
  }

  protected override string GetUpdateItemSql()
  {
    return string.Format(@"UPDATE {0} 
                             SET FilePath=@FilePath, 
                                 FolderPath=@FolderPath,
                                 AlbumId=@AlbumId,                                  
                                 MimeType=@MimeType,
                                 Title=@Title, 
                                 DiscNumber=@DiscNumber, 
                                 TrackNumber=@TrackNumber,
                                 Duration=@Duration,
                                 Year=@Year,
                                 FileSize=@FileSize,
                                 LastModifed=@LastModifed,
                                 LastPlayed=@LastPlayed,
                                 PlayedCount=@PlayedCount,
                                 ArtistId=@ArtistId,
                                 Temp=@Temp
                             WHERE Id=@Id", GetTableName());
  }

  protected override void FillUpdateStatement(SQLiteCommand statement, long key, Classes.Track item)
  {
    statement.Parameters.AddWithValue("@Id", key);
    statement.Parameters.AddWithValue("@FilePath", item.FilePath);
    statement.Parameters.AddWithValue("@FolderPath", GetFolderPath(item.FilePath));
    statement.Parameters.AddWithValue("@AlbumId", item.Album != null ? item.Album.Id : 0);
    statement.Parameters.AddWithValue("@MimeType", item.MimeType);
    statement.Parameters.AddWithValue("@Title", item.Title);
    statement.Parameters.AddWithValue("@DiscNumber", (int)item.DiscNumber);
    statement.Parameters.AddWithValue("@TrackNumber", (int)item.TrackNumber);
    statement.Parameters.AddWithValue("@Duration", (int)item.Duration.TotalSeconds);
    statement.Parameters.AddWithValue("@Year", (int)item.Year);
    statement.Parameters.AddWithValue("@FileSize", item.FileSize);
    statement.Parameters.AddWithValue("@LastModifed", item.LastModifed.HasValue ? item.LastModifed.Value.Ticks : 0);
    statement.Parameters.AddWithValue("@LastPlayed", item.LastPlayed.HasValue ? item.LastPlayed.Value.Ticks : 0);
    statement.Parameters.AddWithValue("@PlayedCount", item.PlayedCount);
    statement.Parameters.AddWithValue("@ArtistId", item.Artist != null ? item.Artist.Id : 0);
    statement.Parameters.AddWithValue("@Temp", item.IsTemp ? 1 : 0);
  }

  protected override string GetDeleteItemSql()
  {
    return string.Format("DELETE FROM {0} WHERE Id=@id", GetTableName());
  }

  protected override void FillDeleteItemStatement(SQLiteCommand statement, long key)
  {
    statement.Parameters.AddWithValue("@id", key);
  }

  public override async Task<bool> InsertItem(Classes.Track item)
  {
    await base.InsertItem(item);
    SaveGenres(item);
    return true;
  }

  public override async Task<bool> UpdateItem(long key, Classes.Track item)
  {
    await base.UpdateItem(key, item);
    SaveGenres(item);
    return true;
  }

  public override async Task<bool> DeleteItem(long key)
  {
    await base.DeleteItem(key);

    string sql = string.Format("delete from db_track_genres where TrackId=@TrackId");
    using (SQLiteCommand statement = new SQLiteCommand(sql, Database.Instance.Connection)) {
      statement.Parameters.AddWithValue("@TrackId", key);
      await statement.ExecuteNonQueryAsync();
    }
    return true;
  }

  public void TrackPlayed(long trackId, long count, DateTime dateTime)
  {
    if (trackId == 0)
      return;

    string sql = string.Format("update {0} set PlayedCount=@PlayedCount, LastPlayed=@LastPlayed where Id=@TrackId", GetTableName());
    using (SQLiteCommand statement = new SQLiteCommand(sql, Database.Instance.Connection)) {
      statement.Parameters.AddWithValue("@PlayedCount", count);
      statement.Parameters.AddWithValue("@LastPlayed", dateTime.Ticks);
      statement.Parameters.AddWithValue("@TrackId", trackId);
      statement.ExecuteNonQuery();
    }
  } // TrackPlayed

  public async Task<bool> LoadGenres(Classes.Track item)
  {
    item.Genres = new List<Classes.Genre>();
    string sql = string.Format("select GenreId from db_track_genres where TrackId=@TrackId");
    using (SQLiteCommand statement = new SQLiteCommand(sql, Database.Instance.Connection)) {
      statement.Parameters.AddWithValue("@TrackId", item.Id);
      using (var reader = await statement.ExecuteReaderAsync()) {
        while (await reader.ReadAsync()) {
          long genreId = (long)reader.GetValue(0);
          Classes.Genre genre = await ViewModels.GenresViewModel.Instance.GetItem(genreId);
          if (genre != null)
            item.Genres.Add(genre);
        }
      }
    }
    return true;
  }

  private void SaveGenres(Classes.Track item)
  {
    List<Classes.Genre> genres = item.Genres; // Be sure to use item.Genres because of lazy loading
    string sql = string.Format("delete from db_track_genres where TrackId=@TrackId");
    using (SQLiteCommand statement = new SQLiteCommand(sql, Database.Instance.Connection)) {
      statement.Parameters.AddWithValue("@TrackId", item.Id);
      statement.ExecuteNonQuery();
    }

    foreach (Classes.Genre g in genres) {
      sql = string.Format("insert into db_track_genres(TrackId, GenreId) values(@TrackId, @GenreId)");
      using (SQLiteCommand statement = new SQLiteCommand(sql, Database.Instance.Connection)) {
        statement.Parameters.AddWithValue("@TrackId", item.Id);
        statement.Parameters.AddWithValue("@GenreId", g.Id);
        statement.ExecuteNonQuery();
      }
    }
  }

  public async Task<List<Classes.Track>> GetLastAddedTracks(int number)
  {
    List<Classes.Track> res = new List<Classes.Track>();
    string sql = string.Format("select {0} from {1} order by LastModifed desc LIMIT {2}",
      GetAllColumns(), GetTableName(), number.ToString());
    using (SQLiteCommand statement = new SQLiteCommand(sql, Database.Instance.Connection)) {
      using (var reader = await statement.ExecuteReaderAsync()) {
        while (await reader.ReadAsync())
          res.Add(CreateItem(reader));
      }
    }
    return res;
  } // GetLastAddedTracks

  public async Task<List<Classes.Track>> GetPopularTracks(int number)
  {
    List<Classes.Track> res = new List<Classes.Track>();
    string sql = string.Format("select {0} from {1} order by PlayedCount desc, LastPlayed desc LIMIT {2}",
      GetAllColumns(), GetTableName(), number.ToString());
    using (SQLiteCommand statement = new SQLiteCommand(sql, Database.Instance.Connection)) {
      using (var reader = await statement.ExecuteReaderAsync()) {
        while (reader.Read()) {
          Classes.Track t = CreateItem(reader);
          if (t.PlayedCount > 0)
            res.Add(t);
        }
      }
    }
    return res;
  } // GetPopularTracks

  public async Task<List<Classes.Track>> GetTracks()
  {
    return await GetTracks(string.Empty);
  }

  public async Task<List<Classes.Track>> GetTracks(string filter)
  {
    string where = string.Empty;
    Dictionary<string, object> args = null;
    if (!string.IsNullOrEmpty(filter)){
      where = "Title LIKE @Filter";
      args = new Dictionary<string, object>() { {"@Filter", string.Format("%{0}%", filter) } };
    }

    List<Classes.Track> items = new List<Classes.Track>();
    items.AddRange(await GetItems(where, "title", args));
    return items;
  }

  public async Task<List<Classes.Track>> GetTracks(List<string> filePaths)
  {
    int pages = filePaths.Count / 100;
    if (filePaths.Count % 100 >= 1)
      pages++;

    Dictionary<string, Classes.Track> tracksDict = new Dictionary<string, Classes.Track>();
    for (int page = 0; page < pages; page++) {
      Dictionary<string, object>  args = new Dictionary<string, object>();
      StringBuilder sb = new StringBuilder();
      sb.Append("FilePath in (");
      for (int i = 0; i < 100; i++) {
        int idx = page * 100 + i;
        if (idx < filePaths.Count) {
          if (i > 0)
            sb.Append(",");
          string arg = string.Format("@fp{0}", i);
          sb.Append(arg);
          args[arg] = filePaths[idx];
        }
      }
      sb.Append(")");

      ObservableCollection<Classes.Track> tracks = await GetItems(sb.ToString(), args);
      foreach (Classes.Track t in tracks)
        tracksDict[t.FilePath] = t;
    }

    var items = new List<Classes.Track>();
    foreach (string fp in filePaths) {
      Classes.Track item = null;
      if (tracksDict.TryGetValue(fp, out item))
        items.Add(item);
    }
    return items;
  } // GetTracks

  public async Task<List<Classes.Track>> GetTracks(List<long> ids)
  {
    int pages = ids.Count / 100;
    if (ids.Count % 100 >= 1)
      pages++;

    Dictionary<long, Classes.Track> tracksDict = new Dictionary<long, Classes.Track>();
    for (int page = 0; page < pages; page++) {
      Dictionary<string, object>  args = new Dictionary<string, object>();
      StringBuilder sb = new StringBuilder();
      sb.Append("id in (");
      for (int i = 0; i < 100; i++) {
        int idx = page * 100 + i;
        if (idx < ids.Count) {
          if (i > 0)
            sb.Append(",");
          string arg = string.Format("@id{0}", i);
          sb.Append(arg);
          args[arg] = ids[idx];
        }
      }
      sb.Append(")");

      ObservableCollection<Classes.Track> tracks = await GetItems(sb.ToString(), args);
      foreach (Classes.Track t in tracks)
        tracksDict[t.Id] = t;
    }

    var items = new List<Classes.Track>();
    foreach (int id in ids) {
      Classes.Track item = null;
      if (tracksDict.TryGetValue(id, out item))
        items.Add(item);
    }
    return items;
  } // GetTracks

  public async Task<long> CountTracksFromPath(string folderPath)
  {
    long count = 0;
    string sql = string.Format("select count(*) from {0} where FolderPath=@FolderPath", GetTableName());
    using (SQLiteCommand statement = new SQLiteCommand(sql, Database.Instance.Connection)) {
      statement.Parameters.AddWithValue("@FolderPath", folderPath);
      using (var reader = await statement.ExecuteReaderAsync()) {
        if (await reader.ReadAsync())
          count = (long)reader.GetValue(0);
      }
    }

    return count;
  }

  public async Task<Classes.Track> GetTrackFromPath(string path)
  {
    ObservableCollection<Classes.Track> tracks = await GetItems("FilePath=@FilePath", new Dictionary<string, object>() { {"@FilePath", path} });
    if (tracks != null && tracks.Count > 0)
      return tracks[0];
    return null;
  }

  public void SetTracksTemp()
  {
    string sql = "Update db_tracks set Temp=1";
    using (SQLiteCommand statement = new SQLiteCommand(sql, Database.Instance.Connection)) {
      statement.ExecuteNonQuery();
    }
  }

  public void SetTracksNotTemp(string folderPath)
  {
    if (folderPath.EndsWith("\\"))
      folderPath = folderPath.Remove(folderPath.Length - 1, 1);
    string sql = "Update db_tracks set Temp=0 where FolderPath=@FolderPath";
    using (SQLiteCommand statement = new SQLiteCommand(sql, Database.Instance.Connection)) {
      statement.Parameters.AddWithValue("@FolderPath", folderPath);
      statement.ExecuteNonQuery();
    }
  }

  public async Task<int> DeleteTempTracks()
  {
    long count = await ItemsCount();
    string sql = "delete from db_tracks where Temp=1";
    using (SQLiteCommand statement = new SQLiteCommand(sql, Database.Instance.Connection))
      await statement.ExecuteNonQueryAsync();

    sql = string.Format("delete from db_track_genres where not exists(select 1 from db_tracks where Id=db_track_genres.TrackId)");
    using (SQLiteCommand statement = new SQLiteCommand(sql, Database.Instance.Connection))
      await statement.ExecuteNonQueryAsync();

    int res = (int)(count - await ItemsCount());
    return res;
  }
}