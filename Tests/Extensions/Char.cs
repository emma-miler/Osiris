using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Osiris.Extensions;

namespace Tests.Extensions
{
    [TestClass]
    public class Char
    {
        [TestMethod]
        public void Repeat()
        {
            Assert.AreEqual("ttt", 't'.Repeat(3));
        }
    }
}