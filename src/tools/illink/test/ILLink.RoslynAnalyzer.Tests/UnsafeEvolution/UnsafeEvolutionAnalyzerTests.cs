// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Threading.Tasks;
using ILLink.CodeFix.UnsafeEvolution;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = ILLink.RoslynAnalyzer.Tests.CSharpAnalyzerVerifier<
    ILLink.CodeFix.UnsafeEvolution.UnsafeEvolutionAnalyzer>;

namespace ILLink.RoslynAnalyzer.Tests.UnsafeEvolution
{
    public class UnsafeEvolutionAnalyzerTests
    {
        private static Task RunAsync(string source, params DiagnosticResult[] expected) =>
            VerifyCS.VerifyAnalyzerAsync(
                src: source,
                consoleApplication: false,
                analyzerOptions: null,
                additionalReferences: null,
                allowUnsafe: true,
                expected: expected);

        // ---- IL5005: meaningless unsafe on types and special members ----

        [Theory]
        [InlineData("class", "class")]
        [InlineData("struct", "struct")]
        [InlineData("interface", "interface")]
        [InlineData("record", "record")]
        [InlineData("record struct", "record struct")]
        public async Task IL5005_FiresOn_UnsafeTypeDeclaration(string declKind, string expectedKindArg)
        {
            string source = $$"""
                public unsafe {{declKind}} C {}
                """;

            int unsafeColumn = "public ".Length + 1; // 1-based
            await RunAsync(source,
                VerifyCS.Diagnostic(UnsafeEvolutionDescriptors.MeaninglessUnsafeModifier)
                    .WithLocation(1, unsafeColumn)
                    .WithArguments(expectedKindArg, "C"));
        }

        [Fact]
        public async Task IL5005_FiresOn_UnsafeDelegate()
        {
            string source = """
                public unsafe delegate void D();
                """;

            await RunAsync(source,
                VerifyCS.Diagnostic(UnsafeEvolutionDescriptors.MeaninglessUnsafeModifier)
                    .WithLocation(1, 8)
                    .WithArguments("delegate", "D"));
        }

        [Fact]
        public async Task IL5005_FiresOn_UnsafeStaticConstructor()
        {
            string source = """
                public class C
                {
                    static unsafe C() {}
                }
                """;

            await RunAsync(source,
                VerifyCS.Diagnostic(UnsafeEvolutionDescriptors.MeaninglessUnsafeModifier)
                    .WithSpan(3, 12, 3, 18)
                    .WithArguments("static constructor", "C"));
        }

        [Fact]
        public async Task IL5005_DoesNotFireOn_UnsafeInstanceConstructor()
        {
            // Instance constructors can be requires-unsafe under the new rules.
            string source = """
                public class C
                {
                    public unsafe C() {}
                }
                """;

            await RunAsync(source);
        }

        [Fact]
        public async Task IL5005_FiresOn_UnsafeDestructor()
        {
            string source = """
                public class C
                {
                    unsafe ~C() {}
                }
                """;

            await RunAsync(source,
                VerifyCS.Diagnostic(UnsafeEvolutionDescriptors.MeaninglessUnsafeModifier)
                    .WithSpan(3, 5, 3, 11)
                    .WithArguments("destructor", "C"));
        }

        // ---- IL5006: unnecessary unsafe modifier on signatures with no pointer types ----

        [Fact]
        public async Task IL5006_FiresOn_MethodWithoutPointerSignature()
        {
            string source = """
                public class C
                {
                    public unsafe void M() {}
                }
                """;

            await RunAsync(source,
                VerifyCS.Diagnostic(UnsafeEvolutionDescriptors.UnnecessaryUnsafeModifier)
                    .WithSpan(3, 12, 3, 18)
                    .WithArguments("M"));
        }

        [Fact]
        public async Task IL5006_DoesNotFireOn_MethodWithPointerParameter()
        {
            string source = """
                public class C
                {
                    public unsafe void M(int* p) {}
                }
                """;

            await RunAsync(source);
        }

        [Fact]
        public async Task IL5006_DoesNotFireOn_MethodWithPointerReturn()
        {
            string source = """
                public class C
                {
                    public unsafe int* M() => null;
                }
                """;

            await RunAsync(source);
        }

        [Fact]
        public async Task IL5006_DoesNotFireOn_MethodWithPointerInNestedType()
        {
            // int*[] is allowed; nested pointer type counts as a pointer for our heuristic.
            string source = """
                public class C
                {
                    public unsafe int*[] M() => null;
                }
                """;

            await RunAsync(source);
        }

