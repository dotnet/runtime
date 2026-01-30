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
    ILLink.RoslynAnalyzer.DynamicallyAccessedMembersAnalyzer,
    ILLink.CodeFix.RequiresUnsafeCodeFixProvider>;

namespace ILLink.RoslynAnalyzer.Tests
{
    public class RequiresUnsafeCodeFixTests
    {
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
            test.TestState.AnalyzerConfigFiles.Add(
                ("/.editorconfig", SourceText.From(@$"
is_global = true
build_property.{MSBuildPropertyOptionNames.EnableUnsafeAnalyzer} = true")));
            // Enable unsafe code compilation
            test.SolutionTransforms.Add((solution, projectId) =>
            {
                var project = solution.GetProject(projectId)!;
                var compilationOptions = (CSharpCompilationOptions)project.CompilationOptions!;
                compilationOptions = compilationOptions.WithAllowUnsafe(true);
                return solution.WithProjectCompilationOptions(projectId, compilationOptions);
            });
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
                    [RequiresUnsafe]
                    public static int M1() => 0;

                    public void M2()
                    {
                        int x = M1();
                    }
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static int M1() => 0;

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

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe)
                        .WithSpan(10, 17, 10, 19)
                        .WithArguments("C.M1()", "", "")
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
                    [RequiresUnsafe]
                    public static void M1() { }

                    public void M2()
                    {
                        M1();
                    }
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static void M1() { }

                    public void M2()
                    {
                        // TODO(unsafe): Baselining unsafe usage
                        unsafe
                        {
                            M1();
                        }
                    }
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe)
                        .WithSpan(10, 9, 10, 11)
                        .WithArguments("C.M1()", "", "")
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
                    [RequiresUnsafe]
                    public static int M1() => 0;

                    public int M2()
                    {
                        return M1();
                    }
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static int M1() => 0;

                    public int M2()
                    {
                        // TODO(unsafe): Baselining unsafe usage
                        unsafe
                        {
                            return M1();
                        }
                    }
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe)
                        .WithSpan(10, 16, 10, 18)
                        .WithArguments("C.M1()", "", "")
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
                    [RequiresUnsafe]
                    public static bool M1() => true;

                    public void M2()
                    {
                        if (M1())
                        {
                            System.Console.WriteLine("yes");
                        }
                    }
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static bool M1() => true;

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

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe)
                        .WithSpan(10, 13, 10, 15)
                        .WithArguments("C.M1()", "", "")
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
                    [RequiresUnsafe]
                    public static int M1() => 0;

