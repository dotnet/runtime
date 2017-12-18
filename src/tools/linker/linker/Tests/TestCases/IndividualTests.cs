using System;
using Mono.Linker.Tests.Cases.References.Individual;
using Mono.Linker.Tests.TestCases;
using Mono.Linker.Tests.TestCasesRunner;
using NUnit.Framework;

namespace Mono.Linker.Tests.TestCases
{
	[TestFixture]
	public class IndividualTests
	{
		[Test]
		public void CanSkipUnresolved ()
		{
			var testcase = CreateIndividualCase (typeof (CanSkipUnresolved));
			var result = Run (testcase);

			// We can't use the ResultChecker on the output because there will be unresolved types/methods
			// Let's just make sure that the output assembly exists.  That's enough to verify that the linker didn't throw due to the
			// missing types/methods
			if (!result.OutputAssemblyPath.Exists ())
				Assert.Fail ($"The linked assembly is missing.  Should have existed at {result.OutputAssemblyPath}");
		}

		private TestCase CreateIndividualCase (Type testCaseType)
		{
			return TestDatabase.CreateCollector ().CreateIndividualCase (testCaseType);
		}

		protected virtual LinkedTestCaseResult Run (TestCase testCase)
		{
			var runner = new TestRunner (new ObjectFactory ());
			return runner.Run (testCase);
		}
	}
}
