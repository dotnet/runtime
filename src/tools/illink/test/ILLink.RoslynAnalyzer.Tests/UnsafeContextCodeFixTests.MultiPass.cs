// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if UNSAFE_MIGRATION
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    /// <summary>
    /// Later passes (e.g. under other build configurations) merge with the placeholder contexts of earlier passes and
    /// account for code in inactive <c>#if</c> arms.
    /// </summary>
    public partial class UnsafeContextCodeFixTests
    {
        [Fact]
        public Task SecondPass_ReplacesPlaceholderWrapperWithBlock()
        {
            return VerifyAsync("""
                class C
                {
                    static unsafe int A() => 0;
                    static unsafe int B() => 0;

                    int M()
                    {
                        return /* SAFETY: To be audited */ unsafe(A()) + {|CS9362:B()|};
                    }
                }
                """, """
                class C
                {
                    static unsafe int A() => 0;
                    static unsafe int B() => 0;

                    int M()
                    {
                        // SAFETY: To be audited
                        unsafe
                        {
                            return A() + B();
                        }
                    }
                }
                """);
        }

        [Fact]
        public Task SecondPass_WidensPlaceholderWrapper()
        {
            return VerifyAsync("""
                class C
                {
                    static unsafe int A() => 0;
                    static unsafe int B() => 0;

                    int P => /* SAFETY: To be audited */ unsafe(A()) * {|CS9362:B()|};
                }
                """, """
                class C
                {
                    static unsafe int A() => 0;
                    static unsafe int B() => 0;

                    int P => /* SAFETY: To be audited */ unsafe(A() * B());
                }
                """);
        }

        [Fact]
        public Task SecondPass_KeepsAuditedWrapper()
        {
            // Merging would broaden the audited claim and a clause holds one wrapper, so no fix is offered.
            const string source = """
                class C
                {
                    static unsafe int A() => 0;
                    static unsafe int B() => 0;

                    int P => /* SAFETY: A has no preconditions. */ unsafe(A()) * {|CS9362:B()|};
                }
                """;
            return VerifyAsync(source, source);
        }

        [Fact]
        public Task SecondPass_MergesPlaceholderBlockBetweenOperations()
        {
            return VerifyAsync("""
                class C
                {
                    void M(int* p)
                    {
                        {|CS9360:*|}p = 1;
                        // SAFETY: To be audited
                        unsafe
                        {
                            *p = 2;
                        }
                        {|CS9360:*|}p = 3;
                    }
                }
                """, """
                class C
                {
                    void M(int* p)
                    {
                        // SAFETY: To be audited
                        unsafe
                        {
                            *p = 1;
                            *p = 2;
                            *p = 3;
                        }
                    }
                }
                """);
        }

        [Fact]
        public Task PlaceholderBlockWithInnerComment_IsNotExtended()
        {
            // Extending would drop the comment before the closing brace.
            return VerifyAsync("""
                class C
                {
                    void M(int* p, int* q)
                    {
                        // SAFETY: To be audited
                        unsafe
                        {
                            *p = 1;
                            // Trailing note.
                        }
                        {|CS9360:*|}q = 2;
                    }
                }
                """, """
                class C
                {
                    void M(int* p, int* q)
                    {
                        // SAFETY: To be audited
                        unsafe
                        {
                            *p = 1;
                            // Trailing note.
                        }
                        // SAFETY: To be audited
                        unsafe
                        {
                            *q = 2;
                        }
                    }
                }
                """);
        }

        [Fact]
        public Task InactiveUseAfterBlock_SplitsDeclaration()
        {
            // `n` is only used in an inactive arm, so a block containing its declaration would break that configuration.
            return VerifyAsync("""
                class C
                {
                    static unsafe int Read() => 0;

                    static void Log(int value) { }

                    int M()
                    {
                        var n = {|CS9362:Read()|};
                #if NEVER
                        Log(n);
                #endif
                        return 0;
                    }
                }
                """, """
                class C
                {
                    static unsafe int Read() => 0;

                    static void Log(int value) { }

                    int M()
                    {
                        int n;
                        // SAFETY: To be audited
                        unsafe
                        {
                            n = Read();
                        }
                #if NEVER
                        Log(n);
                #endif
                        return 0;
                    }
                }
                """);
        }

        [Fact]
        public Task InactiveDeclarationInRange_IsNotHidden()
        {
            // Merging both statements would move the inactive declaration of `x` into the block, away from its use.
            return VerifyAsync("""
                class C
                {
                    static void Use(int value) { }

                    void M(int* p)
                    {
                        {|CS9360:*|}p = 1;
                #if NEVER
                        int x = 0;
                #endif
                        {|CS9360:*|}p = 2;
                #if NEVER
                        Use(x);
                #endif
                    }
                }
                """, """
                class C
                {
                    static void Use(int value) { }

                    void M(int* p)
                    {
                        // SAFETY: To be audited
                        unsafe
                        {
                            *p = 1;
                        }
                #if NEVER
                        int x = 0;
                #endif
                        // SAFETY: To be audited
                        unsafe
                        {
                            *p = 2;
                        }
                #if NEVER
                        Use(x);
                #endif
                    }
                }
                """);
        }

        [Fact]
        public Task InactiveArmInBlock_KeepsStringLiterals()
        {
            return VerifyAsync("""
                class C
                {
                    static void Use(string value) { }

                    void M(int* p)
                    {
                        if ({|CS9360:*|}p == 0)
                        {
                #if NEVER
                            Use(@"first
                second");
                #endif
                            {|CS9360:*|}p = 1;
                        }
                    }
                }
                """, """
                class C
                {
                    static void Use(string value) { }

                    void M(int* p)
                    {
                        // SAFETY: To be audited
                        unsafe
                        {
                            if (*p == 0)
                            {
                #if NEVER
                            Use(@"first
                second");
                #endif
                                *p = 1;
                            }
                        }
                    }
                }
                """);
        }

        [Theory]
        [InlineData("_ = (System.Action)(() => { });")]
        [InlineData("_ = (System.Action)delegate { };")]
        [InlineData("static void Local() { } Local();")]
        [InlineData("yield return 1;")]
        [InlineData("unsafe { }")]
        [InlineData("_ = /* SAFETY: Already audited. */ unsafe(0);")]
        [InlineData("_ = /* SAFETY: To be audited */ unsafe(0);")]
        public Task InactiveContextBoundary_IsNotEnclosed(string inactiveCode)
        {
            return VerifyAsync($$"""
                class C
                {
                    static unsafe int Read() => 0;

                    System.Collections.Generic.IEnumerable<int> M()
                    {
                        if ({|CS9362:Read()|} == 0)
                        {
                #if NEVER
                            {{inactiveCode}}
                #endif
                            {|CS9362:Read()|};
                        }
                        yield return 0;
                    }
                }
                """, $$"""
                class C
                {
                    static unsafe int Read() => 0;

                    System.Collections.Generic.IEnumerable<int> M()
                    {
                        if (/* SAFETY: To be audited */ unsafe(Read()) == 0)
                        {
                #if NEVER
                            {{inactiveCode}}
                #endif
                            // SAFETY: To be audited
                            unsafe
                            {
                                Read();
                            }
                        }
                        yield return 0;
                    }
                }
                """);
        }

        [Fact]
        public Task InactiveLambdaBetweenStatements_IsNotEnclosed()
        {
            return VerifyAsync("""
                class C
                {
                    static unsafe void M() { }

                    void Caller()
                    {
                        {|CS9362:M()|};
                #if NEVER
                        _ = (System.Action)(() => { });
                #endif
                        {|CS9362:M()|};
                    }
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
                #if NEVER
                        _ = (System.Action)(() => { });
                #endif
                        // SAFETY: To be audited
                        unsafe
                        {
                            M();
                        }
                    }
                }
                """);
        }

        [Theory]
        [InlineData("Use(() => Read(1))")]
        [InlineData("/* SAFETY: Already audited. */ unsafe(Read(1))")]
        [InlineData("/* SAFETY: To be audited */ unsafe(Read(1))")]
        public Task InactiveContextInExpression_NoFix(string inactiveExpression)
        {
            string source = $$"""
                class C
                {
                    static unsafe int Read(int value) => value;
                    static int Use(System.Func<int> callback) => callback();

                    int M()
                    {
                        return {|CS9362:Read(
                #if NEVER
                            {{inactiveExpression}} +
                #endif
                            0)|};
                    }
                }
                """;
            return VerifyAsync(source, source);
        }

        [Fact]
        public Task DeconstructionFollowedByYield_SplitsDeclarations()
        {
            return VerifyAsync("""
                using System.Collections.Generic;

                class C
                {
                    static unsafe (int, string) Get() => (0, "");

                    IEnumerable<int> M()
                    {
                        var (first, second) = {|CS9362:Get()|};
                        yield return first;
                    }
                }
                """, """
                using System.Collections.Generic;

                class C
                {
                    static unsafe (int, string) Get() => (0, "");

                    IEnumerable<int> M()
                    {
                        int first;
                        string second;
                        // SAFETY: To be audited
                        unsafe
                        {
                            (first, second) = Get();
                        }
                        yield return first;
                    }
                }
                """);
        }

        [Fact]
        public Task MixedDeconstruction_KeepsDiscardsAndExistingTargets()
        {
            return VerifyAsync("""
                using System.Collections.Generic;

                class C
                {
                    static unsafe (int, (string, long)) Get() => (0, ("", 0));

                    IEnumerable<int> M(long existing)
                    {
                        (int first, (var _, existing)) = {|CS9362:Get()|};
                        yield return first;
                    }
                }
                """, """
                using System.Collections.Generic;

                class C
                {
                    static unsafe (int, (string, long)) Get() => (0, ("", 0));

                    IEnumerable<int> M(long existing)
                    {
                        int first;
                        // SAFETY: To be audited
                        unsafe
                        {
                            (first, (var _, existing)) = Get();
                        }
                        yield return first;
                    }
                }
                """);
        }
    }
}
#endif
