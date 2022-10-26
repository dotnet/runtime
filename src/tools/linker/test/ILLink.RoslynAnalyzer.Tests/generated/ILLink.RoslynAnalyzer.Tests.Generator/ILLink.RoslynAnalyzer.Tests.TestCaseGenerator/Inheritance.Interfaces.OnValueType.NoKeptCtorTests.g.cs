using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Inheritance.Interfaces.OnValueType
{
	public sealed partial class NoKeptCtorTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Inheritance.Interfaces.OnValueType.NoKeptCtor";

		[Fact]
		public Task InterfaceCanBeRemovedFromClassWithOnlyStaticMethodUsed ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InterfaceCanBeRemovedFromClassWithOnlyStaticMethodUsedWithCctor ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InterfaceTypeRemovedWhenOnlyUsedByClassWithOnlyStaticMethod ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InterfaceTypeRemovedWhenOnlyUsedByClassWithOnlyStaticMethodMultiple ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}