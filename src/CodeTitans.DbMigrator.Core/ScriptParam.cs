namespace CodeTitans.DbMigrator.Core
{
    /// <summary>
    /// Class wrapping parameter passed to the migration script.
    /// </summary>
    public sealed class ScriptParam
    {
        private string _name;
        private string _scriptParamName;

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
            }
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

        #endregion

        #region Overrides of Object

        public override string ToString()
        {
            return string.Concat(Name, ": ", Value);
        }

        #endregion
    }
}
