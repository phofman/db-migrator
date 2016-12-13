using System;
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
            const string versionString = "1.7";
            const string dbName = "T1";

            var executor = Migrator.CreateForTSql("(localdb)\\thb", null, null, null);
            var args = new List<ScriptParam>();
            args.Add(new ScriptParam(ScriptParam.DatabaseNameParamName, dbName));
            args.Add(new ScriptParam(ScriptParam.DatabaseNameParamVersion, versionString));

            // drop existing database:
            var removed = executor.DropDatabase(args).Result;
            Assert.IsTrue(removed, "Failed to drop database");

            // create new empty database and initialize its version:
            var versioning = new DefaultSettingsVersioning();
            var created = executor.CreateDatabase(args, versioning).Result;
            Assert.IsTrue(created, "Failed to create database");

            var version = executor.GetVersionAsync(versioning).Result;
            Assert.AreEqual(new Version(versionString), version);
        }

        [TestMethod]
        public void CreateDatabase_with_success()
        {
            var scripts = Scanner.LoadScripts(@"T:\thb\playground\sezam\THB.Ewidencja.DB\Skrypty");
            const string VersionString = "4.45";

            Assert.IsNotNull(scripts);

            var executor = Migrator.CreateForTSql("(localdb)\\thb", null, null, null);
            var args = new List<ScriptParam>();
            args.Add(new ScriptParam(ScriptParam.DatabaseNameParamName, "PustaBaza"));
            //args.Add(new ScriptParam(ScriptParam.DatabaseNameParamVersion, VersionString));

            // drop existing database...
            var removed = executor.DropDatabase(args).Result;
            Assert.IsTrue(removed, "Failed to drop database");

            // and create the new one...
            var versioning = new DefaultSettingsVersioning();
                //new ExistingTableVersioning("KonfiguracjaSystemu", "WersjaDB", false);
            var count = executor.ExecuteAsync(scripts, args, versioning).Result;
            Assert.AreEqual(scripts.Count, count, "Too few scripts executed");
            //Assert.AreEqual(0, versioning.Skipped, "Should not skip any scripts");

            var currentVersion = executor.GetVersionAsync(versioning).Result;
            Assert.IsTrue(currentVersion > new Version(0, 0));

            // if enforced the version:
            if (ScriptParam.Find(args, ScriptParam.DatabaseNameParamVersion) != null)
            {
                Assert.AreEqual(new Version(VersionString), currentVersion);
            }
        }
    }
}
