// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Interop.UnitTests;
using Xunit;

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
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.ComClassGenerator());
            TestUtils.AssertPostSourceGeneratorCompilation(newComp);
            // We'll create one syntax tree for the new interface.
            Assert.Equal(comp.SyntaxTrees.Count() + 1, newComp.SyntaxTrees.Count());

            VerifyShape(newComp, "C");
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
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.ComClassGenerator());
            TestUtils.AssertPostSourceGeneratorCompilation(newComp);
            // We'll create one syntax tree per user-defined interface.
            Assert.Equal(comp.SyntaxTrees.Count() + 3, newComp.SyntaxTrees.Count());

            VerifyShape(newComp, "C");
            VerifyShape(newComp, "D");
            VerifyShape(newComp, "E");
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
