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
        private readonly bool _failOnNonExistance;
        private readonly string _tableName;
        private readonly string _columnName;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public ExistingTableVersioning(string tableName, string columnName, bool failOnNonExistance = true)
        {
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentNullException(nameof(tableName));
            if (string.IsNullOrEmpty(columnName))
                throw new ArgumentNullException(nameof(columnName));

            _tableName = tableName;
            _columnName = columnName;
            _failOnNonExistance = failOnNonExistance;
        }

        /// <inheritdoc />
        public async Task<Version> GetVersionAsync(IDbExecutor executor)
        {
            var exists = await executor.CheckIfTableExistsAsync(_tableName);
            if (!exists)
                return _failOnNonExistance ? null : new Version(0, 0, 0, 0);

            try
            {
                var query = $"SELECT TOP 1 [{_columnName}] AS Version FROM [{_tableName}]";
                var versionString = await executor.ExecuteScalarAsync<string>(query);

                if (string.IsNullOrEmpty(versionString))
                    return _failOnNonExistance ? null : new Version(0, 0, 0, 0);

                return new Version(versionString);
            }
            catch (Exception ex)
            {
                DebugLog.Write(ex);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<bool> UpdateAsync(IDbExecutor executor, Version version, int scriptBatchIndex)
        {
            if (version == null)
                throw new ArgumentOutOfRangeException(nameof(version));

            try
            {
                var exists = await executor.CheckIfTableExistsAsync(_tableName);
                if (!exists)
                {
                    return false;
                }

                // execute the real update:
                var versionParam = new ScriptParam(ScriptParam.DatabaseNameParamVersion, version.ToString());
                var query = $"UPDATE [{_tableName}] SET [{_columnName}] = {versionParam.SqlParamName}\r\n";

                DebugLog.WriteLine("Updating database version to: " + versionParam.Value);
                await executor.ExecuteNonQueryAsync(query, new[] { versionParam });

                return true;
            }
            catch (Exception ex)
            {
                DebugLog.Write(ex);
                return false;
            }
        }

        /// <inheritdoc />
        public Task OnSkippedAsync(IDbExecutor executor, MigrationScript script, int scriptBatchIndex, IEnumerable<ScriptParam> args)
        {
            return Task.FromResult<object>(null);
        }
    }
}
