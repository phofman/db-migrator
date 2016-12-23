using System;
using System.Collections.Generic;
using System.Reflection;

namespace CodeTitans.DbMigrator.CLI
{
    public sealed class Arguments
    {
        #region Properties

        public string Action
        {
            get;
            private set;
        }

        public string ScriptsPath
        {
            get;
            private set;
        }

        public PrintFormat Format
        {
            get;
            private set;
        }

        public string[] ScriptFilters
        {
            get;
            private set;
        }

        public int[] ScriptLevels
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the application version.
        /// </summary>
        public static Version AppVersion
        {
            get
            {
                var name = new AssemblyName(Assembly.GetExecutingAssembly().GetName().FullName);
                return name.Version;
            }
        }

        #endregion

        public static Arguments Parse(string[] args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            if (args.Length == 0 || (args.Length == 1 && (args[0] == "/help" || args[0] == "--help" || args[0] == "/?")))
            {
                return null;
            }

            var result = new Arguments();
            var filters = new List<string>();
            var levels = new List<int>();

            foreach (var a in args)
            {
                if (a.StartsWith("/action:"))
                {
                    result.Action = GetStringValue(a, 8).ToLowerInvariant();
                    continue;
                }

                if (a.StartsWith("/filter:"))
                {
                    filters.Add(GetStringValue(a, 8));
                    continue;
                }

                if (a.StartsWith("/accept-level:"))
                {
                    AddLevels(levels, GetStringValue(a, 14));
                    continue;
                }

                if (a.StartsWith("/format:"))
                {
                    result.Format = GetPrintFormat(GetStringValue(a, 8));
                    continue;
                }

                if (string.IsNullOrEmpty(result.ScriptsPath))
                {
                    result.ScriptsPath = GetStringValue(a, 0);
                    continue;
                }
            }

            // set the default action, in case not specified via arguments...
            if (string.IsNullOrEmpty(result.Action))
            {
                result.Action = "update";
            }

            result.ScriptFilters = filters.ToArray();
            result.ScriptLevels = levels.ToArray();
            return result;
        }

        private static PrintFormat GetPrintFormat(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return PrintFormat.Path;

            var clearedText = text.Replace("-", "").Replace(" ", "");
            PrintFormat result;

            if (Enum.TryParse(clearedText, true, out result))
            {
                return result;
            }

            throw new ArgumentException("Invalid value \"" + text + "\" specified for 'format' parameter");
        }

        private static void AddLevels(List<int> levels, string definition)
        {
            if (string.IsNullOrEmpty(definition))
                return;

            var items = definition.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in items)
            {
                int value;
                if (int.TryParse(item, out value))
                {
                    if (!levels.Contains(value))
                    {
                        levels.Add(value);
                    }
                }
            }
        }

        /// <summary>
        /// Extracts string value of the parameter.
        /// </summary>
        private static string GetStringValue(string text, int startAt)
        {
            var result = startAt > 0 ? text.Substring(startAt) : text;
            if (result.Length > 1 && result[0] == '"' && result[result.Length - 1] == '"')
            {
                result = result.Substring(1, result.Length - 2);
            }

            return result;
        }
    }
}
