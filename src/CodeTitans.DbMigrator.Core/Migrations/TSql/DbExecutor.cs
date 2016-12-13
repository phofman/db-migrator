using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using CodeTitans.DbMigrator.Core.Helpers;

namespace CodeTitans.DbMigrator.Core.Migrations.TSql
{
    sealed class DbTSqlExecutor : IDbExecutor
    {
        private readonly DbConnection _connection;
        private readonly DbTransaction _transaction;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public DbTSqlExecutor(DbConnection connection, DbTransaction transaction)
        {
            _connection = connection;
            _transaction = transaction;
        }

        #region Implementation of IDbExecutor

        /// <inheritdoc />
        public Task<bool> CheckIfTableExistsAsync(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentNullException(nameof(tableName));

            var query = $"IF (OBJECT_ID('{tableName}', 'table') IS NULL)\r\n" +
                        "     SELECT 0\r\n" +
                        "ELSE SELECT 1\r\n";

            return ExecuteScalarAsync<int>(query).ContinueWith(t => t.Result == 1);
        }

        /// <inheritdoc />
        public Task<T> ExecuteScalarAsync<T>(string statement, IEnumerable<ScriptParam> args = null)
        {
            return AdoNetDbHelper.ExecuteScalarQueryAsync(_connection, _transaction, statement, args).ContinueWith(t => (T) t.Result);
        }

        /// <inheritdoc />
        public Task<int> ExecuteNonQueryAsync(string statement, IEnumerable<ScriptParam> args = null)
        {
            return AdoNetDbHelper.ExecuteNonQueryAsync(_connection, _transaction, statement, args);
        }

        #endregion
    }
}
