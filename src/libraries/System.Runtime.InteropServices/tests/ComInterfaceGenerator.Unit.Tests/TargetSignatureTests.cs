// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.Interop;
using Xunit;

using VerifyCS = Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<Microsoft.CodeAnalysis.Testing.EmptySourceGeneratorProvider>;

namespace ComInterfaceGenerator.Unit.Tests
{
    public class TargetSignatureTests
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

            await VerifyVirtualMethodIndexGeneratorAsync(source, "INativeAPI", "Method", (compilation, signature) =>
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

            await VerifyVirtualMethodIndexGeneratorAsync(source, "INativeAPI", "Method", (newComp, signature) =>
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

            await VerifyVirtualMethodIndexGeneratorAsync(source, "INativeAPI", "Method", (_, signature) =>
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

            await VerifyVirtualMethodIndexGeneratorAsync(source, "INativeAPI", "Method", (_, signature) =>
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

            await VerifyVirtualMethodIndexGeneratorAsync(source, "INativeAPI", "Method", (newComp, signature) =>
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

            await VerifyVirtualMethodIndexGeneratorAsync(source, "INativeAPI", "Method", (newComp, signature) =>
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

        [Fact]
        public async Task ComInterfaceMethodHasMemberFunctionCallingConventionByDefault()
        {
            string source = $$"""
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [GeneratedComInterface]
                [Guid("0A617667-4961-4F90-B74F-6DC368E98179")]
                partial interface IComInterface
                {
                    void Method();
                }
                """;

            await VerifyComInterfaceGeneratorAsync(source, "IComInterface", "Method", (newComp, signature) =>
            {
                Assert.Equal(SignatureCallingConvention.Unmanaged, signature.CallingConvention);
                Assert.Equal(newComp.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvMemberFunction"), Assert.Single(signature.UnmanagedCallingConventionTypes), SymbolEqualityComparer.Default);
            });
        }

        [Fact]
        public async Task ComInterfacePreserveSigMethodHasMemberFunctionCallingConventionByDefault()
        {
            string source = $$"""
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [GeneratedComInterface]
                [Guid("0A617667-4961-4F90-B74F-6DC368E98179")]
                partial interface IComInterface
                {
                    [PreserveSig]
                    int Method();
                }
                """;

            await VerifyComInterfaceGeneratorAsync(source, "IComInterface", "Method", (newComp, signature) =>
            {
                Assert.Equal(SignatureCallingConvention.Unmanaged, signature.CallingConvention);
                Assert.Equal(newComp.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvMemberFunction"), Assert.Single(signature.UnmanagedCallingConventionTypes), SymbolEqualityComparer.Default);
            });
        }

        [Fact]
        public async Task ComInterfaceMethodFunctionPointerReturnsInt()
        {
            string source = $$"""
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [GeneratedComInterface]
                [Guid("0A617667-4961-4F90-B74F-6DC368E98179")]
                partial interface IComInterface
                {
                    void Method();
                }
                """;

            await VerifyComInterfaceGeneratorAsync(source, "IComInterface", "Method", (newComp, signature) =>
            {
                Assert.Equal(SpecialType.System_Int32, signature.ReturnType.SpecialType);
            });
        }

        [Fact]
        public async Task ComInterfaceMethodFunctionPointerReturnTypeChangedToOutParameter()
        {
            string source = $$"""
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [GeneratedComInterface]
                [Guid("0A617667-4961-4F90-B74F-6DC368E98179")]
                partial interface IComInterface
                {
                    long Method();
                }
                """;

            await VerifyComInterfaceGeneratorAsync(source, "IComInterface", "Method", (newComp, signature) =>
            {
                Assert.Equal(SpecialType.System_Int32, signature.ReturnType.SpecialType);
                Assert.Equal(2, signature.Parameters.Length);
                Assert.Equal(newComp.CreatePointerTypeSymbol(newComp.GetSpecialType(SpecialType.System_Void)), signature.Parameters[0].Type, SymbolEqualityComparer.Default);
                Assert.Equal(newComp.CreatePointerTypeSymbol(newComp.GetSpecialType(SpecialType.System_Int64)), signature.Parameters[^1].Type, SymbolEqualityComparer.Default);
            });
        }

        [Fact]
        public async Task ComInterfaceMethodPreserveSigFunctionPointerReturnTypePreserved()
        {
            string source = $$"""
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [GeneratedComInterface]
                [Guid("0A617667-4961-4F90-B74F-6DC368E98179")]
                partial interface IComInterface
                {
                    [PreserveSig]
                    long Method();
                }
                """;

            await VerifyComInterfaceGeneratorAsync(source, "IComInterface", "Method", (newComp, signature) =>
            {
                Assert.Equal(SpecialType.System_Int64, signature.ReturnType.SpecialType);
                Assert.Equal(newComp.CreatePointerTypeSymbol(newComp.GetSpecialType(SpecialType.System_Void)), Assert.Single(signature.Parameters).Type, SymbolEqualityComparer.Default);
            });
        }

        private static async Task VerifyVirtualMethodIndexGeneratorAsync(string source, string interfaceName, string methodName, Action<Compilation, IMethodSymbol> signatureValidator)
        {
            VirtualMethodIndexTargetSignatureTest test = new(interfaceName, methodName, signatureValidator)
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            };

            await test.RunAsync();
        }
        private static async Task VerifyComInterfaceGeneratorAsync(string source, string interfaceName, string methodName, Action<Compilation, IMethodSymbol> signatureValidator)
        {
            ComInterfaceTargetSignatureTest test = new(interfaceName, methodName, signatureValidator)
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            };

            await test.RunAsync();
        }

        private abstract class TargetSignatureTestBase : VerifyCS.Test
        {
            private readonly Action<Compilation, IMethodSymbol> _signatureValidator;
            private readonly string _interfaceName;
            private readonly string _methodName;

            protected TargetSignatureTestBase(string interfaceName, string methodName, Action<Compilation, IMethodSymbol> signatureValidator)
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

            protected abstract INamedTypeSymbol FindImplementationInterface(Compilation compilation, INamedTypeSymbol userDefinedInterface);
            private IMethodSymbol FindFunctionPointerInvocationSignature(Compilation compilation)
            {
                INamedTypeSymbol? userDefinedInterface = compilation.Assembly.GetTypeByMetadataName(_interfaceName);
                Assert.NotNull(userDefinedInterface);

                INamedTypeSymbol generatedInterfaceImplementation = FindImplementationInterface(compilation, userDefinedInterface);

                IMethodSymbol methodImplementation = Assert.Single(generatedInterfaceImplementation.GetMembers($"global::{_interfaceName}.{_methodName}").OfType<IMethodSymbol>());

                SyntaxNode emittedImplementationSyntax = methodImplementation.DeclaringSyntaxReferences[0].GetSyntax();

                SemanticModel model = compilation.GetSemanticModel(emittedImplementationSyntax.SyntaxTree);

                IOperation body = model.GetOperation(emittedImplementationSyntax)!;

                return Assert.Single(body.Descendants().OfType<IFunctionPointerInvocationOperation>()).GetFunctionPointerSignature();
            }
        }

        private sealed class VirtualMethodIndexTargetSignatureTest : TargetSignatureTestBase
        {
            public VirtualMethodIndexTargetSignatureTest(string interfaceName, string methodName, Action<Compilation, IMethodSymbol> signatureValidator)
                : base(interfaceName, methodName, signatureValidator)
            {
            }

            protected override IEnumerable<Type> GetSourceGenerators() => new[] { typeof(VtableIndexStubGenerator) };

            protected override INamedTypeSymbol FindImplementationInterface(Compilation compilation, INamedTypeSymbol userDefinedInterface) => Assert.Single(userDefinedInterface.GetTypeMembers("Native"));
        }

        private sealed class ComInterfaceTargetSignatureTest : TargetSignatureTestBase
        {
            public ComInterfaceTargetSignatureTest(string interfaceName, string methodName, Action<Compilation, IMethodSymbol> signatureValidator) : base(interfaceName, methodName, signatureValidator)
            {
            }
            protected override IEnumerable<Type> GetSourceGenerators() => new[] { typeof(Microsoft.Interop.ComInterfaceGenerator) };

            protected override INamedTypeSymbol FindImplementationInterface(Compilation compilation, INamedTypeSymbol userDefinedInterface)
            {
                INamedTypeSymbol? iUnknownDerivedAttributeType = compilation.GetTypeByMetadataName("System.Runtime.InteropServices.Marshalling.IUnknownDerivedAttribute`2");

                Assert.NotNull(iUnknownDerivedAttributeType);

                AttributeData iUnknownDerivedAttribute = Assert.Single(
                    userDefinedInterface.GetAttributes(),
                    attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass?.OriginalDefinition, iUnknownDerivedAttributeType));

                return (INamedTypeSymbol)iUnknownDerivedAttribute.AttributeClass!.TypeArguments[1];
            }
        }
    }
}
