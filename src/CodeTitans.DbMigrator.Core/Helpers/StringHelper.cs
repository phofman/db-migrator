using System;

namespace CodeTitans.DbMigrator.Core.Helpers
{
    public static class StringHelper
    {
        /// <summary>
        /// Extracts string value of the parameter.
        /// </summary>
        public static string GetStringValue(string text, int startAt = 0)
        {
            if (text.Length > startAt + 1 && text[startAt] == '"' && text[text.Length - 1] == '"')
            {
                return text.Substring(startAt + 1, text.Length - 2 - startAt);
            }

            return text.Substring(startAt);
        }

        public static bool IsOption(string argument, string optionName, out string value)
        {
            // if argument starts accodingly with - or /
            if (!string.IsNullOrEmpty(argument) && !string.IsNullOrEmpty(optionName) && (argument[0] == '/' || argument[0] == '-'))
            {
                int endAt = optionName.Length + 1;
                if (argument.Length >= endAt + 1 && (argument[endAt] == ':' || argument[endAt] == '='))
                {
                    if (string.Compare(argument, 1, optionName, 0, optionName.Length, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        value = argument.Substring(endAt + 1);
                        return true;
                    }
                }
            }

            value = null;
            return false;
        }
    }
}
