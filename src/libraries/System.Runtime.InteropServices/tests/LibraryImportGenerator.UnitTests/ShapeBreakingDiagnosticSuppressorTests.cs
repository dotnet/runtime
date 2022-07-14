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

namespace LibraryImportGenerator.UnitTests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/60650", TestRuntimes.Mono)]
    public class ShapeBreakingDiagnosticSuppressorTests
    {
        [Fact]
        public async Task StatefulValueMarshallerMethodsThatDoNotUseInstanceState_SuppressesDiagnostic()
        {
            await VerifySuppressorAsync("""
                using System;
                using System.Runtime.CompilerServices;
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
                        public static int BufferSize { get; } = 1;

                        public void {|#0:FromManaged|}(S s) {}

                        public void {|#1:FromManaged|}(S s, Span<byte> buffer){}

                        public ManagedToUnmanagedIn {|#2:ToUnmanaged|}() => default;

                        public void {|#3:FromUnmanaged|}(ManagedToUnmanagedIn unmanaged) {}

                        public S {|#4:ToManaged|}() => default;
                
                        public void {|#5:Free|}() {}
                
                        public void {|#6:OnInvoked|}() {}

                        public ref byte {|#7:GetPinnableReference|}() => ref Unsafe.NullRef<byte>();
                    }
                }
                """,
                SuppressedDiagnostic(ShapeBreakingDiagnosticSuppressor.MarkMethodsAsStaticSuppression, DiagnosticSeverity.Info).WithLocation(0),
                SuppressedDiagnostic(ShapeBreakingDiagnosticSuppressor.MarkMethodsAsStaticSuppression, DiagnosticSeverity.Info).WithLocation(1),
                SuppressedDiagnostic(ShapeBreakingDiagnosticSuppressor.MarkMethodsAsStaticSuppression, DiagnosticSeverity.Info).WithLocation(2),
                SuppressedDiagnostic(ShapeBreakingDiagnosticSuppressor.MarkMethodsAsStaticSuppression, DiagnosticSeverity.Info).WithLocation(3),
                SuppressedDiagnostic(ShapeBreakingDiagnosticSuppressor.MarkMethodsAsStaticSuppression, DiagnosticSeverity.Info).WithLocation(4),
                SuppressedDiagnostic(ShapeBreakingDiagnosticSuppressor.MarkMethodsAsStaticSuppression, DiagnosticSeverity.Info).WithLocation(5),
                SuppressedDiagnostic(ShapeBreakingDiagnosticSuppressor.MarkMethodsAsStaticSuppression, DiagnosticSeverity.Info).WithLocation(6),
                SuppressedDiagnostic(ShapeBreakingDiagnosticSuppressor.MarkMethodsAsStaticSuppression, DiagnosticSeverity.Info).WithLocation(7));
        }

        [Fact]
        public async Task StatefulLinearCollectionMarshallerMethodsThatDoNotUseInstanceState_SuppressesDiagnostic()
        {
            await VerifySuppressorAsync("""
                using System;
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices.Marshalling;

                struct S
                {
                    public bool b;
                };

                [CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller<>.ManagedToUnmanagedIn))]
                [ContiguousCollectionMarshaller]
                static class Marshaller<TNative>
                {
                    public struct ManagedToUnmanagedIn
                    {
                        public void {|#0:FromManaged|}(S s) {}
                
                        public void {|#1:FromManaged|}(S s, Span<byte> buffer){}
                
                        public ManagedToUnmanagedIn {|#2:ToUnmanaged|}() => default;
                
                        public void {|#3:FromUnmanaged|}(ManagedToUnmanagedIn unmanaged) {}
                
                        public S {|#4:ToManaged|}() => default;

                        public ReadOnlySpan<int> {|#5:GetManagedValuesSource|}() => default;

                        public Span<TNative> {|#6:GetUnmanagedValuesDestination|}() => default;

                        public ReadOnlySpan<TNative> {|#7:GetUnmanagedValuesSource|}(int numElements) => default;

                        public Span<int> {|#8:GetManagedValuesDestination|}(int numElements) => default;
                    }
                }
                """,
                SuppressedDiagnostic(ShapeBreakingDiagnosticSuppressor.MarkMethodsAsStaticSuppression, DiagnosticSeverity.Info).WithLocation(0),
                SuppressedDiagnostic(ShapeBreakingDiagnosticSuppressor.MarkMethodsAsStaticSuppression, DiagnosticSeverity.Info).WithLocation(1),
                SuppressedDiagnostic(ShapeBreakingDiagnosticSuppressor.MarkMethodsAsStaticSuppression, DiagnosticSeverity.Info).WithLocation(2),
                SuppressedDiagnostic(ShapeBreakingDiagnosticSuppressor.MarkMethodsAsStaticSuppression, DiagnosticSeverity.Info).WithLocation(3),
                SuppressedDiagnostic(ShapeBreakingDiagnosticSuppressor.MarkMethodsAsStaticSuppression, DiagnosticSeverity.Info).WithLocation(4),
                SuppressedDiagnostic(ShapeBreakingDiagnosticSuppressor.MarkMethodsAsStaticSuppression, DiagnosticSeverity.Info).WithLocation(5),
                SuppressedDiagnostic(ShapeBreakingDiagnosticSuppressor.MarkMethodsAsStaticSuppression, DiagnosticSeverity.Info).WithLocation(6),
                SuppressedDiagnostic(ShapeBreakingDiagnosticSuppressor.MarkMethodsAsStaticSuppression, DiagnosticSeverity.Info).WithLocation(7),
                SuppressedDiagnostic(ShapeBreakingDiagnosticSuppressor.MarkMethodsAsStaticSuppression, DiagnosticSeverity.Info).WithLocation(8));
        }

        [Fact]
        public async Task MethodWithShapeMatchingNameButDifferingSignature_DoesNotSuppressDiagnostic()
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
                        public void {|#0:Free|}(int i) {}
                    }
                }
                """,
                Diagnostic(ShapeBreakingDiagnosticSuppressor.MarkMethodsAsStaticSuppression.SuppressedDiagnosticId, DiagnosticSeverity.Info).WithLocation(0));
        }

        [Fact]
        public async Task StatefulValueMarshallerMethodsForMarshallerInNonNestedMarshaller_DoesNotSuppressDiagnostics()
        {
            // For performance reasons, we only look on containing types to find a CustomMarshallerAttribute.
            // As a result, we miss some cases of the bad diagnostics.
            // Since we're going to recommend people make their marshallers nested types of their entry-point type,
            // this limitation isn't unreasonable. If the user isn't following our best practices, then they're going
            // to have a worse dev experience.
            await VerifySuppressorAsync("""
                using System.Runtime.InteropServices.Marshalling;

                struct S
                {
                    public bool b;
                };

                [CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToUnmanagedIn))]
                static class Marshaller
                {
                }

                public struct ManagedToUnmanagedIn
                {
                    public void {|#0:Free|}() {}
                }
                """,
                Diagnostic(ShapeBreakingDiagnosticSuppressor.MarkMethodsAsStaticSuppression.SuppressedDiagnosticId, DiagnosticSeverity.Info).WithLocation(0));
        }

        private static DiagnosticResult Diagnostic(string id, DiagnosticSeverity originalDiagnosticSeverity)
        {
            return new DiagnosticResult(id, originalDiagnosticSeverity);
        }

        private static DiagnosticResult SuppressedDiagnostic(SuppressionDescriptor descriptor, DiagnosticSeverity originalDiagnosticSeverity)
        {
            return new DiagnosticResult(descriptor.SuppressedDiagnosticId, originalDiagnosticSeverity).WithIsSuppressed(true);
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

        private class Test : CSharpCodeFixVerifier<EmptyDiagnosticAnalyzer, EmptyCodeFixProvider>.Test
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
