// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace LibraryImportGenerator.UnitTests.Verifiers
{
    public static class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.Diagnostic()"/>
        public static DiagnosticResult Diagnostic()
            => CodeFixVerifier<TAnalyzer, TCodeFix>.Diagnostic();

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.Diagnostic(string)"/>
        public static DiagnosticResult Diagnostic(string diagnosticId)
            => CodeFixVerifier<TAnalyzer, TCodeFix>.Diagnostic(diagnosticId);

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.Diagnostic(DiagnosticDescriptor)"/>
        public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
            => CodeFixVerifier<TAnalyzer, TCodeFix>.Diagnostic(descriptor);

        /// <summary>
        /// Create a <see cref="DiagnosticResult"/> with the diagnostic message created with the provided arguments.
        /// A <see cref="DiagnosticResult"/> with the <see cref="DiagnosticResult.Message"/> property set instead of just the <see cref="DiagnosticResult.MessageArguments"/> property
        /// binds more strongly to the "correct" diagnostic as the test harness will match the diagnostic on the exact message instead of just on the message arguments.
        /// </summary>
        /// <param name="descriptor">The diagnostic descriptor</param>
        /// <param name="arguments">The arguments to use to format the diagnostic message</param>
        /// <returns>A <see cref="DiagnosticResult"/> with a <see cref="DiagnosticResult.Message"/> set with the <paramref name="descriptor"/>'s message format and the <paramref name="arguments"/>.</returns>
        public static DiagnosticResult DiagnosticWithArguments(DiagnosticDescriptor descriptor, params object[] arguments)
        {
            // Generate the specific message here to ensure a stronger match with the correct diagnostic.
            return Diagnostic(descriptor).WithMessage(string.Format(descriptor.MessageFormat.ToString(), arguments)).WithArguments(arguments);
        }

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyAnalyzerAsync(string, DiagnosticResult[])"/>
        public static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new Test
            {
                TestCode = source,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, string)"/>
        public static async Task VerifyCodeFixAsync(string source, string fixedSource, string? fixEquivalenceKey = null)
            => await VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource, fixEquivalenceKey);

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, DiagnosticResult, string)"/>
        public static async Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource, string? fixEquivalenceKey = null)
            => await VerifyCodeFixAsync(source, new[] { expected }, fixedSource, fixEquivalenceKey);

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, DiagnosticResult[], string)"/>
        public static async Task VerifyCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource, string? fixEquivalenceKey = null)
        {
            var test = new Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                CodeActionEquivalenceKey = fixEquivalenceKey,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, DiagnosticResult[], string)"/>
        public static async Task VerifyCodeFixAsync(string source, string fixedSource, params DiagnosticResult[] expected)
        {
            var test = new Test
            {
                TestCode = source,
                FixedCode = fixedSource,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }

        public static async Task VerifyCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource,
            int numIncrementalIterations, int numFixAllIterations)
        {
            var test = new Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                NumberOfIncrementalIterations = numIncrementalIterations,
                NumberOfFixAllIterations = numFixAllIterations
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }

        internal class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, XUnitVerifier>
        {
            public Test()
            {
                // Clear out the default reference assemblies. We explicitly add references from the live ref pack,
                // so we don't want the Roslyn test infrastructure to resolve/add any default reference assemblies
                ReferenceAssemblies = new ReferenceAssemblies(string.Empty);
                TestState.AdditionalReferences.AddRange(SourceGenerators.Tests.LiveReferencePack.GetMetadataReferences());
                TestState.AdditionalReferences.Add(TestUtils.GetAncillaryReference());

                SolutionTransforms.Add((solution, projectId) =>
                {
                    var project = solution.GetProject(projectId)!;
                    var compilationOptions = project.CompilationOptions!;
                    var diagnosticOptions = compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings);

                    // Explicitly enable diagnostics that are not enabled by default
                    var enableAnalyzersOptions = new System.Collections.Generic.Dictionary<string, ReportDiagnostic>();
                    foreach (var analyzer in GetDiagnosticAnalyzers().ToImmutableArray())
                    {
                        foreach (var diagnostic in analyzer.SupportedDiagnostics)
                        {
                            if (diagnostic.IsEnabledByDefault)
                                continue;

                            // Map the default severity to the reporting behaviour.
                            // We cannot simply use ReportDiagnostic.Default here, as diagnostics that are not enabled by default
                            // are treated as suppressed (regardless of their default severity).
                            var report = diagnostic.DefaultSeverity switch
                            {
                                DiagnosticSeverity.Error => ReportDiagnostic.Error,
                                DiagnosticSeverity.Warning => ReportDiagnostic.Warn,
                                DiagnosticSeverity.Info => ReportDiagnostic.Info,
                                DiagnosticSeverity.Hidden => ReportDiagnostic.Hidden,
                                _ => ReportDiagnostic.Default
                            };
                            enableAnalyzersOptions.Add(diagnostic.Id, report);
                        }
                    }

                    compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(
                        compilationOptions.SpecificDiagnosticOptions
                            .SetItems(CSharpVerifierHelper.NullableWarnings)
                            .AddRange(enableAnalyzersOptions)
                            .AddRange(TestUtils.BindingRedirectWarnings));
                    solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);
                    solution = solution.WithProjectParseOptions(projectId, ((CSharpParseOptions)project.ParseOptions!).WithLanguageVersion(LanguageVersion.Preview));
                    return solution;
                });
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
                        reportSuppressedDiagnostics: false,
                        analyzerExceptionFilter: ex =>
                        {
                            // We're hunting down a intermittent issue that causes NullReferenceExceptions deep in Roslyn. To ensure that we get an actionable dump, we're going to FailFast here to force a process dump.
                            if (ex is NullReferenceException)
                            {
                                // Break a debugger here so there's a chance to investigate if someone is already attached.
                                if (System.Diagnostics.Debugger.IsAttached)
                                {
                                    System.Diagnostics.Debugger.Break();
                                }
                                Environment.FailFast($"Encountered a NullReferenceException while running an analyzer. Taking the process down to get an actionable crash dump. Exception information:{ex.ToString()}");
                            }
                            return true;
                        }));
            }
        }
    }
}
