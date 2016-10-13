using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace CodeTitans.DbMigrator.Core.Helpers
{
    /// <summary>
    /// Helper class in creation ADO.NET constructs.
    /// </summary>
    static class AdoNetDbHelper
    {
        public static Task<object> ExecuteScalarQueryAsync(DbConnection connection, string statement, IEnumerable<ScriptParam> args)
        {
            var command = CreateTextCommand(connection, null, statement, args);
            return command.ExecuteScalarAsync();
        }

        public static Task<int> ExecuteNonQueryAsync(DbConnection connection, DbTransaction transaction, string statement, IEnumerable<ScriptParam> args = null)
        {
            var command = CreateTextCommand(connection, transaction, statement, args);
            return command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Creates new SQL command.
        /// </summary>
        public static DbCommand CreateTextCommand(DbConnection connection, DbTransaction transaction, string statement, IEnumerable<ScriptParam> args)
        {
            var command = connection.CreateCommand();
            command.CommandText = statement;
            command.CommandType = CommandType.Text;
            command.Transaction = transaction;

            if (args != null)
            {
                foreach (var arg in args)
                {
                    command.Parameters.Add(CreateParameter(command, arg));
                }
            }
            return command;
        }

        public static IDbDataParameter CreateParameter(IDbCommand command, ScriptParam arg)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = arg.SqlParamName;
            parameter.Value = arg.Value;

            return parameter;
        }
    }
}
