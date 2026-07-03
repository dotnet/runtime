// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Threading.Tasks;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using VerifyCS = ILLink.RoslynAnalyzer.Tests.CSharpCodeFixVerifier<
    ILLink.RoslynAnalyzer.UnnecessaryUnsafeModifierAnalyzer,
    ILLink.CodeFix.RemoveUnnecessaryUnsafeCodeFixProvider>;

namespace ILLink.RoslynAnalyzer.Tests
{
    public class UnnecessaryUnsafeModifierTests
    {
        internal static Solution SetOptions(Solution solution, ProjectId projectId)
        {
            var project = solution.GetProject(projectId)!;
            var parseOptions = ((CSharpParseOptions)project.ParseOptions!)
                .WithLanguageVersion(LanguageVersion.Preview)
                .WithFeatures([.. project.ParseOptions!.Features, new("updated-memory-safety-rules", "")]);
            var compilationOptions = ((CSharpCompilationOptions)project.CompilationOptions!).WithAllowUnsafe(true);
            return solution.WithProjectParseOptions(projectId, parseOptions)
                .WithProjectCompilationOptions(projectId, compilationOptions);
        }

        internal static Task VerifyCodeFix(string source, string fixedSource, params DiagnosticResult[] expected)
        {
            var test = new VerifyCS.Test { TestCode = source, FixedCode = fixedSource };
            test.SolutionTransforms.Add(SetOptions);
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", SourceText.From($"""
                is_global = true
                build_property.{MSBuildPropertyOptionNames.EnableUnsafeAnalyzer} = true
                """)));
            test.ExpectedDiagnostics.AddRange(expected);
            return test.RunAsync();
        }

        [Fact]
        public Task RemovesUnsafeFromMethodWithoutPointer() => VerifyCodeFix(
            """
            class C
            {
                static unsafe int M() => 42;
            }
            """,
            """
            class C
            {
                static int M() => 42;
            }
            """,
            VerifyCS.Diagnostic(DiagnosticId.UnnecessaryUnsafeModifier).WithSpan(3, 12, 3, 18).WithArguments("M"));

        [Fact]
        public Task KeepsUnsafeOnMethodWithPointer() => VerifyCodeFix(
            """
            class C
            {
                static unsafe int M(int* p) => 0;
            }
            """,
            """
            class C
            {
                static unsafe int M(int* p) => 0;
            }
            """);
    }
}
#endif
