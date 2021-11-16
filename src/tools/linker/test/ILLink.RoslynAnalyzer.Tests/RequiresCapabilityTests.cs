
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed class RequiresCapabilityTests : LinkerTestBase
	{
		protected override string TestSuiteName => "RequiresCapability";

		[Fact]
		public Task RequiresCapability ()
		{
			return RunTest (nameof (RequiresCapability));
		}

		[Fact]
		public Task RequiresCapabilityFromCopiedAssembly ()
		{
			return RunTest (nameof (RequiresCapabilityFromCopiedAssembly));
		}

		[Fact]
		public Task RequiresCapabilityReflectionAnalysisEnabled ()
		{
			return RunTest (nameof (RequiresCapabilityReflectionAnalysisEnabled));
		}

		[Fact]
		public Task RequiresInCompilerGeneratedCode ()
		{
			return RunTest (nameof (RequiresInCompilerGeneratedCode));
		}

		[Fact]
		public Task RequiresOnAttributeCtor ()
		{
			return RunTest (nameof (RequiresOnAttributeCtor));
		}
	}
}