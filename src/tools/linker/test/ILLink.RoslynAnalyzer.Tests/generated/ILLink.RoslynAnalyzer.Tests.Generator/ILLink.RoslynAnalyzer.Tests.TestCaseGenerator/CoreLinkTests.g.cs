using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class CoreLinkTests : LinkerTestBase
	{

		protected override string TestSuiteName => "CoreLink";

		[Fact]
		public Task CanOverrideIsTrimmableAttribute ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanUseIsTrimmableAttribute ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CopyOfCoreLibrariesKeepsUnusedTypes ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DelegateAndMulticastDelegateKeepInstantiatedReqs ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InstantiatedStructWithOverridesFromObject ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InstantiatedTypeWithOverridesFromObject ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InvalidIsTrimmableAttribute ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task LinkingOfCoreLibrariesRemovesUnusedMethods ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task LinkingOfCoreLibrariesRemovesUnusedTypes ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NeverInstantiatedTypeWithOverridesFromObject ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NoSecurityPlusOnlyKeepUsedRemovesAllSecurityAttributesFromCoreLibraries ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}