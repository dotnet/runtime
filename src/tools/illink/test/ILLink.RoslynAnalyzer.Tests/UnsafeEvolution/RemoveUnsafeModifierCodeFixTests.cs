// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Threading.Tasks;
using ILLink.CodeFix.UnsafeEvolution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = ILLink.RoslynAnalyzer.Tests.CSharpCodeFixVerifier<
    ILLink.CodeFix.UnsafeEvolution.UnsafeEvolutionAnalyzer,
    ILLink.CodeFix.UnsafeEvolution.RemoveUnsafeModifierCodeFixProvider>;

namespace ILLink.RoslynAnalyzer.Tests.UnsafeEvolution
{
    public class RemoveUnsafeModifierCodeFixTests
    {
        private static Solution SetOptions(Solution solution, ProjectId projectId)
        {
            var project = solution.GetProject(projectId)!;
            var compilationOptions = ((CSharpCompilationOptions)project.CompilationOptions!).WithAllowUnsafe(true);
            return solution.WithProjectCompilationOptions(projectId, compilationOptions);
        }

        private static Task RunAsync(string source, string fixedSource, params DiagnosticResult[] expected)
        {
            var test = new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
            };
            test.ExpectedDiagnostics.AddRange(expected);
            test.SolutionTransforms.Add(SetOptions);
            return test.RunAsync();
        }

        [Fact]
        public async Task Removes_UnsafeOn_Class()
        {
            string source = """
                public unsafe class C
                {
                }
                """;

            string fixedSource = """
                public class C
                {
                }
                """;

            await RunAsync(source, fixedSource,
                VerifyCS.Diagnostic(UnsafeEvolutionDescriptors.MeaninglessUnsafeModifier)
                    .WithSpan(1, 8, 1, 14)
                    .WithArguments("class", "C"));
        }

        [Fact]
        public async Task Removes_UnsafeOn_Struct()
        {
            string source = """
                public unsafe struct S {}
                """;

            string fixedSource = """
                public struct S {}
                """;

            await RunAsync(source, fixedSource,
                VerifyCS.Diagnostic(UnsafeEvolutionDescriptors.MeaninglessUnsafeModifier)
                    .WithSpan(1, 8, 1, 14)
                    .WithArguments("struct", "S"));
        }

        [Fact]
        public async Task Removes_UnsafeOn_RecordStruct()
        {
            string source = """
                public unsafe record struct R(int X);
                """;

            string fixedSource = """
                public record struct R(int X);
                """;

            await RunAsync(source, fixedSource,
                VerifyCS.Diagnostic(UnsafeEvolutionDescriptors.MeaninglessUnsafeModifier)
                    .WithSpan(1, 8, 1, 14)
                    .WithArguments("record struct", "R"));
        }

        [Fact]
        public async Task Removes_UnsafeOn_Delegate()
        {
            string source = """
                public unsafe delegate void D();
                """;

            string fixedSource = """
                public delegate void D();
                """;

            await RunAsync(source, fixedSource,
                VerifyCS.Diagnostic(UnsafeEvolutionDescriptors.MeaninglessUnsafeModifier)
                    .WithSpan(1, 8, 1, 14)
                    .WithArguments("delegate", "D"));
        }

        [Fact]
        public async Task Removes_UnsafeOn_StaticConstructor()
        {
            string source = """
                public class C
                {
                    static unsafe C() {}
                }
                """;

            string fixedSource = """
                public class C
                {
                    static C() {}
                }
                """;

            await RunAsync(source, fixedSource,
                VerifyCS.Diagnostic(UnsafeEvolutionDescriptors.MeaninglessUnsafeModifier)
                    .WithSpan(3, 12, 3, 18)
                    .WithArguments("static constructor", "C"));
        }

        [Fact]
        public async Task Removes_UnsafeOn_Destructor()
        {
            string source = """
                public class C
                {
                    unsafe ~C() {}
                }
                """;

            string fixedSource = """
                public class C
                {
                    ~C() {}
                }
                """;

            await RunAsync(source, fixedSource,
                VerifyCS.Diagnostic(UnsafeEvolutionDescriptors.MeaninglessUnsafeModifier)
                    .WithSpan(3, 5, 3, 11)
                    .WithArguments("destructor", "C"));
        }

        [Fact]
        public async Task Removes_UnsafeOn_MethodWithoutPointerSignature()
        {
            string source = """
                public class C
                {
                    public unsafe void M() {}
                }
                """;

            string fixedSource = """
                public class C
                {
                    public void M() {}
                }
                """;

            await RunAsync(source, fixedSource,
                VerifyCS.Diagnostic(UnsafeEvolutionDescriptors.UnnecessaryUnsafeModifier)
                    .WithSpan(3, 12, 3, 18)
                    .WithArguments("M"));
        }

        [Fact]
        public async Task Removes_UnsafeOn_FieldWithoutPointer()
        {
            string source = """
                public class C
                {
                    private unsafe int _x;
                }
                """;

            string fixedSource = """
                public class C
                {
                    private int _x;
                }
                """;

            await RunAsync(source, fixedSource,
                VerifyCS.Diagnostic(UnsafeEvolutionDescriptors.UnnecessaryUnsafeModifier)
                    .WithSpan(3, 13, 3, 19)
                    .WithArguments("_x"));
        }

        [Fact]
        public async Task PreservesLeadingComment_WhenRemovingUnsafe()
        {
            string source = """
                public class C
                {
                    // Important comment
                    public unsafe void M() {}
                }
                """;

            string fixedSource = """
                public class C
                {
                    // Important comment
                    public void M() {}
                }
                """;

            await RunAsync(source, fixedSource,
                VerifyCS.Diagnostic(UnsafeEvolutionDescriptors.UnnecessaryUnsafeModifier)
                    .WithSpan(4, 12, 4, 18)
                    .WithArguments("M"));
        }

        [Fact]
        public async Task Removes_UnsafeFirst_WhenAlsoPublic()
        {
            // When 'unsafe' is the very first modifier, ensure removal preserves the rest.
            string source = """
                public class C
                {
                    unsafe public void M() {}
                }
                """;

            string fixedSource = """
                public class C
                {
                    public void M() {}
                }
                """;

            await RunAsync(source, fixedSource,
                VerifyCS.Diagnostic(UnsafeEvolutionDescriptors.UnnecessaryUnsafeModifier)
                    .WithSpan(3, 5, 3, 11)
                    .WithArguments("M"));
        }
    }
}
#endif
