using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace CodeTitans.DbMigrator.Core
{
    public static class Scanner
    {
        public static IReadOnlyCollection<MigrationScript> LoadScripts(string path, Func<string, int, bool> filter = null)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            List<MigrationScript> result = new List<MigrationScript>();
            LoadScripts(result, path, new Version(0,0), 0, filter);

            result.Sort();
            return result.Count > 0 ? result.ToArray() : null;
        }

        private static void LoadScripts(List<MigrationScript> result, string path, Version currentLevelVersion, int level, Func<string, int, bool> filter)
        {
            // is it a single file?
            if (File.Exists(path))
            {
                Add(result, path, currentLevelVersion, level);
            }
            else
            {
                if (Directory.Exists(path))
                {
                    // apply files at that level:
                    foreach (var file in Directory.GetFiles(path))
                    {
                        Add(result, file, currentLevelVersion, level);
                    }

                    // look into folders:
                    foreach (var dir in Directory.GetDirectories(path))
                    {
                        Version version;
                        int versionParts;
                        string name;

                        // get their names and version parts:
                        if (TryParseName(Path.GetFileName(dir), out version, out versionParts, out name))
                        {
                            // filter:
                            if (filter == null || filter(name, level))
                            {
                                LoadScripts(result, dir, Merge(currentLevelVersion, version, level), level + versionParts, filter);
                            }
                        }
                    }
                }
            }
        }

        private static void Add(List<MigrationScript> result, string path, Version currentLevelVersion, int level)
        {
            Version version;
            int versionParts;
            string name;

            if (TryParseName(Path.GetFileName(path), out version, out versionParts, out name))
            {
                result.Add(new MigrationScript(Merge(currentLevelVersion, version, level), name, path));
            }
        }

        internal static Version Merge(Version current, Version existing, int level)
        {
            if (existing == null)
                return current;

            switch (level)
            {
                case 0:
                    return existing;
                case 1:
                    return existing.Revision < 0 ? new Version(current.Major, existing.Major, existing.Minor) : new Version(current.Major, existing.Major, existing.Minor, existing.Revision);
                case 2:
                    return new Version(current.Major, current.Minor, existing.Major, existing.Minor);
                case 3:
                    return new Version(current.Major, current.Minor, current.Build, existing.Major);

                default:
                    return current;
            }
        }

        /// <summary>
        /// Extracts the version and the name out of given text.
        /// </summary>
        internal static bool TryParseName(string text, out Version version, out int versionParts, out string name)
        {
            version = null;
            name = null;
            versionParts = 0;

            try
            {
                foreach (Match match in Regex.Matches(text, @"^((?<version>(\d+.){0,3}\d+)$|(?<version>(\d+.){0,3}\d+)[\s|\s*\-|\s*_](?<name>.*))", RegexOptions.IgnoreCase))
                {
                    var value = match.Groups["version"].Value;
                    versionParts = Count(value, '.') + 1;
                    version = value.IndexOf('.') >= 0 ? new Version(value) : new Version(value + ".0");
                    name = match.Groups["name"].Value.TrimStart('-', ' ', '\t').Trim();
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        /// <summary>
        /// Counts the number of occurences.
        /// </summary>
        private static int Count(string value, char c)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            int result = 0;
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == c)
                    result++;
            }

            return result;
        }
    }
}
