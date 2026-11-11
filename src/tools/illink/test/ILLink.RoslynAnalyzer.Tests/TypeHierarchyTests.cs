// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SourceGenerators.Tests;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    public class TypeHierarchyTests
    {
        [Theory]
        [InlineData("System.Int32", false, false)]
        [InlineData("System.String", false, true)]
        [InlineData("System.String", true, false)]
        [InlineData("System.Type", false, true)]
        [InlineData("System.Reflection.TypeInfo", true, true)]
        [InlineData("System.Reflection.IReflect", false, true)]
        [InlineData("System.Array", false, false)]
        [InlineData("System.Object", false, false)]
        public void WellKnownTypeClassificationIsStable(string name, bool byRef, bool expected)
        {
            CSharpCompilation compilation = CSharpCompilation.Create("HierarchyTests", references: LiveReferencePack.GetMetadataReferences());
            INamedTypeSymbol type = compilation.GetTypeByMetadataName(name)!;
            Assert.Equal(expected, IsInteresting(type, byRef));
            Assert.Equal(expected, IsInteresting(type, byRef));
        }

        [Fact]
        public void ConstructedGenericHierarchyUsesTheSameClassification()
        {
            CSharpCompilation compilation = CSharpCompilation.Create("HierarchyTests",
                new[] { CSharpSyntaxTree.ParseText("public class C<T> : System.Reflection.TypeDelegator { public C() : base(typeof(T)) {} }") },
                LiveReferencePack.GetMetadataReferences(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            INamedTypeSymbol definition = compilation.GetTypeByMetadataName("C`1")!;
            Assert.True(IsInteresting(definition, byRef: false));
            Assert.True(IsInteresting(definition.Construct(compilation.GetSpecialType(SpecialType.System_Int32)), byRef: false));
            Assert.True(IsInteresting(definition.Construct(compilation.GetSpecialType(SpecialType.System_String)), byRef: true));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void HierarchyCachesBelongToTheirCompilation(bool interesting)
        {
            CSharpCompilation compilation = CSharpCompilation.Create("CacheTests",
                new[] { CSharpSyntaxTree.ParseText(interesting
                    ? "class C : System.Reflection.TypeDelegator { public C() : base(typeof(int)) {} }"
                    : "class C { }") }, LiveReferencePack.GetMetadataReferences());
            INamedTypeSymbol symbol = compilation.GetTypeByMetadataName("C")!;
            object first = ForwardDataFlowTests.CreateCompilationState();
            object second = ForwardDataFlowTests.CreateCompilationState();
            var classify = (Func<ITypeSymbol, bool, bool>)first.GetType().GetMethod("IsTypeInterestingForDataflow")!
                .CreateDelegate(typeof(Func<ITypeSymbol, bool, bool>), first);
            Assert.Equal(interesting, classify(symbol, false));
            Assert.Equal(interesting, classify(symbol, true));

            var field = first.GetType().GetField("_interestingTypes", BindingFlags.Instance | BindingFlags.NonPublic)!;
            Assert.Single(((IEnumerable)field.GetValue(first)!).Cast<object>());
            Assert.Empty((IEnumerable)field.GetValue(second)!);
            GC.KeepAlive(symbol);
        }

        private static bool IsInteresting(ITypeSymbol type, bool byRef)
        {
            object state = ForwardDataFlowTests.CreateCompilationState();
            var classify = (Func<ITypeSymbol, bool, bool>)state.GetType().GetMethod("IsTypeInterestingForDataflow")!
                .CreateDelegate(typeof(Func<ITypeSymbol, bool, bool>), state);
            return classify(type, byRef);
        }
    }
}
