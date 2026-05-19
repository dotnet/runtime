// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Threading.Tasks;
using ILLink.Shared;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using VerifyCS = ILLink.RoslynAnalyzer.Tests.CSharpCodeFixVerifier<
    ILLink.RoslynAnalyzer.UnsafeV2MigrationAnalyzer,
    ILLink.CodeFix.UnsafeV2MigrationCodeFixProvider>;

namespace ILLink.RoslynAnalyzer.Tests
{
    // Tests use markup syntax {|IL5005:unsafe|} / {|IL5006:unsafe|} to indicate where the
    // analyzer is expected to report a diagnostic. The verifier handles compiler diagnostics
    // separately via CompilerDiagnostics.None so we don't have to enumerate them.
    public class UnsafeV2MigrationCodeFixTests
    {
        static Task VerifyCodeFix(string source, string fixedSource, int? numberOfIterations = null)
        {
            var test = new VerifyCS.Test {
                TestCode = source,
                FixedCode = fixedSource,
                CompilerDiagnostics = CompilerDiagnostics.None,
                MarkupOptions = MarkupOptions.UseFirstDescriptor,
            };
            test.TestState.AnalyzerConfigFiles.Add(
                ("/.editorconfig", SourceText.From(@$"
is_global = true
build_property.{MSBuildPropertyOptionNames.EnableUnsafeV2MigrationAnalyzer} = true")));
            test.SolutionTransforms.Add((solution, projectId) => {
                var project = solution.GetProject(projectId)!;
                var compilationOptions = (CSharpCompilationOptions)project.CompilationOptions!;
                compilationOptions = compilationOptions.WithAllowUnsafe(true);
                return solution.WithProjectCompilationOptions(projectId, compilationOptions);
            });
            if (numberOfIterations is not null)
            {
                test.NumberOfIncrementalIterations = numberOfIterations;
                test.NumberOfFixAllIterations = numberOfIterations;
            }
            return test.RunAsync();
        }

        // ----- Disabled analyzer -----

        [Fact]
        public async Task AnalyzerDisabled_NoDiagnostic()
        {
            // No editorconfig — analyzer should not fire even though source contains 'unsafe'.
            var source = """
                public unsafe class C
                {
                    public unsafe void M() { }
                }
                """;
            var test = new VerifyCS.Test {
                TestCode = source,
                FixedCode = source,
                CompilerDiagnostics = CompilerDiagnostics.None,
            };
            test.SolutionTransforms.Add((solution, projectId) => {
                var project = solution.GetProject(projectId)!;
                var compilationOptions = (CSharpCompilationOptions)project.CompilationOptions!;
                compilationOptions = compilationOptions.WithAllowUnsafe(true);
                return solution.WithProjectCompilationOptions(projectId, compilationOptions);
            });
            await test.RunAsync();
        }

        // ----- IL5005: Remove unsafe from type -----

        [Fact]
        public async Task UnsafeClass_RemovesModifier()
        {
            var source = """
                public {|IL5005:unsafe|} class C
                {
                }
                """;
            var fixedSource = """
                public class C
                {
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        [Fact]
        public async Task UnsafeStruct_RemovesModifier()
        {
            var source = """
                public {|IL5005:unsafe|} struct S
                {
                }
                """;
            var fixedSource = """
                public struct S
                {
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        [Fact]
        public async Task UnsafeInterface_RemovesModifier()
        {
            var source = """
                public {|IL5005:unsafe|} interface I
                {
                }
                """;
            var fixedSource = """
                public interface I
                {
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        [Fact]
        public async Task UnsafeRecord_RemovesModifier()
        {
            var source = """
                public {|IL5005:unsafe|} record R
                {
                }
                """;
            var fixedSource = """
                public record R
                {
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        [Fact]
        public async Task UnsafeFirstModifierOnType_PreservesOtherModifiers()
        {
            var source = """
                {|IL5005:unsafe|} public sealed class C
                {
                }
                """;
            var fixedSource = """
                public sealed class C
                {
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        // ----- IL5006: Member with no body -----

        [Fact]
        public async Task AbstractUnsafeMethodNoPtr_RemovesModifierOnly()
        {
            var source = """
                public abstract class C
                {
                    public abstract {|IL5006:unsafe|} void M();
                }
                """;
            var fixedSource = """
                public abstract class C
                {
                    public abstract void M();
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        [Fact]
        public async Task AbstractUnsafeMethodWithPtr_NoDiagnostic()
        {
            // Pointer in signature -> can't remove 'unsafe' modifier.
            // No body to wrap. Migration is a no-op, so the analyzer should not fire.
            var source = """
                public abstract class C
                {
                    public abstract unsafe void M(int* p);
                }
                """;
            await VerifyCodeFix(source, source);
        }

        [Fact]
        public async Task EventFieldUnsafe_RemovesModifier()
        {
            var source = """
                using System;
                public class C
                {
                    public {|IL5006:unsafe|} event Action E;
                }
                """;
            var fixedSource = """
                using System;
                public class C
                {
                    public event Action E;
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        // ----- IL5006: Method with body -----

        [Fact]
        public async Task UnsafeMethodNoPtr_WrapsBodyAndRemovesModifier()
        {
            var source = """
                public class C
                {
                    public {|IL5006:unsafe|} void M()
                    {
                        System.Console.WriteLine();
                    }
                }
                """;
            var fixedSource = """
                public class C
                {
                    public void M()
                    {
                        // SAFETY-TODO: Audit this unsafe usage
                        unsafe
                        {
                            System.Console.WriteLine();
                        }
                    }
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        [Fact]
        public async Task UnsafeMethodWithPtr_WrapsBodyKeepsModifier()
        {
            var source = """
                public class C
                {
                    public {|IL5006:unsafe|} int M(int* p)
                    {
                        return *p;
                    }
                }
                """;
            var fixedSource = """
                public class C
                {
                    public unsafe int M(int* p)
                    {
                        // SAFETY-TODO: Audit this unsafe usage
                        unsafe
                        {
                            return *p;
                        }
                    }
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        [Fact]
        public async Task UnsafeMethodExpressionBody_ConvertsToBlockBody()
        {
            var source = """
                public class C
                {
                    public {|IL5006:unsafe|} int M() => 42;
                }
                """;
            var fixedSource = """
                public class C
                {
                    public int M()
                    {
                        // SAFETY-TODO: Audit this unsafe usage
                        unsafe
                        {
                            return 42;
                        }
                    }
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        [Fact]
        public async Task UnsafeMethodEmptyBody_DoesNotWrap()
        {
            var source = """
                public class C
                {
                    public {|IL5006:unsafe|} void M()
                    {
                    }
                }
                """;
            var fixedSource = """
                public class C
                {
                    public void M()
                    {
                    }
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        [Fact]
        public async Task UnsafeMethodAlreadyHasUnsafeBlock_DoesNotWrap()
        {
            var source = """
                public class C
                {
                    public {|IL5006:unsafe|} void M()
                    {
                        unsafe { System.Console.WriteLine(); }
                    }
                }
                """;
            var fixedSource = """
                public class C
                {
                    public void M()
                    {
                        unsafe { System.Console.WriteLine(); }
                    }
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        [Fact]
        public async Task UnsafeIteratorMethod_DoesNotWrap()
        {
            var source = """
                using System.Collections.Generic;
                public class C
                {
                    public {|IL5006:unsafe|} IEnumerable<int> M()
                    {
                        yield return 1;
                        yield return 2;
                    }
                }
                """;
            var fixedSource = """
                using System.Collections.Generic;
                public class C
                {
                    public IEnumerable<int> M()
                    {
                        yield return 1;
                        yield return 2;
                    }
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        // ----- IL5006: Constructor -----

        [Fact]
        public async Task UnsafeConstructorWithBody_WrapsBody()
        {
            var source = """
                public class C
                {
                    public {|IL5006:unsafe|} C()
                    {
                        System.Console.WriteLine();
                    }
                }
                """;
            var fixedSource = """
                public class C
                {
                    public C()
                    {
                        // SAFETY-TODO: Audit this unsafe usage
                        unsafe
                        {
                            System.Console.WriteLine();
                        }
                    }
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        // ----- IL5006: Property -----

        [Fact]
        public async Task UnsafeAutoProperty_RemovesModifierOnly()
        {
            var source = """
                public class C
                {
                    public {|IL5006:unsafe|} int X { get; set; }
                }
                """;
            var fixedSource = """
                public class C
                {
                    public int X { get; set; }
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        [Fact]
        public async Task UnsafePropertyTrivialFieldExpressionBody_RemovesModifierOnly()
        {
            var source = """
                public class C
                {
                    private int _x;
                    public {|IL5006:unsafe|} int X => _x;
                }
                """;
            var fixedSource = """
                public class C
                {
                    private int _x;
                    public int X => _x;
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        [Fact]
        public async Task UnsafePropertyComputedExpressionBody_WrapsBody()
        {
            var source = """
                public class C
                {
                    public {|IL5006:unsafe|} int X => 1 + 2;
                }
                """;
            var fixedSource = """
                public class C
                {
                    public int X
                    {
                        get
                        {
                            // SAFETY-TODO: Audit this unsafe usage
                            unsafe
                            {
                                return 1 + 2;
                            }
                        }
                    }
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        [Fact]
        public async Task UnsafePropertyTrivialFieldGetSet_RemovesModifierOnly()
        {
            var source = """
                public class C
                {
                    private int _x;
                    public {|IL5006:unsafe|} int X
                    {
                        get => _x;
                        set => _x = value;
                    }
                }
                """;
            var fixedSource = """
                public class C
                {
                    private int _x;
                    public int X
                    {
                        get => _x;
                        set => _x = value;
                    }
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        [Fact]
        public async Task UnsafePropertyWithPtrTypeAndBlockGetSet_WrapsAccessors()
        {
            var source = """
                public class C
                {
                    private int _x;
                    public {|IL5006:unsafe|} int* P
                    {
                        get { return &_x; }
                        set { _x = *value; }
                    }
                }
                """;
            var fixedSource = """
                public class C
                {
                    private int _x;
                    public unsafe int* P
                    {
                        get
                        {
                            // SAFETY-TODO: Audit this unsafe usage
                            unsafe
                            {
                                return &_x;
                            }
                        }
                        set
                        {
                            // SAFETY-TODO: Audit this unsafe usage
                            unsafe
                            {
                                _x = *value;
                            }
                        }
                    }
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        // ----- IL5006: Indexer -----

        [Fact]
        public async Task UnsafeIndexer_WrapsAccessors()
        {
            var source = """
                public class C
                {
                    public {|IL5006:unsafe|} int this[int i]
                    {
                        get { return i + 1; }
                    }
                }
                """;
            var fixedSource = """
                public class C
                {
                    public int this[int i]
                    {
                        get
                        {
                            // SAFETY-TODO: Audit this unsafe usage
                            unsafe
                            {
                                return i + 1;
                            }
                        }
                    }
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        // ----- IL5006: Operator -----

        [Fact]
        public async Task UnsafeOperator_WrapsBody()
        {
            var source = """
                public class C
                {
                    public static {|IL5006:unsafe|} C operator +(C a, C b)
                    {
                        return a;
                    }
                }
                """;
            var fixedSource = """
                public class C
                {
                    public static C operator +(C a, C b)
                    {
                        // SAFETY-TODO: Audit this unsafe usage
                        unsafe
                        {
                            return a;
                        }
                    }
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        // ----- IL5006: Local function -----

        [Fact]
        public async Task UnsafeLocalFunction_WrapsBody()
        {
            var source = """
                public class C
                {
                    public void Outer()
                    {
                        {|IL5006:unsafe|} void Inner()
                        {
                            System.Console.WriteLine();
                        }
                        Inner();
                    }
                }
                """;
            var fixedSource = """
                public class C
                {
                    public void Outer()
                    {
                        void Inner()
                        {
                            // SAFETY-TODO: Audit this unsafe usage
                            unsafe
                            {
                                System.Console.WriteLine();
                            }
                        }
                        Inner();
                    }
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        // ----- IL5006: Event with accessors -----

        [Fact]
        public async Task UnsafeEventWithAccessors_WrapsAccessors()
        {
            var source = """
                using System;
                public class C
                {
                    public {|IL5006:unsafe|} event Action E
                    {
                        add { System.Console.WriteLine(); }
                        remove { System.Console.WriteLine(); }
                    }
                }
                """;
            var fixedSource = """
                using System;
                public class C
                {
                    public event Action E
                    {
                        add
                        {
                            // SAFETY-TODO: Audit this unsafe usage
                            unsafe
                            {
                                System.Console.WriteLine();
                            }
                        }
                        remove
                        {
                            // SAFETY-TODO: Audit this unsafe usage
                            unsafe
                            {
                                System.Console.WriteLine();
                            }
                        }
                    }
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        // ----- Combined: type AND member -----

        [Fact]
        public async Task UnsafeClassWithUnsafeMember_FixesBoth()
        {
            var source = """
                public {|IL5005:unsafe|} class C
                {
                    public {|IL5006:unsafe|} void M()
                    {
                        System.Console.WriteLine();
                    }
                }
                """;
            var fixedSource = """
                public class C
                {
                    public void M()
                    {
                        // SAFETY-TODO: Audit this unsafe usage
                        unsafe
                        {
                            System.Console.WriteLine();
                        }
                    }
                }
                """;
            // Two diagnostics in different scopes -> BatchFixer applies the fixes in two
            // iterations (one per diagnostic).
            await VerifyCodeFix(source, fixedSource, numberOfIterations: 2);
        }

        // ----- Pointer in nested signature -----

        [Fact]
        public async Task UnsafeMethodWithPointerInArray_KeepsModifier()
        {
            // int*[] still contains a pointer — keep modifier, wrap body.
            var source = """
                public class C
                {
                    public {|IL5006:unsafe|} int*[] M() => null;
                }
                """;
            var fixedSource = """
                public class C
                {
                    public unsafe int*[] M()
                    {
                        // SAFETY-TODO: Audit this unsafe usage
                        unsafe
                        {
                            return null;
                        }
                    }
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }
    }
}
#endif
