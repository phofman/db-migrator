using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeTitans.DbMigrator.Core
{
    /// <summary>
    /// Interface providing access to operations over the database.
    /// </summary>
    public interface IDbExecutor
    {
        /// <summary>
        /// Executes scalar query to database.
        /// </summary>
        Task<T> ExecuteScalarAsync<T>(string statement, IEnumerable<ScriptParam> args = null);

        /// <summary>
        /// Executes query over the database.
        /// </summary>
        Task<int> ExecuteNonQueryAsync(string statement, IEnumerable<ScriptParam> args = null);
    }
}
