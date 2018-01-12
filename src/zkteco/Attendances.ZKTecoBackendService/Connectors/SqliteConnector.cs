using Attendances.ZKTecoBackendService.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;

namespace Attendances.ZKTecoBackendService.Connectors
{
    /// <summary>
    /// One single instance
    /// </summary>
    public class SqliteConnector
    {
        /// <summary>
        /// Inner sqlite connection instance.
        /// </summary>
        private SQLiteConnection _connection;

        public SqliteConnector()
        {
            _connection = new SQLiteConnection(DbInstaller.ConnectionString);
        }

        public SQLiteConnection Connection
        {
            get
            {
                if (_connection.State == ConnectionState.Closed)
                {
                    _connection.Open();
                }
                return _connection;
            }
        }

        public void Execute(string sql, Dictionary<string, object> args)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentNullException("sql");
            }

            try
            {                
                var command = new SQLiteCommand(Connection);
                command.CommandText = sql;
                foreach (var pair in args)
                {
                    command.Parameters.Add(new SQLiteParameter(pair.Key, pair.Value));
                }                
                command.ExecuteNonQuery();
            }
            catch (SQLiteException)
            {
                throw;
            }
        }

        public SQLiteDataReader QuerySet(string sql, Dictionary<string, object> args = null)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentNullException("sql");
            }

            try
            {
                var command = new SQLiteCommand(Connection);
                command.CommandText = sql;
                command.CommandType = CommandType.Text;
                if (args != null && args.Count > 0)
                {
                    foreach (var pair in args)
                    {
                        command.Parameters.Add(new SQLiteParameter(pair.Key, pair.Value));
                    }
                }                
                return command.ExecuteReader();
            }
            catch (SQLiteException)
            {
                throw;
            }
        }

        public object QueryScalar(string sql, Dictionary<string, object> args = null)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentNullException("sql");
            }

            try
            {
                var command = new SQLiteCommand(Connection);
                command.CommandText = sql;
                command.CommandType = CommandType.Text;
                if (args != null && args.Count > 0)
                {
                    foreach (var pair in args)
                    {
                        command.Parameters.Add(new SQLiteParameter(pair.Key, pair.Value));
                    }
                }                    
                return command.ExecuteScalar();
            }
            catch (SQLiteException)
            {
                throw;
            }
        }

        /// <summary>
        /// Release inner connection instance.
        /// </summary>
        public void Dispose()
        {
            if (_connection != null 
                && _connection.State == ConnectionState.Open)
            {
                _connection.Close();
                _connection = null;
            }
        }
    }
}
