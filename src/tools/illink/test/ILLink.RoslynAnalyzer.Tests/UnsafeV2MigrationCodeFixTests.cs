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
        public async Task UnsafeRecordStruct_RemovesModifier()
        {
            var source = """
                public {|IL5005:unsafe|} record struct R
                {
                }
                """;
            var fixedSource = """
                public record struct R
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
        public async Task UnsafeIteratorMethod_WrapsBody_DeveloperFixesFallout()
        {
            // Best effort: we wrap the body even though `unsafe { yield return ... }` is
            // a C# compile error (CS1629). The developer is expected to fix the fallout
            // manually after running the bulk migration.
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
                        // SAFETY-TODO: Audit this unsafe usage
                        unsafe
                        {
                            yield return 1;
                            yield return 2;
                        }
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

        // ----- IL5006: Conversion operator -----

        [Fact]
        public async Task UnsafeConversionOperatorWithPtr_WrapsBodyKeepsModifier()
        {
            var source = """
                public class C
                {
                    public static {|IL5006:unsafe|} explicit operator C(int* p) => new C();
                }
                """;
            var fixedSource = """
                public class C
                {
                    public static unsafe explicit operator C(int* p)
                    {
                        // SAFETY-TODO: Audit this unsafe usage
                        unsafe
                        {
                            return new C();
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
        // ----- Multi-statement body indentation -----

        [Fact]
        public async Task UnsafeMethodMultiStatementBody_PreservesIndentation()
        {
            // Multiple statements inside a body must keep their relative indentation
            // when wrapped in the new 'unsafe' block (Formatter.Annotation re-indents).
            var source = """
                using System;
                public class C
                {
                    public {|IL5006:unsafe|} void M()
                    {
                        Console.WriteLine("a");
                        Console.WriteLine("b");
                        if (true)
                        {
                            Console.WriteLine("c");
                        }
                    }
                }
                """;
            var fixedSource = """
                using System;
                public class C
                {
                    public void M()
                    {
                        // SAFETY-TODO: Audit this unsafe usage
                        unsafe
                        {
                            Console.WriteLine("a");
                            Console.WriteLine("b");
                            if (true)
                            {
                                Console.WriteLine("c");
                            }
                        }
                    }
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        [Fact]
        public async Task UnsafeMethodBodyWithBlankLines_PreservesBlankLines()
        {
            // Blank lines between statements (EndOfLineTrivia in leading trivia) must
            // survive the wrap.
            var source = """
                using System;
                public class C
                {
                    public {|IL5006:unsafe|} void M()
                    {
                        Console.WriteLine("a");

                        Console.WriteLine("b");

                        Console.WriteLine("c");
                    }
                }
                """;
            var fixedSource = """
                using System;
                public class C
                {
                    public void M()
                    {
                        // SAFETY-TODO: Audit this unsafe usage
                        unsafe
                        {
                            Console.WriteLine("a");

                            Console.WriteLine("b");

                            Console.WriteLine("c");
                        }
                    }
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        [Fact]
        public async Task UnsafeMethodBodyWithComments_PreservesComments()
        {
            var source = """
                using System;
                public class C
                {
                    public {|IL5006:unsafe|} void M()
                    {
                        // leading comment
                        Console.WriteLine("a");
                        // between
                        Console.WriteLine("b");
                    }
                }
                """;
            var fixedSource = """
                using System;
                public class C
                {
                    public void M()
                    {
                        // SAFETY-TODO: Audit this unsafe usage
                        unsafe
                        {
                            // leading comment
                            Console.WriteLine("a");
                            // between
                            Console.WriteLine("b");
                        }
                    }
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        [Fact]
        public async Task UnsafeMethodBodyWithPreprocessorDirectives_OnlyRemovesModifier()
        {
            // Bodies containing #if/#elif/#else/#endif are deliberately not wrapped — see
            // the 'BlockNeedsWrap' comment in the analyzer for why. The modifier is still
            // removed when the signature has no pointers, and the body is left alone for
            // the developer to wrap manually.
            var source = """
                public class C
                {
                    public {|IL5006:unsafe|} void M()
                    {
                #if NET
                        System.Console.WriteLine("net");
                #else
                        System.Console.WriteLine("standard");
                #endif
                    }
                }
                """;
            var fixedSource = """
                public class C
                {
                    public void M()
                    {
                #if NET
                        System.Console.WriteLine("net");
                #else
                        System.Console.WriteLine("standard");
                #endif
                    }
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        // ----- Static constructors -----

        [Fact]
        public async Task UnsafeStaticConstructor_WrapsBodyAndRemovesModifier()
        {
            // Per spec: `unsafe` on a static constructor has no meaning under unsafe-v2
            // so we drop the modifier. The body might still contain pointer operations
            // that needed the old implicit unsafe context — wrap it so they keep working.
            var source = """
                public class C
                {
                    static {|IL5006:unsafe|} C()
                    {
                        System.Console.WriteLine();
                    }
                }
                """;
            var fixedSource = """
                public class C
                {
                    static C()
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
        public async Task UnsafeStaticConstructorEmptyBody_OnlyRemovesModifier()
        {
            // Static ctor with no body content — no wrapping needed, just drop the
            // modifier (per spec: `unsafe` on a static ctor has no meaning under
            // unsafe-v2 and is removed unconditionally).
            var source = """
                public class C
                {
                    static {|IL5006:unsafe|} C()
                    {
                    }
                }
                """;
            var fixedSource = """
                public class C
                {
                    static C()
                    {
                    }
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        // ----- Destructors -----

        [Fact]
        public async Task UnsafeDestructor_WrapsBodyAndRemovesModifier()
        {
            // Per spec: `unsafe` on a destructor has no meaning under unsafe-v2 — drop
            // the modifier. The body might still contain pointer operations that needed
            // the old implicit unsafe context — wrap it so they keep working.
            var source = """
                public class C
                {
                    {|IL5006:unsafe|} ~C()
                    {
                        System.Console.WriteLine();
                    }
                }
                """;
            var fixedSource = """
                public class C
                {
                    ~C()
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

        // ----- Constructors with initializers -----

        [Fact]
        public async Task UnsafeConstructorWithInitializer_KeepsModifier()
        {
            // Per spec: `unsafe` on a non-static constructor introduces an unsafe context
            // inside its initializer (`: base(...)` / `: this(...)`), so removing the
            // modifier could break a base/this call that needs that context. We keep the
            // modifier and still wrap the body for the body's own audit needs.
            var source = """
                public class B
                {
                    public B(int x) { }
                }
                public class C : B
                {
                    public {|IL5006:unsafe|} C() : base(0)
                    {
                        System.Console.WriteLine();
                    }
                }
                """;
            var fixedSource = """
                public class B
                {
                    public B(int x) { }
                }
                public class C : B
                {
                    public unsafe C() : base(0)
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
        public async Task UnsafeMethodBodyMixedCommentsBlankLinesNested_AllPreserved()
        {
            // The kind of body we'd find in real BCL code: a comment block, a blank line,
            // nested 'if' blocks, a trailing single-line comment on a statement.
            var source = """
                using System;
                public class C
                {
                    public {|IL5006:unsafe|} void M(int n)
                    {
                        // Initial check.
                        if (n < 0)
                        {
                            throw new ArgumentOutOfRangeException(nameof(n));
                        }

                        // Compute something.
                        int x = n * 2;
                        Console.WriteLine(x); // trailing on this line

                        if (x > 10)
                        {
                            // nested comment
                            Console.WriteLine("big");
                        }
                    }
                }
                """;
            var fixedSource = """
                using System;
                public class C
                {
                    public void M(int n)
                    {
                        // SAFETY-TODO: Audit this unsafe usage
                        unsafe
                        {
                            // Initial check.
                            if (n < 0)
                            {
                                throw new ArgumentOutOfRangeException(nameof(n));
                            }

                            // Compute something.
                            int x = n * 2;
                            Console.WriteLine(x); // trailing on this line

                            if (x > 10)
                            {
                                // nested comment
                                Console.WriteLine("big");
                            }
                        }
                    }
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }
        [Fact]
        public async Task UnsafeDelegateNoPtr_RemovesModifier()
        {
            // Delegates are type declarations. If the delegate signature has no pointer
            // types, the 'unsafe' modifier is redundant under unsafe-v2 and should be
            // dropped.
            var source = """
                public {|IL5005:unsafe|} delegate void D();
                """;
            var fixedSource = """
                public delegate void D();
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        [Fact]
        public async Task UnsafeDelegateWithPtr_RemovesModifierUnconditionally()
        {
            // Per spec: `unsafe` on a delegate declaration has no meaning under unsafe-v2
            // and is removed unconditionally — even when the delegate signature contains
            // pointers. Callers of methods returning such a delegate (or invoking it)
            // already need an unsafe context for the pointer dereference itself.
            var source = """
                public {|IL5005:unsafe|} delegate void D(int* p);
                """;
            var fixedSource = """
                public delegate void D(int* p);
                """;
            await VerifyCodeFix(source, fixedSource);
        }

        [Fact]
        public async Task UnsafeMethodBodyWithPragma_WrapsBody()
        {
            // #pragma / #nullable / #region don't change the body's shape between TFMs,
            // so they don't suppress the wrap the way #if/#elif/#else/#endif do.
            var source = """
                public class C
                {
                    public {|IL5006:unsafe|} void M()
                    {
                #pragma warning disable CS0168
                        int unused;
                #pragma warning restore CS0168
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
                #pragma warning disable CS0168
                            int unused;
                #pragma warning restore CS0168
                            System.Console.WriteLine();
                        }
                    }
                }
                """;
            await VerifyCodeFix(source, fixedSource);
        }
    }
}
#endif
