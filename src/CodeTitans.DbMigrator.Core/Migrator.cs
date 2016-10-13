using System;
using CodeTitans.DbMigrator.Core.Migrations;

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
        public static IDbWorker CreateForTSql(string server, string user, string password)
        {
            if (string.IsNullOrEmpty(server))
                throw new ArgumentNullException(nameof(server));

            return new DbTSqlWorker(server, user, password);
        }
    }
}
