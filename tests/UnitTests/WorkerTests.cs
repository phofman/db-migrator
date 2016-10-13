using System.Collections.Generic;
using CodeTitans.DbMigrator.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public sealed class WorkerTests
    {
        [TestMethod]
        public void CreateDatabase_with_success()
        {
            var scripts = Scanner.LoadScripts(@"T:\thb\playground\sezam\THB.Ewidencja.DB\Skrypty");

            Assert.IsNotNull(scripts);

            var executor = Migrator.CreateForTSql("(localdb)\\thb", null, null);
            var args = new List<ScriptParam>();
            args.Add(new ScriptParam(ScriptParam.DatabaseNameParamName, "CT-NewDb"));

            // drop existing database...
            var removed = executor.DropDatabase(args).Result;
            Assert.IsTrue(removed, "Failed to drop database");

            // and create the new one...
            var count = executor.ExecuteAsync(scripts, args).Result;
            Assert.AreEqual(scripts.Count, count, "Too few scripts executed");
        }
    }
}
