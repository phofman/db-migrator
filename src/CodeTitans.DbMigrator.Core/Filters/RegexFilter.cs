using System;
using System.Text.RegularExpressions;

namespace CodeTitans.DbMigrator.Core.Filters
{
    /// <summary>
    /// Wrapper class that uses .NET regular expressions to match specified script names.
    /// </summary>
    public sealed class RegexFilter : IScriptFilter
    {
        private readonly Regex[] _filters;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public RegexFilter(string[] filters)
        {
            if (filters == null || filters.Length == 0)
                throw new ArgumentNullException(nameof(filters));

            _filters = new Regex[filters.Length];
            for (int i = 0; i < filters.Length; i++)
            {
                _filters[i] = new Regex(filters[i], RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
            }
        }

        /// <inheritdoc />
        public bool Match(string name, Version version, int level)
        {
            foreach (var r in _filters)
            {
                if (r.IsMatch(name))
                    return true;
            }

            return false;
        }
    }
}
