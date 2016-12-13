using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeTitans.DbMigrator.Core.Versioning
{
    /// <summary>
    /// Updates a field inside existing table to store information about database version.
    /// </summary>
    public class ExistingTableVersioning : IDbVersionManager
    {
        private readonly string _tableName;
        private readonly string _columnName;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public ExistingTableVersioning(string tableName, string columnName)
        {
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentNullException(nameof(tableName));
            if (string.IsNullOrEmpty(columnName))
                throw new ArgumentNullException(nameof(columnName));

            _tableName = tableName;
            _columnName = columnName;
        }

        /// <inheritdoc />
        public async Task<Version> GetVersionAsync(IDbExecutor executor)
        {
            var exists = await executor.CheckIfTableExistsAsync(_tableName);
            if (!exists)
                return null;

            try
            {
                var query = $"SELECT [{_columnName}] AS Version FROM [{_tableName}]";
                var versionString = await executor.ExecuteScalarAsync<string>(query);

                return new Version(versionString);
            }
            catch (Exception ex)
            {
                DebugLog.Write(ex);
                return null;
            }
        }

        /// <inheritdoc />
        public Task<bool> UpdateAsync(IDbExecutor executor, MigrationScript script, int scriptBatchIndex, IEnumerable<ScriptParam> args)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task OnSkippedAsync(IDbExecutor executor, MigrationScript script, int scriptBatchIndex, IEnumerable<ScriptParam> args)
        {
            return Task.FromResult<object>(null);
        }
    }
}
