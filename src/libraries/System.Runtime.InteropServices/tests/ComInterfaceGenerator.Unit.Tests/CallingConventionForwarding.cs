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
    public class CallingConventionForwarding
    {
        [Fact]
        public async Task NoSpecifiedCallConvForwardsDefault()
        {
            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                readonly record struct NoCasting {}
                partial interface INativeAPI
                {
                    public static readonly NoCasting TypeKey = default;
                    [VirtualMethodIndex(0)]
                    void Method();
                }
                """;
            Compilation comp = await TestUtils.CreateCompilation(source);
            // Allow the Native nested type name to be missing in the pre-source-generator compilation
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.VtableIndexStubGenerator());

            var signature = await FindFunctionPointerInvocationSignature(newComp, "INativeAPI", "Method");

            Assert.Equal(SignatureCallingConvention.Unmanaged, signature.CallingConvention);
            Assert.Empty(signature.UnmanagedCallingConventionTypes);
        }

        [Fact]
        public async Task SuppressGCTransitionAttributeForwarded()
        {
            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                readonly record struct NoCasting {}
                partial interface INativeAPI
                {
                    public static readonly NoCasting TypeKey = default;
                    [SuppressGCTransitionAttribute]
                    [VirtualMethodIndex(0)]
                    void Method();
                }
                """;
            Compilation comp = await TestUtils.CreateCompilation(source);
            // Allow the Native nested type name to be missing in the pre-source-generator compilation
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.VtableIndexStubGenerator());

            var signature = await FindFunctionPointerInvocationSignature(newComp, "INativeAPI", "Method");

            Assert.Equal(SignatureCallingConvention.Unmanaged, signature.CallingConvention);
            Assert.Equal(newComp.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvSuppressGCTransition"), Assert.Single(signature.UnmanagedCallingConventionTypes), SymbolEqualityComparer.Default);
        }

        [Fact]
        public async Task EmptyUnmanagedCallConvAttributeForwarded()
        {
            string source = """
                using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;

                readonly record struct NoCasting {}
                partial interface INativeAPI
                {
                    public static readonly NoCasting TypeKey = default;
                    [UnmanagedCallConv]
                    [VirtualMethodIndex(0)]
                    void Method();
                }
            """;
            Compilation comp = await TestUtils.CreateCompilation(source);
            // Allow the Native nested type name to be missing in the pre-source-generator compilation
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.VtableIndexStubGenerator());

            var signature = await FindFunctionPointerInvocationSignature(newComp, "INativeAPI", "Method");

            Assert.Equal(SignatureCallingConvention.Unmanaged, signature.CallingConvention);
            Assert.Empty(signature.UnmanagedCallingConventionTypes);
        }

        [Fact]
        public async Task SimpleUnmanagedCallConvAttributeForwarded()
        {
            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                readonly record struct NoCasting {}
                partial interface INativeAPI
                {
                    public static readonly NoCasting TypeKey = default;
                    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
                    [VirtualMethodIndex(0)]
                    void Method();
                }
                """;
            Compilation comp = await TestUtils.CreateCompilation(source);
            // Allow the Native nested type name to be missing in the pre-source-generator compilation
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.VtableIndexStubGenerator());

            var signature = await FindFunctionPointerInvocationSignature(newComp, "INativeAPI", "Method");

            Assert.Equal(SignatureCallingConvention.CDecl, signature.CallingConvention);
            Assert.Empty(signature.UnmanagedCallingConventionTypes);
        }

        [Fact]
        public async Task ComplexUnmanagedCallConvAttributeForwarded()
        {
            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                readonly record struct NoCasting {}
                partial interface INativeAPI
                {
                    public static readonly NoCasting TypeKey = default;
                    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl), typeof(CallConvMemberFunction) })]
                    [VirtualMethodIndex(0)]
                    void Method();
                }
                """;
            Compilation comp = await TestUtils.CreateCompilation(source);
            // Allow the Native nested type name to be missing in the pre-source-generator compilation
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.VtableIndexStubGenerator());

            var signature = await FindFunctionPointerInvocationSignature(newComp, "INativeAPI", "Method");

            Assert.Equal(SignatureCallingConvention.Unmanaged, signature.CallingConvention);
            Assert.Equal(new[]
            {
                newComp.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvCdecl"),
                newComp.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvMemberFunction"),
            },
            signature.UnmanagedCallingConventionTypes,
            SymbolEqualityComparer.Default);
        }

        [Fact]
        public async Task ComplexUnmanagedCallConvAttributeWithSuppressGCTransitionForwarded()
        {
            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                readonly record struct NoCasting {}
                partial interface INativeAPI
                {
                    public static readonly NoCasting TypeKey = default;
                    [SuppressGCTransition]
                    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl), typeof(CallConvMemberFunction) })]
                    [VirtualMethodIndex(0)]
                    void Method();
                }
                """;
            Compilation comp = await TestUtils.CreateCompilation(source);
            // Allow the Native nested type name to be missing in the pre-source-generator compilation
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.VtableIndexStubGenerator());

            var signature = await FindFunctionPointerInvocationSignature(newComp, "INativeAPI", "Method");

            Assert.Equal(SignatureCallingConvention.Unmanaged, signature.CallingConvention);
            Assert.Equal(new[]
            {
                newComp.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvSuppressGCTransition"),
                newComp.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvCdecl"),
                newComp.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvMemberFunction"),
            },
            signature.UnmanagedCallingConventionTypes,
            SymbolEqualityComparer.Default);
        }

        private static async Task<IMethodSymbol> FindFunctionPointerInvocationSignature(Compilation compilation, string userDefinedInterfaceName, string methodName)
        {
            INamedTypeSymbol? userDefinedInterface = compilation.Assembly.GetTypeByMetadataName(userDefinedInterfaceName);
            Assert.NotNull(userDefinedInterface);

            INamedTypeSymbol generatedInterfaceImplementation = Assert.Single(userDefinedInterface.GetTypeMembers("Native"));

            IMethodSymbol methodImplementation = Assert.Single(generatedInterfaceImplementation.GetMembers($"{userDefinedInterfaceName}.{methodName}").OfType<IMethodSymbol>());

            SyntaxNode emittedImplementationSyntax = await methodImplementation.DeclaringSyntaxReferences[0].GetSyntaxAsync();

            SemanticModel model = compilation.GetSemanticModel(emittedImplementationSyntax.SyntaxTree);

            IOperation body = model.GetOperation(emittedImplementationSyntax)!;

            return Assert.Single(body.Descendants().OfType<IFunctionPointerInvocationOperation>()).GetFunctionPointerSignature();
        }
    }
}
