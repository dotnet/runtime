// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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

namespace Microsoft.Interop.UnitTests.Verifiers
{
    public static class CSharpSourceGeneratorVerifier<TSourceGenerator>
        where TSourceGenerator : new()
    {
        public static DiagnosticResult Diagnostic(string diagnosticId)
            => new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error);

        public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
            => new DiagnosticResult(descriptor);

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

        public static async Task VerifySourceGeneratorAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new Test(referenceAncillaryInterop: false)
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }

        public static async Task VerifySourceGeneratorWithAncillaryInteropAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new Test(referenceAncillaryInterop: true)
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }

        public static async Task VerifySourceGeneratorAsync(string[] sources, params DiagnosticResult[] expected)
        {
            var test = new Test(referenceAncillaryInterop: false)
            {
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            };

            foreach (var source in sources)
            {
                test.TestState.Sources.Add(source);
            }

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }

        internal class Test : CSharpSourceGeneratorTest<TSourceGenerator, XUnitVerifier>
        {
            public Test(TestTargetFramework targetFramework)
            {
                if (targetFramework == TestTargetFramework.Net)
                {
                    // Clear out the default reference assemblies. We explicitly add references from the live ref pack,
                    // so we don't want the Roslyn test infrastructure to resolve/add any default reference assemblies
                    ReferenceAssemblies = new ReferenceAssemblies(string.Empty);
                    TestState.AdditionalReferences.AddRange(SourceGenerators.Tests.LiveReferencePack.GetMetadataReferences());
                }
                else
                {
                    ReferenceAssemblies = targetFramework switch
                    {
                        TestTargetFramework.Framework => ReferenceAssemblies.NetFramework.Net48.Default,
                        TestTargetFramework.Standard => ReferenceAssemblies.NetStandard.NetStandard21,
                        TestTargetFramework.Core => ReferenceAssemblies.NetCore.NetCoreApp31,
                        TestTargetFramework.Net6 => ReferenceAssemblies.Net.Net60,
                        _ => ReferenceAssemblies.Default
                    };
                }
                SolutionTransforms.Add(CSharpVerifierHelper.GetTargetFrameworkAnalyzerOptionsProviderTransform(targetFramework));
            }
            public Test(bool referenceAncillaryInterop)
                :this(TestTargetFramework.Net)
            {
                if (referenceAncillaryInterop)
                {
                    TestState.AdditionalReferences.Add(TestUtils.GetAncillaryReference());
                }

                SolutionTransforms.Add(CSharpVerifierHelper.GetAllDiagonsticsEnabledTransform(GetDiagnosticAnalyzers()));
            }

            protected override CompilationWithAnalyzers CreateCompilationWithAnalyzers(Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerOptions options, CancellationToken cancellationToken)
            {
                return new CompilationWithAnalyzers(
                    compilation,
                    analyzers,
                    new CompilationWithAnalyzersOptions(
                        options,
                        onAnalyzerException: null,
                        concurrentAnalysis: !Debugger.IsAttached,
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

            protected override ParseOptions CreateParseOptions()
            {
                return new CSharpParseOptions(LanguageVersion.Preview, DocumentationMode.Diagnose);
            }

            protected async override Task<(Compilation compilation, ImmutableArray<Diagnostic> generatorDiagnostics)> GetProjectCompilationAsync(Project project, IVerifier verifier, CancellationToken cancellationToken)
            {
                var (compilation, diagnostics) = await base.GetProjectCompilationAsync(project, verifier, cancellationToken);
                VerifyFinalCompilation(compilation);
                return (compilation, diagnostics);
            }

            /// <summary>
            /// Verify any expected invariants on the final compilation after the source generators have been applied.
            /// </summary>
            /// <param name="compilation">The compilation.</param>
            /// <remarks>
            /// This function is useful for basic semantic testing of the generated code and can be used instead of verification testing of an exact match to the expected source output.
            /// </remarks>
            protected virtual void VerifyFinalCompilation(Compilation compilation)
            {
            }
        }
    }
}
