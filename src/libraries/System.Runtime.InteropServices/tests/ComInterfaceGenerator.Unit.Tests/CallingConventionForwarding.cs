// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

using VerifyCS = Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<Microsoft.Interop.VtableIndexStubGenerator>;

namespace ComInterfaceGenerator.Unit.Tests
{
    public class CallingConventionForwarding
    {
        [Fact]
        public async Task NoSpecifiedCallConvForwardsDefault()
        {
            string source = $$"""
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [UnmanagedObjectUnwrapper<UnmanagedObjectUnwrapper.TestUnwrapper>]
                partial interface INativeAPI : IUnmanagedInterfaceType
                {
                    static unsafe void* IUnmanagedInterfaceType.VirtualMethodTableManagedImplementation => null;
                    [VirtualMethodIndex(0)]
                    void Method();
                }
                """;

            await VerifySourceGeneratorAsync(source, "INativeAPI", "Method", (compilation, signature) =>
            {
                Assert.Equal(SignatureCallingConvention.Unmanaged, signature.CallingConvention);
                Assert.Empty(signature.UnmanagedCallingConventionTypes);
            });
        }

        [Fact]
        public async Task SuppressGCTransitionAttributeForwarded()
        {
            string source = $$"""
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [UnmanagedObjectUnwrapper<UnmanagedObjectUnwrapper.TestUnwrapper>]
                partial interface INativeAPI : IUnmanagedInterfaceType
                {
                    static unsafe void* IUnmanagedInterfaceType.VirtualMethodTableManagedImplementation => null;
                    [SuppressGCTransitionAttribute]
                    [VirtualMethodIndex(0)]
                    void Method();
                }
                """;

            await VerifySourceGeneratorAsync(source, "INativeAPI", "Method", (newComp, signature) =>
            {
                Assert.Equal(SignatureCallingConvention.Unmanaged, signature.CallingConvention);
                Assert.Equal(newComp.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvSuppressGCTransition"), Assert.Single(signature.UnmanagedCallingConventionTypes), SymbolEqualityComparer.Default);
            });
        }

        [Fact]
        public async Task EmptyUnmanagedCallConvAttributeForwarded()
        {
            string source = $$"""
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [UnmanagedObjectUnwrapper<UnmanagedObjectUnwrapper.TestUnwrapper>]
                partial interface INativeAPI : IUnmanagedInterfaceType
                {
                    static unsafe void* IUnmanagedInterfaceType.VirtualMethodTableManagedImplementation => null;
                    [UnmanagedCallConv]
                    [VirtualMethodIndex(0)]
                    void Method();
                }
            """;

            await VerifySourceGeneratorAsync(source, "INativeAPI", "Method", (_, signature) =>
            {
                Assert.Equal(SignatureCallingConvention.Unmanaged, signature.CallingConvention);
                Assert.Empty(signature.UnmanagedCallingConventionTypes);
            });
        }

        [Fact]
        public async Task SimpleUnmanagedCallConvAttributeForwarded()
        {
            string source = $$"""
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [UnmanagedObjectUnwrapper<UnmanagedObjectUnwrapper.TestUnwrapper>]
                partial interface INativeAPI : IUnmanagedInterfaceType
                {
                    static unsafe void* IUnmanagedInterfaceType.VirtualMethodTableManagedImplementation => null;
                    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
                    [VirtualMethodIndex(0)]
                    void Method();
                }
                """;

            await VerifySourceGeneratorAsync(source, "INativeAPI", "Method", (_, signature) =>
            {
                Assert.Equal(SignatureCallingConvention.CDecl, signature.CallingConvention);
                Assert.Empty(signature.UnmanagedCallingConventionTypes);
            });
        }

        [Fact]
        public async Task ComplexUnmanagedCallConvAttributeForwarded()
        {
            string source = $$"""
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [UnmanagedObjectUnwrapper<UnmanagedObjectUnwrapper.TestUnwrapper>]
                partial interface INativeAPI : IUnmanagedInterfaceType
                {
                    static unsafe void* IUnmanagedInterfaceType.VirtualMethodTableManagedImplementation => null;
                    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl), typeof(CallConvMemberFunction) })]
                    [VirtualMethodIndex(0)]
                    void Method();
                }
                """;

            await VerifySourceGeneratorAsync(source, "INativeAPI", "Method", (newComp, signature) =>
            {
                Assert.Equal(SignatureCallingConvention.Unmanaged, signature.CallingConvention);
                Assert.Equal(new[]
                {
                    newComp.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvCdecl"),
                    newComp.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvMemberFunction"),
                },
                signature.UnmanagedCallingConventionTypes,
                SymbolEqualityComparer.Default);
            });
        }

        [Fact]
        public async Task ComplexUnmanagedCallConvAttributeWithSuppressGCTransitionForwarded()
        {
            string source = $$"""
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [UnmanagedObjectUnwrapper<UnmanagedObjectUnwrapper.TestUnwrapper>]
                partial interface INativeAPI : IUnmanagedInterfaceType
                {
                    static unsafe void* IUnmanagedInterfaceType.VirtualMethodTableManagedImplementation => null;
                    [SuppressGCTransition]
                    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl), typeof(CallConvMemberFunction) })]
                    [VirtualMethodIndex(0)]
                    void Method();
                }
                """;

            await VerifySourceGeneratorAsync(source, "INativeAPI", "Method", (newComp, signature) =>
            {
                Assert.Equal(SignatureCallingConvention.Unmanaged, signature.CallingConvention);
                Assert.Equal(new[]
                {
                    newComp.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvSuppressGCTransition"),
                    newComp.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvCdecl"),
                    newComp.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvMemberFunction"),
                },
                signature.UnmanagedCallingConventionTypes,
                SymbolEqualityComparer.Default);
            });
        }

        private static async Task VerifySourceGeneratorAsync(string source, string interfaceName, string methodName, Action<Compilation, IMethodSymbol> signatureValidator)
        {
            CallingConventionForwardingTest test = new(interfaceName, methodName, signatureValidator)
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            };

            await test.RunAsync();
        }

