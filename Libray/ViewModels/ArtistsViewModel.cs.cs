using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.ViewModels
{
  class ArtistsViewModel : TableViewModelBase<Classes.Artist, long>
  {
    public ArtistsViewModel(SQLiteConnection connection) : 
      base(connection)
    {

    }

    static ArtistsViewModel m_Instance;
    public static ArtistsViewModel Instance 
    {
      get {
        lock (typeof(ArtistsViewModel)) {
          if (m_Instance == null)
            m_Instance = new ArtistsViewModel(Database.Instance.Connection);
        }

        return m_Instance;
      }
    }

    public override string GetTableName()
    {
      return "db_artists";
    }

    protected override List<string> GetCreateTable()
    {
      return null;
    }

    protected override string GetAllColumns()
    {
      return "Id, Name, ArtistArt, AlbumsCount, TracksCount";
    }

    protected override string GetDefaultOrderBy()
    {
      return "Name";
    }

    protected override void FillSelectAllStatement(SQLiteCommand statement)
    {
      // nothing to do
    }

    protected override Classes.Artist CreateItem(DbDataReader reader)
    {
      Classes.Artist a = new Classes.Artist();
      a.Id = (long)reader.GetValue(0);
      a.Name = (string)reader.GetValue(1);
      a.ArtistArt = (string)reader.GetValue(2);
      a.AlbumsCount = (long)reader.GetValue(3);
      a.TracksCount = (long)reader.GetValue(4);
      return a;
    }

    protected override string GetSelectItemSql()
    {
      return string.Format("{0} WHERE Id=@id", GetSelectAllSql(false));
    }

    protected override void FillSelectItemStatement(SQLiteCommand statement, long key)
    {
      statement.Parameters.AddWithValue("@id", key);
    }

    protected override string GetInsertItemSql()
    {
      return string.Format("INSERT INTO {0} ({1}) VALUES (NULL, @Name, @ArtistArt, @AlbumsCount, @TracksCount)", GetTableName(), GetAllColumns());
    }

    protected override void FillInsertStatement(SQLiteCommand statement, Classes.Artist item)
    {
      statement.Parameters.AddWithValue("@Name", item.Name == null ? string.Empty : item.Name);
      statement.Parameters.AddWithValue("@ArtistArt", item.ArtistArt == null ? string.Empty : item.ArtistArt);
      statement.Parameters.AddWithValue("@AlbumsCount", item.AlbumsCount);
      statement.Parameters.AddWithValue("@TracksCount", item.TracksCount);
    }

    protected override string GetUpdateItemSql()
    {
      return string.Format(@"UPDATE {0} 
                             SET Name=@Name,
                                 ArtistArt=@ArtistArt,
                                 AlbumsCount=@AlbumsCount,
                                 TracksCount=@TracksCount
                             WHERE Id=@Id", GetTableName());
    }

    protected override void FillUpdateStatement(SQLiteCommand statement, long key, Classes.Artist item)
    {
      statement.Parameters.AddWithValue("@Id", item.Id);
      statement.Parameters.AddWithValue("@Name", item.Name);
      statement.Parameters.AddWithValue("@ArtistArt", item.ArtistArt);
      statement.Parameters.AddWithValue("@AlbumsCount", item.AlbumsCount);
      statement.Parameters.AddWithValue("@TracksCount", item.TracksCount);
    }

    protected override string GetDeleteItemSql()
    {
      return string.Format("DELETE FROM {0} WHERE Id=@Id", GetTableName());
    }

    protected override void FillDeleteItemStatement(SQLiteCommand statement, long key)
    {
      statement.Parameters.AddWithValue("@Id", key);
    }

    protected string GetSelectArtistSql()
    {
      return string.Format("{0} WHERE Name=@Name", GetSelectAllSql(false));
    }

    public async Task<Classes.Artist> GetItem(string name)
    {
      using (SQLiteCommand statement = new SQLiteCommand(GetSelectArtistSql(), Database.Instance.Connection)) {
        statement.Parameters.AddWithValue("@Name", name);
        using (var reader = await statement.ExecuteReaderAsync()) {
          if (await reader.ReadAsync()) {
            var item = CreateItem(reader);
            return item;
          }
        }
      }
      return null;
    }

    public async Task<List<Classes.Artist>> GetArtists()
    {
      return await GetArtists(string.Empty);
    }

    public async Task<List<Classes.Artist>> GetArtists(string filter)
    {
      string where = string.Empty;
      Dictionary<string, object> args = null;
      if (!string.IsNullOrEmpty(filter)){
        where = "Name LIKE @Filter";
        args = new Dictionary<string, object>() { {"@Filter", string.Format("%{0}%", filter) } };
      }

      List<Classes.Artist> items = new List<Classes.Artist>();
      items.AddRange(await GetItems(where, args));
      return items;
    }

    public async Task<bool> OptimizeData()
    {
      string sql = string.Format(@"select art.Id, art.Name, art.ArtistArt,
                                   (select coalesce(count(trk.Id), 0) from {1} trk                                                     
                                    where trk.ArtistId = art.Id) as tracks,
                                   (select coalesce(count(alb.Id), 0) from {2} alb where ArtistId = art.Id) as albums
                                   from {0} art", 
                                   GetTableName(), 
                                   TracksViewModel.Instance.GetTableName(), 
                                   AlbumsViewModel.Instance.GetTableName());
      using (SQLiteCommand statement = new SQLiteCommand(sql, Database.Instance.Connection)) {
        using (var reader = await statement.ExecuteReaderAsync()) {
          while (await reader.ReadAsync()) {
            var item = CreateItem(reader);
            item.TracksCount = (long)reader.GetValue(3);
            item.AlbumsCount = (long)reader.GetValue(4);
            if (item.TracksCount == 0 && item.AlbumsCount == 0)
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
