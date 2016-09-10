using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection;
using System.Threading.Tasks;

namespace CodeTitans.DbMigrator.Core
{
    /// <summary>
    /// Worker class to connect to dabase and perform special actions.
    /// </summary>
    public sealed class DbWorker
    {
        public const string DatabaseNameParamName = "DbName";

        private readonly string _connectionString;

        /// <summary>
        /// Intializes with specified connection string.
        /// </summary>
        public DbWorker(string connectinString)
        {
            if (string.IsNullOrEmpty(connectinString))
                throw new ArgumentNullException(nameof(connectinString));

            _connectionString = connectinString;
        }

        /// <summary>
        /// Initializes connection to specified server with given user and password.
        /// </summary>
        public DbWorker(string server, string user, string password)
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
        public async Task<int> ExecuteAsync(IReadOnlyCollection<MigrationScript> scripts, IEnumerable<ScriptParam> args = null, Action<MigrationScript, int> afterExecution = null)
        {
            if (scripts == null)
                return 0;

            int result = 0;
            var connection = new SqlConnection(_connectionString);
            var scriptParams = CreateScriptParams(args, connection);

            try
            {
                await connection.OpenAsync();

                foreach (var script in scripts)
                {
                    // execute the single migration script and check, if succeeded:
                    if (await ExecuteScriptAsync(connection, script, result, scripts.Count, scriptParams))
                    {
                        if (afterExecution != null)
                        {
                            afterExecution(script, result);
                        }
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

        /// <summary>
        /// Creates a set of default parameters along with the externally given ones.
        /// </summary>
        private static IReadOnlyCollection<ScriptParam> CreateScriptParams(IEnumerable<ScriptParam> args, SqlConnection connection)
        {
            var scriptParams = new List<ScriptParam>();

            // defaults:
            scriptParams.Add(new ScriptParam("AppName", "DB-Migrator"));
            scriptParams.Add(new ScriptParam("AppVersion", GetCurrentVersion()));
            if (!string.IsNullOrEmpty(connection.Database))
            {
                scriptParams.Add(new ScriptParam("DbName", connection.Database));
            }
            if (!string.IsNullOrEmpty(connection.DataSource))
            {
                scriptParams.Add(new ScriptParam("DbServer", connection.DataSource));
            }

            if (args != null)
            {
                scriptParams.AddRange(args);
            }

            return scriptParams;
        }

        /// <summary>
        /// Gets the version of currently executed assembly.
        /// </summary>
        private static string GetCurrentVersion()
        {
            var name = new AssemblyName(Assembly.GetExecutingAssembly().FullName);
            return name.Version.ToString();
        }

        private async Task<bool> ExecuteScriptAsync(SqlConnection connection, MigrationScript script, int currentIndex, int count, IEnumerable<ScriptParam> args)
        {
            SqlTransaction transaction = null;
            string currentStatement = null;

            try
            {
                DebugLog.Write(string.Format("Preparing {0}/{1} - {2} ({3})", currentIndex + 1, count, script.Name, script.RelativePath));
                script.Load(args);
                DebugLog.WriteLine(" ... [DONE]");
                DebugLog.Write("Executing ");

                transaction = script.Contains("CREATE DATABASE") ? null : connection.BeginTransaction("DbMigrator");
                foreach (var statement in script.Statements)
                {
                    currentStatement = statement;
                    await ExecuteNonQueryAsync(connection, transaction, statement);
                    DebugLog.Write(".");
                }

                if (transaction != null)
                    transaction.Commit();
                DebugLog.WriteLine(" [DONE]!");
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

        private Task<int> ExecuteNonQueryAsync(SqlConnection connection, SqlTransaction transaction, string statement, IEnumerable<ScriptParam> args = null)
        {
            var command = connection.CreateCommand();
            command.CommandText = statement;
            command.Transaction = transaction;

            if (args != null)
            {
                foreach (var arg in args)
                {
                    command.Parameters.AddWithValue(arg.SqlParamName, arg.Value);
                }
            }

            return command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Removes specified database.
        /// </summary>
        public Task<bool> DropDatabase(IEnumerable<ScriptParam> args, bool closeExistingConnections = true)
        {
            string db = null;

            // find the database name inside arguments:
            foreach (var arg in args)
            {
                if (string.Compare(DatabaseNameParamName, arg.Name, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    db = arg.Value;
                    break;
                }
            }

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
            var args = new[] { new ScriptParam(DatabaseNameParamName, database) };
            try
            {
                await connection.OpenAsync();

                // close connections:
                if (closeExistingConnections)
                {
                    await ExecuteNonQueryAsync(connection, null, $"IF EXISTS (SELECT name FROM sys.databases WHERE name = @{DatabaseNameParamName})\r\n" +
                                                                 $"    ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE", args);
                }

                // drop database:
                await ExecuteNonQueryAsync(connection, null, $"IF EXISTS (SELECT name FROM sys.databases WHERE name = @{DatabaseNameParamName})\r\n" +
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
    }
}
