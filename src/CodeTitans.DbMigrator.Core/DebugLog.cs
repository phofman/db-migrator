using System;

namespace CodeTitans.DbMigrator.Core
{
    public static class DebugLog
    {
        public static void Write(Exception ex)
        {
            WriteLine("### ### ### ### ### ####");
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.GetType().Name);
            Console.WriteLine(ex.StackTrace);
            if (ex.InnerException != null)
            {
                Write(ex.InnerException);
            }
        }

        public static void Write(string message)
        {
            Console.Write(message);
        }

        public static void WriteLine(string message)
        {
            Console.WriteLine(message);
        }
    }
}
