// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Interop;
using Microsoft.Interop.UnitTests;
using SourceGenerators.Tests;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace LibraryImportGenerator.UnitTests
{
    public class AttributeForwarding
    {
        private const string ModuleNameWithSpecialCharacters = "Native\\\"Library\t\r\n\u0085\u2028\u2029";
        private const string EntryPointWithSpecialCharacters = "Export\\\"Name\t\r\n\u0085\u2028\u2029\U0001F680";

        [Theory]
        [InlineData("SuppressGCTransition", "System.Runtime.InteropServices.SuppressGCTransitionAttribute")]
        [InlineData("UnmanagedCallConv", "System.Runtime.InteropServices.UnmanagedCallConvAttribute")]
        [InlineData("WasmImportLinkage", "System.Runtime.InteropServices.WasmImportLinkageAttribute")]
        [InlineData("System.Diagnostics.StackTraceHidden", "System.Diagnostics.StackTraceHiddenAttribute")]
        [InlineData("System.Diagnostics.DebuggerHidden", "System.Diagnostics.DebuggerHiddenAttribute")]
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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public Task DllImportStringArgumentsAreEscaped(bool requiresMarshalling)
        {
            string source = CreateImportWithSpecialCharacters(requiresMarshalling ? "string value" : "int value");
            return VerifySourceGeneratorAsync(source, VerifySpecialCharacters, expectForwarder: !requiresMarshalling);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [OuterLoop("Uses the network for downlevel ref packs")]
        public Task DownlevelDllImportStringArgumentsAreEscaped(bool requiresMarshalling)
        {
            string source = CreateImportWithSpecialCharacters(requiresMarshalling ? "ref int value" : "int value");
            return VerifyDownlevelSourceGeneratorAsync(source, VerifySpecialCharacters, expectForwarder: !requiresMarshalling);
        }

        private static string CreateImportWithSpecialCharacters(string parameter) => $$"""
            using System.Runtime.InteropServices;
            partial class C
            {
                [LibraryImport({{Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(ModuleNameWithSpecialCharacters, quote: true)}},
                    EntryPoint = {{Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(EntryPointWithSpecialCharacters, quote: true)}},
                    StringMarshalling = StringMarshalling.Utf16)]
                public static partial void Method({{parameter}});
            }
            """;

        private static void VerifySpecialCharacters(IMethodSymbol targetMethod, Compilation _)
        {
            DllImportData? import = targetMethod.GetDllImportData();
            Assert.NotNull(import);
            Assert.Equal(ModuleNameWithSpecialCharacters, import.ModuleName);
            Assert.Equal(EntryPointWithSpecialCharacters, import.EntryPointName);
            Assert.Equal(CharSet.Unicode, import.CharacterSet);
            Assert.True(import.ExactSpelling);
        }

        [Theory]
        [InlineData("@event", "event", false)]
        [InlineData("@event", "event", true)]
        [InlineData(@"M\u0065thod", "Method", false)]
        [InlineData(@"M\u0065thod", "Method", true)]
        public Task DefaultEntryPointUsesLogicalMethodName(string identifier, string expectedEntryPoint, bool requiresMarshalling)
        {
            string source = $$"""
                using System.Runtime.InteropServices;
                namespace @namespace;
                partial class @class
                {
                    [LibraryImport("DoesNotExist", StringMarshalling = StringMarshalling.Utf16)]
                    public static partial void {{identifier}}({{(requiresMarshalling ? "string" : "int")}} @return);
                }
                """;
            return VerifySourceGeneratorAsync(
                source,
                (targetMethod, _) => Assert.Equal(expectedEntryPoint, targetMethod.GetDllImportData()!.EntryPointName),
                expectForwarder: !requiresMarshalling);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [OuterLoop("Uses the network for downlevel ref packs")]
        public Task DownlevelDefaultEntryPointUsesLogicalMethodName(bool requiresMarshalling)
        {
            string source = $$"""
                using System.Runtime.InteropServices;
                namespace @namespace;
                partial class @class
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial void @event({{(requiresMarshalling ? "ref int" : "int")}} @return);
                }
                """;
            return VerifyDownlevelSourceGeneratorAsync(
                source,
                (targetMethod, _) => Assert.Equal("event", targetMethod.GetDllImportData()!.EntryPointName),
                expectForwarder: !requiresMarshalling);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public Task GenerateForwardersOptionSelectsInteropBoundary(bool generateForwarders)
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class C
                {
                    [LibraryImport("DoesNotExist", SetLastError = true)]
                    public static partial int Method(int value);
                }
                """;
            return VerifySourceGeneratorAsync(
                source,
                (targetMethod, _) => Assert.Equal(generateForwarders, targetMethod.GetDllImportData()!.SetLastError),
                expectForwarder: generateForwarders,
                generateForwarders: generateForwarders);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(false)]
        [InlineData(true)]
        public Task ForwarderPreservesExplicitSetLastError(bool? setLastError)
        {
            string argument = setLastError.HasValue ? $", SetLastError = {(setLastError.Value ? "true" : "false")}" : "";
            string source = $$"""
                using System.Runtime.InteropServices;
                partial class C
                {
                    [LibraryImport("DoesNotExist"{{argument}})]
                    public static partial int Method(int value);
                }
                """;
            return VerifySourceGeneratorAsync(
                source,
                (targetMethod, _) =>
                {
                    Assert.Equal(setLastError.GetValueOrDefault(), targetMethod.GetDllImportData()!.SetLastError);
                    var arguments = GetDllImportAttribute(targetMethod).NamedArguments
                        .Where(namedArgument => namedArgument.Key == nameof(DllImportAttribute.SetLastError));
                    if (setLastError.HasValue)
                    {
                        Assert.Equal(setLastError.Value, Assert.Single(arguments).Value.Value);
                    }
                    else
                    {
                        Assert.Empty(arguments);
                    }
                },
                expectForwarder: true,
                generateForwarders: true);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(nameof(System.Runtime.InteropServices.StringMarshalling.Utf16))]
        [InlineData(nameof(System.Runtime.InteropServices.StringMarshalling.Utf8))]
        public Task ForwarderIncludesCharSetOnlyForUtf16(string? stringMarshalling)
        {
            string argument = stringMarshalling is not null ? $", StringMarshalling = StringMarshalling.{stringMarshalling}" : "";
            string source = $$"""
                using System.Runtime.InteropServices;
                partial class C
                {
                    [LibraryImport("DoesNotExist"{{argument}})]
                    public static partial int Method(int value);
                }
                """;
            return VerifySourceGeneratorAsync(
                source,
                (targetMethod, _) =>
                {
                    var arguments = GetDllImportAttribute(targetMethod).NamedArguments
                        .Where(namedArgument => namedArgument.Key == nameof(DllImportAttribute.CharSet));
                    if (stringMarshalling == nameof(System.Runtime.InteropServices.StringMarshalling.Utf16))
                    {
                        Assert.Equal((int)CharSet.Unicode, Assert.Single(arguments).Value.Value);
                    }
                    else
                    {
                        Assert.Empty(arguments);
                    }
                },
                expectForwarder: true,
                generateForwarders: true);
        }

        private static AttributeData GetDllImportAttribute(IMethodSymbol targetMethod)
            => Assert.Single(targetMethod.GetAttributes().Where(attribute => attribute.AttributeClass!.ToDisplayString() == typeof(DllImportAttribute).FullName));

        private static Task VerifySourceGeneratorAsync(
            string source,
            Action<IMethodSymbol, Compilation> targetPInvokeAssertion,
            bool expectForwarder = false,
            bool? generateForwarders = null)
        {
            var test = new GeneratedTargetPInvokeTest<Microsoft.Interop.LibraryImportGenerator, Microsoft.Interop.Analyzers.LibraryImportDiagnosticsAnalyzer>(targetPInvokeAssertion, expectForwarder)
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            };

            if (generateForwarders.HasValue)
            {
                test.SolutionTransforms.Add((solution, projectId) =>
                    solution.AddAnalyzerConfigDocument(
                        DocumentId.CreateNewId(projectId),
                        "GeneratorOptions.globalconfig",
                        SourceText.From($"""
                            is_global = true
                            {OptionsHelper.GenerateForwardersOption} = {(generateForwarders.Value ? "true" : "false")}
                            """, Encoding.UTF8),
                        filePath: "/GeneratorOptions.globalconfig"));
            }

            return test.RunAsync();
        }

        private static Task VerifyDownlevelSourceGeneratorAsync(string source, Action<IMethodSymbol, Compilation> targetPInvokeAssertion, bool expectForwarder)
        {
            var test = new GeneratedTargetPInvokeTest<DownlevelLibraryImportGenerator, EmptyDiagnosticAnalyzer>(targetPInvokeAssertion, expectForwarder, TestTargetFramework.Standard2_0)
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            };
            return test.RunAsync();
        }

        class GeneratedTargetPInvokeTest<TSourceGenerator, TAnalyzer>
            : Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<TSourceGenerator, TAnalyzer>.Test
            where TSourceGenerator : new()
            where TAnalyzer : DiagnosticAnalyzer, new()
        {
            private readonly Action<IMethodSymbol, Compilation> _targetPInvokeAssertion;
            private readonly bool _expectForwarder;

            public GeneratedTargetPInvokeTest(Action<IMethodSymbol, Compilation> targetPInvokeAssertion, bool expectForwarder)
                : base(referenceAncillaryInterop: false)
            {
                _targetPInvokeAssertion = targetPInvokeAssertion;
                _expectForwarder = expectForwarder;
            }

            public GeneratedTargetPInvokeTest(Action<IMethodSymbol, Compilation> targetPInvokeAssertion, bool expectForwarder, TestTargetFramework targetFramework)
                : base(targetFramework)
            {
                _targetPInvokeAssertion = targetPInvokeAssertion;
                _expectForwarder = expectForwarder;
            }

            private IMethodSymbol GetGeneratedPInvokeTargetFromCompilation(Compilation compilation)
            {
                SyntaxTree generatedCode = compilation.SyntaxTrees.Single(tree => tree.FilePath.EndsWith("LibraryImports.g.cs", StringComparison.Ordinal));
                SemanticModel model = compilation.GetSemanticModel(generatedCode);

                var localFunctions = generatedCode.GetRoot()
                    .DescendantNodes().OfType<LocalFunctionStatementSyntax>()
                    .ToList();
                if (_expectForwarder)
                {
                    Assert.Empty(localFunctions);
                    MethodDeclarationSyntax method = Assert.Single(generatedCode.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>());
                    Assert.Null(method.Body);
                    IMethodSymbol target = Assert.IsAssignableFrom<IMethodSymbol>(model.GetDeclaredSymbol(method));
                    Assert.NotNull(target.GetDllImportData());
                    return target;
                }

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
