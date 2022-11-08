using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Inheritance.Interfaces.OnReferenceType
{
	public sealed partial class BaseProvidesInterfaceMemberTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Inheritance.Interfaces.OnReferenceType.BaseProvidesInterfaceMember";

		[Fact]
		public Task GenericInterfaceWithEvent ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericInterfaceWithGenericBaseMethod ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericInterfaceWithGenericBaseMethodWithExplicit ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericInterfaceWithGenericMethod ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericInterfaceWithGenericMethod2 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericInterfaceWithGenericMethod3 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericInterfaceWithMethod ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericInterfaceWithMethod2 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericInterfaceWithMethod3 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericInterfaceWithMethodComplex ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericInterfaceWithMethodComplex2 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericInterfaceWithMethodManyBaseInterfaces ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericInterfaceWithMethodManyBases ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericInterfaceWithMethodManyBases2 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericInterfaceWithMethodManyBases3 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericInterfaceWithMethodManyVariations ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericInterfaceWithMethodNested ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericInterfaceWithMethodOnNoInstanceCtor ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericInterfaceWithMethodOnNoInstanceCtor2 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericInterfaceWithPropertyGetter ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericInterfaceWithPropertyGetter2 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericInterfaceWithPropertySetter ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericInterfaceWithPropertySetter2 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InterfaceRemovedWhenMethodUsedDirectly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task SimpleEvent ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task SimpleMethod ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task SimpleMethodOnNoInstanceCtor ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task SimpleMethodOnNoInstanceCtor2 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task SimpleProperty ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}