using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class UnreachableBodyTests : LinkerTestBase
	{

		protected override string TestSuiteName => "UnreachableBody";

		[Fact]
		public Task BodyWithManyVariables ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task BodyWithManyVariablesWithSymbols ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanDisableLazyBodyMarking ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DoesNotApplyToCopiedAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DoesNotApplyToCopiedAssembly2 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ExplicitInstructionCheck ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InterfaceMethod ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task LinkedOtherIncludedLibrary ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task LinkedOtherIncludedLibraryNoInstanceCtor ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MixOfMethods ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NotWorthConvertingEmpty ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NotWorthConvertingReturnDouble ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NotWorthConvertingReturnFalse ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NotWorthConvertingReturnFloat ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NotWorthConvertingReturnInt ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NotWorthConvertingReturnLong ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NotWorthConvertingReturnNull ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NotWorthConvertingReturnTrue ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OverrideOfAbstractAndInterfaceMethodCalledFromLocal ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OverrideOfAbstractAndInterfaceMethodCalledFromLocal2 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OverrideOfAbstractAndInterfaceMethodCalledFromLocal3 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OverrideOfAbstractAndInterfaceMethodWhenInterfaceRemoved2 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OverrideOfAbstractIsStubbed ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OverrideOfAbstractIsStubbedWithUnusedInterface ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OverrideOfAVirtual ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task SimpleGetter ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task SimpleMethod ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task SimpleSetter ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task WorksWithDynamicDependency ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task WorksWithLinkXml ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task WorksWithPreserveDependency ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}