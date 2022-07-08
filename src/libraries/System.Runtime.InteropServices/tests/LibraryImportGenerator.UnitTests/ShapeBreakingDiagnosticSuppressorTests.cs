// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LibraryImportGenerator.UnitTests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.Interop.Analyzers;
using Xunit;

namespace LibraryImportGenerator.Unit.Tests
{
    public class ShapeBreakingDiagnosticSuppressorTests
    {
        [Fact]
        public async Task EmptyParameterlessFreeMethodHasSuppressedDiagnostic()
        {
            await VerifySuppressorAsync("""
                using System.Runtime.InteropServices.Marshalling;

                struct S
                {
                    public bool b;
                };

                [CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToUnmanagedIn))]
                static class Marshaller
                {
                    public struct ManagedToUnmanagedIn
                    {
                        private int i;
                        public void FromManaged(S s) { i = s.b ? 1 : 0; }
                        public ManagedToUnmanagedIn ToUnmanaged() => this;

                        public void {|#0:Free|}() {}
                    }
                }
                """,
                new DiagnosticResult(ShapeBreakingDiagnosticSuppressor.MarkMethodsAsStaticSuppression.SuppressedDiagnosticId, DiagnosticSeverity.Info).WithLocation(0).WithIsSuppressed(true));
        }

        private static async Task VerifySuppressorAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new Test
            {
                TestCode = source,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }

        class Test : CSharpCodeFixVerifier<EmptyDiagnosticAnalyzer, EmptyCodeFixProvider>.Test
        {
            public Test()
            {
                // Don't verify that we don't emit any diagnostics when they're disabled with #pragma warning disable
                // This check doesn't work when we set up the CompilationWithAnalyzers object to report suppressed diagnostics
                TestBehaviors |= TestBehaviors.SkipSuppressionCheck;
            }
            protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            {
                return new DiagnosticAnalyzer[]
                {
                    // Our diagnostic suppressor
                    new ShapeBreakingDiagnosticSuppressor(),
                    // Each of the analyzers the suppressor supports suppressing diagnostics from
                    new Microsoft.CodeQuality.Analyzers.QualityGuidelines.MarkMembersAsStaticAnalyzer()
                };
            }

            protected override CompilationWithAnalyzers CreateCompilationWithAnalyzers(Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerOptions options, CancellationToken cancellationToken)
            {
                return new CompilationWithAnalyzers(
                    compilation,
                    analyzers,
                    new CompilationWithAnalyzersOptions(
                        options,
                        onAnalyzerException: null,
                        concurrentAnalysis: true,
                        logAnalyzerExecutionTime: true,
                        reportSuppressedDiagnostics: true)); // We're specifically testing a DiagnosticSuppressor here, so we want to test that we find suppressed diagnostics.
            }
        }
    }
}
