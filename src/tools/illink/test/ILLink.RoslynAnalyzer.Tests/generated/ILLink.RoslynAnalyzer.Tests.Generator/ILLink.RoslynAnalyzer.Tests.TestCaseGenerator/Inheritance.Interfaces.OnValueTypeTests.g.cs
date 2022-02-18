using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Inheritance.Interfaces
{
	public sealed partial class OnValueTypeTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Inheritance.Interfaces.OnValueType";

		[Fact]
		public Task StructImplementingInterfaceMethodsNested ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task StructImplementingInterfaceMethodsNested2 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task StructUsedFromConcreteTypeHasInterfaceMethodRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task StructUsedFromConcreteTypeHasInterfaceMethodRemoved2 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task StructUsedFromInterfaceHasInterfaceMethodKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task StructWithNestedStructImplementingInterface ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedExplicitInterfaceHasMethodPreservedViaXml ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedExplicitInterfaceIsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedInterfaceHasMethodPreservedViaXml ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedInterfaceTypeIsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}