using System;
using System.Collections.Generic;

namespace CodeTitans.DbMigrator.Core
{
    /// <summary>
    /// Interface defining operations to keep the database version in sync with executed scripts.
    /// </summary>
    public interface IDbVersionManager
    {
        /// <summary>
        /// Gets current version of the database.
        /// </summary>
        Version GetVersion(IDbExecutor executor);

        /// <summary>
        /// Increments the database version.
        /// New version value could be extracted from the script, that just finished or from arguments.
        /// If scripts are run inside the batch the index will keep increasing.
        /// </summary>
        void Update(IDbExecutor executor, MigrationScript script, int scriptBatchIndex, IEnumerable<ScriptParam> args);
    }
}
