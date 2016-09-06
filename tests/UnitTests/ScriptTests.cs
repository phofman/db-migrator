using System;
using System.Collections.Generic;
using CodeTitans.DbMigrator.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public sealed class ScriptTests
    {
        [TestMethod]
        public void SortingOrder()
        {
            var items = new List<MigrationScript>();

            items.Add(new MigrationScript(new Version(3, 1), "X"));
            items.Add(new MigrationScript(new Version(1, 2), "X"));
            items.Add(new MigrationScript(new Version(2, 4), "X"));
            items.Add(new MigrationScript(new Version(6, 1), "X"));
            items.Add(new MigrationScript(new Version(2, 3), "X"));

            items.Sort();

            Assert.AreEqual(5, items.Count);
            Assert.AreEqual("1.2", items[0].Version.ToString());
            Assert.AreEqual("6.1", items[4].Version.ToString());
        }
    }
}
