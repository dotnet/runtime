using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class GenericsTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Generics";

		[Fact]
		public Task ArrayVariantCasting ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CorrectOverloadedMethodGetsStrippedInGenericClass ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DerivedClassWithMethodOfSameNameAsBaseButDifferentNumberOfGenericParametersUnusedBaseWillGetStripped ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericInstanceInterfaceMethodImplementedWithDifferentGenericArgumentNameDoesNotGetStripped ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MdArrayVariantCasting ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MethodWithParameterWhichHasGenericParametersAndOverrideUsesADifferentNameForGenericParameter ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MethodWithParameterWhichHasGenericParametersAndOverrideUsesADifferentNameForGenericParameterNestedCase ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MethodWithParameterWhichHasGenericParametersAndOverrideUsesADifferentNameForGenericParameterNestedCase2 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NewConstraintOnClass ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OverrideWithAnotherVirtualMethodOfSameNameWithDifferentParameterType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedOverloadedGenericMethodInGenericClassIsNotStripped ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedOverloadedGenericMethodInstanceInGenericClassIsNotStripped ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedOverloadedGenericMethodWithNoParametersIsNotStripped ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task VariantCasting ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}