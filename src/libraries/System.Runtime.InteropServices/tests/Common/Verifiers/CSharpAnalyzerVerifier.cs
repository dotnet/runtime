// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.Interop;

namespace Microsoft.Interop.UnitTests.Verifiers
{
    public static class CSharpAnalyzerVerifier<TAnalyzer>
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer}.Diagnostic()"/>
        public static DiagnosticResult Diagnostic()
            => CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic();

        /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer}.Diagnostic(string)"/>
        public static DiagnosticResult Diagnostic(string diagnosticId)
            => CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);

        /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer}.Diagnostic(DiagnosticDescriptor)"/>
        public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
            => CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(descriptor);

        /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer}.VerifyAnalyzerAsync(string, DiagnosticResult[])"/>
        public static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new Test
            {
                TestCode = source,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }

        // Code fix tests support both analyzer and code fix testing. This test class is derived from the code fix test
        // to avoid the need to maintain duplicate copies of the customization work.
        internal class Test : CSharpCodeFixVerifier<TAnalyzer, EmptyCodeFixProvider>.Test
        {
            public Test() : base()
            {
                // Disable SYSLIB1092 recommendation diagnostic by default (same as source generator tests)
                DisabledDiagnostics.Add(GeneratorDiagnostics.Ids.NotRecommendedGeneratedComInterfaceUsage);
            }

            // Exclude CS8795 (Partial method must have an implementation) since without the generator,
            // partial methods won't have implementations.
            // Exclude CS0751 (A partial member must be declared within a partial type) since tests
            // intentionally test non-partial types containing LibraryImport methods.
            // Other compiler diagnostics are still checked.
            protected override bool IsCompilerDiagnosticIncluded(Diagnostic diagnostic, CompilerDiagnostics compilerDiagnostics)
                => base.IsCompilerDiagnosticIncluded(diagnostic, compilerDiagnostics)
                    && diagnostic.Id != "CS8795"
                    && diagnostic.Id != "CS0751";
        }
    }
}
