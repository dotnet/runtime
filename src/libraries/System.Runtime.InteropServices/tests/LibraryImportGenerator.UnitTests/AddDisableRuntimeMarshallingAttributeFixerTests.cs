// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Model;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.Interop.Analyzers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Interop;

using VerifyCS = LibraryImportGenerator.UnitTests.Verifiers.CSharpCodeFixVerifier<
    LibraryImportGenerator.UnitTests.AddDisableRuntimeMarshallingAttributeFixerTests.MockAnalyzer,
    Microsoft.Interop.Analyzers.AddDisableRuntimeMarshallingAttributeFixer>;
using Xunit;
using System.IO;

namespace LibraryImportGenerator.UnitTests
{
    public class AddDisableRuntimeMarshallingAttributeFixerTests
    {
        [Fact]
        public static async Task Adds_NewFile_With_Attribute()
        {
            // Source will have CS8795 (Partial method must have an implementation) without generator run
            var source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
                partial class Foo
                {
                    [LibraryImport("Foo")]
                    public static partial void {|CS8795:PInvoke|}(S {|#0:s|});
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
            var expectedPropertiesFile = "[assembly: System.Runtime.CompilerServices.DisableRuntimeMarshalling]" + Environment.NewLine;

            var diagnostic = VerifyCS.Diagnostic(GeneratorDiagnostics.Ids.TypeNotSupported).WithLocation(0).WithArguments("S", "s");
            await VerifyCodeFixAsync(source, propertiesFile: null, expectedPropertiesFile, diagnostic);
        }

        [Fact]
        public static async Task Appends_Attribute_To_Existing_AssemblyInfo_File()
        {
            var source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
                partial class Foo
                {
                    [LibraryImport("Foo")]
                    public static partial void {|CS8795:PInvoke|}(S {|#0:s|});
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
            var propertiesFile = """
                using System.Reflection;

                [assembly: AssemblyMetadata("MyMetadata", "Value")]
                """;
            var expectedPropertiesFile = """
                using System.Reflection;

                [assembly: AssemblyMetadata("MyMetadata", "Value")]
                [assembly: System.Runtime.CompilerServices.DisableRuntimeMarshalling]

                """;

            var diagnostic = VerifyCS.Diagnostic(GeneratorDiagnostics.Ids.TypeNotSupported).WithLocation(0).WithArguments("S", "s");
            await VerifyCodeFixAsync(source, propertiesFile, expectedPropertiesFile, diagnostic);
        }

        private static async Task VerifyCodeFixAsync(string source, string? propertiesFile, string? expectedPropertiesFile, DiagnosticResult diagnostic)
        {
            var test = new Test();
            // We don't care about validating the settings for the MockAnalyzer and we're also hitting failures on Mono
            // with this check in this case, so skip the check for now.
            test.TestBehaviors = TestBehaviors.SkipGeneratedCodeCheck;
            test.TestCode = source;
            test.FixedCode = source;
            test.BatchFixedCode = source;
            test.ExpectedDiagnostics.Add(diagnostic);
            if (propertiesFile is not null)
            {
                test.TestState.Sources.Add(($"{Test.FilePathPrefix}Properties{Path.DirectorySeparatorChar}AssemblyInfo.cs", propertiesFile));
            }
            if (expectedPropertiesFile is not null)
            {
                test.FixedState.Sources.Add(($"{Test.FilePathPrefix}Properties{Path.DirectorySeparatorChar}AssemblyInfo.cs", expectedPropertiesFile));
                test.BatchFixedState.Sources.Add(($"{Test.FilePathPrefix}Properties{Path.DirectorySeparatorChar}AssemblyInfo.cs", expectedPropertiesFile));
            }
            await test.RunAsync();
        }

        class Test : VerifyCS.Test
        {
            public const string FilePathPrefix = "/Project/";

            protected override string DefaultFilePathPrefix => FilePathPrefix;
        }

        // The Roslyn SDK doesn't provide a good test harness for testing a code fix that triggers
        // on a source-generator-introduced diagnostic. This analyzer does a decent enough job of triggering
        // the specific diagnostic in the right place for us to test the code fix.
        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        public class MockAnalyzer : DiagnosticAnalyzer
        {
            private static readonly DiagnosticDescriptor AddDisableRuntimeMarshallingAttributeRule = GeneratorDiagnostics.ParameterTypeNotSupported;

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(AddDisableRuntimeMarshallingAttributeRule);

            public override void Initialize(AnalysisContext context)
            {
                context.EnableConcurrentExecution();
                context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
                context.RegisterSymbolAction(context =>
                {
                    var symbol = (IParameterSymbol)context.Symbol;

                    if (context.Symbol.ContainingAssembly.GetAttributes().Any(attr => attr.AttributeClass!.ToDisplayString() == TypeNames.System_Runtime_CompilerServices_DisableRuntimeMarshallingAttribute))
                    {
                        return;
                    }

                    if (symbol.ContainingSymbol is IMethodSymbol { IsStatic: true, IsPartialDefinition: true })
                    {
                        context.ReportDiagnostic(context.Symbol.CreateDiagnostic(
                            AddDisableRuntimeMarshallingAttributeRule,
                            ImmutableDictionary<string, string>.Empty
                                .Add(
                                    GeneratorDiagnosticProperties.AddDisableRuntimeMarshallingAttribute,
                                    GeneratorDiagnosticProperties.AddDisableRuntimeMarshallingAttribute),
                            symbol.Type.ToDisplayString(), symbol.Name));
                    }
                }, SymbolKind.Parameter);
            }
        }
    }
}
