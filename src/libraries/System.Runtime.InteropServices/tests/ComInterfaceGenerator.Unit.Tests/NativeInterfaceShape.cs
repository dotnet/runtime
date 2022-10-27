// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Interop.UnitTests;
using Xunit;

namespace ComInterfaceGenerator.Unit.Tests
{
    public class NativeInterfaceShape
    {
        [Fact]
        public async Task NativeInterfaceNestedInUserInterface()
        {
            string source = @"
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

readonly record struct NoCasting {}
partial interface INativeAPI
{
    public static readonly NoCasting TypeKey = default;
    [VirtualMethodIndex(0)]
    void Method();
}";
            Compilation comp = await TestUtils.CreateCompilation(source);
            // Allow the Native nested type name to be missing in the pre-source-generator compilation
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.VtableIndexStubGenerator());

            INamedTypeSymbol? userDefinedInterface = newComp.Assembly.GetTypeByMetadataName("INativeAPI");
            Assert.NotNull(userDefinedInterface);

            Assert.Single(userDefinedInterface.GetTypeMembers("Native"));
        }

        [Fact]
        public async Task NativeInterfaceInheritsFromUserInterface()
        {
            string source = @"
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

readonly record struct NoCasting {}
partial interface INativeAPI
{
    public static readonly NoCasting TypeKey = default;
    [VirtualMethodIndex(0)]
    void Method();
}";
            Compilation comp = await TestUtils.CreateCompilation(source);
            // Allow the Native nested type name to be missing in the pre-source-generator compilation
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.VtableIndexStubGenerator());

            INamedTypeSymbol? userDefinedInterface = newComp.Assembly.GetTypeByMetadataName("INativeAPI");
            Assert.NotNull(userDefinedInterface);

            Assert.Equal(userDefinedInterface, Assert.Single(Assert.Single(userDefinedInterface.GetTypeMembers("Native")).Interfaces), SymbolEqualityComparer.Default);
        }

        [Fact]
        public async Task NativeInterfaceHasDynamicInterfaceCastableImplementationAttribute()
        {
            string source = @"
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

readonly record struct NoCasting {}
partial interface INativeAPI
{
    public static readonly NoCasting TypeKey = default;
    [VirtualMethodIndex(0)]
    void Method();
}";
            Compilation comp = await TestUtils.CreateCompilation(source);
            // Allow the Native nested type name to be missing in the pre-source-generator compilation
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.VtableIndexStubGenerator());

            INamedTypeSymbol? userDefinedInterface = newComp.Assembly.GetTypeByMetadataName("INativeAPI");
            Assert.NotNull(userDefinedInterface);

            INamedTypeSymbol dynamicInterfaceCastableImplementationAttribute = newComp.GetTypeByMetadataName("System.Runtime.InteropServices.DynamicInterfaceCastableImplementationAttribute")!;

            Assert.Contains(
                dynamicInterfaceCastableImplementationAttribute,
                Assert.Single(userDefinedInterface.GetTypeMembers("Native")).GetAttributes().Select(attr => attr.AttributeClass),
                SymbolEqualityComparer.Default);
        }
    }
}
