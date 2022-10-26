using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Attributes
{
	public sealed partial class OnlyKeepUsedTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Attributes.OnlyKeepUsed";

		[Fact]
		public Task ComAttributesArePreserved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ComAttributesAreRemovedWhenFeatureExcluded ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ContextStaticIsPreservedOnField ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CoreLibraryUnusedAssemblyAttributesAreRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CoreLibraryUsedAssemblyAttributesAreKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task FixedLengthArrayAttributesArePreserved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MethodWithUnmanagedConstraint ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NullableOnConstraints ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ThreadStaticIsPreservedOnField ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}