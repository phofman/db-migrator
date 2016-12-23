using System;

namespace CodeTitans.DbMigrator.Core
{
    /// <summary>
    /// Interface defining helper filters of script inclusion.
    /// </summary>
    public interface IScriptFilter
    {
        /// <summary>
        /// Checks, if given script matches.
        /// </summary>
        bool Match(string name, Version version, int level);
    }
}
