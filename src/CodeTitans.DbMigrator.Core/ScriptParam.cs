﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CodeTitans.DbMigrator.Core
{
    /// <summary>
    /// Class wrapping parameter passed to the migration script.
    /// </summary>
    public sealed class ScriptParam
    {
        #region Predefined Parameter Names

        /// <summary>
        /// Name of the parameter that holds the name of the database.
        /// </summary>
        public const string DatabaseNameParamName = "DbName";

        #endregion

        private string _name;
        private string _scriptParamName;
        private string _sqlParamName;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ScriptParam()
        {
        }

        /// <summary>
        /// Init constructor.
        /// </summary>
        public ScriptParam(string name, string value)
        {
            Name = name;
            Value = value;
        }

        #region Properties

        /// <summary>
        /// Name of the parameter.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                _scriptParamName = string.IsNullOrEmpty(value) ? null : "$(" + value + ")";
                _sqlParamName = string.IsNullOrEmpty(value) ? null : "@" + SecureSqlName(value);
            }
        }

        /// <summary>
        /// Gets the SQL-valid name, out of give text.
        /// </summary>
        private static string SecureSqlName(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException(nameof(value));

            var buffer = new StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsLetterOrDigit(value[i]))
                {
                    buffer.Append(value[i]);
                }
                else
                {
                    buffer.Append('_');
                }
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Value of the parameter.
        /// </summary>
        public string Value
        {
            get;
            set;
        }

        /// <summary>
        /// Internal text representing the parameter inside the script, using during the 'replace' stage.
        /// </summary>
        internal string ScriptParamName
        {
            get { return _scriptParamName; }
        }

        /// <summary>
        /// Internal text representing SQL variable name.
        /// </summary>
        internal string SqlParamName
        {
            get { return _sqlParamName; }
        }

        #endregion

        #region Overrides of Object

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Concat(Name, ": ", Value);
        }

        #endregion

        /// <summary>
        /// Creates a set of default parameters along with the externally given ones.
        /// </summary>
        public static IReadOnlyCollection<ScriptParam> CreateDefaults(IEnumerable<ScriptParam> args, string databaseServer = null, string databaseName = null)
        {
            var scriptParams = new List<ScriptParam>();

            // defaults:
            scriptParams.Add(new ScriptParam("AppName", "CodeTitans DB-Migrator"));
            scriptParams.Add(new ScriptParam("AppVersion", GetCurrentVersion()));
            if (!string.IsNullOrWhiteSpace(databaseServer))
            {
                scriptParams.Add(new ScriptParam("DbServer", databaseServer));
            }
            if (!string.IsNullOrWhiteSpace(databaseName))
            {
                scriptParams.Add(new ScriptParam("DbName", databaseName));
            }

            // copy source items:
            if (args != null)
            {
                scriptParams.AddRange(args);
            }

            return scriptParams;
        }

        /// <summary>
        /// Gets the version of currently executed assembly.
        /// </summary>
        private static string GetCurrentVersion()
        {
            var name = new AssemblyName(Assembly.GetExecutingAssembly().FullName);
            return name.Version.ToString();
        }
    }
}