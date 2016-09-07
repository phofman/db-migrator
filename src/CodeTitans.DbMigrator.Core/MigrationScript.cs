using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeTitans.DbMigrator.Core
{
    /// <summary>
    /// Single script responsible of database migration.
    /// </summary>
    [DebuggerDisplay("{Version} - {Name}")]
    public sealed class MigrationScript : IComparable<MigrationScript>
    {
        private static readonly Regex StatementSeparators = new Regex(@"^\s*GO\s*", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private string[] _statements;
        private readonly string _path;

        public MigrationScript(Version version, string name)
        {
            if (version == null)
                throw new ArgumentNullException(nameof(version));

            Version = version;
            Name = name;
        }

        public MigrationScript(Version version, string name, string path, string relativePath)
            : this(version, name)
        {
            _path = path;
            RelativePath = relativePath != null ? relativePath.Replace('\\', '/') : null;
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
        /// Gets the relative path of the migration script file.
        /// </summary>
        public string RelativePath
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

                return _statements = Load();
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

        /// <summary>
        /// Loads content of the migration script.
        /// </summary>
        public string[] Load(IEnumerable<KeyValuePair<string, string>> args = null)
        {
            bool hasEmpty = false;
            var content = StatementSeparators.Split(File.ReadAllText(_path));

            // remove redundant stuff:
            for (int i = 0; i < content.Length; i++)
            {
                // apply arguments into the script:
                if (args != null)
                {
                    var line = new StringBuilder(content[i].Trim());
                    foreach (var a in args)
                    {
                        line.Replace("$(" + a.Key + ")", a.Value);
                    }
                    content[i] = line.ToString();
                }
                else
                {
                    content[i] = content[i].Trim();
                }

                // mark items to delete items:
                if (string.IsNullOrEmpty(content[i]))
                {
                    hasEmpty = true;
                }
            }

            // remove all empty entries:
            if (hasEmpty)
            {
                var list = new List<string>(content);
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (string.IsNullOrEmpty(list[i]))
                    {
                        list.RemoveAt(i);
                    }
                }

                content = list.ToArray();
            }

            return _statements = content;
        }

        /// <summary>
        /// Forgets loaded content of migration script.
        /// </summary>
        public void Unload()
        {
            _statements = null;
        }

        /// <summary>
        /// Checks if any of the statements contains given text.
        /// </summary>
        public bool Contains(string text)
        {
            if (_statements == null || string.IsNullOrEmpty(text))
                return false;

            foreach (var s in _statements)
            {
                if (s.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return RelativePath ?? (Name ?? string.Empty);
        }
    }
}
