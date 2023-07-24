// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.Interop.UnitTests.Verifiers
{
    public static class CSharpAnalyzerVerifier<TAnalyzer>
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer}.Diagnostic()"/>
        public static DiagnosticResult Diagnostic()
            => AnalyzerVerifier<TAnalyzer>.Diagnostic();

        /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer}.Diagnostic(string)"/>
        public static DiagnosticResult Diagnostic(string diagnosticId)
            => AnalyzerVerifier<TAnalyzer>.Diagnostic(diagnosticId);

        /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer}.Diagnostic(DiagnosticDescriptor)"/>
        public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
            => AnalyzerVerifier<TAnalyzer>.Diagnostic(descriptor);

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
        { }
    }
}
