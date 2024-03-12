using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class WarningsTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Warnings";

		[Fact]
		public Task CanDisableWarnAsError ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanDisableWarnings ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanNotSingleWarnPerAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanNotWarnAsErrorForDisabledVersion ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanSetWarningVersion0 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanSetWarningVersion5 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanSetWarningVersion9999 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanSingleWarn ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanSingleWarnPerAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanSingleWarnWithIndividualWarnAsError ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanSingleWarnWithNoWarn ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanSingleWarnWithWarnAsError ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanWarnAsError ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanWarnAsErrorGlobal ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InvalidWarningVersion ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NoWarnRegardlessOfWarnAsError ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task WarningsFromTrimmableAssembliesCanSurviveSingleWarn ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}
