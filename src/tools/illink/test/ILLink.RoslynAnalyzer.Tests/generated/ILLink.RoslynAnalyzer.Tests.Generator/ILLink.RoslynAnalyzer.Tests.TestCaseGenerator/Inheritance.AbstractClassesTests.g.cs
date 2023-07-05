using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Inheritance
{
	public sealed partial class AbstractClassesTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Inheritance.AbstractClasses";

		[Fact]
		public Task TypeWithBaseInCopiedAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeWithBaseInCopiedAssembly2 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeWithBaseInCopiedAssembly3 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeWithBaseInCopiedAssembly4 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeWithBaseInCopiedAssembly5 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeWithBaseInCopiedAssembly6 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedAbstractMethodRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedVirtualMethodRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedOverrideOfAbstractMethodIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedOverrideOfVirtualMethodIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}