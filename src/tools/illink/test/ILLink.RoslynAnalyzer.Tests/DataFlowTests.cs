// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.Shared.DataFlow;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    public sealed partial class DataFlowTests : LinkerTestBase
    {
        protected override string TestSuiteName => "DataFlow";

        [Fact]
        public void StructuredLocalValueEqualityAndDeepCopy()
        {
            LocalValue<TestValue> value = Tuple(
                Scalar(1),
                Tuple(Scalar(2), Scalar(3)));
            LocalValue<TestValue> copy = value.DeepCopy();

            Assert.Equal(value, copy);
            Assert.Equal(value.GetHashCode(), copy.GetHashCode());
            Assert.NotSame(value.Elements[0].ScalarValue.Box, copy.Elements[0].ScalarValue.Box);
            Assert.NotSame(value.Elements[1].Elements[0].ScalarValue.Box, copy.Elements[1].Elements[0].ScalarValue.Box);
            Assert.NotEqual(value, Tuple(Scalar(1), Tuple(Scalar(2), Scalar(4))));
            Assert.Equal(LocalValue<TestValue>.Top, LocalValue<TestValue>.Top.DeepCopy());
            Assert.Equal(LocalValue<TestValue>.Unknown, LocalValue<TestValue>.Unknown.DeepCopy());
        }

        [Fact]
        public void StructuredLocalValueLatticeMergesCompatibleShapes()
        {
            LocalValueLattice<TestValue, TestValueLattice> lattice = new(default(TestValueLattice));
            LocalValue<TestValue> left = Tuple(Scalar(1), Tuple(Scalar(2), Scalar(3)));
            LocalValue<TestValue> right = Tuple(Scalar(4), Tuple(Scalar(5), Scalar(6)));

            Assert.Equal(Tuple(Scalar(5), Tuple(Scalar(7), Scalar(7))), lattice.Meet(left, right));
            Assert.Equal(left, lattice.Meet(LocalValue<TestValue>.Top, left));
            Assert.Equal(right, lattice.Meet(right, LocalValue<TestValue>.Top));
            Assert.Equal(LocalValue<TestValue>.Unknown, lattice.Meet(LocalValue<TestValue>.Unknown, left));
            Assert.Equal(LocalValue<TestValue>.Unknown, lattice.Meet(right, LocalValue<TestValue>.Unknown));
            Assert.Equal(1, Scalar(1).GetScalarValueOrTop(new TestValue(99)).Box.Value);
            Assert.Equal(99, left.GetScalarValueOrTop(new TestValue(99)).Box.Value);
        }

        [Fact]
        public void StructuredLocalValueLatticeRejectsIncompatibleShapes()
        {
            LocalValueLattice<TestValue, TestValueLattice> lattice = new(default(TestValueLattice));

            Assert.Equal(LocalValue<TestValue>.Unknown, lattice.Meet(Scalar(1), Tuple(Scalar(1))));
            Assert.Equal(LocalValue<TestValue>.Unknown, lattice.Meet(Tuple(Scalar(1)), Tuple(Scalar(1), Scalar(2))));
            Assert.Equal(
                Tuple(LocalValue<TestValue>.Unknown, Scalar(3)),
                lattice.Meet(Tuple(Scalar(1), Scalar(2)), Tuple(Tuple(Scalar(1)), Scalar(3))));
        }

        [Fact]
        public void StructuredLocalValueLatticeSatisfiesLatticeLaws()
        {
            LocalValueLattice<TestValue, TestValueLattice> lattice = new(default(TestValueLattice));
            LocalValue<TestValue>[] values =
            [
                LocalValue<TestValue>.Top,
                LocalValue<TestValue>.Unknown,
                Scalar(1),
                Scalar(2),
                Tuple(Scalar(1)),
                Tuple(Scalar(2)),
                Tuple(Scalar(1), Scalar(2)),
                Tuple(Tuple(Scalar(1)), Scalar(2))
            ];

            foreach (LocalValue<TestValue> left in values)
            {
                Assert.Equal(left, lattice.Meet(left, left));

                foreach (LocalValue<TestValue> middle in values)
                {
                    Assert.Equal(lattice.Meet(left, middle), lattice.Meet(middle, left));

                    foreach (LocalValue<TestValue> right in values)
                    {
                        Assert.Equal(
                            lattice.Meet(lattice.Meet(left, middle), right),
                            lattice.Meet(left, lattice.Meet(middle, right)));
                    }
                }
            }
        }

        private static LocalValue<TestValue> Scalar(int value) => new(new TestValue(value));

        private static LocalValue<TestValue> Tuple(params LocalValue<TestValue>[] elements) =>
            new(ImmutableArray.Create(elements));

        private sealed class TestValueBox
        {
            public int Value { get; }

            public TestValueBox(int value) => Value = value;
        }

        private readonly struct TestValue : IEquatable<TestValue>, IDeepCopyValue<TestValue>
        {
            public TestValueBox Box { get; }

            public TestValue(int value) => Box = new TestValueBox(value);

            public bool Equals(TestValue other) => Box.Value == other.Box.Value;

            public override bool Equals(object? obj) => obj is TestValue other && Equals(other);

            public override int GetHashCode() => Box.Value;

            public TestValue DeepCopy() => new(Box.Value);
        }

        private readonly struct TestValueLattice : ILattice<TestValue>
        {
            public TestValue Top => default;

            public TestValue Meet(TestValue left, TestValue right) =>
                new(left.Box.Value | right.Box.Value);
        }

        [Fact]
        public Task AnnotatedMembersAccessedViaReflection()
        {
            return RunTest(nameof(AnnotatedMembersAccessedViaReflection));
        }

        [Fact]
        public Task AnnotatedMembersAccessedViaUnsafeAccessor()
        {
            return RunTest();
        }

        [Fact]
        public Task ApplyTypeAnnotations()
        {
            return RunTest();
        }

        [Fact]
        public Task AssemblyGetTypeDataFlow()
        {
            return RunTest();
        }

        [Fact]
        public Task AssemblyQualifiedNameDataflow()
        {
            return RunTest(nameof(AssemblyQualifiedNameDataflow));
        }

        [Fact]
        public Task ArrayDataFlow()
        {
            return RunTest();
        }

        [Fact]
        public Task AttributeConstructorDataflow()
        {
            return RunTest(nameof(AttributeConstructorDataflow));
        }

        [Fact]
        public Task AttributeFieldDataflow()
        {
            return RunTest(nameof(AttributeFieldDataflow));
        }

        [Fact]
        public Task AttributePrimaryConstructorDataflow()
        {
            return RunTest();
        }

        [Fact]
        public Task AttributePropertyDataflow()
        {
            return RunTest(nameof(AttributePropertyDataflow));
        }

        [Fact]
        public Task ByRefDataflow()
        {
            return RunTest(nameof(ByRefDataflow));
        }

        [Fact]
        public Task CompilerGeneratedCodeDataflow()
        {
            return RunTest();
        }

        [Fact]
        public Task CompilerGeneratedCodeInPreservedAssembly()
        {
            return RunTest();
        }

        [Fact]
        public Task CompilerGeneratedCodeInPreservedAssemblyWithWarning()
        {
            return RunTest();
        }

        [Fact]
        public Task CompilerGeneratedTypes()
        {
            return RunTest();
        }

        [Fact]
        public Task CompilerGeneratedTypesNet90()
        {
            return RunTest();
        }

        [Fact]
        public Task CompilerGeneratedTypesReleaseNet90()
        {
            return RunTest();
        }

        [Fact]
        public Task CompilerGeneratedTypesRelease()
        {
            return RunTest();
        }

        [Fact]
        public Task ComplexTypeHandling()
        {
            return RunTest();
        }

        [Fact]
        public Task CompilerGeneratedCodeAccessedViaReflection()
        {
            return RunTest();
        }

        [Fact]
        public Task ConstructedTypesDataFlow()
        {
            return RunTest();
        }

        [Fact]
        public Task ConstructorDataFlow()
        {
            return RunTest();
        }

        [Fact]
        public Task DataflowInLocalMethodGroupArgument()
        {
            return RunTest();
        }

        [Fact]
        public Task DeconstructFieldTarget()
        {
            return RunTest();
        }

        [Fact]
        public Task DeconstructUserDefinedConversion()
        {
            return RunTest();
        }

        [Fact]
        public Task DependencyInjectionPattern()
        {
            return RunTest();
        }

        [Fact]
        public Task DynamicDependencyDataflow()
        {
            return RunTest(nameof(DynamicDependencyDataflow));
        }

        [Fact]
        public Task DynamicObjects()
        {
            return RunTest();
        }

        [Fact]
        public Task EmptyArrayIntrinsicsDataFlow()
        {
            // https://github.com/dotnet/linker/issues/2273
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task EventDataFlow()
        {
            return RunTest();
        }

        [Fact]
        public Task ExtensionsDataFlow()
        {
            return RunTest();
        }

        [Fact]
        public Task ExtensionMembersDataFlow()
        {
            return RunTest();
        }

        [Fact]
        public Task FeatureCheckDataFlow()
        {
            return RunTest();
        }

        [Fact]
        public Task FeatureGuardAttributeDataFlow()
        {
            return RunTest();
        }

        [Fact]
        public Task FieldDataFlow()
        {
            return RunTest(nameof(FieldDataFlow));
        }

        [Fact]
        public Task FieldKeyword()
        {
            return RunTest();
        }

        [Fact]
        public Task FileScopedClasses()
        {
            return RunTest();
        }

        [Fact]
        public Task FunctionPointerDataflow()
        {
            return RunTest();
        }

        [Fact]
        public Task GenericParameterDataFlow()
        {
            return RunTest();
        }

        [Fact]
        public Task GenericParameterWarningLocation()
        {
            return RunTest();
        }

        [Fact]
        public Task InlineArrayDataflow()
        {
            return RunTest();
        }

        [Fact]
        public Task InterpolatedStringDataFlow()
        {
            return RunTest();
        }

        [Fact]
        public Task MakeGenericDataFlow()
        {
            return RunTest();
        }

        [Fact]
        public Task RequiresDynamicCodeAnalyzerIntrinsics()
        {
            return RunTest();
        }

        [Fact]
        public Task MethodByRefReturnDataFlow()
        {
            return RunTest();
        }

        [Fact]
        public Task MultipleReturnsDataFlow()
        {
            return RunTest();
        }

        [Fact]
        public Task GetInterfaceDataFlow()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task GetNestedTypeOnAllAnnotatedType()
        {
            // https://github.com/dotnet/linker/issues/2273
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task GetTypeDataFlow()
        {
            return RunTest();
        }

        [Fact]
        public Task GetTypeInfoDataFlow()
        {
            return RunTest(nameof(GetTypeInfoDataFlow));
        }

        [Fact]
        public Task TypeInfoAsTypeDataFlow()
        {
            return RunTest(nameof(TypeInfoAsTypeDataFlow));
        }

        [Fact]
        public Task TypeHandleDataFlow()
        {
            return RunTest(nameof(TypeHandleDataFlow));
        }

        [Fact]
        public Task IReflectDataflow()
        {
            return RunTest(nameof(IReflectDataflow));
        }

        [Fact]
        public Task LocalDataFlow()
        {
            return RunTest(nameof(LocalDataFlow));
        }

        [Fact]
        public Task ExceptionalDataFlow()
        {
            return RunTest(nameof(ExceptionalDataFlow));
        }

        [Fact]
        public Task LocalDataFlowKeptMembers()
        {
            return RunTest(nameof(LocalDataFlowKeptMembers));
        }

        [Fact]
        public Task MemberTypes()
        {
            return RunTest(nameof(MemberTypes));
        }

        [Fact]
        public Task MemberTypesAllOnCopyAssembly()
        {
            return RunTest(nameof(MemberTypesAllOnCopyAssembly));
        }

        [Fact]
        public Task MemberTypesRelationships()
        {
            return RunTest(nameof(MemberTypesRelationships));
        }

        [Fact]
        public Task MethodOutParameterDataFlow()
        {
            return RunTest();
        }

        [Fact]
        public Task MethodParametersDataFlow()
        {
            return RunTest(nameof(MethodParametersDataFlow));
        }

        [Fact]
        public Task MethodReturnParameterDataFlow()
        {
            return RunTest(nameof(MethodReturnParameterDataFlow));
        }

        [Fact]
        public Task MethodThisDataFlow()
        {
            return RunTest(nameof(MethodThisDataFlow));
        }

        [Fact]
        public Task NullableAnnotations()
        {
            return RunTest();
        }

        [Fact]
        public Task ObjectGetTypeDataflow()
        {
            return RunTest();
        }

        [Fact]
        public Task PropertyDataFlow()
        {
            return RunTest(nameof(PropertyDataFlow));
        }

        [Fact]
        public Task RefFieldDataFlow()
        {
            return RunTest(nameof(RefFieldDataFlow));
        }

        [Fact(Skip = "https://github.com/dotnet/linker/issues/2273")]
        public Task SuppressWarningWithLinkAttributes()
        {
            return RunTest(nameof(SuppressWarningWithLinkAttributes));
        }

        [Fact]
        public Task TypeBaseTypeDataFlow()
        {
            return RunTest();
        }

        [Fact]
        public Task UnresolvedMembers()
        {
            // https://github.com/dotnet/linker/issues/2273
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task VirtualMethodHierarchyDataflowAnnotationValidation()
        {
            return RunTest(nameof(VirtualMethodHierarchyDataflowAnnotationValidation));
        }

        [Fact(Skip = "https://github.com/dotnet/linker/issues/2273")]
        public Task XmlAnnotations()
        {
            return RunTest(nameof(XmlAnnotations));
        }
    }
}
