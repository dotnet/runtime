// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

        [Fact]
        public async Task ComInterfaceInheritingAcrossCompilationsCalculatesCorrectVTableIndex()
        {
            string baseSource = $$"""
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [GeneratedComInterface]
                [Guid("0A617667-4961-4F90-B74F-6DC368E98179")]
                public partial interface IComInterface
                {
                    void Method();
                }
                """;

            string derivedSource = $$"""
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
                
                [GeneratedComInterface]
                [Guid("0A617667-4961-4F90-B74F-6DC368E9817A")]
                partial interface {|#1:IComInterface2|} : IComInterface
                {
                    void DerivedMethod();
                }
                """;

            TargetFunctionPointerInvocationTest test = new(
                "IComInterface2",
                "DerivedMethod",
                (newComp, invocation) =>
                {
                    Assert.Equal(4, Assert.IsAssignableFrom<ILiteralOperation>(Assert.IsAssignableFrom<IConversionOperation>(invocation.Target).Operand.ChildOperations.Last()).ConstantValue.Value);
                },
                new ComInterfaceImplementationLocator(),
                [typeof(Microsoft.Interop.ComInterfaceGenerator)]
            )
            {
                TestState =
                {
                    Sources = { derivedSource },
                    AdditionalProjects =
                    {
                        ["Base"] =
                        {
                            Sources = { baseSource }
                        }
                    },
                    AdditionalProjectReferences = { "Base" },
                },
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
                ExpectedDiagnostics =
                {
                    VerifyCS.DiagnosticWithArguments(GeneratorDiagnostics.BaseInterfaceDefinedInOtherAssembly, "IComInterface2", "IComInterface").WithLocation(1).WithSeverity(DiagnosticSeverity.Warning)
                }
            };

            test.TestState.AdditionalProjects["Base"].AdditionalReferences.AddRange(test.TestState.AdditionalReferences);

            // The Roslyn SDK doesn't apply the compilation options from CreateCompilationOptions to AdditionalProjects-based projects.
            test.SolutionTransforms.Add((sln, _) =>
            {
                var additionalProject = sln.Projects.First(proj => proj.Name == "Base");
                return additionalProject.WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true)).Solution;
            });

            await test.RunAsync();
        }

        private static async Task VerifyVirtualMethodIndexGeneratorAsync(string source, string interfaceName, string methodName, Action<Compilation, IMethodSymbol> signatureValidator)
        {
            TargetFunctionPointerInvocationTest test = new(
                interfaceName,
                methodName,
                (newComp, invocation) => signatureValidator(newComp, invocation.GetFunctionPointerSignature()),
                new VirtualMethodIndexImplementationLocator(),
                [typeof(VtableIndexStubGenerator)])
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            };

            await test.RunAsync();
        }

        private static async Task VerifyComInterfaceGeneratorAsync(string source, string interfaceName, string methodName, Action<Compilation, IMethodSymbol> signatureValidator)
        {
            TargetFunctionPointerInvocationTest test = new(
                interfaceName,
                methodName,
                (newComp, invocation) => signatureValidator(newComp, invocation.GetFunctionPointerSignature()),
                new ComInterfaceImplementationLocator(),
                [typeof(Microsoft.Interop.ComInterfaceGenerator)])
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            };

            await test.RunAsync();
        }

        private interface IImplementationLocator
        {
            INamedTypeSymbol FindImplementationInterface(Compilation compilation, INamedTypeSymbol userDefinedInterface);
        }

        private sealed class TargetFunctionPointerInvocationTest(
            string interfaceName,
            string methodName,
            Action<Compilation, IFunctionPointerInvocationOperation> signatureValidator,
            IImplementationLocator implementationTypeLocator,
            IEnumerable<Type> sourceGenerators)
            : VerifyCS.Test(referenceAncillaryInterop: true)
        {
            protected override void VerifyFinalCompilation(Compilation compilation)
            {
                signatureValidator(compilation, FindFunctionPointerInvocation(compilation));
            }

            protected sealed override IEnumerable<Type> GetSourceGenerators() => sourceGenerators;

            private IFunctionPointerInvocationOperation FindFunctionPointerInvocation(Compilation compilation)
            {
                INamedTypeSymbol? userDefinedInterface = compilation.Assembly.GetTypeByMetadataName(interfaceName);
                Assert.NotNull(userDefinedInterface);

                INamedTypeSymbol generatedInterfaceImplementation = implementationTypeLocator.FindImplementationInterface(compilation, userDefinedInterface);

                IMethodSymbol methodImplementation = Assert.Single(generatedInterfaceImplementation.GetMembers($"global::{interfaceName}.{methodName}").OfType<IMethodSymbol>());

                SyntaxNode emittedImplementationSyntax = methodImplementation.DeclaringSyntaxReferences[0].GetSyntax();

                SemanticModel model = compilation.GetSemanticModel(emittedImplementationSyntax.SyntaxTree);

                IOperation body = model.GetOperation(emittedImplementationSyntax)!;

                return Assert.Single(body.Descendants().OfType<IFunctionPointerInvocationOperation>());
            }
        }

        private sealed class VirtualMethodIndexImplementationLocator : IImplementationLocator
        {
            public INamedTypeSymbol FindImplementationInterface(Compilation compilation, INamedTypeSymbol userDefinedInterface) => Assert.Single(userDefinedInterface.GetTypeMembers("Native"));
        }

        private sealed class ComInterfaceImplementationLocator : IImplementationLocator
        {
            public INamedTypeSymbol FindImplementationInterface(Compilation compilation, INamedTypeSymbol userDefinedInterface)
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
