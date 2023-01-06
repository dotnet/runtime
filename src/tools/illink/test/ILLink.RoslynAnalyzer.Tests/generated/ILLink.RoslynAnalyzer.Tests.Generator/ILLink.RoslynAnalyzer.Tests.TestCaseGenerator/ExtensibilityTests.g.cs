using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class ExtensibilityTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Extensibility";

		[Fact]
		public Task CustomStepCanPreserveMethodsAfterMark ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CustomStepCanPreserveMethodsBeforeMark ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CustomStepCanResolveTypesAfterSweep ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CustomStepsCanShareState ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CustomWarningUsage ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MarkHandlerUsage ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MarkSubStepsDispatcherUsage ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task SubStepDispatcherFields ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task SubStepDispatcherUsage ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}