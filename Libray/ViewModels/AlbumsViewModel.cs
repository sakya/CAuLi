using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.ViewModels
{
  class AlbumsViewModel : TableViewModelBase<Classes.Album, long>
  {
    public AlbumsViewModel(SQLiteConnection connection) : 
      base(connection)
    {

    }

    static AlbumsViewModel m_Instance;
    public static AlbumsViewModel Instance 
    {
      get {
        lock (typeof(AlbumsViewModel)) {
          if (m_Instance == null)
            m_Instance = new AlbumsViewModel(Database.Instance.Connection);
        }

        return m_Instance;
      }
    }

    public override string GetTableName()
    {
      return "db_Albums";
    }

    protected override List<string> GetCreateTable()
    {
      return null;
    }

    protected override string GetAllColumns()
    {
      return "Id, ArtistId, Title, AlbumArt, Year, TracksCount, Duration";
    }

    protected override string GetDefaultOrderBy()
    {
      return "Title";
    }

    protected override void FillSelectAllStatement(SQLiteCommand statement)
    {
      // nothing to do
    }

    protected override Classes.Album CreateItem(DbDataReader reader)
    {
      Classes.Album a = new Classes.Album();
      a.Id = (long)reader.GetValue(0);
      a.Artist = new Classes.Artist() { Id = (long)reader.GetValue(1), IsLazy = true };
      a.Title = (string)reader.GetValue(2);
      a.AlbumArt = (string)reader.GetValue(3);
      a.Year = (uint)(long)reader.GetValue(4);
      a.TracksCount = (long)reader.GetValue(5);
      a.Duration = TimeSpan.FromSeconds((long)reader.GetValue(6));

      if (a.Title == null)
        a.Title = string.Empty;
      return a;
    }

    protected override string GetSelectItemSql()
    {
      return string.Format("{0} WHERE Id = ?", GetSelectAllSql(false));
    }

    protected override void FillSelectItemStatement(SQLiteCommand statement, long key)
    {
      statement.Parameters.AddWithValue("@id", key);
    }

    protected override string GetInsertItemSql()
    {
      return string.Format("INSERT INTO {0} ({1}) VALUES (NULL, @ArtistId, @Title, @AlbumArt, @Year, @TracksCount, @Duration)", GetTableName(), GetAllColumns());
    }

    protected override void FillInsertStatement(SQLiteCommand statement, Classes.Album item)
    {
      statement.Parameters.AddWithValue("@ArtistId", item.Artist != null ? item.Artist.Id : 0);
      statement.Parameters.AddWithValue("@Title", item.Title == null ? string.Empty : item.Title);
      statement.Parameters.AddWithValue("@AlbumArt", item.AlbumArt == null ? string.Empty : item.AlbumArt);
      statement.Parameters.AddWithValue("@Year", (int)item.Year);
      statement.Parameters.AddWithValue("@TracksCount", item.TracksCount);
      statement.Parameters.AddWithValue("@Duration", (int)item.Duration.TotalSeconds);
    }

    protected override string GetUpdateItemSql()
    {
      return string.Format(@"UPDATE {0} 
                             SET ArtistId=@ArtistId, 
                                 Title=@Title, 
                                 AlbumArt=@AlbumArt,
                                 Year=@Year, 
                                 TracksCount=@TracksCount, 
                                 Duration=@Duration
                             WHERE Id=@Id", GetTableName());
    }

    protected override void FillUpdateStatement(SQLiteCommand statement, long key, Classes.Album item)
    {
      statement.Parameters.AddWithValue("@Id", key);
      statement.Parameters.AddWithValue("@ArtistId", item.Artist != null ? item.Artist.Id : 0);
      statement.Parameters.AddWithValue("@Title", item.Title);
      statement.Parameters.AddWithValue("@AlbumArt", item.AlbumArt);
      statement.Parameters.AddWithValue("@Year", (int)item.Year);
      statement.Parameters.AddWithValue("@TracksCount", item.TracksCount);
      statement.Parameters.AddWithValue("@Duration", (int)item.Duration.TotalSeconds);
    }

    protected override string GetDeleteItemSql()
    {
      return string.Format("DELETE FROM {0} WHERE Id=@Id", GetTableName());
    }

    protected override void FillDeleteItemStatement(SQLiteCommand statement, long key)
    {
      statement.Parameters.AddWithValue("@Id", key);
    }

    public async Task<List<Classes.Album>> GetAlbums(string filter)
    {
      string where = string.Empty;
      Dictionary<string, object> args = null;
      if (!string.IsNullOrEmpty(filter)){
        where = "Title LIKE @Filter";
        args = new Dictionary<string, object>() { {"@Filter", string.Format("%{0}%", filter) } };
      }

      List<Classes.Album> items = new List<Classes.Album>();
      items.AddRange(await GetItems(where, args));
      return items;
    }

    public async Task<List<Classes.Album>> GetAlbums()
    {
      return await GetAlbums(string.Empty);
    }

    public async Task<List<Classes.Album>> GetArtistAlbums(long artistId)
    {
      string where = string.Empty;
      if (artistId > 0)
        where = "ArtistId=@ArtistId";

      List<Classes.Album> items = new List<Classes.Album>();
      items.AddRange(await GetItems(where, artistId > 0 ? new Dictionary<string, object>() { {"@ArtistId", artistId} } : null));
      return items;
    }

    public async Task<Classes.Album> GetItem(long artistId, string title)
    {
      string sql = string.Format("Select {0} from {1} where ArtistId=@ArtistId and Title=@Title", GetAllColumns(), GetTableName());
      using (SQLiteCommand statement = new SQLiteCommand(sql, Database.Instance.Connection)) {
        statement.Parameters.AddWithValue("@ArtistId", artistId);
        statement.Parameters.AddWithValue("@Title", title);

        using (var reader = await statement.ExecuteReaderAsync()) {
          if (await reader.ReadAsync()) {
            var item = CreateItem(reader);
            return item;
          }
        }
      }
      return null;
    }

    public async Task<bool> OptimizeData()
    {
      string sql = string.Format(@"select alb.Id, alb.ArtistId, alb.Title, alb.AlbumArt, alb.Year, alb.TracksCount, alb.Duration, 
                                   coalesce(count(trk.id), 0),  coalesce(sum(trk.Duration), 0),
                                   case when (select count(distinct(year)) from {1} where AlbumId = alb.Id) = 1 then (select distinct(year) from {1} where AlbumId = alb.Id limit 1) else 0 end as year
                                   from {0} alb 
                                   left outer join {1} trk 
                                     on trk.AlbumId = alb.Id
                                   group by alb.Id, alb.ArtistId, alb.Title, alb.AlbumArt, alb.Year, alb.TracksCount, alb.Duration",
                                   GetTableName(),
                                   ViewModels.TracksViewModel.Instance.GetTableName());
      using (SQLiteCommand statement = new SQLiteCommand(sql, Database.Instance.Connection)) {
        using (var reader = await statement.ExecuteReaderAsync()) {
          while (await reader.ReadAsync()) {
            var item = CreateItem(reader);
            item.TracksCount = (long)reader.GetValue(7);
            item.Duration = TimeSpan.FromSeconds((long)reader.GetValue(8));
            item.Year = (uint)(long)reader.GetValue(9);
            if (item.TracksCount == 0)
              await DeleteItem(item.Id);
            else
              await UpdateItem(item.Id, item);
          }
        }
      }
      return true;
    }
  }
}
