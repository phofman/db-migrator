using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        Task<Version> GetVersionAsync(IDbExecutor executor);

        /// <summary>
        /// Increments the database version.
        /// New version value could be extracted from the script, that just finished or from arguments.
        /// If scripts are run inside the batch the index will keep increasing.
        /// </summary>
        Task UpdateAsync(IDbExecutor executor, MigrationScript script, int scriptBatchIndex, IEnumerable<ScriptParam> args);

        /// <summary>
        /// Notifies version manager, that given script was skipped after the version check.
        /// </summary>
        Task OnSkippedAsync(IDbExecutor executor, MigrationScript script, int scriptBatchIndex, IEnumerable<ScriptParam> args);
    }
}
