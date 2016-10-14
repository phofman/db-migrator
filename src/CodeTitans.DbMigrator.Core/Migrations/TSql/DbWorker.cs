using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using CodeTitans.DbMigrator.Core.Helpers;

namespace CodeTitans.DbMigrator.Core.Migrations.TSql
{
    /// <summary>
    /// Worker class to connect to dabase and perform special actions.
    /// </summary>
    public sealed class DbTSqlWorker : IDbWorker
    {
        private readonly SqlConnectionStringBuilder _connectionString;

        /// <summary>
        /// Intializes with specified connection string.
        /// </summary>
        public DbTSqlWorker(string connectinString)
        {
            if (string.IsNullOrEmpty(connectinString))
                throw new ArgumentNullException(nameof(connectinString));

            _connectionString = new SqlConnectionStringBuilder(connectinString);
        }

        /// <summary>
        /// Initializes connection to specified server with given user and password.
        /// </summary>
        public DbTSqlWorker(string server, string database, string user, string password)
        {
            if (string.IsNullOrEmpty(server))
                throw new ArgumentNullException(nameof(server));

            _connectionString = new SqlConnectionStringBuilder();
            _connectionString.DataSource = server;
            if (!string.IsNullOrEmpty(database))
            {
                _connectionString.InitialCatalog = database;
            }

            // define user credentials:
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
            {
                _connectionString.IntegratedSecurity = true;
            }
            else
            {
                _connectionString.UserID = user;
                _connectionString.Password = password;
            }
        }

        private void UpdateWithDatabaseName(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                _connectionString.InitialCatalog = name;
            }
        }

