using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Osiris.Extensions;

namespace Tests.Extensions
{
    [TestClass]
    public class String
    {
        [TestMethod]
        public void Repeat()
        {
            Assert.AreEqual("testtesttest", "test".Repeat(3));
        }

        [TestMethod]
        public void ReplaceLast()
        {
            Assert.AreEqual("testabc", "testabc".ReplaceLast("123", "abc"));
            Assert.AreEqual("testabc123", "testabcabc".ReplaceLast("abc", "123"));
            Assert.AreEqual("123abc", "123123".ReplaceLast("123", "abc"));
        }
    }
}