using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Inheritance.Interfaces.OnReferenceType
{
	public sealed partial class NoKeptCtorTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Inheritance.Interfaces.OnReferenceType.NoKeptCtor";

		[Fact]
		public Task ComInterfaceTypeRemovedWhenOnlyUsedByClassWithOnlyStaticMethod ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DynamicDependencyPreservesInterfaceMethod ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ExplicitInterfaceCanBeRemovedFromClassWithOnlyStaticMethodUsed ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericWithConstraintDoesNotCauseOtherTypesToKeepInterface ()
		{
			return RunTest (allowMissingWarnings: true);
		}

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
		public Task InterfaceFromCopiedAssemblyCanBeRemoved ()
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

		[Fact]
		public Task LocalDowncastDoesNotCuaseOtherTypesToKeepInterface ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ObjectHardCastedToInterface ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task PreserveDependencyPreservesInterfaceMethod ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedTypeHasExplicitInterfaceMethodPreservedViaXml ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedTypeHasExplicitInterfacePropertyPreservedViaXml ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedTypeHasInterfaceMethodPreservedViaXml ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedTypeWithPreserveFields ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedTypeWithPreserveMethods ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedTypeWithPreserveMethodsAndInterfaceTypeMarked ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}