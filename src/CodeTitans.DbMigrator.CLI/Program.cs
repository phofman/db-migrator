using System;
using CodeTitans.DbMigrator.Core;

namespace CodeTitans.DbMigrator.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            Arguments e;

            try
            {
                e = Arguments.Parse(args);

                if (e == null)
                {
                    PrintHelpInfo();
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error - invalid arguments.");
                Console.WriteLine(ex.Message);

                Environment.ExitCode = 1;
                return;
            }

            switch (e.Action)
            {
                case "info":
                case "help":
                    PrintHelpInfo();
                    return;

                case "app_ver":
                case "app_version":
                    Console.WriteLine(Arguments.AppVersion);
                    return;

                case "scan":
                    Console.WriteLine("Scanning for scripts...");
                    Scan(e.ScriptsPath, e.ScriptFilters, e.ScriptLevels, e.Format);
                    return;

                default:
                    Environment.ExitCode = 2;
                    Console.WriteLine("Error - unrecognized action to execute (\"{0}\").", e.Action);
                    return;
            }
        }

        private static void Scan(string path, string[] filters, int[] acceptedLevels, PrintFormat format)
        {
            var scripts = Scanner.LoadScripts(path, filters, acceptedLevels);
            if (scripts == null || scripts.Count == 0)
            {
                Console.WriteLine("Found no scripts.");
            }
            else
            {
                Console.WriteLine("Found {0} script(s)", scripts.Count);

                // list scripts:
                string f;

                switch (format)
                {
                    default:
                    case PrintFormat.Path:
                        f = "{1}";
                        break;
                    case PrintFormat.Name:
                        f = "{0}";
                        break;
                    case PrintFormat.Version:
                        f = "{2}";
                        break;
                    case PrintFormat.NamePath:
                        f = "{0}, {1}";
                        break;
                    case PrintFormat.VersionName:
                        f = "{2}, {0}";
                        break;
                    case PrintFormat.VersionPath:
                        f = "{2}, {1}";
                        break;
                }

                // print in defined format:
                foreach (var s in scripts)
                {
                    Console.WriteLine(f, s.Name, s.RelativePath, s.Version);
                }
            }
        }

        private static void PrintHelpInfo()
        {
            Console.WriteLine("CodeTitans (2016) dbMigrator {0}, Database Migation Tool", Arguments.AppVersion);
            Console.WriteLine();
            Console.WriteLine("dbMirator.exe /action:<action> [<scripts_location>]");
            Console.WriteLine();
            Console.WriteLine(" Actions:");
            Console.WriteLine("  - help - prints help info");
            Console.WriteLine("  - app_version - prints application version");
            Console.WriteLine();
            Console.WriteLine("  - version - gets the database schema version");
            Console.WriteLine("  - create - creates empty database");
            Console.WriteLine("  - drop - drops the database");
            Console.WriteLine("  - update - updates the database using provided scripts");
            Console.WriteLine("  - scan - scans the specified scripts and prints info about them");
        }
    }
}
