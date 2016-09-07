using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CodeTitans.DbMigrator.Core
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class DbWorker
    {
        private readonly string _connectionString;

        public DbWorker(string connectinString)
        {
            if (string.IsNullOrEmpty(connectinString))
                throw new ArgumentNullException(nameof(connectinString));

            _connectionString = connectinString;
        }

        public async Task<int> ExecuteAsync(IEnumerable<MigrationScript> scripts, IEnumerable<KeyValuePair<string, string>> args = null)
        {
            if (scripts == null)
                return 0;

            int result = 0;
            var connection = new SqlConnection(_connectionString);
            var scriptParams = CreateScriptParams(args, connection);
            int count = scripts.Count();

            try
            {
                await connection.OpenAsync();

                foreach (var script in scripts)
                {
                    // execute the single migration script and check, if succeeded:
                    if (await ExecuteScriptAsync(connection, script, result, count, scriptParams))
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

        /// <summary>
        /// Creates a set of default parameters along with the externally given ones.
        /// </summary>
        private static IEnumerable<KeyValuePair<string, string>> CreateScriptParams(IEnumerable<KeyValuePair<string, string>> args, SqlConnection connection)
        {
            var scriptParams = new List<KeyValuePair<string, string>>();

            // defaults:
            scriptParams.Add(new KeyValuePair<string, string>("AppName", "DB-Migrator"));
            scriptParams.Add(new KeyValuePair<string, string>("AppVersion", GetCurrentVersion()));
            if (!string.IsNullOrEmpty(connection.Database))
            {
                scriptParams.Add(new KeyValuePair<string, string>("DbName", "[" + connection.Database + "]"));
            }
            if (!string.IsNullOrEmpty(connection.DataSource))
            {
                scriptParams.Add(new KeyValuePair<string, string>("DbServer", "[" + connection.DataSource + "]"));
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

        private async Task<bool> ExecuteScriptAsync(SqlConnection connection, MigrationScript script, int currentIndex, int count, IEnumerable<KeyValuePair<string, string>> args)
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

        private Task<int> ExecuteNonQueryAsync(SqlConnection connection, SqlTransaction transaction, string statement)
        {
            var command = connection.CreateCommand();
            command.CommandText = statement;
            command.Transaction = transaction;

            return command.ExecuteNonQueryAsync();
        }
    }
}
