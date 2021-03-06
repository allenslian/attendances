﻿using System.Data.SQLite;
using System.IO;

namespace Attendances.ZKTecoBackendService.Utils
{
    public static class DbInstaller
    {
        const string SYNCDB = "syncdb.sqlite";

        private static SQLiteConnectionStringBuilder _builder;

        private static string _dbPath;

        static DbInstaller()
        {
            _dbPath = Path.Combine(GlobalConfig.AppRootFolder, "data", SYNCDB);

            _builder = new SQLiteConnectionStringBuilder
            {
                DataSource = _dbPath,
                Version = 3
            };
        }

        public static string ConnectionString
        {
            get { return _builder.ConnectionString; }
        }

        #region install database

        public static void Install()
        {
            if (!File.Exists(_dbPath))
            {
                var dir = Path.GetDirectoryName(_dbPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                SQLiteConnection.CreateFile(_dbPath);
                CreateTableStructures(_builder);
            }           
        }

        private static void CreateTableStructures(SQLiteConnectionStringBuilder builder)
        {
            var connection = new SQLiteConnection(builder.ConnectionString);
            try
            {
                connection.Open();
                var tx = connection.BeginTransaction();

                var command = new SQLiteCommand(connection);
                CreateUserMapTable(command);
                CreateQueueTable(command);
                CreateFailedQueueTable(command);
                CreateAttendanceLogTable(command);
                CreateAttendanceLogArchiveTable(command);

                tx.Commit();
            }
            catch
            {
                throw;
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }

        private static void CreateUserMapTable(SQLiteCommand command)
        {
            var sql = @"CREATE TABLE IF NOT EXISTS user_maps(
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            user_id        TEXT NOT NULL,
                            enroll_number  TEXT NOT NULL,
                            project_id     TEXT NOT NULL,
                            create_at      TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                            change_at      TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
                        );";
            command.CommandText = sql;
            command.CommandType = System.Data.CommandType.Text;
            command.ExecuteNonQuery();
        }

        private static void CreateQueueTable(SQLiteCommand command)
        {
            var sql = @"CREATE TABLE IF NOT EXISTS queue(
                            id TEXT PRIMARY KEY,
                            refer_id TEXT NOT NULL,
                            message TEXT NOT NULL,
                            event_type INT NOT NULL,
                            create_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                        );";
            command.CommandText = sql;
            command.CommandType = System.Data.CommandType.Text;
            command.ExecuteNonQuery();
        }

        private static void CreateFailedQueueTable(SQLiteCommand command)
        {
            var sql = @"CREATE TABLE IF NOT EXISTS failed_queue(
                            id TEXT PRIMARY KEY,
                            refer_id TEXT NOT NULL,
                            message TEXT NOT NULL,
                            retry_times INT NOT NULL,
                            kind INT NOT NULL DEFAULT 0,
                            handler TEXT NOT NULL DEFAULT '',
                            create_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                        );";
            command.CommandText = sql;
            command.CommandType = System.Data.CommandType.Text;
            command.ExecuteNonQuery();
        }

        private static void CreateAttendanceLogTable(SQLiteCommand command)
        {
            var sql = @"CREATE TABLE IF NOT EXISTS attendance_logs(
                            id TEXT PRIMARY KEY,
                            machine_id INT NOT NULL,
                            enroll_number  TEXT NOT NULL,
                            project_id     TEXT NOT NULL,
                            log_date       TIMESTAMP NOT NULL,
                            mode           INT NOT NULL,
                            state          INT NOT NULL,
                            work_code      INT NOT NULL,
                            device_name    TEXT NOT NULL,
                            device_type    INT NOT NULL,
                            log_status     INT NOT NULL,
                            create_at      TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                            change_at      TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                            sync           INT  NOT NULL
                        );";
            command.CommandText = sql;
            command.CommandType = System.Data.CommandType.Text;
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// TODO: New feature not implemented.
        /// </summary>
        /// <param name="command"></param>
        private static void CreateAttendanceLogArchiveTable(SQLiteCommand command)
        {
            var sql = @"CREATE TABLE IF NOT EXISTS attendance_logs_archive(
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            refer_no    TEXT NOT NULL,
                            machine_id INT NOT NULL,
                            enroll_number  TEXT NOT NULL,
                            project_id     TEXT NOT NULL,
                            log_date       TIMESTAMP NOT NULL,
                            mode           INT NOT NULL,
                            state          INT NOT NULL,
                            work_code      INT NOT NULL, 
                            device_name    TEXT NOT NULL,
                            device_type    INT NOT NULL,
                            log_status     INT NOT NULL,
                            create_at      TIMESTAMP  NOT NULL DEFAULT CURRENT_TIMESTAMP
                        );";
            command.CommandText = sql;
            command.CommandType = System.Data.CommandType.Text;
            command.ExecuteNonQuery();
        }

        #endregion
    }
}
