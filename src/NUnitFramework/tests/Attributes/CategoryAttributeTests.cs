// ***********************************************************************
// Copyright (c) 2007 Charlie Poole
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using NUnit.Framework.Api;
using NUnit.Framework.Internal;
using NUnit.TestData.CategoryAttributeData;
using NUnit.TestUtilities;

namespace NUnit.Framework.Attributes
{
	/// <summary>
	/// Summary description for CategoryAttributeTests.
	/// </summary>
	[TestFixture]
	public class CategoryAttributeTests
	{
		TestSuite fixture;

		[SetUp]
		public void CreateFixture()
		{
			fixture = TestBuilder.MakeFixture( typeof( FixtureWithCategories ) );
		}

		[Test]
		public void CategoryOnFixture()
		{
			Assert.That( fixture.Properties.Contains("Category", "DataBase"));
		}

		[Test]
		public void CategoryOnTestMethod()
		{
			Test test1 = (Test)fixture.Tests[0];
			Assert.That( test1.Properties.Contains("Category", "Long") );
		}

		[Test]
		public void CanDeriveFromCategoryAttribute()
		{
			Test test2 = (Test)fixture.Tests[1];
			Assert.Contains( "Critical", test2.Properties["Category"] );
		}

        [Test]
        public void CanSpecifyOnMethodAndTestCase()
        {
#if !NUNITLITE
            TestSuite test3 = (TestSuite)fixture.Tests[2];
            Assert.Contains("Top", test3.Properties["Category"]);
            Test testCase = (Test)test3.Tests[0];
            Assert.Contains("Bottom", testCase.Properties["Category"]);
#endif
        }

        [Test]
        public void TestWithInvalidCategoryNameIsNotRunnable()
        {
            Test test4 = (Test)fixture.Tests[3];
            Assert.That(test4.RunState, Is.EqualTo(RunState.NotRunnable));
        }
	}
}
