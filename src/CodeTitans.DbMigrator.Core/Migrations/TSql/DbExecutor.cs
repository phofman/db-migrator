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
        public Task<T> ExecuteScalarAsync<T>(string statement, IEnumerable<ScriptParam> args = null)
        {
            return AdoNetDbHelper.ExecuteScalarQueryAsync(_connection, statement, args).ContinueWith(t => (T) t.Result);
        }

        /// <inheritdoc />
        public Task<int> ExecuteNonQueryAsync(string statement, IEnumerable<ScriptParam> args = null)
        {
            return AdoNetDbHelper.ExecuteNonQueryAsync(_connection, _transaction, statement, args);
        }

        #endregion
    }
}
