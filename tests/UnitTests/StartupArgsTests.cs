using CodeTitans.DbMigrator.CLI;
using CodeTitans.DbMigrator.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class StartupArgsTests
    {
        [TestMethod]
        public void ExtractParameters()
        {
            string value;

            Assert.IsTrue(StringHelper.IsOption("/test:123", "test", out value));
            Assert.AreEqual("123", value);

            Assert.IsTrue(StringHelper.IsOption("/a:\"123\"", "a", out value));
            Assert.AreEqual("\"123\"", value);

            Assert.IsFalse(StringHelper.IsOption("/:123", "test", out value));
            Assert.IsNull(value);
        }

        [TestMethod]
        public void StringExtraction()
        {
            Assert.AreEqual("test", StringHelper.GetStringValue("test"));
            Assert.AreEqual("test", StringHelper.GetStringValue("\"test\""));
            Assert.AreEqual("\"", StringHelper.GetStringValue("\""));
            Assert.AreEqual("abc", StringHelper.GetStringValue("x=\"abc\"", 2));
        }

        [TestMethod]
        public void ParseActions()
        {
            var a = Arguments.Parse("/action:scan");

            Assert.IsNotNull(a);
            Assert.AreEqual(ActionRequest.Scan, a.Action);
        }
    }
}
