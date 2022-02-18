using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Inheritance.Interfaces.OnReferenceType
{
	public sealed partial class NoKeptCtorButInterfaceNeededTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Inheritance.Interfaces.OnReferenceType.NoKeptCtorButInterfaceNeeded";

		[Fact]
		public Task ArrayWithIndexAssignedToReturnValue ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task FieldDowncastedToInterface ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericTypeWithConstraint ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericTypeWithConstraint2 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericTypeWithConstraint3 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InterfaceOnMultipleBases ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task LocalAndNestedInterfaces ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task LocalArrayPassedAsParameter ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task LocalDowncastedToInterface ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task LocalPassedAsParameter ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task LocalPassedAsParameterToGeneric ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task LocalPassedAsParameterToGenericWithConstraint ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task LocalPassedAsParameterToGenericWithConstraint2 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NestedInterfaces1 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NestedInterfaces2 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NestedInterfaces3 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NestedInterfacesWithExplicit1 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NestedInterfacesWithExplicitAndNormal1 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ParameterAndLocal ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ParameterOutAndLocal ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ParameterRefAndLocal ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReturnValueDowncastedToInterface ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}