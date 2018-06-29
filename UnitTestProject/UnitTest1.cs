using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            IMyDao interfaceProxy = TestDaoFactory.Get<IMyDao, MyDao>(null, null);
            interfaceProxy.foo(3);

            IMyDao classProxy = TestDaoFactory.Get<IMyDao>(null, null);
            classProxy.foo(3);
        }
    }
}
