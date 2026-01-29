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
    ILLink.RoslynAnalyzer.RequiresUnsafeAnalyzer,
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
            int? numberOfIterations = null)
        {
            var test = new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource
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
                        // TODO(unsafe): Baselining unsafe usage
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

            await VerifyRequiresUnsafeCodeFix(
                source: test,
                fixedSource: fixedSource,
                baselineExpected: new[] {
                    VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe)
                        .WithSpan(10, 17, 10, 21)
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
                        .WithSpan(10, 9, 10, 13)
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
                        .WithSpan(10, 16, 10, 20)
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
                        .WithSpan(10, 13, 10, 17)
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
    }
}
#endif
