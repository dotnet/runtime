using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class ReferencesTests : LinkerTestBase
	{

		protected override string TestSuiteName => "References";

		[Fact]
		public Task AssemblyOnlyUsedByUsingWithCsc ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AssemblyReferenceIsRemovedWhenUnused ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CopyAreKeptFully ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CopyUsedAreKeptFully ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CopyWithLinkedWillHaveAttributeDepsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CopyWithLinkedWillHaveMethodDepsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferencesAreRemovedWhenAllUsagesAreRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithEntryPoint ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UserAssembliesAreLinkedByDefault ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}