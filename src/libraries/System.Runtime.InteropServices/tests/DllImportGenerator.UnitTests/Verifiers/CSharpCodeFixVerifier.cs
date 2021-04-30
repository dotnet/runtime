
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace DllImportGenerator.UnitTests.Verifiers
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
        public static async Task VerifyCodeFixAsync(string source, string fixedSource, string? codeActionEquivalenceKey = null)
            => await VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource, codeActionEquivalenceKey);

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, DiagnosticResult, string)"/>
        public static async Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource, string? codeActionEquivalenceKey = null)
            => await VerifyCodeFixAsync(source, new[] { expected }, fixedSource, codeActionEquivalenceKey);

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, DiagnosticResult[], string)"/>
        public static async Task VerifyCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource, string? codeActionEquivalenceKey = null)
        {
            var test = new Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                CodeActionEquivalenceKey = codeActionEquivalenceKey,
                CodeActionValidationMode = CodeActionValidationMode.None,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }

        internal class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, XUnitVerifier>
        {
            public Test()
            {
                var (refAssem, ancillary) = TestUtils.GetReferenceAssemblies();
                ReferenceAssemblies = refAssem;
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
                            .AddRange(enableAnalyzersOptions));
                    solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);
                    solution = solution.WithProjectMetadataReferences(projectId, project.MetadataReferences.Concat(ImmutableArray.Create(ancillary)));
                    solution = solution.WithProjectParseOptions(projectId, ((CSharpParseOptions)project.ParseOptions!).WithLanguageVersion(LanguageVersion.Preview));
                    return solution;
                });
            }

            protected override ParseOptions CreateParseOptions()
                => ((CSharpParseOptions)base.CreateParseOptions()).WithPreprocessorSymbols("DLLIMPORTGENERATOR_ENABLED");
        }
    }
}
