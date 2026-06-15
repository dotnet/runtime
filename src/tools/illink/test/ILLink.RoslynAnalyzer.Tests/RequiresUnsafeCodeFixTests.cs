// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System;
using System.Threading.Tasks;
using ILLink.CodeFix;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using VerifyCS = ILLink.RoslynAnalyzer.Tests.CSharpCodeFixVerifier<
    ILLink.RoslynAnalyzer.DynamicallyAccessedMembersAnalyzer,
    ILLink.CodeFix.RequiresUnsafeCodeFixProvider>;

namespace ILLink.RoslynAnalyzer.Tests
{
    public class RequiresUnsafeCodeFixTests
    {
        static Solution SetOptions(Solution solution, ProjectId projectId)
        {
            var project = solution.GetProject(projectId)!;
            var parseOptions = (CSharpParseOptions)project.ParseOptions!;
            parseOptions = parseOptions.WithLanguageVersion(LanguageVersion.Preview)
                .WithFeatures([.. parseOptions.Features, new("updated-memory-safety-rules", "")]);
            var compilationOptions = (CSharpCompilationOptions)project.CompilationOptions!;
            compilationOptions = compilationOptions.WithAllowUnsafe(true);
            return solution.WithProjectParseOptions(projectId, parseOptions)
                .WithProjectCompilationOptions(projectId, compilationOptions);
        }

