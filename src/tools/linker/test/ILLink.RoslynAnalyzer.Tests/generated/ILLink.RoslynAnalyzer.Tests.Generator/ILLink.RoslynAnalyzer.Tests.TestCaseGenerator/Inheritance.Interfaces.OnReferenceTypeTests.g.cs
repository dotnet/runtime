using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Inheritance.Interfaces
{
	public sealed partial class OnReferenceTypeTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Inheritance.Interfaces.OnReferenceType";

		[Fact]
		public Task ClassImplementingInterfaceMethodsNested ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ClassImplementingInterfaceMethodsNested2 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ClassImplemtingInterfaceMethodsThroughBaseClass2 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ClassImplemtingInterfaceMethodsThroughBaseClass3 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ClassImplemtingInterfaceMethodsThroughBaseClass4 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ClassImplemtingInterfaceMethodsThroughBaseClass5 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ClassImplemtingInterfaceMethodsThroughBaseClass6 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ClassUsedFromConcreteTypeHasInterfaceMethodRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ClassUsedFromConcreteTypeHasInterfaceMethodRemoved2 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ClassUsedFromInterfaceHasInterfaceMethodKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ExplicitInterfaceMethodWhichCreatesInstanceOfParentType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InterfaceMarkOrderingDoesNotMatter ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InterfaceMarkOrderingDoesNotMatter2 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InterfaceMarkOrderingDoesNotMatter3 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InterfaceTypeInOtherUsedOnlyByCopiedAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InterfaceTypeInOtherUsedOnlyByCopiedAssemblyExplicit ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InterfaceTypeOnlyUsedHasInterfacesRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InterfaceUsedOnlyAsConstraintIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InterfaceWithInterfaceFromOtherAssemblyWhenExplicitMethodUsed ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ObjectCastedToSecondInterfaceHasMemberRemovedButInterfaceKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeGetsMarkedThatImplementsAlreadyMarkedInterfaceMethod ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedComInterfaceIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedComInterfaceIsRemovedWhenComFeatureExcluded ()
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
		public Task UnusedGenericInterfaceIsRemoved ()
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