        [Fact]
        public async Task IL5006_DoesNotFireOn_MethodWithFunctionPointerParameter()
        {
            string source = """
                public class C
                {
                    public unsafe void M(delegate*<int, void> p) {}
                }
                """;

            await RunAsync(source);
        }

        [Fact]
        public async Task IL5006_DoesNotFireOn_ExternMethod()
        {
            string source = """
                using System.Runtime.InteropServices;
                public class C
                {
                    [DllImport("foo")]
                    public static extern unsafe int M();
                }
                """;

            await RunAsync(source);
        }

        [Fact]
        public async Task IL5006_DoesNotFireOn_PartialMethod()
        {
            string source = """
                public partial class C
                {
                    public unsafe partial void M();
                    public unsafe partial void M() {}
                }
                """;

            await RunAsync(source);
        }

        [Fact]
        public async Task IL5006_DoesNotFireOn_MemberInsideUnsafeType()
        {
            // The type itself triggers IL5005; the member is fixed transitively.
            string source = """
                public unsafe class C
                {
                    public unsafe void M() {}
                }
                """;

            await RunAsync(source,
                VerifyCS.Diagnostic(UnsafeEvolutionDescriptors.MeaninglessUnsafeModifier)
                    .WithSpan(1, 8, 1, 14)
                    .WithArguments("class", "C"));
        }

        [Fact]
        public async Task IL5006_FiresOn_FieldWithoutPointerType()
        {
            string source = """
                public class C
                {
                    private unsafe int _x;
                }
                """;

            await RunAsync(source,
                VerifyCS.Diagnostic(UnsafeEvolutionDescriptors.UnnecessaryUnsafeModifier)
                    .WithSpan(3, 13, 3, 19)
                    .WithArguments("_x"),
                DiagnosticResult.CompilerWarning("CS0169").WithSpan(3, 24, 3, 26).WithArguments("C._x"));
        }

        [Fact]
        public async Task IL5006_DoesNotFireOn_FieldWithPointerType()
        {
            string source = """
                public class C
                {
                    private unsafe int* _x;
                }
                """;

            await RunAsync(source,
                DiagnosticResult.CompilerWarning("CS0169").WithSpan(3, 25, 3, 27).WithArguments("C._x"));
        }

        [Fact]
        public async Task IL5006_FiresOn_PropertyWithoutPointer()
        {
            string source = """
                public class C
                {
                    public unsafe int P { get; set; }
                }
                """;

            await RunAsync(source,
                VerifyCS.Diagnostic(UnsafeEvolutionDescriptors.UnnecessaryUnsafeModifier)
                    .WithSpan(3, 12, 3, 18)
                    .WithArguments("P"));
        }

        [Fact]
        public async Task IL5006_FiresOn_IndexerWithoutPointer()
        {
            string source = """
                public class C
                {
                    public unsafe int this[int i] => 0;
                }
                """;

            await RunAsync(source,
                VerifyCS.Diagnostic(UnsafeEvolutionDescriptors.UnnecessaryUnsafeModifier)
                    .WithSpan(3, 12, 3, 18)
                    .WithArguments("this[]"));
        }

        [Fact]
        public async Task IL5006_FiresOn_LocalFunctionWithoutPointer()
        {
            string source = """
                public class C
                {
                    public void M()
                    {
                        unsafe void Local() {}
                        Local();
                    }
                }
                """;

            await RunAsync(source,
                VerifyCS.Diagnostic(UnsafeEvolutionDescriptors.UnnecessaryUnsafeModifier)
                    .WithSpan(5, 9, 5, 15)
                    .WithArguments("Local"));
        }

        [Fact]
        public async Task IL5006_FiresOn_EventFieldWithoutPointer()
        {
            string source = """
                using System;
                public class C
                {
                    public unsafe event Action E;
                }
                """;

            await RunAsync(source,
                VerifyCS.Diagnostic(UnsafeEvolutionDescriptors.UnnecessaryUnsafeModifier)
                    .WithSpan(4, 12, 4, 18)
                    .WithArguments("E"),
                DiagnosticResult.CompilerWarning("CS0067").WithSpan(4, 32, 4, 33).WithArguments("C.E"));
        }

        [Fact]
        public async Task IL5006_FiresOn_EventWithExplicitAccessorsWithoutPointer()
        {
            string source = """
                using System;
                public class C
                {
                    public unsafe event Action E { add { } remove { } }
                }
                """;

            await RunAsync(source,
                VerifyCS.Diagnostic(UnsafeEvolutionDescriptors.UnnecessaryUnsafeModifier)
                    .WithSpan(4, 12, 4, 18)
                    .WithArguments("E"));
        }
    }
}
#endif
