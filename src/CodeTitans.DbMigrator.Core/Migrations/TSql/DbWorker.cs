using System;
using System.Collections.Generic;
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
        private readonly string _connectionString;

        /// <summary>
        /// Intializes with specified connection string.
        /// </summary>
        public DbTSqlWorker(string connectinString)
        {
            if (string.IsNullOrEmpty(connectinString))
                throw new ArgumentNullException(nameof(connectinString));

            _connectionString = connectinString;
        }

        /// <summary>
        /// Initializes connection to specified server with given user and password.
        /// </summary>
        public DbTSqlWorker(string server, string user, string password)
        {
            if (string.IsNullOrEmpty(server))
                throw new ArgumentNullException(nameof(server));

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
            {
                _connectionString = $"Data Source={server};Integrated Security=True;";
            }
            else
            {
                _connectionString = $"Data Source={server};User Id={user};Password={password};";
            }
        }

        /// <summary>
        /// Executes specified set of migration scripts over the database.
        /// </summary>
        public async Task<int> ExecuteAsync(IReadOnlyCollection<MigrationScript> scripts, IEnumerable<ScriptParam> args = null, IDbVersionManager manager = null)
        {
            if (scripts == null)
                return 0;

            int result = 0;
            var connection = new SqlConnection(_connectionString);
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

        private async Task<bool> ExecuteScriptAsync(SqlConnection connection, MigrationScript script, int currentIndex, int count, IEnumerable<ScriptParam> args, IDbVersionManager manager = null)
        {
            SqlTransaction transaction = null;
            string currentStatement = null;
            IDbExecutor executor = null;

            try
            {
                DebugLog.Write(string.Format("Preparing {0}/{1} - {2} ({3})", currentIndex + 1, count, script.Name, script.RelativePath));
                script.Load(args);

                transaction = script.Contains("CREATE DATABASE") ? null : connection.BeginTransaction("DbMigrator");

                if (manager != null)
                {
                    executor = new DbTSqlExecutor(connection, transaction);

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

            return Task.FromResult(false);
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

            var connection = new SqlConnection(_connectionString);
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

        /// <summary>
        /// Executes scalar query to database.
        /// </summary>
        public async Task<T> ExecuteScalarAsync<T>(string statement, IEnumerable<ScriptParam> args = null)
        {
            var connection = new SqlConnection(_connectionString);

            try
            {
                // open connection:
                await connection.OpenAsync();

                // execute query:
                var result = await AdoNetDbHelper.ExecuteScalarQueryAsync(connection, statement, args);

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
