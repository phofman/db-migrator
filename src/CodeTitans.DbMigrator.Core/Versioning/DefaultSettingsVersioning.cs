using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeTitans.DbMigrator.Core.Versioning
{
    public sealed class DefaultSettingsVersion : IDbVersionManager
    {
        private readonly Version _defaultVersion;
        private readonly string _tableName;

        public DefaultSettingsVersion(string tableName, Version defaultVersion)
        {
            _defaultVersion = defaultVersion ?? new Version(1, 0);
            _tableName = tableName ?? "Settings";
        }

        #region Implementation of IDbVersionManager

        /// <inheritdoc />
        public async Task<Version> GetVersionAsync(IDbExecutor executor)
        {
            if (executor == null)
                throw new ArgumentNullException(nameof(executor));

            try
            {
                var exists = await CheckIfTableExistsAsync(executor, _tableName);
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

        private static Task<bool> CheckIfTableExistsAsync(IDbExecutor executor, string tableName)
        {
            var query = $"IF (OBJECT_ID('{tableName}', 'table') IS NULL)" +
                        "     SELECT 0" +
                        "ELSE SELECT 1";

            return executor.ExecuteScalarAsync<int>(query).ContinueWith(t => t.Result == 1);
        }

        private static Task CreateTable(IDbExecutor executor, string tableName, Version defaultVersion)
        {
            var query = $"CREATE TABLE [{tableName}]" +
                        "(" +
                        "    Name nvarchar(20) NOT NULL PRIMARY KEY UNIQUE" +
                        "    Value nvarchar(256) NOT NULL" +
                        ");" +
                        $"INSERT [{tableName}] (Name, Value) VALUES ('Version', '{defaultVersion}')";

            return executor.ExecuteNonQueryAsync(query);
        }

        /// <inheritdoc />
        public async Task UpdateAsync(IDbExecutor executor, MigrationScript script, int scriptBatchIndex, IEnumerable<ScriptParam> args)
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
                var exists = await CheckIfTableExistsAsync(executor, _tableName);
                if (!exists)
                {
                    await CreateTable(executor, _tableName, version);
                }
                else
                {
                    // execute the real update:
                    var versionParam = new ScriptParam(ScriptParam.DatabaseNameParamVersion, version.ToString());
                    var query = $"UPDATE [{_tableName}] SET Value = {versionParam.SqlParamName} WHERE Name = 'Version'";
                    await executor.ExecuteNonQueryAsync(query, new[] { versionParam });
                }
            }
            catch (Exception ex)
            {
                DebugLog.Write(ex);
            }
        }

        #endregion
    }
}
