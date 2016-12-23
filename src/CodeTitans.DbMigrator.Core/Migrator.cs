using System;
using System.Collections.Generic;
using CodeTitans.DbMigrator.Core.Migrations.TSql;

namespace CodeTitans.DbMigrator.Core
{
    /// <summary>
    /// Factory class to instantiate migration workers.
    /// </summary>
    public static class Migrator
    {
        /// <summary>
        /// Create new instance of the T-SQL migration worker.
        /// </summary>
        public static IDbWorker CreateForTSql(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            return new DbTSqlWorker(connectionString);
        }

        /// <summary>
        /// Create new instance of the T-SQL migration worker.
        /// </summary>
        public static IDbWorker CreateForTSql(string server, string database, string user, string password)
        {
            if (string.IsNullOrEmpty(server))
                throw new ArgumentNullException(nameof(server));

            return new DbTSqlWorker(server, database, user, password);
        }

        /// <summary>
        /// Initializes collection of script parameters.
        /// </summary>
        public static List<ScriptParam> InitParams(string databaseName, string databaseVersion = null)
        {
            var result = new List<ScriptParam>();

            if (!string.IsNullOrEmpty(databaseName))
            {
                result.Add(new ScriptParam(ScriptParam.DatabaseNameParamName, databaseName));
                result.Add(new ScriptParam(ScriptParam.DatabaseNameParamVersion, databaseVersion));
            }

            return result;
        }
    }
}
