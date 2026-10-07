// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Threading.Tasks;
using ILLink.CodeFix;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    /// <summary>
    /// Verifies that <see cref="UnsafeContextCodeFixProvider"/> introduces audited inner unsafe contexts for the
    /// unsafe-v2 diagnostics (CS9360, CS9361, CS9362, CS9363 and CS9376).
    /// </summary>
    public partial class UnsafeContextCodeFixTests
    {
        private static Task VerifyAsync(
            string source,
            string fixedSource,
            string? equivalenceKey = null,
            bool updatedMemorySafetyRules = true,
            int? iterations = null,
            CodeFixTestBehaviors behaviors = CodeFixTestBehaviors.None)
        {
            var test = new CSharpCodeFixVerifier<DynamicallyAccessedMembersAnalyzer, UnsafeContextCodeFixProvider>.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                CodeActionEquivalenceKey = equivalenceKey,
                CodeFixTestBehaviors = behaviors,
            };
            if (iterations is not null)
            {
                test.NumberOfIncrementalIterations = iterations;
                test.NumberOfFixAllIterations = iterations;
            }

            test.SolutionTransforms.Add((solution, projectId) => SetOptions(solution, projectId, updatedMemorySafetyRules));
            return test.RunAsync();
        }

        private static Solution SetOptions(Solution solution, ProjectId projectId, bool updatedMemorySafetyRules)
        {
            var project = solution.GetProject(projectId)!;
            var parseOptions = ((CSharpParseOptions)project.ParseOptions!).WithLanguageVersion(LanguageVersion.Preview);
            if (updatedMemorySafetyRules)
                parseOptions = parseOptions.WithFeatures([.. parseOptions.Features, new("updated-memory-safety-rules", "")]);

            var compilationOptions = ((CSharpCompilationOptions)project.CompilationOptions!).WithAllowUnsafe(true);
            return solution
                .WithProjectParseOptions(projectId, parseOptions)
                .WithProjectCompilationOptions(projectId, compilationOptions);
        }

        [Fact]
        public Task ExpressionStatement()
        {
            return VerifyAsync("""
                class C
                {
                    static unsafe void M() { }

                    void Caller()
                    {
                        // Leading comment.
                        {|CS9362:M()|};
                    }
                }
                """, """
                class C
                {
                    static unsafe void M() { }

                    void Caller()
                    {
                        // Leading comment.
                        // SAFETY: To be audited
                        unsafe
                        {
                            M();
                        }
                    }
                }
                """);
        }

        [Fact]
        public Task SplitsDeclarationUsedAfterBlock()
        {
            return VerifyAsync("""
                class C
                {
                    static unsafe int M() => 0;

                    int Caller()
                    {
                        var value = {|CS9362:M()|};
                        return value + 1;
                    }
                }
                """, """
                class C
                {
                    static unsafe int M() => 0;

                    int Caller()
                    {
                        int value;
                        // SAFETY: To be audited
                        unsafe
                        {
                            value = M();
                        }
                        return value + 1;
                    }
                }
                """);
        }

        [Fact]
        public Task PointerDereferences_MergeAdjacentAndProportionateGap()
        {
            return VerifyAsync("""
                class C
                {
                    void M(int* p, int offset)
                    {
                        {|CS9360:*|}p = 1;
                        offset += 4;
                        p{|CS9360:[|}offset] = 2;
                    }
                }
                """, """
                class C
                {
                    void M(int* p, int offset)
                    {
                        // SAFETY: To be audited
                        unsafe
                        {
                            *p = 1;
                            offset += 4;
                            p[offset] = 2;
                        }
                    }
                }
                """);
        }

        [Fact]
        public Task ExpressionBodiedMember_KeepsExpressionBody()
        {
            return VerifyAsync("""
                class C
                {
                    int* _p;

                    int Value => {|CS9360:*|}_p;
                }
                """, """
                class C
                {
                    int* _p;

                    int Value => /* SAFETY: To be audited */ unsafe(*_p);
                }
                """);
        }

        [Fact]
        public Task VoidExpressionBody_ConvertsToBlockBody()
        {
            return VerifyAsync("""
                class C
                {
                    static unsafe void M() { }

                    void Caller() => {|CS9362:M()|};
                }
                """, """
                class C
                {
                    static unsafe void M() { }

                    void Caller()
                    {
                        // SAFETY: To be audited
                        unsafe
                        {
                            M();
                        }
                    }
                }
                """);
        }

        [Fact]
        public Task FieldInitializer()
        {
            return VerifyAsync("""
                class C
                {
                    static unsafe int M() => 0;

                    [System.Obsolete]
                    static int s_value = {|CS9362:M()|};
                }
                """, """
                class C
                {
                    static unsafe int M() => 0;

                    [System.Obsolete]
                    static int s_value = /* SAFETY: To be audited */ unsafe(M());
                }
                """);
        }
    }
}
#endif
