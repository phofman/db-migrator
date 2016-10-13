using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeTitans.DbMigrator.Core
{
    /// <summary>
    /// Basic operations provided by any kind of migration workers.
    /// </summary>
    public interface IDbWorker
    {
        /// <summary>
        /// Creates new instance of the database if doesn't exist.
        /// </summary>
        Task<bool> CreateDatabase(string name, IEnumerable<ScriptParam> args = null, IDbVersionManager manager = null);

        /// <summary>
        /// Creates new instance of the database if doesn't exist.
        /// </summary>
        Task<bool> CreateDatabase(IEnumerable<ScriptParam> args, IDbVersionManager manager = null);

        /// <summary>
        /// Removes specified database.
        /// </summary>
        Task<bool> DropDatabase(IEnumerable<ScriptParam> args, bool closeExistingConnections = true);

        /// <summary>
        /// Removes specified database.
        /// </summary>
        Task<bool> DropDatabase(string database, bool closeExistingConnections = true);

        /// <summary>
        /// Executes specified set of migration scripts over the database.
        /// </summary>
        Task<int> ExecuteAsync(IReadOnlyCollection<MigrationScript> scripts, IEnumerable<ScriptParam> args = null, IDbVersionManager manager = null);
    }
}
