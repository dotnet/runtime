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
using VerifyUnsafeModifierMigrationCS = ILLink.RoslynAnalyzer.Tests.CSharpCodeFixVerifier<
    ILLink.RoslynAnalyzer.UnsafeMigrationAnalyzer,
    ILLink.CodeFix.UnsafeModifierMigrationCodeFixProvider>;
using VerifyUnsafeUsageMigrationCS = ILLink.RoslynAnalyzer.Tests.CSharpCodeFixVerifier<
    ILLink.RoslynAnalyzer.UnsafeMigrationAnalyzer,
    ILLink.CodeFix.UnsafeUsageMigrationCodeFixProvider>;

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

        static Solution SetConsoleOptions(Solution solution, ProjectId projectId)
        {
            solution = SetOptions(solution, projectId);
            var compilationOptions = (CSharpCompilationOptions)solution.GetProject(projectId)!.CompilationOptions!;
            return solution.WithProjectCompilationOptions(
                projectId,
                compilationOptions.WithOutputKind(OutputKind.ConsoleApplication));
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

        static void AddUnsafeMigrationConfig(VerifyUnsafeModifierMigrationCS.Test test)
        {
            test.SolutionTransforms.Add(SetOptions);
            var config = ("/.editorconfig", SourceText.From(@$"
is_global = true
build_property.{MSBuildPropertyOptionNames.EnableUnsafeMigration} = true"));
            test.TestState.AnalyzerConfigFiles.Add(config);
        }

        static void AddUnsafeMigrationConfig(
            VerifyUnsafeUsageMigrationCS.Test test,
            bool consoleApplication = false)
        {
            test.SolutionTransforms.Add(consoleApplication ? SetConsoleOptions : SetOptions);
            var config = ("/.editorconfig", SourceText.From(@$"
is_global = true
build_property.{MSBuildPropertyOptionNames.EnableUnsafeMigration} = true"));
            test.TestState.AnalyzerConfigFiles.Add(config);
        }

        static Task VerifyUnsafeModifierMigrationCodeFix(
            string source,
            string fixedSource,
            DiagnosticResult[] baselineExpected,
            DiagnosticResult[] fixedExpected,
            int? numberOfIterations = null)
        {
            var test = new VerifyUnsafeModifierMigrationCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource
            };
            test.ExpectedDiagnostics.AddRange(baselineExpected);
            AddUnsafeMigrationConfig(test);
            if (numberOfIterations != null)
            {
                test.NumberOfIncrementalIterations = numberOfIterations;
                test.NumberOfFixAllIterations = numberOfIterations;
            }
            test.FixedState.ExpectedDiagnostics.AddRange(fixedExpected);
            return test.RunAsync();
        }

        static Task VerifyUnsafeUsageMigrationCodeFix(
            string source,
            string fixedSource,
            DiagnosticResult[] baselineExpected,
            DiagnosticResult[] fixedExpected,
            int? numberOfIterations = null,
            bool consoleApplication = false)
        {
            var test = new VerifyUnsafeUsageMigrationCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource
            };
            test.ExpectedDiagnostics.AddRange(baselineExpected);
            AddUnsafeMigrationConfig(test, consoleApplication);
            if (numberOfIterations != null)
            {
                test.NumberOfIncrementalIterations = numberOfIterations;
                test.NumberOfFixAllIterations = numberOfIterations;
            }
            test.FixedState.ExpectedDiagnostics.AddRange(fixedExpected);
            return test.RunAsync();
        }

        static DiagnosticResult UnsafeModifierMigrationDiagnostic()
            => VerifyUnsafeModifierMigrationCS.Diagnostic(DiagnosticDescriptors.GetDiagnosticDescriptor(
                DiagnosticId.UnsafeModifierMigration,
                diagnosticSeverity: DiagnosticSeverity.Info));

        static DiagnosticResult UnsafeUsageMigrationDiagnostic()
            => VerifyUnsafeUsageMigrationCS.Diagnostic(DiagnosticDescriptors.GetDiagnosticDescriptor(
                DiagnosticId.UnsafeUsageMigration,
                diagnosticSeverity: DiagnosticSeverity.Info));

        [Fact]
        public void UnsafeMigrationDiagnostics_HaveUnsafeCategoryWithoutHelpLinks()
        {
            foreach (DiagnosticId diagnosticId in new[]
            {
                DiagnosticId.UnsafeModifierMigration,
                DiagnosticId.UnsafeUsageMigration
            })
            {
                DiagnosticDescriptor descriptor = DiagnosticDescriptors.GetDiagnosticDescriptor(diagnosticId);
                Assert.Equal("Unsafe", descriptor.Category);
                Assert.Empty(descriptor.HelpLinkUri);
            }
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

        [Fact]
        public async Task UnsafeModifierMigration_RemovesUnsafeFromTypeDeclarations()
        {
            var source = """
                {|#0:unsafe|} class DangerousClass
                {
                }

                unsafe struct DangerousStruct
                {
                }

                unsafe interface IDangerous
                {
                }

                unsafe record DangerousRecord;

                unsafe record struct DangerousRecordStruct;

                public unsafe delegate void DangerousDelegate();

                public class WithStaticConstructor
                {
                    static unsafe WithStaticConstructor()
                    {
                    }
                }

                public class WithDestructor
                {
                    unsafe ~WithDestructor()
                    {
                    }
                }
                """;

            var fixedSource = """
                class DangerousClass
                {
                }

                struct DangerousStruct
                {
                }

                interface IDangerous
                {
                }

                record DangerousRecord;

                record struct DangerousRecordStruct;

                public delegate void DangerousDelegate();

                public class WithStaticConstructor
                {
                    static WithStaticConstructor()
                    {
                    }
                }

                public class WithDestructor
                {
                    ~WithDestructor()
                    {
                    }
                }
                """;

            await VerifyUnsafeModifierMigrationCodeFix(
                source,
                fixedSource,
                baselineExpected: [
                    UnsafeModifierMigrationDiagnostic().WithLocation(0)
                ],
                fixedExpected: []);
        }

        [Fact]
        public async Task UnsafeModifierMigration_RemovesLegacyMemberUnsafeAndPreservesDocumentedOrPointerSignatures()
        {
            var source = """
                public class C
                {
                    public {|#0:unsafe|} void RemovableMethod() { }

                    public void Host()
                    {
                        unsafe void RemovableLocal()
                        {
                        }
                    }

                    public unsafe int RemovableProperty => 0;

                    public unsafe event System.Action RemovableEvent
                    {
                        add { }
                        remove { }
                    }

                    // <safety>This is not XML documentation.</safety>
                    public unsafe void CommentOnlySafetyTag() { }

                    /// <safety>Documented.</safety>
                    public unsafe void DocumentedMethod() { }

                    public unsafe void PointerArray(delegate* unmanaged<int>[] callbacks) { }

                    public unsafe delegate* unmanaged<int> FunctionPointerProperty => default;
                }
                """;

            var fixedSource = """
                public class C
                {
                    public void RemovableMethod() { }

                    public void Host()
                    {
                        void RemovableLocal()
                        {
                        }
                    }

                    public int RemovableProperty => 0;

                    public event System.Action RemovableEvent
                    {
                        add { }
                        remove { }
                    }

                    // <safety>This is not XML documentation.</safety>
                    public void CommentOnlySafetyTag() { }

                    /// <safety>Documented.</safety>
                    public unsafe void DocumentedMethod() { }

                    public unsafe void PointerArray(delegate* unmanaged<int>[] callbacks) { }

                    public unsafe delegate* unmanaged<int> FunctionPointerProperty => default;
                }
                """;

            await VerifyUnsafeModifierMigrationCodeFix(
                source,
                fixedSource,
                baselineExpected: [
                    UnsafeModifierMigrationDiagnostic().WithLocation(0)
                ],
                fixedExpected: []);
        }

        [Fact]
        public async Task UnsafeModifierMigration_PreservesMultiEventImplementationContract()
        {
            var source = """
                using System;

                public interface IEvents
                {
                    /// <safety>Documented.</safety>
                    unsafe event Action E1;

                    /// <safety>Documented.</safety>
                    unsafe event Action E2;
                }

                public class C : IEvents
                {
                    public unsafe event Action E1, E2;
                }
                """;

            await VerifyUnsafeModifierMigrationCodeFix(
                source,
                source,
                baselineExpected: [],
                fixedExpected: []);
        }

        [Fact]
        public async Task UnsafeUsageMigration_AddsUnsafeAndSafetyDocsToInteropDeclarations()
        {
            var source = """
                using System.Runtime.InteropServices;

                public static partial class Native
                {
                    {|#0:public static extern int PInvoke();|}

                    [LibraryImport("Native")]
                    static partial void LibraryImport();

                    public static safe extern int SafeExtern();

                    public static unsafe extern int UnsafeExtern();
                }

                [StructLayout(LayoutKind.Explicit)]
                public struct ExplicitLayout
                {
                    [FieldOffset(0)]
                    public int Value;
                }
                """;

            var fixedSource = """
                using System.Runtime.InteropServices;

                public static partial class Native
                {
                    /// <safety>TODO: Audit.</safety>
                    public static unsafe extern int PInvoke();

                    /// <safety>TODO: Audit.</safety>
                    [LibraryImport("Native")]
                    static unsafe partial void LibraryImport();

                    /// <safety>TODO: Audit.</safety>
                    public static safe extern int SafeExtern();

                    /// <safety>TODO: Audit.</safety>
                    public static unsafe extern int UnsafeExtern();
                }

                [StructLayout(LayoutKind.Explicit)]
                public struct ExplicitLayout
                {
                    /// <safety>TODO: Audit.</safety>
                    [FieldOffset(0)]
                    public unsafe int Value;
                }
                """;

            await VerifyUnsafeUsageMigrationCodeFix(
                source,
                fixedSource,
                baselineExpected: [
                    UnsafeUsageMigrationDiagnostic().WithLocation(0),
                    DiagnosticResult.CompilerError("CS9389").WithSpan(5, 19, 5, 25),
                    DiagnosticResult.CompilerError("CS9392").WithSpan(19, 16, 19, 21)
                ],
                fixedExpected: []);
        }

        [Fact]
        public async Task UnsafeUsageMigration_DoesNotFlagPointerValues()
        {
            var source = """
                public class C
                {
                    public int* Field;

                    public int* Identity(int* value) => value;

                    public int* ReadField() => Field;
                }
                """;

            await VerifyUnsafeUsageMigrationCodeFix(
                source,
                source,
                baselineExpected: [],
                fixedExpected: []);
        }

        [Fact]
        public async Task UnsafeUsageMigration_PropagatesUnsafeToMatchingPartialDeclarations()
        {
            var source = """
                public partial class C
                {
                    /// <safety>Documented.</safety>
                    public unsafe partial void M();

                    {|#0:public partial void M() { }|}
                }
                """;

            var fixedSource = """
                public partial class C
                {
                    /// <safety>Documented.</safety>
                    public unsafe partial void M();

                    public unsafe partial void M() { }
                }
                """;

            await VerifyUnsafeUsageMigrationCodeFix(
                source,
                fixedSource,
                baselineExpected: [
                    UnsafeUsageMigrationDiagnostic().WithSpan(6, 5, 6, 32),
                    DiagnosticResult.CompilerError("CS0764").WithSpan(6, 25, 6, 26)
                ],
                fixedExpected: []);
        }

        [Fact]
        public async Task UnsafeUsageMigration_PropagatesContractsWithoutTouchingSafeImplementations()
        {
            var source = """
                using System;

                public class UnsafeBase
                {
                    /// <safety>Documented.</safety>
                    public virtual unsafe int BaseMethod() => 0;
                }

                public class Derived : UnsafeBase
                {
                    {|#0:public override int BaseMethod() => 1;|}
                }

                public interface IUnsafeContract
                {
                    /// <safety>Documented.</safety>
                    unsafe int M();
                }

                public class ImplicitImpl : IUnsafeContract
                {
                    public int M() => 1;
                }

                public class ExplicitImpl : IUnsafeContract
                {
                    int IUnsafeContract.M() => 2;
                }

                public interface IAccessorContract
                {
                    int Value
                    {
                        unsafe get;
                        set;
                    }
                }

                public class AccessorImpl : IAccessorContract
                {
                    public int Value
                    {
                        get
                        {
                            return 0;
                        }

                        set
                        {
                        }
                    }
                }

                public interface IEventContract
                {
                    /// <safety>Documented.</safety>
                    unsafe event Action E;
                }

                public class EventImpl : IEventContract
                {
                    public event Action E;
                }

                public interface ISafeContract
                {
                    void Shared();
                }

                public interface IUnsafeOnlyContract
                {
                    /// <safety>Documented.</safety>
                    unsafe void Shared();
                }

                public class MixedImpl : ISafeContract, IUnsafeOnlyContract
                {
                    public void Shared()
                    {
                    }
                }
                """;

            var fixedSource = """
                using System;

                public class UnsafeBase
                {
                    /// <safety>Documented.</safety>
                    public virtual unsafe int BaseMethod() => 0;
                }

                public class Derived : UnsafeBase
                {
                    public override unsafe int BaseMethod() => 1;
                }

                public interface IUnsafeContract
                {
                    /// <safety>Documented.</safety>
                    unsafe int M();
                }

                public class ImplicitImpl : IUnsafeContract
                {
                    public unsafe int M() => 1;
                }

                public class ExplicitImpl : IUnsafeContract
                {
                    unsafe int IUnsafeContract.M() => 2;
                }

                public interface IAccessorContract
                {
                    int Value
                    {
                        unsafe get;
                        set;
                    }
                }

                public class AccessorImpl : IAccessorContract
                {
                    public int Value
                    {
                        unsafe get
                        {
                            return 0;
                        }

                        set
                        {
                        }
                    }
                }

                public interface IEventContract
                {
                    /// <safety>Documented.</safety>
                    unsafe event Action E;
                }

                public class EventImpl : IEventContract
                {
                    public unsafe event Action E;
                }

                public interface ISafeContract
                {
                    void Shared();
                }

                public interface IUnsafeOnlyContract
                {
                    /// <safety>Documented.</safety>
                    unsafe void Shared();
                }

                public class MixedImpl : ISafeContract, IUnsafeOnlyContract
                {
                    public void Shared()
                    {
                    }
                }
                """;

            await VerifyUnsafeUsageMigrationCodeFix(
                source,
                fixedSource,
                baselineExpected: [
                    UnsafeUsageMigrationDiagnostic().WithLocation(0)
                ],
                fixedExpected: []);
        }

        [Fact]
        public async Task UnsafeUsageMigration_UsesUnsafeExpressionsForScopeSensitiveSites()
        {
            var source = """
                using System;
                using System.IO;
                using System.Threading.Tasks;

                public interface IValueProvider
                {
                    /// <safety>Documented.</safety>
                    unsafe int Value();
                }

                public sealed class ValueProvider : IValueProvider
                {
                    {|#0:public int Value() => 1;|}
                }

                public interface IResourceProvider
                {
                    /// <safety>Documented.</safety>
                    unsafe IDisposable Open();
                }

                public sealed class ResourceProvider : IResourceProvider
                {
                    public IDisposable Open() => new MemoryStream();
                }

                public class C
                {
                    private readonly int _field = new ValueProvider(/* keep */).Value();

                    public C() : this(new ValueProvider().Value())
                    {
                    }

                    public C(int value)
                    {
                    }

                    public async Task<int> AwaitAsync()
                        => await Task.FromResult(new ValueProvider().Value());

                    public int ExpressionBodied()
                        => new ValueProvider().Value();

                    public int LocalFunction()
                    {
                        int Local() => new ValueProvider().Value();
                        return Local();
                    }

                    public Func<int> Lambda()
                        => () => new ValueProvider().Value();

                    public int DirectiveWrapped()
                    {
                #line 100
                        int value = new ValueProvider().Value();
                #line default
                        return value;
                    }

                    public int CatchFilter()
                    {
                        try
                        {
                            throw new Exception();
                        }
                        catch (Exception) when (new ValueProvider().Value() > 0)
                        {
                            return 1;
                        }
                    }

                    public int UsingDeclaration()
                    {
                        using var resource = new ResourceProvider().Open();
                        return 1;
                    }
                }
                """;

            var fixedSource = """
                using System;
                using System.IO;
                using System.Threading.Tasks;

                public interface IValueProvider
                {
                    /// <safety>Documented.</safety>
                    unsafe int Value();
                }

                public sealed class ValueProvider : IValueProvider
                {
                    public unsafe int Value() => 1;
                }

                public interface IResourceProvider
                {
                    /// <safety>Documented.</safety>
                    unsafe IDisposable Open();
                }

                public sealed class ResourceProvider : IResourceProvider
                {
                    public unsafe IDisposable Open() => new MemoryStream();
                }

                public class C
                {
                    private readonly int _field = unsafe(/* SAFETY: Audit */new ValueProvider(/* keep */).Value());

                    public C() : this(unsafe(/* SAFETY: Audit */new ValueProvider().Value()))
                    {
                    }

                    public C(int value)
                    {
                    }

                    public async Task<int> AwaitAsync()
                        => await Task.FromResult(unsafe(/* SAFETY: Audit */new ValueProvider().Value()));

                    public int ExpressionBodied()
                        => unsafe(/* SAFETY: Audit */new ValueProvider().Value());

                    public int LocalFunction()
                    {
                        int Local() => unsafe(/* SAFETY: Audit */new ValueProvider().Value());
                        return Local();
                    }

                    public Func<int> Lambda()
                        => () => unsafe(/* SAFETY: Audit */new ValueProvider().Value());

                    public int DirectiveWrapped()
                    {
                #line 100
                        int value = unsafe(/* SAFETY: Audit */new ValueProvider().Value());
                #line default
                        return value;
                    }

                    public int CatchFilter()
                    {
                        try
                        {
                            throw new Exception();
                        }
                        catch (Exception) when (unsafe(/* SAFETY: Audit */new ValueProvider().Value()) > 0)
                        {
                            return 1;
                        }
                    }

                    public int UsingDeclaration()
                    {
                        using var resource = unsafe(/* SAFETY: Audit */new ResourceProvider().Open());
                        return 1;
                    }
                }
                """;

            await VerifyUnsafeUsageMigrationCodeFix(
                source,
                fixedSource,
                baselineExpected: [
                    UnsafeUsageMigrationDiagnostic().WithLocation(0)
                ],
                fixedExpected: []);
        }

        [Fact]
        public async Task UnsafeUsageMigration_DoesNotWrapAwaitInsideUnsafeExpression()
        {
            var source = """
                using System.Threading.Tasks;

                public class C
                {
                    /// <safety>Documented.</safety>
                    private static unsafe int Unsafe(int value) => value;

                    /// <safety>Documented.</safety>
                    private static unsafe int Unsafe2() => 2;

                    public static async Task<int> M()
                    {
                        int first = {|#0:Unsafe(await Task.FromResult(1))|};
                        int second = {|#1:Unsafe2()|};
                        return first + second;
                    }
                }
                """;

            var fixedSource = """
                using System.Threading.Tasks;

                public class C
                {
                    /// <safety>Documented.</safety>
                    private static unsafe int Unsafe(int value) => value;

                    /// <safety>Documented.</safety>
                    private static unsafe int Unsafe2() => 2;

                    public static async Task<int> M()
                    {
                        int first = {|#0:Unsafe(await Task.FromResult(1))|};
                        int second;
                        unsafe
                        {
                            // SAFETY: Audit
                            second = Unsafe2();
                        }
                        return first + second;
                    }
                }
                """;

            await VerifyUnsafeUsageMigrationCodeFix(
                source,
                fixedSource,
                baselineExpected: [
                    UnsafeUsageMigrationDiagnostic().WithLocation(0),
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithLocation(0)
                        .WithArguments("C.Unsafe(int)"),
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithLocation(1)
                        .WithArguments("C.Unsafe2()")
                ],
                fixedExpected: [
                    UnsafeUsageMigrationDiagnostic().WithLocation(0),
                    DiagnosticResult.CompilerError(RequiresUnsafeCodeFixProvider.UnsafeMemberOperationDiagnosticId)
                        .WithLocation(0)
                        .WithArguments("C.Unsafe(int)")
                ],
                numberOfIterations: 2);
        }

        [Fact]
        public async Task UnsafeUsageMigration_SplitsRefLikeLocalsIntoScopedForwardDeclarations()
        {
            var source = """
                using System;
                using System.Runtime.CompilerServices;

                public class C
                {
                    [SkipLocalsInit]
                    public int Sum()
                    {
                        Span<int> span = {|#0:stackalloc int[4]|};
                        return span.Length;
                    }
                }
                """;

            var fixedSource = """
                using System;
                using System.Runtime.CompilerServices;

                public class C
                {
                    [SkipLocalsInit]
                    public int Sum()
                    {
                        scoped Span<int> span;
                        unsafe
                        {
                            // SAFETY: Audit
                            span = stackalloc int[4];
                        }
                        return span.Length;
                    }
                }
                """;

            await VerifyUnsafeUsageMigrationCodeFix(
                source,
                fixedSource,
                baselineExpected: [
                    UnsafeUsageMigrationDiagnostic().WithLocation(0),
                    DiagnosticResult.CompilerError("CS9361").WithLocation(0)
                ],
                fixedExpected: []);
        }

        [Fact]
        public async Task UnsafeUsageMigration_PreservesCommentsInLocalInitializer()
        {
            var source = """
                using System;
                using System.Runtime.CompilerServices;

                public class C
                {
                    [SkipLocalsInit]
                    public int Sum()
                    {
                        Span<int> span = {|#0:stackalloc int[4 /* keep */]|};
                        return span.Length;
                    }
                }
                """;

            var fixedSource = """
                using System;
                using System.Runtime.CompilerServices;

                public class C
                {
                    [SkipLocalsInit]
                    public int Sum()
                    {
                        Span<int> span = unsafe(/* SAFETY: Audit */stackalloc int[4 /* keep */]);
                        return span.Length;
                    }
                }
                """;

            await VerifyUnsafeUsageMigrationCodeFix(
                source,
                fixedSource,
                baselineExpected: [
                    UnsafeUsageMigrationDiagnostic().WithLocation(0),
                    DiagnosticResult.CompilerError("CS9361").WithLocation(0)
                ],
                fixedExpected: []);
        }

        [Fact]
        public async Task UnsafeUsageMigration_UsesExpressionWhenRefLikeLocalFlowsToAnotherSpan()
        {
            var source = """
                using System;
                using System.Runtime.CompilerServices;

                public class C
                {
                    [SkipLocalsInit]
                    public int Length()
                    {
                        scoped ReadOnlySpan<int> source;
                        {
                            Span<int> stackSpan = {|#0:stackalloc int[4]|};
                            source = stackSpan;
                        }
                        return source.Length;
                    }
                }
                """;

            var fixedSource = """
                using System;
                using System.Runtime.CompilerServices;

                public class C
                {
                    [SkipLocalsInit]
                    public int Length()
                    {
                        scoped ReadOnlySpan<int> source;
                        {
                            Span<int> stackSpan = unsafe(/* SAFETY: Audit */stackalloc int[4]);
                            source = stackSpan;
                        }
                        return source.Length;
                    }
                }
                """;

            await VerifyUnsafeUsageMigrationCodeFix(
                source,
                fixedSource,
                baselineExpected: [
                    UnsafeUsageMigrationDiagnostic().WithLocation(0),
                    DiagnosticResult.CompilerError("CS9361").WithLocation(0)
                ],
                fixedExpected: []);
        }

        [Fact]
        public async Task UnsafeUsageMigration_HandlesTopLevelForwardDeclaration()
        {
            var source = """
                using System;
                using System.Runtime.CompilerServices;

                [module: SkipLocalsInit]

                Span<int> span = {|#0:stackalloc int[4]|};
                Console.WriteLine(span.Length);
                """;

            var fixedSource = """
                using System;
                using System.Runtime.CompilerServices;

                [module: SkipLocalsInit]

                scoped Span<int> span;

                unsafe
                {
                    // SAFETY: Audit
                    span = stackalloc int[4];
                }
                Console.WriteLine(span.Length);
                """;

            await VerifyUnsafeUsageMigrationCodeFix(
                source,
                fixedSource,
                baselineExpected: [
                    UnsafeUsageMigrationDiagnostic().WithLocation(0),
                    DiagnosticResult.CompilerError("CS9361").WithLocation(0)
                ],
                fixedExpected: [],
                consoleApplication: true);
        }

        [Fact]
        public async Task UnsafeUsageMigration_WrapsWholeBodiesForMultipleSafeSites()
        {
            var source = """
                using System;

                public interface ICounter
                {
                    /// <safety>Documented.</safety>
                    unsafe int Next();
                }

                public sealed class Counter : ICounter
                {
                    {|#0:public int Next() => 1;|}
                }

                public class C
                {
                    public int WrapWholeBody()
                    {
                        int Local() => new Counter().Next();
                        Func<int> lambda = () => new Counter().Next();
                        return new Counter().Next() + Local() + lambda();
                    }
                }
                """;

            var fixedSource = """
                using System;

                public interface ICounter
                {
                    /// <safety>Documented.</safety>
                    unsafe int Next();
                }

                public sealed class Counter : ICounter
                {
                    public unsafe int Next() => 1;
                }

                public class C
                {
                    public int WrapWholeBody()
                    {
                        unsafe
                        {
                            // SAFETY: Audit
                            int Local() => new Counter().Next();
                            Func<int> lambda = () => new Counter().Next();
                            return new Counter().Next() + Local() + lambda();
                        }
                    }
                }
                """;

            await VerifyUnsafeUsageMigrationCodeFix(
                source,
                fixedSource,
                baselineExpected: [
                    UnsafeUsageMigrationDiagnostic().WithLocation(0)
                ],
                fixedExpected: []);
        }
    }
}
#endif
