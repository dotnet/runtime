// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Interop.UnitTests;
using Xunit;

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
                partial interface INativeAPI
                {
                    void Method();
                    void Method2();
                }
                """;
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.ComInterfaceGenerator());
            TestUtils.AssertPostSourceGeneratorCompilation(newComp);
            // We'll create one syntax tree for the new interface.
            Assert.Equal(comp.SyntaxTrees.Count() + 1, newComp.SyntaxTrees.Count());

            VerifyShape(newComp, "INativeAPI");
        }

        [Fact]
        public async Task MultipleComInterfaces()
        {
            string source = $$"""
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
                
                [GeneratedComInterface]
                partial interface I
                {
                    void Method();
                    void Method2();
                }
                [GeneratedComInterface]
                partial interface J
                {
                    void Method();
                    void Method2();
                }
                """;
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.ComInterfaceGenerator());
            TestUtils.AssertPostSourceGeneratorCompilation(newComp);
            // We'll create one syntax tree per user-defined interface.
            Assert.Equal(comp.SyntaxTrees.Count() + 2, newComp.SyntaxTrees.Count());

            VerifyShape(newComp, "I");
            VerifyShape(newComp, "J");
        }

        [Fact]
        public async Task EmptyComInterface()
        {
            string source = $$"""
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
                
                [GeneratedComInterface]
                partial interface I
                {
                    void Method();
                    void Method2();
                }
                [GeneratedComInterface]
                partial interface Empty
                {
                }
                [GeneratedComInterface]
                partial interface J
                {
                    void Method();
                    void Method2();
                }
                """;
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.ComInterfaceGenerator());
            TestUtils.AssertPostSourceGeneratorCompilation(newComp);
            // We'll create one syntax tree per user-defined interface.
            Assert.Equal(comp.SyntaxTrees.Count() + 3, newComp.SyntaxTrees.Count());

            VerifyShape(newComp, "I");
            VerifyShape(newComp, "Empty");
            VerifyShape(newComp, "J");
        }

        [Fact]
        public async Task InheritingComInterfaces()
        {
            string source = $$"""
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
                
                [GeneratedComInterface]
                partial interface I
                {
                    void Method();
                    void Method2();
                }
                [GeneratedComInterface]
                partial interface J : I
                {
                    void MethodA();
                    void MethodB();
                }
                """;
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.ComInterfaceGenerator());
            TestUtils.AssertPostSourceGeneratorCompilation(newComp);
            // We'll create one syntax tree per user-defined interface.
            Assert.Equal(comp.SyntaxTrees.Count() + 2, newComp.SyntaxTrees.Count());

            VerifyShape(newComp, "I");
            VerifyShape(newComp, "J");
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
