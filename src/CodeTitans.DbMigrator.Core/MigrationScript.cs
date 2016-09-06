using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace CodeTitans.DbMigrator.Core
{
    /// <summary>
    /// Single script responsible of database migration.
    /// </summary>
    [DebuggerDisplay("{Version} - {Name}")]
    public sealed class MigrationScript : IComparable<MigrationScript>
    {
        private static readonly Regex StatementSeparators = new Regex(@"^\w*GO\w*", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private string[] _statements;
        private readonly string _path;

        public MigrationScript(Version version, string name)
        {
            if (version == null)
                throw new ArgumentNullException(nameof(version));

            Version = version;
            Name = name;
        }

        public MigrationScript(Version version, string name, string path)
            : this(version, name)
        {
            _path = path;
        }

        #region Properties

        /// <summary>
        /// Gets the destination version, this script will update the database to.
        /// </summary>
        public Version Version
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the name of the migration.
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the set of SQL statements to execute.
        /// </summary>
        public IReadOnlyCollection<string> Statements
        {
            get
            {
                if (_statements != null)
                    return _statements;

                _statements = StatementSeparators.Split(File.ReadAllText(_path));

                // remove redundant stuff:
                for (int i = 0; i < _statements.Length; i++)
                    _statements[i] = _statements[i].Trim();

                return _statements;
            }
        }

        #endregion

        #region Implementation of IComparable<in MigrationScript>

        public int CompareTo(MigrationScript other)
        {
            if (other == null)
                return 1;

            return Version.CompareTo(other.Version);
        }

        #endregion
    }
}
