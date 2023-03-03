using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.BCLFeatures
{
	public sealed partial class ETWTests : LinkerTestBase
	{

		protected override string TestSuiteName => "BCLFeatures.ETW";

		[Fact]
		public Task BaseRemovedEventSource ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task BaseRemovedEventSourceEmptyBody ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task BaseRemovedEventSourceNonVoidReturn ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CustomEventSource ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CustomLibraryEventSource ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task Excluded ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task LocalsOfModifiedMethodAreRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NonEventWithLog ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task StubbedMethodWithExceptionHandlers ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}