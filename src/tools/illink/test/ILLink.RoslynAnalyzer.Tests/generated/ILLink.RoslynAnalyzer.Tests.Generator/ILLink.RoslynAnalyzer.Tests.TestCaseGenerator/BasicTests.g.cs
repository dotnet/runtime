using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    public sealed partial class BasicTests : LinkerTestBase
    {

        protected override string TestSuiteName => "Basic";

        [Fact]
        public Task Calli()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task ComplexNestedClassesHasUnusedRemoved()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task DelegateBeginInvokeEndInvokePair()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task ExceptionRegions()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task FieldRVA()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task FieldSignature()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task FieldsOfEnum()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task Finalizer()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task First()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task FunctionPointer()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task GenericParameters()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task GenericType()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task InitializerForArrayIsKept()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task InstanceFields()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task InstantiatedTypeWithOverridesFromObject()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task InterfaceCalls()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task InterfaceMethodImplementedOnBaseClassDoesNotGetStripped()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task InterfaceOrder()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task LibraryModeTest()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task LinkerHandlesRefFields()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task MethodSpecSignature()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task MultiDimArraySignature()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task MultiLevelNestedClassesAllRemovedWhenNonUsed()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task NestedDelegateInvokeMethodsPreserved()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task NeverInstantiatedTypeWithOverridesFromObject()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task Resources()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task Switch()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task TypeOf()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task TypeSpecSignature()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task UninvokedInterfaceMemberGetsRemoved()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task UnusedClassGetsRemoved()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task UnusedDelegateGetsRemoved()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task UnusedEnumGetsRemoved()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task UnusedEventGetsRemoved()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task UnusedFieldGetsRemoved()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task UnusedFieldsOfStructsAreKept()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task UnusedMethodGetsRemoved()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task UnusedNestedClassGetsRemoved()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task UnusedPropertyGetsRemoved()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task UnusedPropertySetterRemoved()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task UsedEnumIsKept()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task UsedEventIsKept()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task UsedEventOnInterfaceIsKept()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task UsedEventOnInterfaceIsRemovedWhenUsedFromClass()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task UsedGenericInterfaceIsKept()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task UsedInterfaceIsKept()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task UsedPropertyIsKept()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task UsedStructIsKept()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task VirtualMethods()
        {
            return RunTest(allowMissingWarnings: true);
        }

    }
}
