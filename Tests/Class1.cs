using System;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class TestClass1
    {
        [Test]
        public void Test1()
        {
            Assert.That(new[] {1,2,3,4,5}, Is.SubsetOf(new[] {1,2,3,4,5,6}));
        }
    }
}
