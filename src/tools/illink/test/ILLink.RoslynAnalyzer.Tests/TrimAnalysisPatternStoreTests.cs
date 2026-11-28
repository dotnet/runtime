// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerators.Tests;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    public class TrimAnalysisPatternStoreTests
    {
        private static readonly Type s_storeType = typeof(ValueSet<SingleValue>).Assembly.GetType(
            "ILLink.RoslynAnalyzer.TrimAnalysis.TrimAnalysisPatternStore", throwOnError: true)!;

        [Theory]
        [InlineData(false, 0)]
        [InlineData(true, 0)]
        [InlineData(true, 1)]
        [InlineData(true, 3)]
        public void RepeatedContributionsSnapshotEachValueOnce(bool methodCall, int argumentCount)
        {
            (IOperation operation, IMethodSymbol method) = CreateOperation();
            object store = CreateStore();
            var counter = new CopyCounter();
            var value = new MutableValue(1, counter);
            FeatureContext firstContext = new(new ValueSet<string>(new[] { "first", "shared" }));
            FeatureContext secondContext = new(new ValueSet<string>(new[] { "second", "shared" }));
            AddContribution(store, methodCall, argumentCount, value, operation, method, firstContext);
            Assert.Equal(methodCall ? argumentCount + 1 : 1, counter.Copies);
            value.Id = 2;
            object firstPattern = GetPatterns(store, methodCall)[operation]!;
            Assert.Equal(1, Assert.IsType<MutableValue>(Assert.Single(GetValues(firstPattern, methodCall).GetKnownValues())).Id);

            AddContribution(store, methodCall, argumentCount, value, operation, method, secondContext);
            value.Id = 3;
            object mergedPattern = GetPatterns(store, methodCall)[operation]!;
            Assert.Equal(new[] { 1, 2 }, GetValues(mergedPattern, methodCall).GetKnownValues()
                .Cast<MutableValue>().Select(item => item.Id).OrderBy(id => id));
            Assert.Equal(new[] { "shared" }, ReadProperty<FeatureContext>(mergedPattern, "FeatureContext").EnabledFeatures.GetKnownValues());

            value.Id = 1;
            counter.Copies = 0;
            object unchangedStore = CreateStore();
            AddContribution(unchangedStore, methodCall, argumentCount, value, operation, method, firstContext);
            counter.Copies = 0;
            AddContribution(unchangedStore, methodCall, argumentCount, value, operation, method, firstContext);
            Assert.Equal(methodCall ? argumentCount + 1 : 1, counter.Copies);
        }

        [Theory]
        [InlineData(AssignmentTarget.Empty, false)]
        [InlineData(AssignmentTarget.Unannotated, false)]
        [InlineData(AssignmentTarget.MultipleUnannotated, false)]
        [InlineData(AssignmentTarget.Annotated, true)]
        [InlineData(AssignmentTarget.Mixed, true)]
        [InlineData(AssignmentTarget.Unexpected, true)]
        [InlineData(AssignmentTarget.Unknown, true)]
        public void AssignmentStorageSkipsOnlyTargetsWithoutRequirements(AssignmentTarget targetKind, bool stored)
        {
            (IOperation operation, IMethodSymbol method) = CreateOperation();
            object store = CreateStore();
            var counter = new CopyCounter();
            ValueSet<SingleValue> source = new(new MutableValue(1, counter));
            SingleValue unannotated = CreateTarget(method, annotated: false);
            SingleValue annotated = CreateTarget(method, annotated: true);
            ValueSet<SingleValue> target = targetKind switch
            {
                AssignmentTarget.Empty => default,
                AssignmentTarget.Unannotated => new(unannotated),
                AssignmentTarget.MultipleUnannotated => new(new[] { unannotated,
                    CreateTarget(method.ContainingType.GetMembers("Other").OfType<IMethodSymbol>().Single(), annotated: false) }),
                AssignmentTarget.Annotated => new(annotated),
                AssignmentTarget.Mixed => new(new[] { unannotated, annotated }),
                AssignmentTarget.Unexpected => source,
                AssignmentTarget.Unknown => ValueSet<SingleValue>.Unknown,
                _ => throw new InvalidOperationException()
            };

            AddAssignment(store, source, target, operation, method, FeatureContext.None);

            Assert.Equal(stored ? 1 : 0, GetPatterns(store, methodCall: false).Count);
            int expectedCopies = stored ? 1 : 0;
            if (targetKind == AssignmentTarget.Unexpected)
            {
                expectedCopies++;
            }
            Assert.Equal(expectedCopies, counter.Copies);
        }

        private static object CreateStore() => Activator.CreateInstance(s_storeType,
            new object[] { default(ValueSetLattice<SingleValue>), new FeatureContextLattice() })!;

        private static (IOperation Operation, IMethodSymbol Method) CreateOperation()
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText("class C { static void M() { M(); } static void Other() { } }");
            CSharpCompilation compilation = CSharpCompilation.Create("PatternTests", new[] { tree }, LiveReferencePack.GetMetadataReferences(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            SemanticModel model = compilation.GetSemanticModel(tree);
            InvocationExpressionSyntax invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
            return (model.GetOperation(invocation)!, (IMethodSymbol)model.GetSymbolInfo(invocation).Symbol!);
        }

        private static void AddContribution(object store, bool methodCall, int argumentCount, MutableValue value,
            IOperation operation, IMethodSymbol method, FeatureContext context)
        {
            ValueSet<SingleValue> values = new(value);
            if (!methodCall)
            {
                AddAssignment(store, values, new ValueSet<SingleValue>(CreateTarget(method, annotated: true)),
                    operation, method, context);
                return;
            }

            ImmutableArray<ValueSet<SingleValue>> arguments = Enumerable.Repeat(values, argumentCount).ToImmutableArray();
            object[] parameters = { method, values, arguments, operation, method, context };
            s_storeType.GetMethod("AddMethodCall")!.Invoke(store, parameters);
        }

        private static void AddAssignment(object store, ValueSet<SingleValue> source, ValueSet<SingleValue> target,
            IOperation operation, IMethodSymbol method, FeatureContext context)
        {
            object[] parameters = { source, target, operation, method, context };
            s_storeType.GetMethod("AddAssignment")!.Invoke(store, parameters);
        }

        private static IDictionary GetPatterns(object store, bool methodCall) =>
            (IDictionary)s_storeType.GetField(methodCall ? "MethodCallPatterns" : "AssignmentPatterns", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(store)!;

        private static ValueSet<SingleValue> GetValues(object pattern, bool methodCall) =>
            ReadProperty<ValueSet<SingleValue>>(pattern, methodCall ? "Instance" : "Source");

        private static T ReadProperty<T>(object value, string name) => (T)value.GetType().GetProperty(name)!.GetValue(value)!;

        private static SingleValue CreateTarget(IMethodSymbol method, bool annotated)
        {
            Type targetType = s_storeType.Assembly.GetType("ILLink.Shared.TrimAnalysis.MethodReturnValue", throwOnError: true)!;
            ConstructorInfo constructor = targetType.GetConstructors().Single(candidate => candidate.GetParameters().Length == 3);
            object annotation = Enum.Parse(constructor.GetParameters()[2].ParameterType, annotated ? "PublicMethods" : "None");
            return (SingleValue)constructor.Invoke(new[] { (object)method, false, annotation });
        }

        public enum AssignmentTarget
        {
            Empty,
            Unannotated,
            MultipleUnannotated,
            Annotated,
            Mixed,
            Unexpected,
            Unknown
        }

        private sealed class CopyCounter
        {
            public int Copies;
        }

        private sealed record MutableValue : SingleValue, IValueWithMutableState
        {
            public int Id { get; set; }
            public CopyCounter Counter { get; }
            public bool HasMutableState => true;

            public MutableValue(int id, CopyCounter counter) => (Id, Counter) = (id, counter);

            public override SingleValue DeepCopy()
            {
                Counter.Copies++;
                return new MutableValue(Id, Counter);
            }
        }
    }
}