                    public void M2()
                    {
                        unsafe
                        {
                            int x = M1();
                        }
                    }
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
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
        public async Task NoWarning_InsideUnsafeMethod()
        {
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static int M1() => 0;

                    public unsafe void M2()
                    {
                        int x = M1();
                    }
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            // No diagnostics expected - method has unsafe modifier
            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: test, // No change expected
                baselineExpected: Array.Empty<DiagnosticResult>(),
                fixedExpected: Array.Empty<DiagnosticResult>());
        }

        [Fact]
        public async Task NoWarning_InsideUnsafeClass()
        {
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public unsafe class C
                {
                    [RequiresUnsafe]
                    public static int M1() => 0;

                    public void M2()
                    {
                        int x = M1();
                    }
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            // No diagnostics expected - class has unsafe modifier
            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: test, // No change expected
                baselineExpected: Array.Empty<DiagnosticResult>(),
                fixedExpected: Array.Empty<DiagnosticResult>());
        }

        [Fact]
        public async Task NoWarning_InsideUnsafeProperty()
        {
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static int M1() => 0;

                    public unsafe int P
                    {
                        get => M1();
                    }
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            // No diagnostics expected - property has unsafe modifier
            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: test, // No change expected
                baselineExpected: Array.Empty<DiagnosticResult>(),
                fixedExpected: Array.Empty<DiagnosticResult>());
        }

        [Fact]
        public async Task NoWarning_InsideUnsafeLocalFunction()
        {
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static int M1() => 0;

                    public void M2()
                    {
                        unsafe int Local() => M1();
                        _ = Local();
                    }
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            // No diagnostics expected - local function has unsafe modifier
            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: test, // No change expected
                baselineExpected: Array.Empty<DiagnosticResult>(),
                fixedExpected: Array.Empty<DiagnosticResult>());
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_ExpressionBodiedMethod()
        {
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static int M1() => 0;

                    public int M2() => M1();
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static int M1() => 0;

                    public int M2()
                    {
                        // TODO(unsafe): Baselining unsafe usage
                        unsafe
                        {
                            return M1();
                        }
                    }
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe)
                        .WithSpan(8, 24, 8, 26)
                        .WithArguments("C.M1()", "", "")
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
                    [RequiresUnsafe]
                    public static void M1() { }

                    public void M2() => M1();
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static void M1() { }

                    public void M2()
                    {
                        // TODO(unsafe): Baselining unsafe usage
                        unsafe
                        {
                            M1();
                        }
                    }
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe)
                        .WithSpan(8, 25, 8, 27)
                        .WithArguments("C.M1()", "", "")
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
                    [RequiresUnsafe]
                    public static void M1() { }

                    ~C() => M1();
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static void M1() { }

                    ~C()
                    {
                        // TODO(unsafe): Baselining unsafe usage
                        unsafe
                        {
                            M1();
                        }
                    }
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe)
                        .WithSpan(8, 13, 8, 15)
                        .WithArguments("C.M1()", "", "")
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
                    [RequiresUnsafe]
                    public static int M1() => 0;

                    public int P => M1();
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static int M1() => 0;

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

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe)
                        .WithSpan(8, 21, 8, 23)
                        .WithArguments("C.M1()", "", "")
                },
                fixedExpected: Array.Empty<DiagnosticResult>(),
                codeActionIndex: 0);
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_ExpressionBodiedAccessor()
        {
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static int M1() => 0;

                    public int P
                    {
                        get => M1();
                    }
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static int M1() => 0;

                    public int P
                    {
                        [RequiresUnsafe()]
                        get => M1();
                    }
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe)
                        .WithSpan(10, 16, 10, 18)
                        .WithArguments("C.M1()", "", "")
                },
                fixedExpected: Array.Empty<DiagnosticResult>(),
                codeActionIndex: 0);
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
                    [RequiresUnsafe]
                    public static int M1() => 0;

                    public void M2()
                    {
                        int Local() => M1();
                        _ = Local();
                    }
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static int M1() => 0;

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

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe)
                        .WithSpan(10, 24, 10, 26)
                        .WithArguments("C.M1()", "", "")
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
                    [RequiresUnsafe]
                    public static int M1() => 0;

                    public void M2()
                    {
                        int x = M1();
                        int y = x + 1;
                        System.Console.WriteLine(y);
                    }
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static int M1() => 0;

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

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe)
                        .WithSpan(10, 17, 10, 19)
                        .WithArguments("C.M1()", "", "")
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
                    [RequiresUnsafe]
                    public static int M1() => 0;

                    public void M2()
                    {
                        int x = M1();
                        int y = 42;
                        System.Console.WriteLine(y);
                    }
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static int M1() => 0;

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

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe)
                        .WithSpan(10, 17, 10, 19)
                        .WithArguments("C.M1()", "", "")
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
                    [RequiresUnsafe]
                    public static int M1() => 0;

                    public void M2()
                    {
                        var x = M1();
                        System.Console.WriteLine(x);
                    }
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static int M1() => 0;

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

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe)
                        .WithSpan(10, 17, 10, 19)
                        .WithArguments("C.M1()", "", "")
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

                    [RequiresUnsafe]
                    public static ref int M1(ref int x) => ref x;

                    public void M2()
                    {
                        ref int x = ref M1(ref _field);
                    }
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            // ref locals can't be forward-declared, so wrap the whole declaration
            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    private int _field;

                    [RequiresUnsafe]
                    public static ref int M1(ref int x) => ref x;

                    public void M2()
                    {
                        // TODO(unsafe): Baselining unsafe usage
                        unsafe
                        {
                            ref int x = ref M1(ref _field);
                        }
                    }
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe)
                        .WithSpan(12, 25, 12, 27)
                        .WithArguments("C.M1(ref Int32)", "", "")
                },
                fixedExpected: Array.Empty<DiagnosticResult>());
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_RefLocalFromUnsafeAs_NoForwardDeclaration()
        {
            // Pattern matching real-world usage: ref byte x = ref Unsafe.As<T, byte>(ref source)
            // The Unsafe.As call has [RequiresUnsafe], and the result is assigned to a ref local.
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

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }

                namespace System.Runtime.CompilerServices
                {
                    public static class Unsafe
                    {
                        [RequiresUnsafe]
                        public static ref TTo As<TFrom, TTo>(ref TFrom source) => throw null!;
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

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }

                namespace System.Runtime.CompilerServices
                {
                    public static class Unsafe
                    {
                        [RequiresUnsafe]
                        public static ref TTo As<TFrom, TTo>(ref TFrom source) => throw null!;
                    }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe)
                        .WithSpan(9, 26, 9, 47)
                        .WithArguments("System.Runtime.CompilerServices.Unsafe.As<TFrom, TTo>(ref TFrom)", "", "")
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

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }

                namespace System.Runtime.CompilerServices
                {
                    public static class Unsafe
                    {
                        [RequiresUnsafe]
                        public static ref TTo As<TFrom, TTo>(ref TFrom source) => throw null!;
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

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }

                namespace System.Runtime.CompilerServices
                {
                    public static class Unsafe
                    {
                        [RequiresUnsafe]
                        public static ref TTo As<TFrom, TTo>(ref TFrom source) => throw null!;
                        public static ref T Add<T>(ref T source, int elementOffset) => throw null!;
                    }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe)
                        .WithSpan(9, 34, 9, 57)
                        .WithArguments("System.Runtime.CompilerServices.Unsafe.As<TFrom, TTo>(ref TFrom)", "", "")
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

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }

                namespace System.Runtime.CompilerServices
                {
                    public static class Unsafe
                    {
                        [RequiresUnsafe]
                        public static ref TTo As<TFrom, TTo>(ref TFrom source) => throw null!;
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

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }

                namespace System.Runtime.CompilerServices
                {
                    public static class Unsafe
                    {
                        [RequiresUnsafe]
                        public static ref TTo As<TFrom, TTo>(ref TFrom source) => throw null!;
                        public static ref T Add<T>(ref T source, int elementOffset) => throw null!;
                    }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe)
                        .WithSpan(9, 34, 9, 57)
                        .WithArguments("System.Runtime.CompilerServices.Unsafe.As<TFrom, TTo>(ref TFrom)", "", "")
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
                    [RequiresUnsafe]
                    public static ref readonly int M1(in int x) => ref x;

                    public void M2()
                    {
                        int value = 42;
                        ref readonly int x = ref M1(in value);
                    }
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static ref readonly int M1(in int x) => ref x;

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

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe)
                        .WithSpan(11, 34, 11, 36)
                        .WithArguments("C.M1(in Int32)", "", "")
                },
                fixedExpected: Array.Empty<DiagnosticResult>());
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_NotOfferedForStatementsWithPragmaDirectives()
        {
            // When statements to wrap have #pragma directives in their leading trivia,
            // the "Wrap in unsafe block" fix should NOT be offered because it would
            // destroy the directive structure. Only the "Add attribute" fix should be available.
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static int M1() => 0;

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

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            // The fix adds [RequiresUnsafe] attribute to the method instead of wrapping in unsafe block
            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static int M1() => 0;

                    [RequiresUnsafe()]
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

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            // Use codeActionIndex: 0 since only "Add attribute" is offered (not "Wrap in unsafe block")
            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe)
                        .WithSpan(10, 17, 10, 19)
                        .WithArguments("C.M1()", "", "")
                },
                fixedExpected: Array.Empty<DiagnosticResult>(),
                codeActionIndex: 0);
        }

        [Fact]
        public async Task CodeFix_AddRequiresUnsafeAttribute_Method()
        {
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static int M1() => 0;

                    public int M2()
                    {
                        return M1();
                    }
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static int M1() => 0;

                    [RequiresUnsafe()]
                    public int M2()
                    {
                        return M1();
                    }
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            var addAttributeTest = new VerifyCS.Test
            {
                TestCode = test,
                FixedCode = fixedSource,
                CodeActionIndex = 0
            };
            addAttributeTest.ExpectedDiagnostics.Add(
                VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe)
                    .WithSpan(10, 16, 10, 18)
                    .WithArguments("C.M1()", "", ""));
            addAttributeTest.TestState.AnalyzerConfigFiles.Add(
                ("/.editorconfig", SourceText.From(@$"
is_global = true
build_property.{MSBuildPropertyOptionNames.EnableUnsafeAnalyzer} = true")));
            addAttributeTest.SolutionTransforms.Add((solution, projectId) =>
            {
                var project = solution.GetProject(projectId)!;
                var compilationOptions = (CSharpCompilationOptions)project.CompilationOptions!;
                compilationOptions = compilationOptions.WithAllowUnsafe(true);
                return solution.WithProjectCompilationOptions(projectId, compilationOptions);
            });
            await addAttributeTest.RunAsync();
        }

        [Fact]
        public async Task CodeFix_AddRequiresUnsafeAttribute_Constructor()
        {
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static void M1() { }

                    public C()
                    {
                        M1();
                    }
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static void M1() { }

                    [RequiresUnsafe()]
                    public C()
                    {
                        M1();
                    }
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            var addAttributeTest = new VerifyCS.Test
            {
                TestCode = test,
                FixedCode = fixedSource,
                CodeActionIndex = 0
            };
            addAttributeTest.ExpectedDiagnostics.Add(
                VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe)
                    .WithSpan(10, 9, 10, 11)
                    .WithArguments("C.M1()", "", ""));
            addAttributeTest.TestState.AnalyzerConfigFiles.Add(
                ("/.editorconfig", SourceText.From(@$"
is_global = true
build_property.{MSBuildPropertyOptionNames.EnableUnsafeAnalyzer} = true")));
            addAttributeTest.SolutionTransforms.Add((solution, projectId) =>
            {
                var project = solution.GetProject(projectId)!;
                var compilationOptions = (CSharpCompilationOptions)project.CompilationOptions!;
                compilationOptions = compilationOptions.WithAllowUnsafe(true);
                return solution.WithProjectCompilationOptions(projectId, compilationOptions);
            });
            await addAttributeTest.RunAsync();
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_NotOfferedForExpressionBodyWithPreprocessorDirectives()
        {
            // When an expression-bodied member has preprocessor directives (#if/#else/#endif),
            // the "Wrap in unsafe block" fix should NOT be offered because it would destroy
            // the conditional compilation structure. Only the "Add attribute" fix should be available.
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static int M1() => 0;

                    public int M2()
                #if SOME_DEFINE
                        => 42;
                #else
                        => M1();
                #endif
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            // The fix should add [RequiresUnsafe] attribute (CodeActionIndex = 0)
            // The "Wrap in unsafe block" fix (CodeActionIndex = 1) should NOT be available
            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static int M1() => 0;

                    [RequiresUnsafe()]
                    public int M2()
                #if SOME_DEFINE
                        => 42;
                #else
                        => M1();
                #endif
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            var addAttributeTest = new VerifyCS.Test
            {
                TestCode = test,
                FixedCode = fixedSource,
                CodeActionIndex = 0,
                NumberOfIncrementalIterations = 1,
                NumberOfFixAllIterations = 1
            };
            addAttributeTest.ExpectedDiagnostics.Add(
                VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe)
                    .WithSpan(12, 12, 12, 14)
                    .WithArguments("C.M1()", "", ""));
            addAttributeTest.TestState.AnalyzerConfigFiles.Add(
                ("/.editorconfig", SourceText.From(@$"
is_global = true
build_property.{MSBuildPropertyOptionNames.EnableUnsafeAnalyzer} = true")));
            addAttributeTest.SolutionTransforms.Add((solution, projectId) =>
            {
                var project = solution.GetProject(projectId)!;
                var compilationOptions = (CSharpCompilationOptions)project.CompilationOptions!;
                compilationOptions = compilationOptions.WithAllowUnsafe(true);
                return solution.WithProjectCompilationOptions(projectId, compilationOptions);
            });
            await addAttributeTest.RunAsync();
        }

        [Fact]
        public async Task CodeFix_WrapInUnsafeBlock_SwitchCaseSection()
        {
            var test = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static int M1() => 0;

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

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static int M1() => 0;

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

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe)
                        .WithSpan(13, 24, 13, 26)
                        .WithArguments("C.M1()", "", "")
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
                    [RequiresUnsafe]
                    public static int M1() => 0;

                    public int M2(bool condition)
                    {
                        if (condition)
                            return M1();
                        return 0;
                    }
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static int M1() => 0;

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

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe)
                        .WithSpan(11, 20, 11, 22)
                        .WithArguments("C.M1()", "", "")
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
                    [RequiresUnsafe]
                    public static int M1() => 0;

                    public int M2()
                    {
                        int x = 1;
                        static int LocalFunc() => M1();
                        return LocalFunc() + x;
                    }
                }

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            var fixedSource = """
                using System.Diagnostics.CodeAnalysis;

                public class C
                {
                    [RequiresUnsafe]
                    public static int M1() => 0;

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

                namespace System.Diagnostics.CodeAnalysis
                {
                    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
                    public sealed class RequiresUnsafeAttribute : Attribute { }
                }
                """;

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe)
                        .WithSpan(11, 35, 11, 37)
                        .WithArguments("C.M1()", "", "")
                },
                fixedExpected: Array.Empty<DiagnosticResult>());
        }
    }
}
#endif
