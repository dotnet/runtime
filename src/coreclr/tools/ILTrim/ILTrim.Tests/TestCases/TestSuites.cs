using System;
using Mono.Linker.Tests.TestCasesRunner;
using Xunit;

namespace Mono.Linker.Tests.TestCases
{
	public class All
	{
        [Theory]
		[MemberData(nameof(TestDatabase.BasicTests), MemberType = typeof(TestDatabase))]
		public void Basic (string t)
		{
			Run (t);
		}

        [Theory]
		[MemberData(nameof(TestDatabase.MultiAssembly), MemberType = typeof(TestDatabase))]
		public void MultiAssembly (string t)
		{
			Run (t);
		}

        [Theory]
        [MemberData(nameof(TestDatabase.LinkXml), MemberType = typeof(TestDatabase))]
        public void LinkXml(string t)
        {
            Run(t);
        }

        [Theory]
        [MemberData(nameof(TestDatabase.FeatureSettings), MemberType = typeof(TestDatabase))]
        public void FeatureSettings(string t)
        {
            Run(t);
        }

        protected virtual void Run (string testName)
		{
            TestCase testCase = TestDatabase.GetTestCaseFromName(testName) ?? throw new InvalidOperationException ($"Unknown test {testName}");
			var runner = new TestRunner (new ObjectFactory ());
			var linkedResult = runner.Run (testCase);
            if (linkedResult != null)
            {
                new ResultChecker().Check(linkedResult);
            }
		}
	}
}
