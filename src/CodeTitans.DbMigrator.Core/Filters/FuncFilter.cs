using System;

namespace CodeTitans.DbMigrator.Core.Filters
{
    /// <summary>
    /// Wrapper class around a simple function to filter scripts.
    /// </summary>
    public sealed class FuncFilter : IScriptFilter
    {
        private readonly Func<string, Version, int, bool> _filter;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public FuncFilter(Func<string, Version, int, bool> filter)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            _filter = filter;
        }

        /// <inheritdoc />
        public bool Match(string name, Version version, int level)
        {
            return _filter(name, version, level);
        }
    }
}
