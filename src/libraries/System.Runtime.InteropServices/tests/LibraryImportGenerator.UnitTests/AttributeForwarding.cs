﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.Interop;
using Microsoft.Interop.UnitTests;
using SourceGenerators.Tests;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<Microsoft.Interop.LibraryImportGenerator>;

namespace LibraryImportGenerator.UnitTests
{
    public class AttributeForwarding
    {
        [Theory]
        [InlineData("SuppressGCTransition", "System.Runtime.InteropServices.SuppressGCTransitionAttribute")]
        [InlineData("UnmanagedCallConv", "System.Runtime.InteropServices.UnmanagedCallConvAttribute")]
        public async Task KnownParameterlessAttribute(string attributeSourceName, string attributeMetadataName)
        {
            string source = $$"""
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
                [assembly:DisableRuntimeMarshalling]
                partial class C
                {
                    [{{attributeSourceName}}]
                    [LibraryImportAttribute("DoesNotExist")]
                    public static partial S Method1();
                }
                
                [NativeMarshalling(typeof(Marshaller))]
                struct S
                {
                }
                
                struct Native
                {
                }
                
                [CustomMarshaller(typeof(S), MarshalMode.Default, typeof(Marshaller))]
                static class Marshaller
                {
                    public static Native ConvertToUnmanaged(S s) => default;
                
                    public static S ConvertToManaged(Native n) => default;
                }
                """;

            await VerifySourceGeneratorAsync(
                source,
                (targetMethod, newComp) =>
                {
                    ITypeSymbol attributeType = newComp.GetTypeByMetadataName(attributeMetadataName)!;
                    Assert.NotNull(attributeType);

                    Assert.Contains(
                        targetMethod.GetAttributes(),
                        attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeType));
                });
        }

