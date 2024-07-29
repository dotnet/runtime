// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

using VerifyCS = Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<Microsoft.Interop.ComClassGenerator>;

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
                :base(referenceAncillaryInterop: false)
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

                Assert.Collection(Assert.IsAssignableFrom<INamedTypeSymbol>(iUnknownDerivedAttribute.AttributeClass).TypeArguments,
                    infoType =>
                    {
                        Assert.True(Assert.IsAssignableFrom<INamedTypeSymbol>(infoType).IsFileLocal);
                    });
            }
        }
    }
}