        class CallingConventionForwardingTest : VerifyCS.Test
        {
            private readonly Action<Compilation, IMethodSymbol> _signatureValidator;
            private readonly string _interfaceName;
            private readonly string _methodName;

            public CallingConventionForwardingTest(string interfaceName, string methodName, Action<Compilation, IMethodSymbol> signatureValidator)
                : base(referenceAncillaryInterop: true)
            {
                _signatureValidator = signatureValidator;
                _interfaceName = interfaceName;
                _methodName = methodName;
            }

            protected override void VerifyFinalCompilation(Compilation compilation)
            {
                _signatureValidator(compilation, FindFunctionPointerInvocationSignature(compilation));
            }
            private IMethodSymbol FindFunctionPointerInvocationSignature(Compilation compilation)
            {
                INamedTypeSymbol? userDefinedInterface = compilation.Assembly.GetTypeByMetadataName(_interfaceName);
                Assert.NotNull(userDefinedInterface);

                INamedTypeSymbol generatedInterfaceImplementation = Assert.Single(userDefinedInterface.GetTypeMembers("Native"));

                IMethodSymbol methodImplementation = Assert.Single(generatedInterfaceImplementation.GetMembers($"global::{_interfaceName}.{_methodName}").OfType<IMethodSymbol>());

                SyntaxNode emittedImplementationSyntax = methodImplementation.DeclaringSyntaxReferences[0].GetSyntax();

                SemanticModel model = compilation.GetSemanticModel(emittedImplementationSyntax.SyntaxTree);

                IOperation body = model.GetOperation(emittedImplementationSyntax)!;

                return Assert.Single(body.Descendants().OfType<IFunctionPointerInvocationOperation>()).GetFunctionPointerSignature();
            }
        }
    }
}
