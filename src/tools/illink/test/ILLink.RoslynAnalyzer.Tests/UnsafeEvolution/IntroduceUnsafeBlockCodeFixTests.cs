// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System;
using System.Threading.Tasks;
using ILLink.CodeFix.UnsafeEvolution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = ILLink.RoslynAnalyzer.Tests.CSharpCodeFixVerifier<
    ILLink.RoslynAnalyzer.DynamicallyAccessedMembersAnalyzer,
    ILLink.CodeFix.UnsafeEvolution.IntroduceUnsafeBlockCodeFixProvider>;

namespace ILLink.RoslynAnalyzer.Tests.UnsafeEvolution
{
    public class IntroduceUnsafeBlockCodeFixTests
    {
        private static Solution SetOptions(Solution solution, ProjectId projectId)
        {
            var project = solution.GetProject(projectId)!;
            var parseOptions = (CSharpParseOptions)project.ParseOptions!;
            parseOptions = parseOptions.WithLanguageVersion(LanguageVersion.Preview)
                .WithFeatures([.. parseOptions.Features, new("updated-memory-safety-rules", "")]);
            var compilationOptions = ((CSharpCompilationOptions)project.CompilationOptions!).WithAllowUnsafe(true);
            return solution.WithProjectParseOptions(projectId, parseOptions)
                .WithProjectCompilationOptions(projectId, compilationOptions);
        }

        private static Task RunAsync(
            string source,
            string fixedSource,
            DiagnosticResult[] baselineExpected,
            DiagnosticResult[]? fixedExpected = null,
            int? numberOfIterations = null)
        {
            var test = new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
            };
            test.ExpectedDiagnostics.AddRange(baselineExpected);
            test.FixedState.ExpectedDiagnostics.AddRange(fixedExpected ?? []);
            test.SolutionTransforms.Add(SetOptions);
            if (numberOfIterations is not null)
            {
                test.NumberOfIncrementalIterations = numberOfIterations;
                test.NumberOfFixAllIterations = numberOfIterations;
            }
            return test.RunAsync();
        }

        // ---- CS9362: requires-unsafe member call ----

