// Licensed to the .NET Foundation under one or more agreements.
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
#pragma warning disable RS1039 // This call to 'SemanticModel.GetDeclaredSymbol()' will always return 'null' https://github.com/dotnet/roslyn-analyzers/issues/7061
                IMethodSymbol targetMethod = (IMethodSymbol)model.GetDeclaredSymbol(innerDllImport)!;
#pragma warning restore RS1039 // This call to 'SemanticModel.GetDeclaredSymbol()' will always return 'null'
                return targetMethod;
            }

            protected override void VerifyFinalCompilation(Compilation compilation)
            {
                _targetPInvokeAssertion(GetGeneratedPInvokeTargetFromCompilation(compilation), compilation);
            }
        }
    }
}
