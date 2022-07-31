using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.ViewModels
{
  class GenresViewModel : TableViewModelBase<Classes.Genre, long>
  {
    public GenresViewModel(SQLiteConnection connection) : 
      base(connection)
    {

    }

    static GenresViewModel m_Instance;
    public static GenresViewModel Instance 
    {
      get {
        lock (typeof(GenresViewModel)) {
          if (m_Instance == null)
            m_Instance = new GenresViewModel(Database.Instance.Connection);
        }

        return m_Instance;
      }
    }

    public override string GetTableName()
    {
      return "db_genres";
    }

    protected override List<string> GetCreateTable()
    {
      return null;
    }

    protected override string GetAllColumns()
    {
      return "Id, Name, TracksCount";
    }

    protected override string GetDefaultOrderBy()
    {
      return "Name";
    }

    protected override void FillSelectAllStatement(SQLiteCommand statement)
    {
      // nothing to do
    }

    protected override Classes.Genre CreateItem(DbDataReader reader)
    {
      Classes.Genre a = new Classes.Genre();
      a.Id = (long)reader.GetValue(0);
      a.Name = (string)reader.GetValue(1);
      a.TracksCount = (long)reader.GetValue(2);
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
      return string.Format("INSERT INTO {0} ({1}) VALUES (NULL, @Name, @TracksCount)", GetTableName(), GetAllColumns());
    }

    protected override void FillInsertStatement(SQLiteCommand statement, Classes.Genre item)
    {
      statement.Parameters.AddWithValue("@Name", item.Name);
      statement.Parameters.AddWithValue("@TracksCount", item.TracksCount);
    }

    protected override string GetUpdateItemSql()
    {
      return string.Format(@"UPDATE {0} 
                             SET Name=@Name,
                                 TracksCount=@TracksCount
                             WHERE Id=@Id", GetTableName());
    }

    protected override void FillUpdateStatement(SQLiteCommand statement, long key, Classes.Genre item)
    {
      statement.Parameters.AddWithValue("@Id", item.Id);
      statement.Parameters.AddWithValue("@Name", item.Name);
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

    public override async Task<bool> DeleteItem(long key)
    {
      await base.DeleteItem(key);

      string sql = string.Format("delete from db_track_genres where GenreId=@GenreId");
      using (SQLiteCommand statement = new SQLiteCommand(sql, Database.Instance.Connection)) {
        statement.Parameters.AddWithValue("@GenreId", key);
        await statement.ExecuteNonQueryAsync();
      }
      return true;
    }

    protected string GetSelectGenreSql()
    {
      return string.Format("{0} WHERE Name=@Name", GetSelectAllSql(false));
    }

    public async Task<Classes.Genre> GetItem(string name)
    {
      using (SQLiteCommand statement = new SQLiteCommand(GetSelectGenreSql(), Database.Instance.Connection)) {
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

    public async Task<List<Classes.Genre>> GetGenres(string filter)
    {
      string where = string.Empty;
      Dictionary<string, object> args = null;
      if (!string.IsNullOrEmpty(filter)){
        where = "Name LIKE @Filter";
        args = new Dictionary<string, object>() { {"@Filter", string.Format("%{0}%", filter) } };
      }

      List<Classes.Genre> items = new List<Classes.Genre>();
      items.AddRange(await GetItems(where, args));
      return items;
    }

    public async Task<List<Classes.Genre>> GetGenres()
    {
      return await GetGenres(string.Empty);
    }

    public async Task<bool> OptimizeData()
    {
      string sql = string.Format(@"select gen.Id, gen.Name, 
                                   (select count(trk.TrackId) from db_track_genres trk
                                    where trk.GenreId = gen.Id) as tracks
                                   from {0} gen", 
                                   GetTableName());
      using (SQLiteCommand statement = new SQLiteCommand(sql, Database.Instance.Connection)) {
        using (var reader = await statement.ExecuteReaderAsync()) {
          while (await reader.ReadAsync()) {
            var item = CreateItem(reader);
            item.TracksCount = (long)reader.GetValue(2);
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
