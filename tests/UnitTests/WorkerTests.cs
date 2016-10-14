using System.Collections.Generic;
using CodeTitans.DbMigrator.Core;
using CodeTitans.DbMigrator.Core.Versioning;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public sealed class WorkerTests
    {
        [TestMethod]
        public void CreateDatabaseFromScratch_with_success()
        {
            var executor = Migrator.CreateForTSql("(localdb)\\thb", null, null, null);
            var args = new List<ScriptParam>();
            args.Add(new ScriptParam(ScriptParam.DatabaseNameParamName, "T1"));
            args.Add(new ScriptParam(ScriptParam.DatabaseNameParamVersion, "1.7"));

            // drop existing database:
            var removed = executor.DropDatabase(args).Result;
            Assert.IsTrue(removed, "Failed to drop database");

            // create new empty database and initialize its version:
            var created = executor.CreateDatabase(args, new DefaultSettingsVersioning()).Result;
            Assert.IsTrue(created, "Failed to create database");
        }

        [TestMethod]
        public void CreateDatabase_with_success()
        {
            var scripts = Scanner.LoadScripts(@"T:\thb\playground\sezam\THB.Ewidencja.DB\Skrypty");

            Assert.IsNotNull(scripts);

            var executor = Migrator.CreateForTSql("(localdb)\\thb", null, null, null);
            var args = new List<ScriptParam>();
            args.Add(new ScriptParam(ScriptParam.DatabaseNameParamName, "CT-NewDb"));

            // drop existing database...
            var removed = executor.DropDatabase(args).Result;
            Assert.IsTrue(removed, "Failed to drop database");

            // and create the new one...
            var versioning = new DefaultSettingsVersioning();
            var count = executor.ExecuteAsync(scripts, args, versioning).Result;
            Assert.AreEqual(scripts.Count, count, "Too few scripts executed");
            //Assert.AreEqual(0, versioning.Skipped, "Should not skip any scripts");
        }
    }
}
