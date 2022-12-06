using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class BasicTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Basic";

		[Fact]
		public Task ComplexNestedClassesHasUnusedRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DelegateBeginInvokeEndInvokePair ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InitializerForArrayIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InstantiatedTypeWithOverridesFromObject ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InterfaceMethodImplementedOnBaseClassDoesNotGetStripped ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task LinkerHandlesRefFields ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MultiLevelNestedClassesAllRemovedWhenNonUsed ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NeverInstantiatedTypeWithOverridesFromObject ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UninvokedInterfaceMemberGetsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedClassGetsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedEventGetsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedFieldGetsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedFieldsOfStructsAreKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedMethodGetsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedNestedClassGetsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedPropertyGetsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedPropertySetterRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedEventIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedGenericInterfaceIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedInterfaceIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedPropertyIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedStructIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}