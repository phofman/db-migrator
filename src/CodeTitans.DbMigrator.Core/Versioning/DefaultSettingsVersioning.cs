using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeTitans.DbMigrator.Core.Versioning
{
    /// <summary>
    /// Class that manages the database version tracking and updating.
    /// </summary>
    public sealed class DefaultSettingsVersioning : IDbVersionManager
    {
        private readonly Version _defaultVersion;
        private readonly string _tableName;
        private int _skippedCounter;

        public DefaultSettingsVersioning(string tableName = null, Version defaultVersion = null)
        {
            _defaultVersion = defaultVersion ?? new Version(1, 0);
            _tableName = tableName ?? "GlobalSettings";
        }

        /// <summary>
        /// Gets the number of skipped scripts.
        /// </summary>
        public int SkippedCounter
        {
            get { return _skippedCounter; }
        }

        #region Implementation of IDbVersionManager

        /// <inheritdoc />
        public async Task<Version> GetVersionAsync(IDbExecutor executor)
        {
            if (executor == null)
                throw new ArgumentNullException(nameof(executor));

            try
            {
                var exists = await executor.CheckIfTableExistsAsync(_tableName);
                if (!exists)
                {
                    await CreateTable(executor, _tableName, _defaultVersion);
                    return _defaultVersion;
                }

                var query = $"SELECT Value FROM [{_tableName}] WHERE Name = 'Version'";
                var versionString = await executor.ExecuteScalarAsync<string>(query);

                return new Version(versionString);
            }
            catch (Exception ex)
            {
                DebugLog.Write(ex);
                return null;
            }
        }

        private static Task CreateTable(IDbExecutor executor, string tableName, Version defaultVersion)
        {
            var query = $"CREATE TABLE [{tableName}]\r\n" +
                        "(\r\n" +
                        "    Name nvarchar(20) NOT NULL PRIMARY KEY,\r\n" +
                        "    Value nvarchar(256) NOT NULL\r\n" +
                        ");\r\n\r\n" +
                        $"INSERT [{tableName}] (Name, Value) VALUES ('Version', '{defaultVersion}')\r\n";

            DebugLog.WriteLine("Initialized database version: " + defaultVersion);
            return executor.ExecuteNonQueryAsync(query);
        }

        /// <inheritdoc />
        public async Task<bool> UpdateAsync(IDbExecutor executor, MigrationScript script, int scriptBatchIndex, IEnumerable<ScriptParam> args)
        {
            var version = script != null ? script.Version : null;
            if (version == null)
            {
                var v = ScriptParam.FindValue(args, ScriptParam.DatabaseNameParamVersion);
                if (v != null)
                {
                    version = new Version(v);
                }
            }

            // inform about invalid version:
            if (version == null)
                throw new ArgumentOutOfRangeException(nameof(args));

            try
            {
                var exists = await executor.CheckIfTableExistsAsync(_tableName);
                if (!exists)
                {
                    await CreateTable(executor, _tableName, version);
                }
                else
                {
                    // execute the real update:
                    var versionParam = new ScriptParam(ScriptParam.DatabaseNameParamVersion, version.ToString());
                    var query = $"UPDATE [{_tableName}] SET Value = {versionParam.SqlParamName} WHERE Name = 'Version'\r\n";

                    DebugLog.WriteLine("Updating database version to: " + versionParam.Value);
                    await executor.ExecuteNonQueryAsync(query, new[] { versionParam });
                }

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
            _skippedCounter++;
            return Task.FromResult(true);
        }

        #endregion
    }
}
