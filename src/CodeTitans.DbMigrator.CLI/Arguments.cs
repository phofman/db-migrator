using System;
using System.Collections.Generic;
using System.Reflection;
using CodeTitans.DbMigrator.Core.Helpers;

namespace CodeTitans.DbMigrator.CLI
{
    public sealed class Arguments
    {
        #region Properties

        public ActionRequest Action
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

        public static Arguments Parse(params string[] args)
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
            string value;

            result.Action = ActionRequest.Execute;

            foreach (var a in args)
            {
                if (StringHelper.IsOption(a, "action", out value))
                {
                    result.Action = GetActionRequest(StringHelper.GetStringValue(value));
                    continue;
                }

                if (StringHelper.IsOption(a, "filter", out value))
                {
                    filters.Add(StringHelper.GetStringValue(value));
                    continue;
                }

                if (StringHelper.IsOption(a, "accept-level", out value))
                {
                    AddLevels(levels, StringHelper.GetStringValue(value));
                    continue;
                }

                if (StringHelper.IsOption(a, "format", out value))
                {
                    result.Format = GetPrintFormat(StringHelper.GetStringValue(value));
                    continue;
                }

                if (string.IsNullOrEmpty(result.ScriptsPath))
                {
                    result.ScriptsPath = StringHelper.GetStringValue(value);
                    continue;
                }
            }

            result.ScriptFilters = filters.ToArray();
            result.ScriptLevels = levels.ToArray();
            return result;
        }

        private static ActionRequest GetActionRequest(string text)
        {
            if (string.IsNullOrEmpty(text))
                return ActionRequest.Help;

            var clearedText = text.Trim();
            ActionRequest result;

            if (Enum.TryParse(clearedText, true, out result))
            {
                return result;
            }

            if (string.Compare(clearedText, "info", StringComparison.InvariantCultureIgnoreCase) == 0)
                return ActionRequest.Help;

            if (string.Compare(clearedText, "app_ver", StringComparison.InvariantCultureIgnoreCase) == 0)
                return ActionRequest.Version;
            if (string.Compare(clearedText, "app_version", StringComparison.InvariantCultureIgnoreCase) == 0)
                return ActionRequest.Version;

            throw new ArgumentException("Invalid value \"" + text + "\" specified for 'action' parameter");
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
    }
}
