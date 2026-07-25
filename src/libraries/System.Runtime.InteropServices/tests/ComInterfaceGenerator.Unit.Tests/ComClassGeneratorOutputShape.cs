// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

using VerifyCS = Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<Microsoft.Interop.ComClassGenerator, Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer>;

namespace ComInterfaceGenerator.Unit.Tests
{
    public class ComClassGeneratorOutputShape
    {
        [Fact]
        public async Task SingleComClass()
        {
            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [GeneratedComInterface]
                partial interface INativeAPI
                {
                }

                [GeneratedComClass]
                partial class C : INativeAPI {}
                """;

            await VerifySourceGeneratorAsync(source, "C");
        }

        [Fact]
        public async Task MultipleComClasses()
        {
            string source = $$"""
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
                
                [GeneratedComInterface]
                partial interface I
                {
                }
                [GeneratedComInterface]
                partial interface J
                {
                }
                
                [GeneratedComClass]
                partial class C : I, J
                {
                }

                [GeneratedComClass]
                partial class D : I, J
                {
                }

                [GeneratedComClass]
                partial class E : C
                {
                }
                """;

            await VerifySourceGeneratorAsync(source, "C", "D", "E");
        }

        [Fact]
        public async Task GenericComClass()
        {
            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [GeneratedComInterface]
                partial interface INativeAPI
                {
                }

                [GeneratedComClass]
                partial class GenericClass<T> : INativeAPI where T : class, new()
                {
                }
                """;

            await VerifySourceGeneratorAsync(source, "GenericClass`1");
        }

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("interface")]
        [InlineData("record")]
        [InlineData("record class")]
        [InlineData("record struct")]
        public async Task NestedComClass(string containingTypeKeyword)
        {
            string source = $$"""
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [GeneratedComInterface]
                partial interface INativeAPI
                {
                }

                partial {{containingTypeKeyword}} ContainingType
                {
                    [GeneratedComClass]
                    partial class C : INativeAPI {}
                }
                """;

            await VerifySourceGeneratorAsync(source, "ContainingType+C");
        }

        private static async Task VerifySourceGeneratorAsync(string source, params string[] typeNames)
        {
            GeneratedShapeTest test = new(typeNames)
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            };

            await test.RunAsync();
        }
        class GeneratedShapeTest : VerifyCS.Test
        {
            private readonly string[] _typeNames;

            public GeneratedShapeTest(params string[] typeNames)
                : base(referenceAncillaryInterop: false)
            {
                _typeNames = typeNames;
            }

            protected override void VerifyFinalCompilation(Compilation compilation)
            {
                // Generate one source file per attributed interface.
                Assert.Equal(TestState.Sources.Count + _typeNames.Length, compilation.SyntaxTrees.Count());
                Assert.All(_typeNames, name => VerifyShape(compilation, name));
            }

            private static void VerifyShape(Compilation comp, string userDefinedClassMetadataName)
            {
                INamedTypeSymbol? userDefinedClass = comp.Assembly.GetTypeByMetadataName(userDefinedClassMetadataName);
                Assert.NotNull(userDefinedClass);

                INamedTypeSymbol? comExposedClassAttribute = comp.GetTypeByMetadataName("System.Runtime.InteropServices.Marshalling.ComExposedClassAttribute`1");

                Assert.NotNull(comExposedClassAttribute);

                AttributeData iUnknownDerivedAttribute = Assert.Single(
                    userDefinedClass.GetAttributes(),
                    attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass?.OriginalDefinition, comExposedClassAttribute));

                Assert.NotNull(iUnknownDerivedAttribute.AttributeClass);
                ITypeSymbol typeArgument = Assert.Single(iUnknownDerivedAttribute.AttributeClass.TypeArguments);
                Assert.True(Assert.IsType<INamedTypeSymbol>(typeArgument, exactMatch: false).IsFileLocal);
            }
        }
    }
}
