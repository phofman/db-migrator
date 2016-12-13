using CodeTitans.DbMigrator.Core.Migrations.TSql;
using CodeTitans.DbMigrator.Core.Versioning;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public sealed class VersioningTests
    {
        [TestMethod]
        public void GetVersionOfDatabase_with_success()
        {
            var executor = new DbTSqlWorker("(localdb)\\thb", "THBDB", null, null);
            var manager = new ExistingTableVersioning("KonfiguracjaSystemu", "WersjaDB");

            var version = executor.GetVersionAsync(manager).Result;
            Assert.IsNotNull(version);
        }
    }
}
