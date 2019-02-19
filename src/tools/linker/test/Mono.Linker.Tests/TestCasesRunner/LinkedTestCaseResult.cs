using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.TestCases;

namespace Mono.Linker.Tests.TestCasesRunner {
	public class LinkedTestCaseResult {
		public readonly TestCase TestCase;
		public readonly NPath InputAssemblyPath;
		public readonly NPath OutputAssemblyPath;
		public readonly NPath ExpectationsAssemblyPath;

		public LinkedTestCaseResult (TestCase testCase, NPath inputAssemblyPath, NPath outputAssemblyPath, NPath expectationsAssemblyPath)
		{
			TestCase = testCase;
			InputAssemblyPath = inputAssemblyPath;
			OutputAssemblyPath = outputAssemblyPath;
			ExpectationsAssemblyPath = expectationsAssemblyPath;
		}
	}
}