        [Fact]
        public async Task UnmanagedCallConvAttribute_EmptyCallConvArray()
        {
            string source = """
                using System;
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
                [assembly:DisableRuntimeMarshalling]
                partial class C
                {
                    [UnmanagedCallConv(CallConvs = new Type[0])]
                    [LibraryImportAttribute("DoesNotExist")]
                    public static partial S Method1();
                }

                [NativeMarshalling(typeof(Marshaller))]
                struct S
                {
                }

                struct Native
                {
                }

                [CustomMarshaller(typeof(S), MarshalMode.Default, typeof(Marshaller))]
                static class Marshaller
                {
                    public static Native ConvertToUnmanaged(S s) => default;

                    public static S ConvertToManaged(Native n) => default;
                }
                """;

            await VerifySourceGeneratorAsync(
                source,
                (targetMethod, newComp) =>
                {
                    ITypeSymbol attributeType = newComp.GetTypeByMetadataName("System.Runtime.InteropServices.UnmanagedCallConvAttribute")!;
                    Assert.NotNull(attributeType);

                    Assert.Contains(
                        targetMethod.GetAttributes(),
                        attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeType)
                            && attr.NamedArguments.Length == 1
                            && attr.NamedArguments[0].Key == "CallConvs"
                            && attr.NamedArguments[0].Value.Values.Length == 0);
                });
        }

        [Fact]
        public async Task UnmanagedCallConvAttribute_SingleCallConvType()
        {
            string source = """
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
                [assembly:DisableRuntimeMarshalling]
                partial class C
                {
                    [UnmanagedCallConv(CallConvs = new[]{typeof(CallConvStdcall)})]
                    [LibraryImportAttribute("DoesNotExist")]
                    public static partial S Method1();
                }

                [NativeMarshalling(typeof(Marshaller))]
                struct S
                {
                }

                struct Native
                {
                }

                [CustomMarshaller(typeof(S), MarshalMode.Default, typeof(Marshaller))]
                static class Marshaller
                {
                    public static Native ConvertToUnmanaged(S s) => default;

                    public static S ConvertToManaged(Native n) => default;
                }
                """;

            await VerifySourceGeneratorAsync(
                source,
                (targetMethod, newComp) =>
                {
                    ITypeSymbol attributeType = newComp.GetTypeByMetadataName("System.Runtime.InteropServices.UnmanagedCallConvAttribute")!;
                    ITypeSymbol callConvType = newComp.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvStdcall")!;

                    Assert.NotNull(attributeType);

                    Assert.Contains(
                        targetMethod.GetAttributes(),
                        attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeType)
                            && attr.NamedArguments.Length == 1
                            && attr.NamedArguments[0].Key == "CallConvs"
                            && attr.NamedArguments[0].Value.Values.Length == 1
                            && SymbolEqualityComparer.Default.Equals(
                                (INamedTypeSymbol?)attr.NamedArguments[0].Value.Values[0].Value!,
                                callConvType));
                });
        }

        [Fact]
        public async Task UnmanagedCallConvAttribute_MultipleCallConvTypes()
        {
            string source = """
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
                [assembly:DisableRuntimeMarshalling]
                partial class C
                {
                    [UnmanagedCallConv(CallConvs = new[]{typeof(CallConvStdcall), typeof(CallConvSuppressGCTransition)})]
                    [LibraryImportAttribute("DoesNotExist")]
                    public static partial S Method1();
                }

                [NativeMarshalling(typeof(Marshaller))]
                struct S
                {
                }

                struct Native
                {
                }

                [CustomMarshaller(typeof(S), MarshalMode.Default, typeof(Marshaller))]
                static class Marshaller
                {
                    public static Native ConvertToUnmanaged(S s) => default;

                    public static S ConvertToManaged(Native n) => default;
                }
                """;

            await VerifySourceGeneratorAsync(
                source,
                (targetMethod, newComp) =>
                {
                    ITypeSymbol attributeType = newComp.GetTypeByMetadataName("System.Runtime.InteropServices.UnmanagedCallConvAttribute")!;
                    ITypeSymbol callConvType = newComp.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvStdcall")!;
                    ITypeSymbol callConvType2 = newComp.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvSuppressGCTransition")!;

                    Assert.NotNull(attributeType);

                    Assert.Contains(
                        targetMethod.GetAttributes(),
                        attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeType)
                            && attr.NamedArguments.Length == 1
                            && attr.NamedArguments[0].Key == "CallConvs"
                            && attr.NamedArguments[0].Value.Values.Length == 2
                            && SymbolEqualityComparer.Default.Equals(
                                (INamedTypeSymbol?)attr.NamedArguments[0].Value.Values[0].Value!,
                                callConvType)
                            && SymbolEqualityComparer.Default.Equals(
                                (INamedTypeSymbol?)attr.NamedArguments[0].Value.Values[1].Value!,
                                callConvType2));
                });
        }

        [Fact]
        public async Task DefaultDllImportSearchPathsAttribute()
        {
            string source = $$"""
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
                [assembly:DisableRuntimeMarshalling]
                partial class C
                {
                    [DefaultDllImportSearchPaths(DllImportSearchPath.System32 | DllImportSearchPath.UserDirectories)]
                    [LibraryImportAttribute("DoesNotExist")]
                    public static partial S Method1();
                }
                
                [NativeMarshalling(typeof(Marshaller))]
                struct S
                {
                }
                
                struct Native
                {
                }
                
                [CustomMarshaller(typeof(S), MarshalMode.Default, typeof(Marshaller))]
                static class Marshaller
                {
                    public static Native ConvertToUnmanaged(S s) => default;
                
                    public static S ConvertToManaged(Native n) => default;
                }
                """;

            await VerifySourceGeneratorAsync(
                source,
                (targetMethod, newComp) =>
                {
                    ITypeSymbol attributeType = newComp.GetTypeByMetadataName("System.Runtime.InteropServices.DefaultDllImportSearchPathsAttribute")!;

                    Assert.NotNull(attributeType);

                    DllImportSearchPath expected = DllImportSearchPath.System32 | DllImportSearchPath.UserDirectories;

                    Assert.Contains(
                        targetMethod.GetAttributes(),
                        attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeType)
                            && attr.ConstructorArguments.Length == 1
                            && expected == (DllImportSearchPath)attr.ConstructorArguments[0].Value!);
                });
        }

        [Fact]
        public async Task OtherAttributeType()
        {
            string source = """
                using System;
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
                [assembly:DisableRuntimeMarshalling]

                class OtherAttribute : Attribute {}

                partial class C
                {
                    [Other]
                    [LibraryImportAttribute("DoesNotExist")]
                    public static partial S Method1();
                }

                [NativeMarshalling(typeof(Marshaller))]
                struct S
                {
                }

                struct Native
                {
                }

                [CustomMarshaller(typeof(S), MarshalMode.Default, typeof(Marshaller))]
                static class Marshaller
                {
                    public static Native ConvertToUnmanaged(S s) => default;

                    public static S ConvertToManaged(Native n) => default;
                }
                """;

            await VerifySourceGeneratorAsync(
                source,
                (targetMethod, newComp) =>
                {
                    ITypeSymbol attributeType = newComp.GetTypeByMetadataName("OtherAttribute")!;

                    Assert.NotNull(attributeType);


                    Assert.DoesNotContain(
                        targetMethod.GetAttributes(),
                        attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeType));
                });
        }

        [Fact]
        [OuterLoop("Uses the network for downlevel ref packs")]
        public async Task InOutAttributes_Forwarded_To_ForwardedParameter()
        {
            // This code is invalid configuration from the source generator's perspective.
            // We just use it as validation for forwarding the In and Out attributes.
            string source = """
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                partial class C
                {
                    [LibraryImportAttribute("DoesNotExist")]
                    [return: MarshalAs(UnmanagedType.Bool)]
                    public static partial bool Method1([In, Out] int {|SYSLIB1051:a|});
                }
                """ + CodeSnippets.LibraryImportAttributeDeclaration;

            await VerifySourceGeneratorAsync(
                source,
                (targetMethod, newComp) =>
                {
                    INamedTypeSymbol marshalAsAttribute = newComp.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_MarshalAsAttribute)!;
                    INamedTypeSymbol inAttribute = newComp.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_InAttribute)!;
                    INamedTypeSymbol outAttribute = newComp.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_OutAttribute)!;
                    Assert.Collection(targetMethod.Parameters,
                        param => Assert.Collection(param.GetAttributes(),
                            attr =>
                            {
                                Assert.Equal(inAttribute, attr.AttributeClass, SymbolEqualityComparer.Default);
                                Assert.Empty(attr.ConstructorArguments);
                                Assert.Empty(attr.NamedArguments);
                            },
                            attr =>
                            {
                                Assert.Equal(outAttribute, attr.AttributeClass, SymbolEqualityComparer.Default);
                                Assert.Empty(attr.ConstructorArguments);
                                Assert.Empty(attr.NamedArguments);
                            }));
                },
                TestTargetFramework.Standard);
        }

        [Fact]
        [OuterLoop("Uses the network for downlevel ref packs")]
        public async Task MarshalAsAttribute_Forwarded_To_ForwardedParameter()
        {
            string source = """
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                partial class C
                {
                    [LibraryImportAttribute("DoesNotExist")]
                    [return: MarshalAs(UnmanagedType.Bool)]
                    public static partial bool Method1([MarshalAs(UnmanagedType.I2)] int a);
                }
                """ + CodeSnippets.LibraryImportAttributeDeclaration;

            await VerifySourceGeneratorAsync(
                source,
                (targetMethod, newComp) =>
                {
                    INamedTypeSymbol marshalAsAttribute = newComp.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_MarshalAsAttribute)!;
                    Assert.Collection(targetMethod.Parameters,
                        param => Assert.Collection(param.GetAttributes(),
                            attr =>
                            {
                                Assert.Equal(marshalAsAttribute, attr.AttributeClass, SymbolEqualityComparer.Default);
                                Assert.Equal(UnmanagedType.I2, (UnmanagedType)attr.ConstructorArguments[0].Value!);
                                Assert.Empty(attr.NamedArguments);
                            }));
                },
                TestTargetFramework.Standard);
        }

        private static Task VerifySourceGeneratorAsync(string source, Action<IMethodSymbol, Compilation> targetPInvokeAssertion, TestTargetFramework targetFramework = TestTargetFramework.Net)
        {
            var test = new GeneratedTargetPInvokeTest(targetPInvokeAssertion, targetFramework)
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            };

            return test.RunAsync();
        }

        class GeneratedTargetPInvokeTest : VerifyCS.Test
        {
            private readonly Action<IMethodSymbol, Compilation> _targetPInvokeAssertion;

            public GeneratedTargetPInvokeTest(Action<IMethodSymbol, Compilation> targetPInvokeAssertion, TestTargetFramework targetFramework)
                :base(targetFramework)
            {
                _targetPInvokeAssertion = targetPInvokeAssertion;
            }

            private static IMethodSymbol GetGeneratedPInvokeTargetFromCompilation(Compilation compilation)
            {
                // The last syntax tree is the generated code
                SyntaxTree generatedCode = compilation.SyntaxTrees.Last();
                SemanticModel model = compilation.GetSemanticModel(generatedCode);

                var localFunctions = generatedCode.GetRoot()
                    .DescendantNodes().OfType<LocalFunctionStatementSyntax>()
                    .ToList();
                LocalFunctionStatementSyntax innerDllImport = Assert.Single(localFunctions);
                IMethodSymbol targetMethod = (IMethodSymbol)model.GetDeclaredSymbol(innerDllImport)!;
                return targetMethod;
            }

            protected override void VerifyFinalCompilation(Compilation compilation)
            {
                _targetPInvokeAssertion(GetGeneratedPInvokeTargetFromCompilation(compilation), compilation);
            }
        }
    }
}