        static Task VerifyRequiresUnsafeCodeFix(
            string source,
            string fixedSource,
            DiagnosticResult[] baselineExpected,
            DiagnosticResult[] fixedExpected,
            int? numberOfIterations = null,
            int codeActionIndex = 1)
        {
            var test = new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                CodeActionIndex = codeActionIndex
            };
            test.ExpectedDiagnostics.AddRange(baselineExpected);
            test.SolutionTransforms.Add(SetOptions);
            if (numberOfIterations != null)
            {
                test.NumberOfIncrementalIterations = numberOfIterations;
                test.NumberOfFixAllIterations = numberOfIterations;
            }
            test.FixedState.ExpectedDiagnostics.AddRange(fixedExpected);
            return test.RunAsync();
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_SimpleStatement()
        {
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public void M2()
                    {
                        int x = M1();
                    }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public void M2()
                    {
                        int x;
                        // TODO(unsafe): Baselining unsafe usage
                        unsafe
                        {
                            x = M1();
                        }
                    }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithSpan(9, 17, 9, 21)
                        .WithArguments("C.M1()")
                },
                fixedExpected: Array.Empty<DiagnosticResult>());
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_ExpressionStatement()
        {
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe void M1() { }

                    public void M2()
                    {
                        M1();
                    }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe void M1() { }

                    public void M2()
                    {
                        // TODO(unsafe): Baselining unsafe usage
                        unsafe
                        {
                            M1();
                        }
                    }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithSpan(9, 9, 9, 13)
                        .WithArguments("C.M1()")
                },
                fixedExpected: Array.Empty<DiagnosticResult>());
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_ReturnStatement()
        {
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public int M2()
                    {
                        return M1();
                    }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public int M2()
                    {
                        // TODO(unsafe): Baselining unsafe usage
                        unsafe
                        {
                            return M1();
                        }
                    }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithSpan(9, 16, 9, 20)
                        .WithArguments("C.M1()")
                },
                fixedExpected: Array.Empty<DiagnosticResult>());
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_IfStatement()
        {
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe bool M1() => true;

                    public void M2()
                    {
                        if (M1())
                        {
                            System.Console.WriteLine("yes");
                        }
                    }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe bool M1() => true;

                    public void M2()
                    {
                        // TODO(unsafe): Baselining unsafe usage
                        unsafe
                        {
                            if (M1())
                        {
                            System.Console.WriteLine("yes");
                            }
                        }
                    }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithSpan(9, 13, 9, 17)
                        .WithArguments("C.M1()")
                },
                fixedExpected: Array.Empty<DiagnosticResult>());
        }

        [Fact]
        public async Task NoWarning_InsideUnsafeBlock()
        {
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public void M2()
                    {
                        unsafe
                        {
                            int x = M1();
                        }
                    }
                }
                """;

            // No diagnostics expected - already in unsafe block
            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: test, // No change expected
                baselineExpected: Array.Empty<DiagnosticResult>(),
                fixedExpected: Array.Empty<DiagnosticResult>());
        }

        [Fact]
        public async Task CodeFix_InsideUnsafeMethod()
        {
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public unsafe void M2()
                    {
                        int x = M1();
                    }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public unsafe void M2()
                    {
                        int x;
                        // TODO(unsafe): Baselining unsafe usage
                        unsafe
                        {
                            x = M1();
                        }
                    }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithSpan(9, 17, 9, 21)
                        .WithArguments("C.M1()")
                },
                fixedExpected: Array.Empty<DiagnosticResult>(),
                codeActionIndex: 0);
        }

        [Fact]
        public async Task CodeFix_InsideUnsafeClass()
        {
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public unsafe class C
                {
                    public static unsafe int M1() => 0;

                    public void M2()
                    {
                        int x = M1();
                    }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public unsafe class C
                {
                    public static unsafe int M1() => 0;

                    public void M2()
                    {
                        int x;
                        // TODO(unsafe): Baselining unsafe usage
                        unsafe
                        {
                            x = M1();
                        }
                    }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithSpan(9, 17, 9, 21)
                        .WithArguments("C.M1()")
                },
                fixedExpected: Array.Empty<DiagnosticResult>());
        }

        [Fact]
        public async Task CodeFix_InsideUnsafeProperty()
        {
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public unsafe int P
                    {
                        get => M1();
                    }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public unsafe int P
                    {
                        get
                        {
                            // TODO(unsafe): Baselining unsafe usage
                            unsafe
                            {
                                return M1();
                            }
                        }
                    }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithSpan(9, 16, 9, 20)
                        .WithArguments("C.M1()")
                },
                fixedExpected: Array.Empty<DiagnosticResult>(),
                codeActionIndex: 1);
        }

        [Fact]
        public async Task CodeFix_InsideUnsafeLocalFunction()
        {
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public void M2()
                    {
                        unsafe int Local() => M1();

                        _ = Local();
                    }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public unsafe void M2()
                    {
                        unsafe int Local()
                        {
                            // TODO(unsafe): Baselining unsafe usage
                            unsafe
                            {
                                return M1();
                            }
                        }
                        // TODO(unsafe): Baselining unsafe usage

                        unsafe
                        {
                            _ = Local();
                        }
                    }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithSpan(9, 31, 9, 35)
                        .WithArguments("C.M1()"),
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithSpan(11, 13, 11, 20)
                        .WithArguments("Local()")
                },
                fixedExpected: Array.Empty<DiagnosticResult>(),
                numberOfIterations: 3,
                codeActionIndex: 0);
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_ExpressionBodiedMethod()
        {
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public int M2() => M1();
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public int M2()
                    {
                        // TODO(unsafe): Baselining unsafe usage
                        unsafe
                        {
                            return M1();
                        }
                    }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithSpan(7, 24, 7, 28)
                        .WithArguments("C.M1()")
                },
                fixedExpected: Array.Empty<DiagnosticResult>());
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_ExpressionBodiedVoidMethod()
        {
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe void M1() { }

                    public void M2() => M1();
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe void M1() { }

                    public void M2()
                    {
                        // TODO(unsafe): Baselining unsafe usage
                        unsafe
                        {
                            M1();
                        }
                    }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithSpan(7, 25, 7, 29)
                        .WithArguments("C.M1()")
                },
                fixedExpected: Array.Empty<DiagnosticResult>());
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_ExpressionBodiedDestructor()
        {
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe void M1() { }

                    ~C() => M1();
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe void M1() { }

                    ~C()
                    {
                        // TODO(unsafe): Baselining unsafe usage
                        unsafe
                        {
                            M1();
                        }
                    }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithSpan(7, 13, 7, 17)
                        .WithArguments("C.M1()")
                },
                fixedExpected: Array.Empty<DiagnosticResult>());
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_ExpressionBodiedProperty()
        {
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public int P => M1();
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public int P
                    {
                        get
                        {
                            // TODO(unsafe): Baselining unsafe usage
                            unsafe
                            {
                                return M1();
                            }
                        }
                    }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithSpan(7, 21, 7, 25)
                        .WithArguments("C.M1()")
                },
                fixedExpected: Array.Empty<DiagnosticResult>(),
                codeActionIndex: 1);
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_ExpressionBodiedAccessor()
        {
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public int P
                    {
                        get => M1();
                    }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public int P
                    {
                        get
                        {
                            // TODO(unsafe): Baselining unsafe usage
                            unsafe
                            {
                                return M1();
                            }
                        }
                    }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithSpan(9, 16, 9, 20)
                        .WithArguments("C.M1()")
                },
                fixedExpected: Array.Empty<DiagnosticResult>(),
                codeActionIndex: 1);
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_LocalFunction_ConvertsExpressionBody()
        {
            // Local functions with expression bodies are converted to block bodies
            // with the unsafe block inside, preserving their scope.
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public void M2()
                    {
                        int Local() => M1();
                        _ = Local();
                    }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public void M2()
                    {
                        int Local()
                        {
                            // TODO(unsafe): Baselining unsafe usage
                            unsafe
                            {
                                return M1();
                            }
                        }

                        _ = Local();
                    }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithSpan(9, 24, 9, 28)
                        .WithArguments("C.M1()")
                },
                fixedExpected: Array.Empty<DiagnosticResult>());
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_ForwardDeclaration()
        {
            // When a variable is declared with unsafe code and used later,
            // use forward declaration instead of expanding the unsafe block.
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public void M2()
                    {
                        int x = M1();
                        int y = x + 1;
                        System.Console.WriteLine(y);
                    }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public void M2()
                    {
                        int x;
                        // TODO(unsafe): Baselining unsafe usage
                        unsafe
                        {
                            x = M1();
                        }
                        int y = x + 1;
                        System.Console.WriteLine(y);
                    }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithSpan(9, 17, 9, 21)
                        .WithArguments("C.M1()")
                },
                fixedExpected: Array.Empty<DiagnosticResult>());
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_UnusedVariable()
        {
            // When a variable declared with unsafe code is not used after,
            // forward declaration still applies consistently.
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public void M2()
                    {
                        int x = M1();
                        int y = 42;
                        System.Console.WriteLine(y);
                    }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public void M2()
                    {
                        int x;
                        // TODO(unsafe): Baselining unsafe usage
                        unsafe
                        {
                            x = M1();
                        }
                        int y = 42;
                        System.Console.WriteLine(y);
                    }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithSpan(9, 17, 9, 21)
                        .WithArguments("C.M1()")
                },
                fixedExpected: Array.Empty<DiagnosticResult>());
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_ForwardDeclaration_VarType()
        {
            // When using 'var', the forward declaration should use the explicit type.
            var test = """
                using System.Diagnostics.CodeAnalysis;

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

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public void M2()
                    {
                        int x;
                        // TODO(unsafe): Baselining unsafe usage
                        unsafe
                        {
                            x = M1();
                        }
                        System.Console.WriteLine(x);
                    }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithSpan(9, 17, 9, 21)
                        .WithArguments("C.M1()")
                },
                fixedExpected: Array.Empty<DiagnosticResult>());
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_RefLocal_NoForwardDeclaration()
        {
            // Ref locals cannot use forward declaration (can't declare without initializer),
            // so wrap the entire declaration in the unsafe block.
            // Note: If the ref local is used after the declaration, those uses will be broken
            // and need manual fixing. This test has no usage after the declaration.
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    private int _field;

                    public static unsafe ref int M1(ref int x) => ref x;

                    public void M2()
                    {
                        ref int x = ref M1(ref _field);
                    }
                }
                """;

            // ref locals can't be forward-declared, so wrap the whole declaration
            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    private int _field;

                    public static unsafe ref int M1(ref int x) => ref x;

                    public void M2()
                    {
                        // TODO(unsafe): Baselining unsafe usage
                        unsafe
                        {
                            ref int x = ref M1(ref _field);
                        }
                    }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithSpan(11, 25, 11, 39)
                        .WithArguments("C.M1(ref int)")
                },
                fixedExpected: Array.Empty<DiagnosticResult>());
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_RefLocalFromUnsafeAs_NoForwardDeclaration()
        {
            // Pattern matching real-world usage: ref byte x = ref Unsafe.As<T, byte>(ref source)
            // The Unsafe.As call is unsafe, and the result is assigned to a ref local.
            // When the ref local is used after the declaration, those statements must be included
            // in the unsafe block since ref locals can't be forward-declared.
            var test = """
                using System.Diagnostics.CodeAnalysis;
                using System.Runtime.CompilerServices;

                public class C
                {
                    public void M2()
                    {
                        char c = 'x';
                        ref byte x = ref Unsafe.As<char, byte>(ref c);
                        int y = x + 1;
                    }
                }

                namespace System.Runtime.CompilerServices
                {
                    public static class Unsafe
                    {
                        public static unsafe ref TTo As<TFrom, TTo>(ref TFrom source) => throw null!;
                    }
                }
                """;

            // ref locals can't be forward-declared, so must expand block to include usages
            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;
                using System.Runtime.CompilerServices;

                public class C
                {
                    public void M2()
                    {
                        char c = 'x';
                        // TODO(unsafe): Baselining unsafe usage
                        unsafe
                        {
                            ref byte x = ref Unsafe.As<char, byte>(ref c);
                            int y = x + 1;
                        }
                    }
                }

                namespace System.Runtime.CompilerServices
                {
                    public static class Unsafe
                    {
                        public static unsafe ref TTo As<TFrom, TTo>(ref TFrom source) => throw null!;
                    }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithSpan(9, 26, 9, 54)
                        .WithArguments("System.Runtime.CompilerServices.Unsafe.As<char, byte>(ref char)")
                },
                fixedExpected: Array.Empty<DiagnosticResult>());
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_ChainedRefLocals_ExpandsToIncludeAll()
        {
            // When a ref local is used to create another ref local, and that second ref local
            // is used later, the unsafe block must expand to include ALL usages.
            // This matches the pattern in CharUnicodeInfo.cs where rsStart is used to create rsDelta.
            var test = """
                using System.Diagnostics.CodeAnalysis;
                using System.Runtime.CompilerServices;

                public class C
                {
                    public void M2()
                    {
                        byte b = 0;
                        ref ushort rsStart = ref Unsafe.As<byte, ushort>(ref b);
                        ref ushort rsDelta = ref Unsafe.Add(ref rsStart, 1);
                        int delta = rsDelta;
                    }
                }

                namespace System.Runtime.CompilerServices
                {
                    public static class Unsafe
                    {
                        public static unsafe ref TTo As<TFrom, TTo>(ref TFrom source) => throw null!;
                        public static ref T Add<T>(ref T source, int elementOffset) => throw null!;
                    }
                }
                """;

            // Both ref locals and the usage of rsDelta must be inside the unsafe block
            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;
                using System.Runtime.CompilerServices;

                public class C
                {
                    public void M2()
                    {
                        byte b = 0;
                        // TODO(unsafe): Baselining unsafe usage
                        unsafe
                        {
                            ref ushort rsStart = ref Unsafe.As<byte, ushort>(ref b);
                            ref ushort rsDelta = ref Unsafe.Add(ref rsStart, 1);
                            int delta = rsDelta;
                        }
                    }
                }

                namespace System.Runtime.CompilerServices
                {
                    public static class Unsafe
                    {
                        public static unsafe ref TTo As<TFrom, TTo>(ref TFrom source) => throw null!;
                        public static ref T Add<T>(ref T source, int elementOffset) => throw null!;
                    }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithSpan(9, 34, 9, 64)
                        .WithArguments("System.Runtime.CompilerServices.Unsafe.As<byte, ushort>(ref byte)")
                },
                fixedExpected: Array.Empty<DiagnosticResult>());
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_RefLocal_RegularVarEscapes_ExpandsToIncludeUsage()
        {
            // When a ref local block expansion pulls in a regular variable declaration,
            // and that regular variable is used after the block, we must expand further.
            // This matches the CharUnicodeInfo.cs pattern.
            var test = """
                using System.Diagnostics.CodeAnalysis;
                using System.Runtime.CompilerServices;

                public class C
                {
                    public int M2()
                    {
                        byte b = 0;
                        ref ushort rsStart = ref Unsafe.As<byte, ushort>(ref b);
                        ref ushort rsDelta = ref Unsafe.Add(ref rsStart, 1);
                        int delta = rsDelta;
                        return delta + 1;
                    }
                }

                namespace System.Runtime.CompilerServices
                {
                    public static class Unsafe
                    {
                        public static unsafe ref TTo As<TFrom, TTo>(ref TFrom source) => throw null!;
                        public static ref T Add<T>(ref T source, int elementOffset) => throw null!;
                    }
                }
                """;

            // Block must expand to include return statement since delta is used there
            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;
                using System.Runtime.CompilerServices;

                public class C
                {
                    public int M2()
                    {
                        byte b = 0;
                        // TODO(unsafe): Baselining unsafe usage
                        unsafe
                        {
                            ref ushort rsStart = ref Unsafe.As<byte, ushort>(ref b);
                            ref ushort rsDelta = ref Unsafe.Add(ref rsStart, 1);
                            int delta = rsDelta;
                            return delta + 1;
                        }
                    }
                }

                namespace System.Runtime.CompilerServices
                {
                    public static class Unsafe
                    {
                        public static unsafe ref TTo As<TFrom, TTo>(ref TFrom source) => throw null!;
                        public static ref T Add<T>(ref T source, int elementOffset) => throw null!;
                    }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithSpan(9, 34, 9, 64)
                        .WithArguments("System.Runtime.CompilerServices.Unsafe.As<byte, ushort>(ref byte)")
                },
                fixedExpected: Array.Empty<DiagnosticResult>());
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_RefReadonlyLocal_NoForwardDeclaration()
        {
            // Ref readonly locals cannot use forward declaration, so wrap the declaration.
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe ref readonly int M1(in int x) => ref x;

                    public void M2()
                    {
                        int value = 42;
                        ref readonly int x = ref M1(in value);
                    }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe ref readonly int M1(in int x) => ref x;

                    public void M2()
                    {
                        int value = 42;
                        // TODO(unsafe): Baselining unsafe usage
                        unsafe
                        {
                            ref readonly int x = ref M1(in value);
                        }
                    }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithSpan(10, 34, 10, 46)
                        .WithArguments("C.M1(in int)")
                },
                fixedExpected: Array.Empty<DiagnosticResult>());
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_NotOfferedForStatementsWithPragmaDirectives()
        {
            // When statements to wrap have #pragma directives after the diagnostic statement,
            // the unsafe block should not include the directive trivia.
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public void M2()
                    {
                        int x = M1();
                #pragma warning disable CS0168
                        if (x > 0)
                #pragma warning restore CS0168
                        {
                        }
                    }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public unsafe void M2()
                    {
                        int x;
                        // TODO(unsafe): Baselining unsafe usage
                        unsafe
                        {
                            x = M1();
                        }
                #pragma warning disable CS0168
                        if (x > 0)
                #pragma warning restore CS0168
                        {
                        }
                    }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithSpan(9, 17, 9, 21)
                        .WithArguments("C.M1()")
                },
                fixedExpected: Array.Empty<DiagnosticResult>(),
                numberOfIterations: 2,
                codeActionIndex: 0);
        }

        [Fact]
        public async Task CodeFix_AddUnsafeModifier_Method()
        {
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public int M2()
                    {
                        return M1();
                    }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public unsafe int M2()
                    {
                        // TODO(unsafe): Baselining unsafe usage
                        unsafe
                        {
                            return M1();
                        }
                    }
                }
                """;

            var addUnsafeTest = new VerifyCS.Test
            {
                TestCode = test,
                FixedCode = fixedSource,
                CodeActionIndex = 0,
                NumberOfIncrementalIterations = 2,
                NumberOfFixAllIterations = 2
            };
            addUnsafeTest.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                    .WithSpan(9, 16, 9, 20)
                    .WithArguments("C.M1()"));
            addUnsafeTest.SolutionTransforms.Add(SetOptions);
            await addUnsafeTest.RunAsync();
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_Method()
        {
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public int M2()
                    {
                        return M1();
                    }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public int M2()
                    {
                        // TODO(unsafe): Baselining unsafe usage
                        unsafe
                        {
                            return M1();
                        }
                    }
                }
                """;

            var addAttributeTest = new VerifyCS.Test
            {
                TestCode = test,
                FixedCode = fixedSource,
                CodeActionIndex = 1
            };
            addAttributeTest.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                    .WithSpan(9, 16, 9, 20)
                    .WithArguments("C.M1()"));
            addAttributeTest.SolutionTransforms.Add(SetOptions);
            await addAttributeTest.RunAsync();
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_Constructor()
        {
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe void M1() { }

                    public C()
                    {
                        M1();
                    }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe void M1() { }

                    public C()
                    {
                        // TODO(unsafe): Baselining unsafe usage
                        unsafe
                        {
                            M1();
                        }
                    }
                }
                """;

            var addAttributeTest = new VerifyCS.Test
            {
                TestCode = test,
                FixedCode = fixedSource,
                CodeActionIndex = 1
            };
            addAttributeTest.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                    .WithSpan(9, 9, 9, 13)
                    .WithArguments("C.M1()"));
            addAttributeTest.SolutionTransforms.Add(SetOptions);
            await addAttributeTest.RunAsync();
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_NotOfferedForExpressionBodyWithPreprocessorDirectives()
        {
            // When an expression-bodied member has preprocessor directives (#if/#else/#endif),
            // the "Wrap in unsafe block" fix should NOT be offered because it would destroy
            // the conditional compilation structure.
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public int M2()
                #if SOME_DEFINE
                        => 42;
                #else
                        => M1();
                #endif
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public unsafe int M2()
                #if SOME_DEFINE
                        => 42;
                #else
                        => M1();
                #endif
                }
                """;

            // The "Wrap in unsafe block" fix should NOT be available.

            var addAttributeTest = new VerifyCS.Test
            {
                TestCode = test,
                FixedCode = fixedSource,
                CodeActionIndex = 0,
                NumberOfIncrementalIterations = 1,
                NumberOfFixAllIterations = 1
            };
            addAttributeTest.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                    .WithSpan(11, 12, 11, 16)
                    .WithArguments("C.M1()"));
            addAttributeTest.FixedState.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                    .WithSpan(11, 12, 11, 16)
                    .WithArguments("C.M1()"));
            addAttributeTest.SolutionTransforms.Add(SetOptions);
            await addAttributeTest.RunAsync();
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_SwitchCaseSection()
        {
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public int M2(int x)
                    {
                        switch (x)
                        {
                            case 1:
                                return M1();
                            default:
                                return 0;
                        }
                    }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public int M2(int x)
                    {
                        switch (x)
                        {
                            case 1:
                                // TODO(unsafe): Baselining unsafe usage
                                unsafe
                                {
                                    return M1();
                                }
                            default:
                                return 0;
                        }
                    }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithSpan(12, 24, 12, 28)
                        .WithArguments("C.M1()")
                },
                fixedExpected: Array.Empty<DiagnosticResult>());
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_IfStatementWithoutBraces()
        {
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public int M2(bool condition)
                    {
                        if (condition)
                            return M1();
                        return 0;
                    }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public int M2(bool condition)
                    {
                        if (condition)
                        {
                            // TODO(unsafe): Baselining unsafe usage
                            unsafe
                            {
                                return M1();
                            }
                        }
                        return 0;
                    }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithSpan(10, 20, 10, 24)
                        .WithArguments("C.M1()")
                },
                fixedExpected: Array.Empty<DiagnosticResult>());
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_ExpressionBodiedLocalFunction()
        {
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public int M2()
                    {
                        int x = 1;
                        static int LocalFunc() => M1();
                        return LocalFunc() + x;
                    }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    public static unsafe int M1() => 0;

                    public int M2()
                    {
                        int x = 1;
                        static int LocalFunc()
                        {
                            // TODO(unsafe): Baselining unsafe usage
                            unsafe
                            {
                                return M1();
                            }
                        }

                        return LocalFunc() + x;
                    }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithSpan(10, 35, 10, 39)
                        .WithArguments("C.M1()")
                },
                fixedExpected: Array.Empty<DiagnosticResult>());
        }
    }
}
#endif
