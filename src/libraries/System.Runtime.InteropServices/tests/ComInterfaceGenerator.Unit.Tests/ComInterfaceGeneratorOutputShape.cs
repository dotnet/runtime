// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.Interop;
using Microsoft.Interop.UnitTests;
using SourceGenerators.Tests;
using Xunit;

using VerifyCS = Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<Microsoft.Interop.ComInterfaceGenerator>;

namespace ComInterfaceGenerator.Unit.Tests
{
    public class ComInterfaceGeneratorOutputShape
    {
        [Fact]
        public async Task SingleComInterface()
        {
            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [GeneratedComInterface]
                [Guid("9D3FD745-3C90-4C10-B140-FAFB01E3541D")]
                partial interface INativeAPI
                {
                    void Method();
                    void Method2();
                }
                """;

            await VerifyGeneratedTypeShapes(source, "INativeAPI");
        }

        [Fact]
        public async Task MultipleComInterfaces()
        {
            string source = $$"""
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
                
                [GeneratedComInterface]
                [Guid("9D3FD745-3C90-4C10-B140-FAFB01E3541D")]
                partial interface I
                {
                    void Method();
                    void Method2();
                }
                [GeneratedComInterface]
                [Guid("734AFCEC-8862-43CB-AB29-5A7954929E23")]
                partial interface J
                {
                    void Method();
                    void Method2();
                }
                """;

            await VerifyGeneratedTypeShapes(source, "I", "J");
        }

        [Fact]
        public async Task EmptyComInterface()
        {
            string source = $$"""
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
                
                [GeneratedComInterface]
                [Guid("9D3FD745-3C90-4C10-B140-FAFB01E3541D")]
                partial interface I
                {
                    void Method();
                    void Method2();
                }
                [GeneratedComInterface]
                [Guid("734AFCEC-8862-43CB-AB29-5A7954929E23")]
                partial interface Empty
                {
                }
                [GeneratedComInterface]
                [Guid("734AFCEC-8862-43CB-AB29-5A7954929E23")]
                partial interface J
                {
                    void Method();
                    void Method2();
                }
                """;

            await VerifyGeneratedTypeShapes(source, "I", "Empty", "J");
        }

        [Fact]
        public async Task InheritingComInterfaces()
        {
            string source = $$"""
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
                
                [GeneratedComInterface]
                [Guid("9D3FD745-3C90-4C10-B140-FAFB01E3541D")]
                partial interface I
                {
                    void Method();
                    void Method2();
                }
                [GeneratedComInterface]
                [Guid("734AFCEC-8862-43CB-AB29-5A7954929E23")]
                partial interface J : I
                {
                    void MethodA();
                    void MethodB();
                }
                """;

            await VerifyGeneratedTypeShapes(source, "I", "J");
        }

        [Fact]
        public async Task InheritingComInterfacesGenerateShadowingMethodsWithDefaultImplementations()
        {
            string source = $$"""
               using System.Runtime.InteropServices;
               using System.Runtime.InteropServices.Marshalling;
       
               [GeneratedComInterface]
               [Guid("9D3FD745-3C90-4C10-B140-FAFB01E3541D")]
               partial interface I
               {
                   void Method();
                   void Method2();
               }
               [GeneratedComInterface]
               [Guid("734AFCEC-8862-43CB-AB29-5A7954929E23")]
               partial interface J : I
               {
                   void MethodA();
                   void MethodB();
               }
               """;

            var test = new VerifyCompilationTest<Microsoft.Interop.ComInterfaceGenerator>(false)
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck | TestBehaviors.SkipGeneratedCodeCheck,
                CompilationVerifier = VerifyCompilation
            };
            await test.RunAsync();

            static void VerifyCompilation(Compilation comp)
            {
                var j = comp.GetTypeByMetadataName("J");
                Assert.NotNull(j);

                var shadowingMethod = Assert.Single(j.GetMembers("Method"));
                VerifyShadowingMethodShape(shadowingMethod);

                shadowingMethod = Assert.Single(j.GetMembers("Method2"));
                VerifyShadowingMethodShape(shadowingMethod);
            }
        }

        [Fact]
        public async Task InheritingComInterfacesGenerateShadowingMethodsWithDefaultImplementations_LongInheritanceChain()
        {
            string source = $$"""
               using System.Runtime.InteropServices;
               using System.Runtime.InteropServices.Marshalling;
       
               [GeneratedComInterface]
               [Guid("9D3FD745-3C90-4C10-B140-FAFB01E3541D")]
               partial interface I
               {
                   void Method();
               }
               [GeneratedComInterface]
               [Guid("734AFCEC-8862-43CB-AB29-5A7954929E23")]
               partial interface J : I
               {
                   void Method2();
               }
               [GeneratedComInterface]
               [Guid("0DB41042-0255-4CDD-B73A-9C5D5F31303D")]
               partial interface K : J
               {
                   void MethodA();
                   void MethodB();
               }
               """;

            var test = new VerifyCompilationTest<Microsoft.Interop.ComInterfaceGenerator>(false)
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck | TestBehaviors.SkipGeneratedCodeCheck,
                CompilationVerifier = VerifyCompilation
            };
            await test.RunAsync();

            static void VerifyCompilation(Compilation comp)
            {
                var k = comp.GetTypeByMetadataName("K");
                Assert.NotNull(k);

                var shadowingMethod = Assert.Single(k.GetMembers("Method"));
                VerifyShadowingMethodShape(shadowingMethod);

                shadowingMethod = Assert.Single(k.GetMembers("Method2"));
                Assert.False(shadowingMethod.IsAbstract);
                Assert.True(shadowingMethod.IsVirtual);
                VerifyShadowingMethodShape(shadowingMethod);
            }
        }

        private static void VerifyShadowingMethodShape(ISymbol method)
        {
            Assert.False(method.IsAbstract);
            Assert.True(method.IsVirtual);

            var syntax = Assert.IsType<MethodDeclarationSyntax>(Assert.Single(method.DeclaringSyntaxReferences).GetSyntax());
            Assert.Contains(syntax.Modifiers, token => token.IsKind(SyntaxKind.NewKeyword));
        }

        [Fact]
        public async Task ValidateAttributesAreCopiedToShadowingMethods()
        {
            var source = $$"""
                using System;
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                namespace Test
                {
                    [GeneratedComInterface]
                    [Guid("EA4319EA-AE9A-4261-B42D-BB027AD81F5F")]
                    partial interface IFoo
                    {
                        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("message")]
                        void Bar();
                    }

                    [GeneratedComInterface]
                    [Guid("8A501001-02CA-490A-AA23-0ECC646F07A3")]
                    partial interface IDerivedIface : IFoo
                    {
                    }
                }
            """;

            var test = new VerifyCompilationTest<Microsoft.Interop.ComInterfaceGenerator>(false)
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck | TestBehaviors.SkipGeneratedCodeCheck,
                CompilationVerifier = VerifyCompilation
            };
            await test.RunAsync();

            static void VerifyCompilation(Compilation comp)
            {
                Assert.True(comp.GetTypeByMetadataName("Test.IDerivedIface")
                    ?.GetMembers()
                    .Where(m => m.Kind == SymbolKind.Method && m.Name == "Bar")
                    .SingleOrDefault()
                    ?.GetAttributes()
                    .Any(att => att.AttributeClass?.Name == nameof(RequiresUnreferencedCodeAttribute)));
            }
        }

        private static async Task VerifyGeneratedTypeShapes(string source, params string[] typeNames)
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

            private static void VerifyShape(Compilation comp, string userDefinedInterfaceMetadataName)
            {
                INamedTypeSymbol? userDefinedInterface = comp.Assembly.GetTypeByMetadataName(userDefinedInterfaceMetadataName);
                Assert.NotNull(userDefinedInterface);

                INamedTypeSymbol? iUnknownDerivedAttributeType = comp.GetTypeByMetadataName("System.Runtime.InteropServices.Marshalling.IUnknownDerivedAttribute`2");

                Assert.NotNull(iUnknownDerivedAttributeType);

                AttributeData iUnknownDerivedAttribute = Assert.Single(
                    userDefinedInterface.GetAttributes(),
                    attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass?.OriginalDefinition, iUnknownDerivedAttributeType));

                Assert.Collection(Assert.IsAssignableFrom<INamedTypeSymbol>(iUnknownDerivedAttribute.AttributeClass).TypeArguments,
                    infoType =>
                    {
                        Assert.True(Assert.IsAssignableFrom<INamedTypeSymbol>(infoType).IsFileLocal);
                    },
                    implementationType =>
                    {
                        Assert.True(Assert.IsAssignableFrom<INamedTypeSymbol>(implementationType).IsFileLocal);
                        Assert.Contains(userDefinedInterface, implementationType.Interfaces, SymbolEqualityComparer.Default);
                        Assert.Contains(implementationType.GetAttributes(), attr => attr.AttributeClass?.ToDisplayString() == typeof(DynamicInterfaceCastableImplementationAttribute).FullName);
                        Assert.All(userDefinedInterface.GetMembers().OfType<IMethodSymbol>().Where(method => method.IsAbstract && !method.IsStatic),
                            method =>
                            {
                                Assert.NotNull(implementationType.FindImplementationForInterfaceMember(method));
                            });
                    });
            }
        }
    }
}