        /// <summary>
        /// Creates connection to the database.
        /// </summary>
        private DbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString.ToString());
        }

        /// <summary>
        /// Creates dedicated executor, matching the database worker.
        /// </summary>
        private IDbExecutor CreateExecutor(DbConnection connection, DbTransaction transaction)
        {
            return new DbTSqlExecutor(connection, transaction);
        }

        /// <summary>
        /// Executes specified set of migration scripts over the database.
        /// </summary>
        public async Task<int> ExecuteAsync(IReadOnlyCollection<MigrationScript> scripts, IEnumerable<ScriptParam> args = null, IDbVersionManager manager = null)
        {
            if (scripts == null)
                return 0;

            int result = 0;
            var connection = CreateConnection();
            var scriptParams = ScriptParam.CreateDefaults(args, connection.DataSource, connection.Database);

            try
            {
                await connection.OpenAsync();

                foreach (var script in scripts)
                {
                    // execute the single migration script and check, if succeeded:
                    if (await ExecuteScriptAsync(connection, script, result, scripts.Count, scriptParams, manager))
                    {
                        result++;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLog.Write(ex);
            }
            finally
            {
                connection.Close();
            }

            return result;
        }

        private async Task<bool> ExecuteScriptAsync(DbConnection connection, MigrationScript script, int currentIndex, int count, IEnumerable<ScriptParam> args, IDbVersionManager manager = null)
        {
            DbTransaction transaction = null;
            string currentStatement = null;
            IDbExecutor executor = null;

            try
            {
                DebugLog.Write(string.Format("Preparing {0}/{1} - {2} ({3})", currentIndex + 1, count, script.Name, script.RelativePath ?? "no path"));
                script.Load(args);

                transaction = script.Contains("CREATE DATABASE") ? null : connection.BeginTransaction();

                if (manager != null && transaction != null)
                {
                    executor = CreateExecutor(connection, transaction);

                    // check the current database version:
                    var version = await manager.GetVersionAsync(executor);
                    if (version == null)
                    {
                        DebugLog.Write(" ... [ABORTED]");
                        throw new InvalidOperationException("Unable to determine database version");
                    }

                    if (version >= script.Version)
                    {
                        DebugLog.WriteLine(string.Concat(" ... [SKIPPED] (db-version: ", version, ")"));

                        await manager.OnSkippedAsync(executor, script, currentIndex, args);
                        transaction.Commit();
                        return true;
                    }
                }

                DebugLog.WriteLine(" ... [DONE]");
                DebugLog.Write("Executing ");

                foreach (var statement in script.Statements)
                {
                    currentStatement = statement;
                    await AdoNetDbHelper.ExecuteNonQueryAsync(connection, transaction, statement);
                    DebugLog.Write(".");
                }

                DebugLog.WriteLine(" [DONE]!");

                // execute custom action after all statements:
                if (manager != null)
                {
                    if (executor == null)
                    {
                        executor = CreateExecutor(connection, transaction);
                    }

                    await manager.UpdateAsync(executor, script, currentIndex, args);
                }

                if (transaction != null)
                    transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                if (transaction != null)
                    transaction.Rollback();
                DebugLog.WriteLine(" ... [FAILED]!");

                DebugLog.Write(ex);
                DebugLog.WriteLine(string.Format("Error encountered during execution of '{0}'", script.RelativePath));
                DebugLog.WriteLine("--- --- --- --- --- --- --- --- --- --- --- ---");
                DebugLog.WriteLine(currentStatement ?? "---");
                DebugLog.WriteLine("--- --- --- --- --- --- --- --- --- --- --- ---");
                return false;
            }
            finally
            {
                // release memory occupied by internal statements:
                script.Unload();
            }
        }

        /// <inheritdoc />
        public Task<bool> CreateDatabase(string name, IEnumerable<ScriptParam> args = null, IDbVersionManager manager = null)
        {
            // find the database name inside arguments if not given upfront:
            if (string.IsNullOrEmpty(name))
            {
                name = ScriptParam.FindValue(args, ScriptParam.DatabaseNameParamName);
            }

            if (string.IsNullOrEmpty(name))
                throw new ArgumentOutOfRangeException(nameof(name));

            var collation = ScriptParam.FindValue(args, ScriptParam.DatabaseNameParamCollation, "Polish_CI_AS");
            var versionString = ScriptParam.FindValue(args, ScriptParam.DatabaseNameParamVersion, "0.0");
            var query1 = $"IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE [name] = '{name}')\r\n" +
                         "BEGIN\r\n" +
                         $"    CREATE DATABASE [{name}] COLLATE {collation}\r\n" +
                         $"END\r\n";
            var query2 = $"USE [{name}]\r\n";

            return ExecuteAsync(new[] { new MigrationScript(new Version(versionString), "Database Creation", new[] { query1, query2 }) },
                    new[]
                    {
                        new ScriptParam(ScriptParam.DatabaseNameParamName, name),
                        new ScriptParam(ScriptParam.DatabaseNameParamVersion, versionString)
                    }, manager).ContinueWith(t => t.Result != 0);
        }

        /// <inheritdoc />
        public Task<bool> CreateDatabase(IEnumerable<ScriptParam> args, IDbVersionManager manager = null)
        {
            // find the database name inside arguments:
            var db = ScriptParam.FindValue(args, ScriptParam.DatabaseNameParamName);

            if (string.IsNullOrEmpty(db))
                throw new ArgumentOutOfRangeException(nameof(args));

            return CreateDatabase(db, args, manager);
        }

        /// <summary>
        /// Removes specified database.
        /// </summary>
        public Task<bool> DropDatabase(IEnumerable<ScriptParam> args, bool closeExistingConnections = true)
        {
            // find the database name inside arguments:
            var db = ScriptParam.FindValue(args, ScriptParam.DatabaseNameParamName);

            if (string.IsNullOrEmpty(db))
                throw new ArgumentOutOfRangeException(nameof(args));

            // remove the datbase:
            return DropDatabase(db, closeExistingConnections);
        }

        /// <summary>
        /// Removes specified database.
        /// </summary>
        public async Task<bool> DropDatabase(string database, bool closeExistingConnections = true)
        {
            if (string.IsNullOrEmpty(database))
                throw new ArgumentNullException(nameof(database));

            var connection = CreateConnection();
            var args = new[] { new ScriptParam(ScriptParam.DatabaseNameParamName, database) };
            try
            {
                await connection.OpenAsync();

                // close connections:
                if (closeExistingConnections)
                {
                    await AdoNetDbHelper.ExecuteNonQueryAsync(connection, null, $"IF EXISTS (SELECT name FROM sys.databases WHERE name = @{ScriptParam.DatabaseNameParamName})\r\n" +
                                                                                $"    ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE", args);
                }

                // drop database:
                await AdoNetDbHelper.ExecuteNonQueryAsync(connection, null, $"IF EXISTS (SELECT name FROM sys.databases WHERE name = @{ScriptParam.DatabaseNameParamName})\r\n" +
                                                                            $"    DROP DATABASE [{database}]", args);

                DebugLog.WriteLine($"Dropped database {database} ... [OK]");
                return true;
            }
            catch (Exception ex)
            {
                DebugLog.WriteLine($"Dropped database {database} ... [FAILED]");
                DebugLog.Write(ex);
                return false;
            }
            finally
            {
                connection.Close();
            }
        }


        /// <inheritdoc />
        public async Task<Version> GetVersionAsync(IDbVersionManager manager)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));

            var connection = CreateConnection();

            try
            {
                await connection.OpenAsync();

                var executor = CreateExecutor(connection, null);
                var version = await manager.GetVersionAsync(executor);

                if (version != null)
                {
                    DebugLog.WriteLine("Database version: " + version);
                }
                else
                {
                    DebugLog.WriteLine("Failed to obtain database version");
                }

                return version;
            }
            catch (Exception ex)
            {
                DebugLog.WriteLine("Failure during loading database version");
                DebugLog.Write(ex);
                return null;
            }
            finally
            {
                connection.Close();
            }
        }

        /// <inheritdoc />
        public async Task<bool> SetVersionAsync(IDbVersionManager manager, Version version)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));
            if (version == null)
                throw new ArgumentNullException(nameof(version));

            var connection = CreateConnection();

            try
            {
                await connection.OpenAsync();

                var executor = CreateExecutor(connection, null);
                var updated = await manager.UpdateAsync(executor, null, -1, new[] { new ScriptParam(ScriptParam.DatabaseNameParamVersion, version.ToString()) });

                if (updated)
                {
                    DebugLog.WriteLine("Updated database version to: " + version);
                }
                else
                {
                    DebugLog.WriteLine("Failed to update database version");
                }

                return updated;
            }
            catch (Exception ex)
            {
                DebugLog.WriteLine("Failure during database version update");
                DebugLog.Write(ex);
                return false;
            }
            finally
            {
                connection.Close();
            }
        }

        /// <summary>
        /// Executes scalar query to database.
        /// </summary>
        public async Task<T> ExecuteScalarAsync<T>(string statement, IEnumerable<ScriptParam> args = null)
        {
            var connection = CreateConnection();

            try
            {
                // open connection:
                await connection.OpenAsync();

                // execute query:
                var result = await AdoNetDbHelper.ExecuteScalarQueryAsync(connection, null, statement, args);

                // give back result:
                return (T) result;
            }
            catch (Exception ex)
            {
                DebugLog.Write(ex);
                throw;
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
