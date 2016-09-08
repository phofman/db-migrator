using System;
using System.Collections.Generic;
using CodeTitans.DbMigrator.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class ScannerTests
    {
        [TestMethod]
        public void MatchFiles_with_success()
        {
            Version version;
            int versionParts;
            string name;

            Assert.IsTrue(Scanner.TryParseName("1_Hello", out version, out versionParts, out name));
            Assert.AreEqual(new Version(1, 0), version);
            Assert.AreEqual(1, versionParts);
            Assert.AreEqual("Hello", name);

            Assert.IsTrue(Scanner.TryParseName("2 Hey", out version, out versionParts, out name));
            Assert.AreEqual(new Version(2, 0), version);
            Assert.AreEqual(1, versionParts);
            Assert.AreEqual("Hey", name);

            Assert.IsTrue(Scanner.TryParseName("3.1 Added droids", out version, out versionParts, out name));
            Assert.AreEqual(new Version(3, 1), version);
            Assert.AreEqual(2, versionParts);
            Assert.AreEqual("Added droids", name);

            Assert.IsTrue(Scanner.TryParseName("3.4", out version, out versionParts, out name));
            Assert.AreEqual(new Version(3, 4), version);
            Assert.AreEqual(2, versionParts);
            Assert.AreEqual("", name);

            Assert.IsTrue(Scanner.TryParseName("4.2-", out version, out versionParts, out name));
            Assert.AreEqual(new Version(4, 2), version);
            Assert.AreEqual(2, versionParts);
            Assert.AreEqual("", name);

            Assert.IsTrue(Scanner.TryParseName("00000-", out version, out versionParts, out name));
            Assert.AreEqual(new Version(0, 0), version);
            Assert.AreEqual(1, versionParts);
            Assert.AreEqual("", name);
        }

        [TestMethod]
        public void TryMatch_and_fail()
        {
            Version version;
            int versionParts;
            string name;

            Assert.IsFalse(Scanner.TryParseName("0Hello", out version, out versionParts, out name));
            Assert.IsFalse(Scanner.TryParseName("Hey", out version, out versionParts, out name));
            Assert.IsFalse(Scanner.TryParseName("_Droid", out version, out versionParts, out name));
        }

        [TestMethod]
        public void ScanFolder_with_success()
        {
            var scripts = Scanner.LoadScripts(@"T:\thb\playground\sezam\THB.Ewidencja.DB\SkryptyAktualizacja");

            Assert.IsNotNull(scripts);
        }

        [TestMethod]
        public void ScanTree_with_success()
        {
            var scripts = Scanner.LoadScripts(@"T:\thb\playground\sezam\THB.Ewidencja.DB\Skrypty", (name, level) => level < 1 || string.Compare(name, "procedury", StringComparison.OrdinalIgnoreCase) == 0);

            Assert.IsNotNull(scripts);
        }

        [TestMethod]
        public void CreateDatabase_with_success()
        {
            var scripts = Scanner.LoadScripts(@"T:\thb\playground\sezam\THB.Ewidencja.DB\Skrypty");

            Assert.IsNotNull(scripts);

            var executor = new DbWorker(@"Data Source=(localdb)\thb;Integrated Security=True;");
            var args = new List<ScriptParam>();
            args.Add(new ScriptParam("DbName", "NewDb"));

            var count = executor.ExecuteAsync(scripts, args).Result;
            Assert.AreEqual(scripts.Count, count, "Too few scripts executed");
        }
    }
}
