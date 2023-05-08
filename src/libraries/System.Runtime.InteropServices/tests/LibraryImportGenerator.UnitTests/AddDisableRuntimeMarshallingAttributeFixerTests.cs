// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.Interop;
using Xunit;
using System.IO;

using VerifyCS = Microsoft.Interop.UnitTests.Verifiers.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer,
    Microsoft.Interop.Analyzers.AddDisableRuntimeMarshallingAttributeFixer>;

namespace LibraryImportGenerator.UnitTests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/60650", TestRuntimes.Mono)]
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
                    public static partial void PInvoke(S {|#0:s|});
                }

                [NativeMarshalling(typeof(Marshaller))]
                struct S
                {
                }

                struct Native
                {
                    public bool b;
                }

                [CustomMarshaller(typeof(S), MarshalMode.Default, typeof(Marshaller))]
                static class Marshaller
                {
                    public static Native ConvertToUnmanaged(S s) => default;

                    public static S ConvertToManaged(Native n) => default;
                }
                """;
            var expectedPropertiesFile = "[assembly: System.Runtime.CompilerServices.DisableRuntimeMarshalling]" + Environment.NewLine;

            var diagnostic = VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                .WithLocation(0)
                .WithArguments("Runtime marshalling must be disabled in this project by applying the 'System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute' to the assembly to enable marshalling this type.", "s");
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
                    public static partial void PInvoke(S {|#0:s|});
                }

                [NativeMarshalling(typeof(Marshaller))]
                struct S
                {
                }

                struct Native
                {
                    public bool b;
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

            var diagnostic = VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                .WithLocation(0)
                .WithArguments("Runtime marshalling must be disabled in this project by applying the 'System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute' to the assembly to enable marshalling this type.", "s");
            await VerifyCodeFixAsync(source, propertiesFile, expectedPropertiesFile, diagnostic);
        }

        private static async Task VerifyCodeFixAsync(string source, string? propertiesFile, string? expectedPropertiesFile, DiagnosticResult diagnostic)
        {
            var test = new Test
            {
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
                TestCode = source,
                FixedCode = source,
                BatchFixedCode = source
            };
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
            private static readonly ImmutableArray<Type> GeneratorTypes = ImmutableArray.Create(typeof(Microsoft.Interop.LibraryImportGenerator));

            public const string FilePathPrefix = "/Project/";

            protected override string DefaultFilePathPrefix => FilePathPrefix;

            protected override IEnumerable<Type> GetSourceGenerators()
            {
                return GeneratorTypes;
            }
        }
    }
}
