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
            string source = $$"""
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
            // We'll create one syntax tree per user-defined interface.
            Assert.Equal(comp.SyntaxTrees.Count() + 2, newComp.SyntaxTrees.Count());

            VerifyShape(newComp, "I");
            VerifyShape(newComp, "J");
        }

        private static void VerifyShape(Compilation newComp, string userDefinedInterfaceMetadataName)
        {
            INamedTypeSymbol? userDefinedInterface = newComp.Assembly.GetTypeByMetadataName(userDefinedInterfaceMetadataName);
            Assert.NotNull(userDefinedInterface);

            AttributeData? iUnknownDerivedAttribute = userDefinedInterface.GetAttributes().FirstOrDefault(attr => attr.AttributeClass?.MetadataName == "System.Runtime.InteropServices.Marshalling.IUnknownDerivedAttribute`2");

            Assert.NotNull(iUnknownDerivedAttribute);

            Assert.Collection(Assert.IsAssignableFrom<INamedTypeSymbol>(iUnknownDerivedAttribute.AttributeClass).TypeArguments,
                infoType =>
                {
                    var source = (TypeDeclarationSyntax)infoType.DeclaringSyntaxReferences[0].GetSyntax();
                    Assert.Contains(SyntaxFactory.Token(SyntaxKind.FileKeyword), source.Modifiers, TokenKindEqualityComparer.Instance);
                },
                implementationType =>
                {
                    var source = (TypeDeclarationSyntax)implementationType.DeclaringSyntaxReferences[0].GetSyntax();
                    Assert.Contains(SyntaxFactory.Token(SyntaxKind.FileKeyword), source.Modifiers, TokenKindEqualityComparer.Instance);
                    Assert.Contains(userDefinedInterface, implementationType.Interfaces, SymbolEqualityComparer.Default);
                    Assert.Contains(implementationType.GetAttributes(), attr => attr.AttributeClass?.MetadataName == typeof(DynamicInterfaceCastableImplementationAttribute).FullName);
                    Assert.All(userDefinedInterface.GetMembers().OfType<IMethodSymbol>().Where(method => method.IsAbstract && !method.IsStatic),
                        method =>
                        {
                            Assert.NotNull(implementationType.FindImplementationForInterfaceMember(method));
                        });
                });
        }

        private sealed class TokenKindEqualityComparer : EqualityComparer<SyntaxToken>
        {
            public static readonly EqualityComparer<SyntaxToken> Instance = new TokenKindEqualityComparer();

            private TokenKindEqualityComparer() { }
            public override bool Equals(SyntaxToken x, SyntaxToken y) => x.IsKind(y.Kind());
            public override int GetHashCode([DisallowNull] SyntaxToken obj) => obj.Kind().GetHashCode();
        }
    }
}
