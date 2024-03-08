using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class SubstitutionsTests : LinkerTestBase
	{

		[Fact]
		public Task EmbeddedFieldSubstitutionsInReferencedAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task EmbeddedMethodSubstitutionsInReferencedAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task EmbeddedSubstitutions ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task EmbeddedSubstitutionsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task EmbeddedSubstitutionsNotProcessedWithIgnoreSubstitutions ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task EmbeddedSubstitutionsNotProcessedWithIgnoreSubstitutionsAndRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task FeatureGuardSubstitutionsDisabled ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InitField ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InitFieldExistingCctor ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task RemoveBody ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ResourceSubstitutions ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task StubBody ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task StubBodyInvalidSyntax ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task StubBodyUnsafe ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task StubBodyWithStaticCtor ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task StubBodyWithValue ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task SubstitutionsErrorCases ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}
