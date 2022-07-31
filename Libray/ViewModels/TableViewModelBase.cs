using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Data.SQLite;
using System.Data.Common;

namespace Library.ViewModels
{
  public abstract class ItemTypeBase
  {
    public long Id
    {
      get;
      set;
    }

    public bool IsLazy
    {
      get;
      set;
    }
  }

  public abstract class TableViewModelBase<TItemType, TKeyType> where TItemType : ItemTypeBase
  {
    protected class TableViewModelCache<CItemType, CKeyType> where CItemType : ItemTypeBase
    {
      Mutex m_Mutex = new Mutex();
      Dictionary<CKeyType, CItemType> m_Cache = new Dictionary<CKeyType, CItemType>();
      List<CKeyType> m_Keys = new List<CKeyType>();

      public TableViewModelCache()
      {
        CacheLength = 50;
      }

      public int CacheLength
      {
        get;
        set;
      }

      public CItemType Get(CKeyType key)
      {
        m_Mutex.WaitOne();
        CItemType item = null;
        if (m_Cache.TryGetValue(key, out item)) {
          m_Keys.Remove(key);
          m_Keys.Add(key);
        }
        m_Mutex.ReleaseMutex();
        return item;
      }

      public void Add(CKeyType key, CItemType item)
      {
        m_Mutex.WaitOne();
        while (m_Keys.Count >= CacheLength) {
          CKeyType lKey = m_Keys.First();
          m_Cache.Remove(lKey);
          m_Keys.Remove(lKey);
        }

        if (m_Keys.Contains(key))
          m_Keys.Remove(key);
        m_Keys.Add(key);
        m_Cache[key] = item;
        m_Mutex.ReleaseMutex();
      }

      public void Remove(CKeyType key)
      {
        m_Mutex.WaitOne();
        if (m_Keys.Contains(key)) {
          m_Keys.Remove(key);
          m_Cache.Remove(key);
        }
        m_Mutex.ReleaseMutex();
      }

      public void Clear()
      {
        m_Mutex.WaitOne();
        m_Keys.Clear();
        m_Cache.Clear();
        m_Mutex.ReleaseMutex();
      }
    } // TableViewModelCache

    TableViewModelCache<TItemType, TKeyType> m_Cache = new TableViewModelCache<TItemType, TKeyType>();
    bool m_CacheEnabled = true;

    public TableViewModelBase(SQLiteConnection connection)
    {
      sqlConnection = connection;
    }

    public abstract string GetTableName();
    protected abstract List<string> GetCreateTable();
    protected abstract string GetAllColumns();
    protected abstract string GetDefaultOrderBy();
    protected abstract void FillSelectAllStatement(SQLiteCommand statement);

    protected abstract TItemType CreateItem(DbDataReader reader);

    protected abstract string GetSelectItemSql();
    protected abstract void FillSelectItemStatement(SQLiteCommand statement, TKeyType key);

    protected abstract string GetDeleteItemSql();
    protected abstract void FillDeleteItemStatement(SQLiteCommand statement, TKeyType key);

    protected abstract string GetInsertItemSql();
    protected abstract void FillInsertStatement(SQLiteCommand statement, TItemType item);

    protected abstract string GetUpdateItemSql();
    protected abstract void FillUpdateStatement(SQLiteCommand statement, TKeyType key, TItemType item);

    protected SQLiteConnection sqlConnection
    {
      get;
      set;
    }

    public void EnableCache()
    {
      m_CacheEnabled = true;
    }

    public void DisableCache()
    {
      m_CacheEnabled = true;
      m_Cache.Clear();
    }

    protected string GetSelectAllSql(bool includeOrderBy)
    {
      if (includeOrderBy && !string.IsNullOrEmpty(GetDefaultOrderBy()))
        return string.Format("SELECT {0} FROM {1} ORDER BY {2}", GetAllColumns(), GetTableName(), GetDefaultOrderBy());
      return string.Format("SELECT {0} FROM {1}", GetAllColumns(), GetTableName());
    }

    public async Task<ObservableCollection<TItemType>> GetAllItems()
    {
      var items = new ObservableCollection<TItemType>();
      using (SQLiteCommand statement = new SQLiteCommand(GetSelectAllSql(true), Database.Instance.Connection)) {
        FillSelectAllStatement(statement);
        using (var reader = await statement.ExecuteReaderAsync()) {
          while (await reader.ReadAsync()) {
            var item = CreateItem(reader);
            items.Add(item);
          }
        }
      }
      return items;
    }

