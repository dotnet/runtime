// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System;
using System.Threading.Tasks;
using ILLink.Shared;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using VerifyCS = ILLink.RoslynAnalyzer.Tests.CSharpCodeFixVerifier<
    ILLink.RoslynAnalyzer.UnsafeMethodMissingRequiresUnsafeAnalyzer,
    ILLink.CodeFix.UnsafeMethodMissingRequiresUnsafeCodeFixProvider>;

namespace ILLink.RoslynAnalyzer.Tests
{
    public class UnsafeMethodMissingRequiresUnsafeTests
    {
        static readonly string RequiresUnsafeAttributeDefinition = """

            namespace System.Diagnostics.CodeAnalysis
            {
                [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                public sealed class RequiresUnsafeAttribute : Attribute { }
            }
            """;

        static Task VerifyCodeFix(
            string source,
            string fixedSource,
            DiagnosticResult[] baselineExpected,
            DiagnosticResult[] fixedExpected,
            int? numberOfIterations = null)
        {
            var test = new VerifyCS.Test {
                TestCode = source,
                FixedCode = fixedSource,
            };
            test.ExpectedDiagnostics.AddRange(baselineExpected);
            test.TestState.AnalyzerConfigFiles.Add(
                ("/.editorconfig", SourceText.From(@$"
is_global = true
build_property.{MSBuildPropertyOptionNames.EnableUnsafeAnalyzer} = true")));
            test.SolutionTransforms.Add((solution, projectId) => {
                var project = solution.GetProject(projectId)!;
                var compilationOptions = (CSharpCompilationOptions)project.CompilationOptions!;
                compilationOptions = compilationOptions.WithAllowUnsafe(true);
                return solution.WithProjectCompilationOptions(projectId, compilationOptions);
            });
            if (numberOfIterations != null) {
                test.NumberOfIncrementalIterations = numberOfIterations;
                test.NumberOfFixAllIterations = numberOfIterations;
            }
            test.FixedState.ExpectedDiagnostics.AddRange(fixedExpected);
            return test.RunAsync();
        }

        static Task VerifyNoDiagnostic(string source)
        {
            var test = new VerifyCS.Test {
                TestCode = source,
                FixedCode = source,
            };
            test.TestState.AnalyzerConfigFiles.Add(
                ("/.editorconfig", SourceText.From(@$"
is_global = true
build_property.{MSBuildPropertyOptionNames.EnableUnsafeAnalyzer} = true")));
            test.SolutionTransforms.Add((solution, projectId) => {
                var project = solution.GetProject(projectId)!;
                var compilationOptions = (CSharpCompilationOptions)project.CompilationOptions!;
                compilationOptions = compilationOptions.WithAllowUnsafe(true);
                return solution.WithProjectCompilationOptions(projectId, compilationOptions);
            });
            return test.RunAsync();
        }

        [Fact]
        public async Task MethodAlreadyAttributed_NoDiagnostic()
        {
            var source = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public unsafe void M() { }
                }
                """ + RequiresUnsafeAttributeDefinition;

            await VerifyNoDiagnostic(source);
        }

        [Fact]
        public async Task NonUnsafeMethod_NoDiagnostic()
        {
            var source = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public void M() { }
                }
                """ + RequiresUnsafeAttributeDefinition;

            await VerifyNoDiagnostic(source);
        }

        [Fact]
        public async Task UnsafeClassNonUnsafeMethod_NoDiagnostic()
        {
            var source = """
                using System.Diagnostics.CodeAnalysis;

                public unsafe class C
                {
                    public void M() { }
                }
                """ + RequiresUnsafeAttributeDefinition;

            await VerifyNoDiagnostic(source);
        }

        [Fact]
        public async Task CodeFix_UnsafeMethod_AddsAttribute()
        {
            var source = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public unsafe void M() { }
                }
                """ + RequiresUnsafeAttributeDefinition;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public unsafe void M() { }
                }
                """ + RequiresUnsafeAttributeDefinition;

            await VerifyCodeFix(
                source,
                fixedSource,
                baselineExpected: new[] {
                    VerifyCS.Diagnostic(DiagnosticId.UnsafeMethodMissingRequiresUnsafe)
                        .WithSpan(5, 24, 5, 25)
                        .WithArguments("C.M()")
                },
                fixedExpected: Array.Empty<DiagnosticResult> ());
        }

        [Fact]
        public async Task CodeFix_UnsafeConstructor_AddsAttribute()
        {
            var source = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public unsafe C() { }
                }
                """ + RequiresUnsafeAttributeDefinition;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public unsafe C() { }
                }
                """ + RequiresUnsafeAttributeDefinition;

            await VerifyCodeFix(
                source,
                fixedSource,
                baselineExpected: new[] {
                    VerifyCS.Diagnostic(DiagnosticId.UnsafeMethodMissingRequiresUnsafe)
                        .WithSpan(5, 19, 5, 20)
                        .WithArguments("C.C()")
                },
                fixedExpected: Array.Empty<DiagnosticResult> ());
        }

        [Fact]
        public async Task CodeFix_UnsafeLocalFunction_AddsAttribute()
        {
            var source = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public void M()
                    {
                        unsafe void LocalFunc() { }
                    }
                }
                """ + RequiresUnsafeAttributeDefinition;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public void M()
                    {
                        [RequiresUnsafe] unsafe void LocalFunc() { }
                    }
                }
                """ + RequiresUnsafeAttributeDefinition;

            await VerifyCodeFix(
                source,
                fixedSource,
                baselineExpected: new[] {
                    VerifyCS.Diagnostic(DiagnosticId.UnsafeMethodMissingRequiresUnsafe)
                        .WithSpan(7, 21, 7, 30)
                        .WithArguments("LocalFunc()")
                },
                fixedExpected: Array.Empty<DiagnosticResult> ());
        }
    }
}
#endif
