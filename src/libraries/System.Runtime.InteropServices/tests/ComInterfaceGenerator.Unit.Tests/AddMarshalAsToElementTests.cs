// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.Interop;
using Xunit;

using VerifyCS = Microsoft.Interop.UnitTests.Verifiers.CSharpCodeFixVerifier<
       Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer,
       Microsoft.Interop.Analyzers.AddMarshalAsToElementFixer>;

namespace ComInterfaceGenerator.Unit.Tests
{
    public class AddMarshalAsToElementTests
    {
        [Fact]
        public async Task ReturnHResultStruct_ReportsDiagnostic()
        {
            var source = """
                    using System.Runtime.InteropServices;
                    using System.Runtime.InteropServices.Marshalling;
                    [GeneratedComInterface]
                    [Guid("0DB41042-0255-4CDD-B73A-9C5D5F31303D")]
                    partial interface I
                    {
                        [PreserveSig]
                        HResult {|#0:Method|}();
                    }

                    struct HResult
                    {
                        public int Value;
                    }
                    """;

            var fixedSource = """
                    using System.Runtime.InteropServices;
                    using System.Runtime.InteropServices.Marshalling;
                    [GeneratedComInterface]
                    [Guid("0DB41042-0255-4CDD-B73A-9C5D5F31303D")]
                    partial interface I
                    {
                        [PreserveSig]
                        [return: MarshalAs(UnmanagedType.Error)]
                        HResult Method();
                    }

                    struct HResult
                    {
                        public int Value;
                    }
                    """;

            await VerifySourceGeneratorAsync(source, fixedSource, VerifyCS.Diagnostic(GeneratorDiagnostics.HResultTypeWillBeTreatedAsStruct).WithLocation(0).WithArguments("HResult"));
        }

        private static Task VerifySourceGeneratorAsync(string source, string fixedSource, params DiagnosticResult[] diagnostics)
        {
            var test = new Test()
            {
                TestCode = source,
                FixedCode = fixedSource,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
                CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck, // The Batch fixer doesn't work with the Roslyn SDK when the test is testing fixing diagnostics from a source generator (with no analyzers present)
            };

            test.ExpectedDiagnostics.AddRange(diagnostics);

            return test.RunAsync();
        }

        private sealed class Test : VerifyCS.Test
        {
            private static readonly ImmutableArray<Type> GeneratorTypes = ImmutableArray.Create(typeof(Microsoft.Interop.ComInterfaceGenerator));

            protected override IEnumerable<Type> GetSourceGenerators() => GeneratorTypes;
        }
    }
}
