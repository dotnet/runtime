using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class CodegenAnnotationTests : LinkerTestBase
	{

		protected override string TestSuiteName => "CodegenAnnotation";

		[Fact]
		public Task ReflectionBlockedTest ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}