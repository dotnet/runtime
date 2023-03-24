using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class StaticsTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Statics";

		[Fact]
		public Task ExplicitStaticCtor ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MixedStaticFieldInitializerAndCtor ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task StaticFieldInitializer ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedStaticConstructorGetsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedStaticFieldInitializer ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedStaticMethodGetsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}