        [Fact]
        public async Task CS9362_WrapsExpressionStatement()
        {
            string source = """
                public class C
                {
                    public static unsafe void M1() { }

                    public void M2()
                    {
                        M1();
                    }
                }
                """;

            string fixedSource = """
                public class C
                {
                    public static unsafe void M1() { }

                    public void M2()
                    {
                        unsafe
                        {
                            // SAFETY-TODO: Audit
                            M1();
                        }
                    }
                }
                """;

            await RunAsync(source, fixedSource, [
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeMemberOperation)
                    .WithSpan(7, 9, 7, 13)
                    .WithArguments("C.M1()"),
            ]);
        }

        [Fact]
        public async Task CS9362_WrapsReturnStatement()
        {
            string source = """
                public class C
                {
                    public static unsafe int M1() => 0;

                    public int M2()
                    {
                        return M1();
                    }
                }
                """;

            string fixedSource = """
                public class C
                {
                    public static unsafe int M1() => 0;

                    public int M2()
                    {
                        unsafe
                        {
                            // SAFETY-TODO: Audit
                            return M1();
                        }
                    }
                }
                """;

            await RunAsync(source, fixedSource, [
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeMemberOperation)
                    .WithSpan(7, 16, 7, 20)
                    .WithArguments("C.M1()"),
            ]);
        }

        [Fact]
        public async Task CS9362_LocalDeclaration_SplitsIntoForwardDecl()
        {
            string source = """
                public class C
                {
                    public static unsafe int M1() => 0;

                    public void M2()
                    {
                        int x = M1();
                        System.Console.WriteLine(x);
                    }
                }
                """;

            string fixedSource = """
                public class C
                {
                    public static unsafe int M1() => 0;

                    public void M2()
                    {
                        int x;
                        unsafe
                        {
                            // SAFETY-TODO: Audit
                            x = M1();
                        }
                        System.Console.WriteLine(x);
                    }
                }
                """;

            await RunAsync(source, fixedSource, [
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeMemberOperation)
                    .WithSpan(7, 17, 7, 21)
                    .WithArguments("C.M1()"),
            ]);
        }

        // ---- CS9361: uninitialized stackalloc -> Span<T> with SkipLocalsInit ----

        [Fact]
        public async Task CS9361_StackAllocToSpan_SplitsWithScopedForwardDecl()
        {
            string source = """
                using System;
                using System.Runtime.CompilerServices;

                [module: SkipLocalsInit]

                public class C
                {
                    public void M()
                    {
                        Span<byte> s = stackalloc byte[10];
                        s[0] = 1;
                    }
                }
                """;

            string fixedSource = """
                using System;
                using System.Runtime.CompilerServices;

                [module: SkipLocalsInit]

                public class C
                {
                    public void M()
                    {
                        scoped Span<byte> s;
                        unsafe
                        {
                            // SAFETY-TODO: Audit
                            s = stackalloc byte[10];
                        }
                        s[0] = 1;
                    }
                }
                """;

            await RunAsync(source, fixedSource, [
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeUninitializedStackAlloc)
                    .WithSpan(10, 24, 10, 43),
            ]);
        }

        // ---- Wrap entire method body when many unsafe operations cluster ----

        [Fact]
        public async Task ManyUnsafeOps_WrapsEntireMemberBody()
        {
            // Five unsafe operations packed into a short method body: density triggers the
            // ShouldWrapEntireBody heuristic (>=3 unsafe ops AND unsafe*4 >= statementCount).
            string source = """
                public class C
                {
                    public static unsafe int M1() => 0;

                    public int M2()
                    {
                        int a = M1();
                        int b = M1();
                        int c = M1();
                        int d = M1();
                        int e = M1();
                        return a + b + c + d + e;
                    }
                }
                """;

            string fixedSource = """
                public class C
                {
                    public static unsafe int M1() => 0;

                    public int M2()
                    {
                        unsafe
                        {
                            // SAFETY-TODO: Audit
                            int a = M1();
                            int b = M1();
                            int c = M1();
                            int d = M1();
                            int e = M1();
                            return a + b + c + d + e;
                        }
                    }
                }
                """;

            await RunAsync(source, fixedSource, [
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeMemberOperation)
                    .WithSpan(7, 17, 7, 21).WithArguments("C.M1()"),
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeMemberOperation)
                    .WithSpan(8, 17, 8, 21).WithArguments("C.M1()"),
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeMemberOperation)
                    .WithSpan(9, 17, 9, 21).WithArguments("C.M1()"),
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeMemberOperation)
                    .WithSpan(10, 17, 10, 21).WithArguments("C.M1()"),
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeMemberOperation)
                    .WithSpan(11, 17, 11, 21).WithArguments("C.M1()"),
            ]);
        }

        // ---- Embedded statement (no surrounding block) ----

        [Fact]
        public async Task EmbeddedStatement_GetsWrappingBlock()
        {
            string source = """
                public class C
                {
                    public static unsafe void M1() {}

                    public void M2(bool cond)
                    {
                        if (cond)
                            M1();
                    }
                }
                """;

            string fixedSource = """
                public class C
                {
                    public static unsafe void M1() {}

                    public void M2(bool cond)
                    {
                        if (cond)
                        {
                            unsafe
                            {
                                // SAFETY-TODO: Audit
                                M1();
                            }
                        }
                    }
                }
                """;

            await RunAsync(source, fixedSource, [
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeMemberOperation)
                    .WithSpan(8, 13, 8, 17)
                    .WithArguments("C.M1()"),
            ]);
        }

        // ---- Local declaration with 'var' resolves to explicit type for forward decl ----

        [Fact]
        public async Task CS9362_LocalDeclaration_VarResolvesToExplicitType()
        {
            string source = """
                public class C
                {
                    public static unsafe int M1() => 0;

                    public void M2()
                    {
                        var x = M1();
                        System.Console.WriteLine(x);
                    }
                }
                """;

            string fixedSource = """
                public class C
                {
                    public static unsafe int M1() => 0;

                    public void M2()
                    {
                        int x;
                        unsafe
                        {
                            // SAFETY-TODO: Audit
                            x = M1();
                        }
                        System.Console.WriteLine(x);
                    }
                }
                """;

            await RunAsync(source, fixedSource, [
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeMemberOperation)
                    .WithSpan(7, 17, 7, 21)
                    .WithArguments("C.M1()"),
            ]);
        }

        // ---- Statement surrounded by #if directives IS wrapped (directives are preserved) ----

        [Fact]
        public async Task StatementInsideIfBranch_IsWrapped()
        {
            // The #if/#endif directives sit in the leading/trailing trivia AROUND the statement,
            // not BETWEEN its tokens, so wrapping is safe - the unsafe block lands fully inside
            // the conditional region.
            string source = """
                public class C
                {
                    public static unsafe void M1() {}

                    public void M2()
                    {
                #if true
                        M1();
                #endif
                    }
                }
                """;

            string fixedSource = """
                public class C
                {
                    public static unsafe void M1() {}

                    public void M2()
                    {
                #if true
                        unsafe
                        {
                            // SAFETY-TODO: Audit
                            M1();
                        }
                #endif
                    }
                }
                """;

            await RunAsync(source, fixedSource, [
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeMemberOperation)
                    .WithSpan(8, 9, 8, 13)
                    .WithArguments("C.M1()"),
            ]);
        }

        // ---- Statement with a directive BETWEEN its tokens is skipped ----

        [Fact]
        public async Task StatementWithInternalDirective_IsSkipped()
        {
            // The #if directive is between the call's arguments, i.e. INSIDE the statement.
            // Splitting / wrapping such a statement would corrupt the directive region.
            string source = """
                public class C
                {
                    public static unsafe void M1(int a, int b) {}

                    public void M2()
                    {
                        M1(
                #if true
                            1
                #else
                            2
                #endif
                            , 3);
                    }
                }
                """;

            await RunAsync(source, source, [
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeMemberOperation)
                    .WithSpan(7, 9, 13, 17)
                    .WithArguments("C.M1(int, int)"),
            ], [
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeMemberOperation)
                    .WithSpan(7, 9, 13, 17)
                    .WithArguments("C.M1(int, int)"),
            ]);
        }

        // ---- Safety: 'using var' whose lifetime would be shortened by wrapping is skipped ----

        [Fact]
        public async Task UsingVarLocal_WhoseLifetimeWouldChange_IsSkipped()
        {
            // 'using var stream = M1();' followed by uses outside an unsafe block must not be
            // wrapped: the wrap would shrink the 'using' lifetime to the block.
            string source = """
                using System.IO;
                public class C
                {
                    public static unsafe Stream M1() => null;

                    public void M2()
                    {
                        using var stream = M1();
                        stream.Flush();
                    }
                }
                """;

            await RunAsync(source, source, [
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeMemberOperation)
                    .WithSpan(8, 28, 8, 32)
                    .WithArguments("C.M1()"),
            ], [
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeMemberOperation)
                    .WithSpan(8, 28, 8, 32)
                    .WithArguments("C.M1()"),
            ]);
        }

        // ---- Safety: 'out var' pattern variable referenced after is skipped ----

        [Fact]
        public async Task LocalWithOutVarInitializer_PatternUsedAfter_IsSkipped()
        {
            // 'int ok = M1(out var value); Use(value);' - if we forward-declare 'ok', 'value'
            // would be trapped inside the unsafe block. Bail out instead of producing bad code.
            string source = """
                public class C
                {
                    public static unsafe int M1(out int v) { v = 0; return 1; }

                    public void M2()
                    {
                        int ok = M1(out var value);
                        System.Console.WriteLine(value);
                        System.Console.WriteLine(ok);
                    }
                }
                """;

            await RunAsync(source, source, [
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeMemberOperation)
                    .WithSpan(7, 18, 7, 35)
                    .WithArguments("C.M1(out int)"),
            ], [
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeMemberOperation)
                    .WithSpan(7, 18, 7, 35)
                    .WithArguments("C.M1(out int)"),
            ]);
        }

        // ---- Whole-body wrap preserves user comments inside the body ----

        [Fact]
        public async Task ManyUnsafeOps_WrappingBody_PreservesComments()
        {
            // Five unsafe operations + interleaved user comments. The comments must survive
            // when we wrap the body in a single 'unsafe' block.
            string source = """
                public class C
                {
                    public static unsafe int M1() => 0;

                    public int M2()
                    {
                        // First comment
                        int a = M1();
                        int b = M1();
                        // Second comment
                        int c = M1();
                        int d = M1();
                        int e = M1();
                        return a + b + c + d + e;
                    }
                }
                """;

            string fixedSource = """
                public class C
                {
                    public static unsafe int M1() => 0;

                    public int M2()
                    {
                        unsafe
                        {
                            // SAFETY-TODO: Audit
                            // First comment
                            int a = M1();
                            int b = M1();
                            // Second comment
                            int c = M1();
                            int d = M1();
                            int e = M1();
                            return a + b + c + d + e;
                        }
                    }
                }
                """;

            await RunAsync(source, fixedSource, [
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeMemberOperation)
                    .WithSpan(8, 17, 8, 21).WithArguments("C.M1()"),
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeMemberOperation)
                    .WithSpan(9, 17, 9, 21).WithArguments("C.M1()"),
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeMemberOperation)
                    .WithSpan(11, 17, 11, 21).WithArguments("C.M1()"),
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeMemberOperation)
                    .WithSpan(12, 17, 12, 21).WithArguments("C.M1()"),
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeMemberOperation)
                    .WithSpan(13, 17, 13, 21).WithArguments("C.M1()"),
            ]);
        }

        // ---- Safety: diagnostic inside an expression-bodied lambda is skipped ----

        [Fact]
        public async Task DiagnosticInsideExpressionBodiedLambda_IsSkipped()
        {
            // Wrapping the outer 'Action a = ...' in unsafe would not put the lambda's body
            // in an unsafe context, so the diagnostic would persist. Bail entirely.
            string source = """
                using System;
                public class C
                {
                    public static unsafe void M1() {}

                    public void M2()
                    {
                        Action a = () => M1();
                        a();
                    }
                }
                """;

            await RunAsync(source, source, [
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeMemberOperation)
                    .WithSpan(8, 26, 8, 30)
                    .WithArguments("C.M1()"),
            ], [
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeMemberOperation)
                    .WithSpan(8, 26, 8, 30)
                    .WithArguments("C.M1()"),
            ]);
        }

        // ---- Safety: 'var x = ...' that resolves to an unnameable type and escapes is skipped ----

        [Fact]
        public async Task LocalWithAnonymousVar_UsedAfter_IsSkipped()
        {
            // 'var x = new { A = M1() }' makes x an anonymous type. We cannot forward-declare
            // such a local because the type cannot be named, and we cannot wrap-as-is because
            // that would trap 'x' inside the unsafe block while 'Use(x)' is outside.
            string source = """
                public class C
                {
                    public static unsafe int M1() => 0;

                    public void M2()
                    {
                        var x = new { A = M1() };
                        System.Console.WriteLine(x);
                    }
                }
                """;

            await RunAsync(source, source, [
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeMemberOperation)
                    .WithSpan(7, 27, 7, 31)
                    .WithArguments("C.M1()"),
            ], [
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeMemberOperation)
                    .WithSpan(7, 27, 7, 31)
                    .WithArguments("C.M1()"),
            ]);
        }

        // ---- Expression-bodied members: arrow body -> block body with unsafe wrap ----

        [Fact]
        public async Task ExpressionBodiedMethod_NonVoid_IsRewrittenToBlockWithReturn()
        {
            string source = """
                public class C
                {
                    public static unsafe int M1() => 0;

                    public int M2() => M1();
                }
                """;

            string fixedSource = """
                public class C
                {
                    public static unsafe int M1() => 0;

                    public int M2()
                    {
                        unsafe
                        {
                            // SAFETY-TODO: Audit
                            return M1();
                        }
                    }
                }
                """;

            await RunAsync(source, fixedSource, [
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeMemberOperation)
                    .WithSpan(5, 24, 5, 28)
                    .WithArguments("C.M1()"),
            ]);
        }

        [Fact]
        public async Task ExpressionBodiedMethod_Void_IsRewrittenToBlockWithExpressionStatement()
        {
            string source = """
                public class C
                {
                    public static unsafe void M1() { }

                    public void M2() => M1();
                }
                """;

            string fixedSource = """
                public class C
                {
                    public static unsafe void M1() { }

                    public void M2()
                    {
                        unsafe
                        {
                            // SAFETY-TODO: Audit
                            M1();
                        }
                    }
                }
                """;

            await RunAsync(source, fixedSource, [
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeMemberOperation)
                    .WithSpan(5, 25, 5, 29)
                    .WithArguments("C.M1()"),
            ]);
        }

        [Fact]
        public async Task ExpressionBodiedProperty_IsRewrittenToGetAccessorWithUnsafeBlock()
        {
            string source = """
                public class C
                {
                    public static unsafe int M1() => 0;

                    public int P => M1();
                }
                """;

            string fixedSource = """
                public class C
                {
                    public static unsafe int M1() => 0;

                    public int P
                    {
                        get
                        {
                            unsafe
                            {
                                // SAFETY-TODO: Audit
                                return M1();
                            }
                        }
                    }
                }
                """;

            await RunAsync(source, fixedSource, [
                DiagnosticResult.CompilerError(UnsafeEvolutionDescriptors.UnsafeMemberOperation)
                    .WithSpan(5, 21, 5, 25)
                    .WithArguments("C.M1()"),
            ]);
        }
    }
}
#endif
