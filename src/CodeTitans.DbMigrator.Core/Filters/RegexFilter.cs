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
        private readonly int[] _acceptedLevels;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public RegexFilter(string[] filters, int[] acceptedLevels = null)
        {
            if (acceptedLevels != null && acceptedLevels.Length > 0)
            {
                _acceptedLevels = acceptedLevels;
                Array.Sort(_acceptedLevels);
            }

            if (filters != null && filters.Length > 0)
            {
                _filters = new Regex[filters.Length];
                for (int i = 0; i < filters.Length; i++)
                {
                    _filters[i] = new Regex(filters[i], RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
                }
            }
        }

        /// <inheritdoc />
        public bool Match(string name, Version version, int level)
        {
            // verify level:
            if (_acceptedLevels != null)
            {
                foreach (var l in _acceptedLevels)
                {
                    if (l == level)
                        return true;
                    if (l > level)
                        break;
                }
            }

            // verify name filter:
            if (_filters != null)
            {
                foreach (var r in _filters)
                {
                    if (r.IsMatch(name))
                        return true;
                }
            }

            return false;
        }
    }
}