    public async Task<ObservableCollection<TItemType>> GetItems(string query, Dictionary<string, object> arguments)
    {
      return await GetItems(query, string.Empty, arguments);
    }

    public async Task<ObservableCollection<TItemType>> GetItems(string query, string orderBy, Dictionary<string, object> arguments)
    {
      var items = new ObservableCollection<TItemType>();
      string sql = GetSelectAllSql(false);
      if (!string.IsNullOrEmpty(query))
        sql = string.Format("{0} WHERE {1}", sql, query);
      if (!string.IsNullOrEmpty(orderBy))
        sql = string.Format("{0} ORDER BY {1}", sql, orderBy);
      else if (!string.IsNullOrEmpty(GetDefaultOrderBy()))
        sql = string.Format("{0} ORDER BY {1}", sql, GetDefaultOrderBy());

      using (SQLiteCommand statement = new SQLiteCommand(sql, Database.Instance.Connection)) {
        FillSelectAllStatement(statement);
        if (arguments != null) {
          foreach (KeyValuePair<string, object> arg in arguments) {
            if (arg.Value is Int32)
              statement.Parameters.AddWithValue(arg.Key, (Int32)arg.Value);
            else if (arg.Value is Int64)
              statement.Parameters.AddWithValue(arg.Key, (Int64)arg.Value);
            else if (arg.Value is string)
              statement.Parameters.AddWithValue(arg.Key, (string)arg.Value);
            else
              throw new Exception(string.Format("Unmanaged parameter {0}", arg.Key));
          }
        }
        using (var reader = await statement.ExecuteReaderAsync()) {
          while (await reader.ReadAsync())
            items.Add(CreateItem(reader));
        }
      }
      return items;
    }

    public async Task<TItemType> GetItem(TKeyType key)
    {
      if (m_CacheEnabled) {
        TItemType fc = m_Cache.Get(key);
        if (fc != null)
          return fc;
      }

      using (SQLiteCommand statement = new SQLiteCommand(GetSelectItemSql(), Database.Instance.Connection)) {
        FillSelectItemStatement(statement, key);

        using (var reader = await statement.ExecuteReaderAsync()) {
          if (await reader.ReadAsync()) {
            var item = CreateItem(reader);
            if (m_CacheEnabled)
              m_Cache.Add(key, item);
            return item;
          }
        }
      }

      return null;
    }

    public virtual async Task<bool> InsertItem(TItemType item)
    {
      using (SQLiteCommand statement = new SQLiteCommand(GetInsertItemSql(), Database.Instance.Connection)) {
        FillInsertStatement(statement, item);
        await statement.ExecuteNonQueryAsync();

        item.Id = GetLastInsertedRowId();
      }
      return true;
    }

    public virtual async Task<bool> UpdateItem(TKeyType key, TItemType item)
    {
      using (SQLiteCommand statement = new SQLiteCommand(GetUpdateItemSql(), Database.Instance.Connection)) {
        FillUpdateStatement(statement, key, item);
        await statement.ExecuteNonQueryAsync();
        if (m_CacheEnabled)
          m_Cache.Add(key, item);
      }
      return true;
    }

    public virtual async Task<bool> DeleteItem(TKeyType key)
    {
      using (SQLiteCommand statement = new SQLiteCommand(GetDeleteItemSql(), Database.Instance.Connection)) {
        FillDeleteItemStatement(statement, key);
        await statement.ExecuteNonQueryAsync();
        m_Cache.Remove(key);
      }
      return true;
    }

    public async Task<long> ItemsCount()
    {
      long count = 0;
      string sql = string.Format("select count(*) from {0}", GetTableName());
      using (SQLiteCommand statement = new SQLiteCommand(sql, Database.Instance.Connection)) {
        count = (long) await statement.ExecuteScalarAsync();
      }
      return count;
    }

    protected string GetFolderPath(string fullPath)
    {
      string folderPath = string.Empty;
      string fileName = fullPath;
      int idx = fullPath.LastIndexOf("\\");
      if (idx > 0){
        folderPath = fullPath.Substring(0, idx);
        fileName = fullPath.Substring(idx + 1).Trim();
      }
      return folderPath;
    }

    protected long GetLastInsertedRowId()
    {
      long lastId = 0;
      string sql = @"select last_insert_rowid()";
      using (SQLiteCommand statement = new SQLiteCommand(sql, Database.Instance.Connection)) { 
        lastId = (long)statement.ExecuteScalar();
      }
      return lastId;
    } // LastInserttediId
  }
}
