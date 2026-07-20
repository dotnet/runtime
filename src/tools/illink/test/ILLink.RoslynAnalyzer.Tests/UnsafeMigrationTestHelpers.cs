// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace ILLink.RoslynAnalyzer.Tests
{
    /// <summary>
    /// Creates analyzer and code-fix test projects configured for the updated memory-safety rules.
    /// The shared setup enables compiler diagnostics such as <c>CS9377</c>, <c>CS9389</c>, and <c>CS9392</c>.
    /// </summary>
    internal static class UnsafeMigrationTestHelpers
    {
        internal static CSharpAnalyzerTest<TAnalyzer, DefaultVerifier> CreateAnalyzerTest<TAnalyzer>(string source)
            where TAnalyzer : DiagnosticAnalyzer, new()
        {
            var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = new ReferenceAssemblies(string.Empty),
            };
            test.TestState.AdditionalReferences.AddRange(SourceGenerators.Tests.LiveReferencePack.GetMetadataReferences());
            test.SolutionTransforms.Add(SetOptions);
            return test;
        }

        internal static CSharpCodeFixVerifier<TAnalyzer, TCodeFix>.Test CreateCodeFixTest<TAnalyzer, TCodeFix>(
            string source,
            string fixedSource)
            where TAnalyzer : DiagnosticAnalyzer, new()
            where TCodeFix : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, new()
        {
            var test = new CSharpCodeFixVerifier<TAnalyzer, TCodeFix>.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
            };
            test.SolutionTransforms.Add(SetOptions);
            return test;
        }

        internal static CSharpCodeFixVerifier<TAnalyzer, TCodeFix>.Test CreateCodeFixTest<TAnalyzer, TCodeFix>(
            string source)
            where TAnalyzer : DiagnosticAnalyzer, new()
            where TCodeFix : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, new()
        {
            var test = new CSharpCodeFixVerifier<TAnalyzer, TCodeFix>.Test
            {
                TestCode = source,
            };
            test.SolutionTransforms.Add(SetOptions);
            return test;
        }

        internal static Solution SetOptions(Solution solution, ProjectId projectId)
        {
            var project = solution.GetProject(projectId)!;
            var parseOptions = (CSharpParseOptions)project.ParseOptions!;
            parseOptions = parseOptions.WithLanguageVersion(LanguageVersion.Preview)
                .WithFeatures([.. parseOptions.Features, new("updated-memory-safety-rules", "")]);

            var compilationOptions = (CSharpCompilationOptions)project.CompilationOptions!;
            // CS9377 is emitted at a warning level above the test framework's default.
            compilationOptions = compilationOptions
                .WithAllowUnsafe(true)
                .WithWarningLevel(999);

            return solution.WithProjectParseOptions(projectId, parseOptions)
                .WithProjectCompilationOptions(projectId, compilationOptions);
        }
    }
}
#endif
