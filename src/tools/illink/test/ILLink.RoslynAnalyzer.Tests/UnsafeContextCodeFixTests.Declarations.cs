// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    public partial class UnsafeContextCodeFixTests
    {
        [Fact]
        public Task Stackalloc_ScopedSplit()
        {
            return VerifyAsync("""
                using System;
                using System.Runtime.CompilerServices;

                [module: SkipLocalsInit]

                class C
                {
                    static void Use(Span<byte> a, ReadOnlySpan<int> b) { }

                    void M(int length)
                    {
                        Span<byte> buffer = {|CS9361:stackalloc byte[256]|};
                        ReadOnlySpan<int> values = length < 100 ? {|CS9361:stackalloc int[100]|} : new int[length];
                        Use(buffer, values);
                    }
                }
                """, """
                using System;
                using System.Runtime.CompilerServices;

                [module: SkipLocalsInit]

                class C
                {
                    static void Use(Span<byte> a, ReadOnlySpan<int> b) { }

                    void M(int length)
                    {
                        scoped Span<byte> buffer;
                        scoped ReadOnlySpan<int> values;
                        // SAFETY: To be audited
                        unsafe
                        {
                            buffer = stackalloc byte[256];
                            values = length < 100 ? stackalloc int[100] : new int[length];
                        }
                        Use(buffer, values);
                    }
                }
                """);
        }

        [Fact]
        public Task Stackalloc_RefStructBuilderArgument_UsesExpression()
        {
            return VerifyAsync("""
                using System;
                using System.Runtime.CompilerServices;

                [module: SkipLocalsInit]

                ref struct Builder
                {
                    public Builder(Span<char> buffer) { }
                    public void Append(char c) { }
                }

                class C
                {
                    void M()
                    {
                        var builder = new Builder({|CS9361:stackalloc char[256]|});
                        builder.Append('a');
                        builder.Append('b');
                    }
                }
                """, """
                using System;
                using System.Runtime.CompilerServices;

                [module: SkipLocalsInit]

                ref struct Builder
                {
                    public Builder(Span<char> buffer) { }
                    public void Append(char c) { }
                }

                class C
                {
                    void M()
                    {
                        var builder = new Builder(/* SAFETY: To be audited */ unsafe(stackalloc char[256]));
                        builder.Append('a');
                        builder.Append('b');
                    }
                }
                """);
        }

        [Fact]
        public Task Stackalloc_NestedScopeEscape_UsesClosedBlock()
        {
            // A scoped split would narrow `a` and break `outer = a`; the block containing `a` and its use is proportionate.
            return VerifyAsync("""
                using System;
                using System.Runtime.CompilerServices;

                [module: SkipLocalsInit]

                class C
                {
                    static void Use(Span<int> a) { }

                    void M(int length, bool b)
                    {
                        scoped Span<int> outer;
                        if (b)
                        {
                            Span<int> a = length < 100 ? {|CS9361:stackalloc int[100]|} : new int[length];
                            outer = a;
                            Use(outer);
                        }
                    }
                }
                """, """
                using System;
                using System.Runtime.CompilerServices;

                [module: SkipLocalsInit]

                class C
                {
                    static void Use(Span<int> a) { }

                    void M(int length, bool b)
                    {
                        scoped Span<int> outer;
                        if (b)
                        {
                            // SAFETY: To be audited
                            unsafe
                            {
                                Span<int> a = length < 100 ? stackalloc int[100] : new int[length];
                                outer = a;
                            }
                            Use(outer);
                        }
                    }
                }
                """);
        }

        [Fact]
        public Task Stackalloc_NestedScopeEscape_KeepsDeclaration()
        {
            // Neither a scoped split nor a proportionate closed block preserves `a`, so its declaration is kept.
            return VerifyAsync("""
                using System;
                using System.Runtime.CompilerServices;

                [module: SkipLocalsInit]

                class C
                {
                    static void Use(Span<int> a) { }

                    void M(int length, bool b)
                    {
                        scoped Span<int> outer;
                        if (b)
                        {
                            Span<int> a = length < 100 ? {|CS9361:stackalloc int[100]|} : new int[length];
                            outer = a;
                            Use(a);
                            Use(a);
                        }
                    }
                }
                """, """
                using System;
                using System.Runtime.CompilerServices;

                [module: SkipLocalsInit]

                class C
                {
                    static void Use(Span<int> a) { }

                    void M(int length, bool b)
                    {
                        scoped Span<int> outer;
                        if (b)
                        {
                            Span<int> a = length < 100 ? /* SAFETY: To be audited */ unsafe(stackalloc int[100]) : new int[length];
                            outer = a;
                            Use(a);
                            Use(a);
                        }
                    }
                }
                """);
        }

        [Fact]
        public Task OutVariable_InIterator()
        {
            return VerifyAsync("""
                using System.Collections.Generic;

                class C
                {
                    static unsafe void Fill(out int value) => value = 0;

                    IEnumerable<int> M()
                    {
                        {|CS9362:Fill(out var value)|};
                        yield return value;
                    }
                }
                """, """
                using System.Collections.Generic;

                class C
                {
                    static unsafe void Fill(out int value) => value = 0;

                    IEnumerable<int> M()
                    {
                        int value;
                        // SAFETY: To be audited
                        unsafe
                        {
                            Fill(out value);
                        }
                        yield return value;
                    }
                }
                """);
        }

        [Fact]
        public Task YieldOperand_UsesExpression()
        {
            return VerifyAsync("""
                using System.Collections.Generic;

                class C
                {
                    static unsafe int Get() => 0;

                    IEnumerable<int> M()
                    {
                        yield return {|CS9362:Get()|};
                    }
                }
                """, """
                using System.Collections.Generic;

                class C
                {
                    static unsafe int Get() => 0;

                    IEnumerable<int> M()
                    {
                        yield return /* SAFETY: To be audited */ unsafe(Get());
                    }
                }
                """);
        }

        [Fact]
        public Task RefLocal_SafeLaterUses_UsesRefExpression()
        {
            return VerifyAsync("""
                class C
                {
                    struct Entry
                    {
                        public int Value;
                        public int Next;
                    }

                    void M(Entry* entries, int i, int value, int next)
                    {
                        ref Entry entry = ref entries{|CS9360:[|}i];
                        entry.Value = value;
                        entry.Next = next;
                    }
                }
                """, """
                class C
                {
                    struct Entry
                    {
                        public int Value;
                        public int Next;
                    }

                    void M(Entry* entries, int i, int value, int next)
                    {
                        ref Entry entry = ref /* SAFETY: To be audited */ unsafe(entries[i]);
                        entry.Value = value;
                        entry.Next = next;
                    }
                }
                """);
        }

        [Fact]
        public Task RefLocals_RelatedLaterUses_ClosedBlock()
        {
            return VerifyAsync("""
                class C
                {
                    static unsafe void Copy(ref byte destination, ref byte source) { }

                    void M(byte* source, byte* destination)
                    {
                        ref byte src = ref {|CS9360:*|}source;
                        ref byte dst = ref {|CS9360:*|}destination;
                        {|CS9362:Copy(ref dst, ref src)|};
                    }
                }
                """, """
                class C
                {
                    static unsafe void Copy(ref byte destination, ref byte source) { }

                    void M(byte* source, byte* destination)
                    {
                        // SAFETY: To be audited
                        unsafe
                        {
                            ref byte src = ref *source;
                            ref byte dst = ref *destination;
                            Copy(ref dst, ref src);
                        }
                    }
                }
                """);
        }

        [Fact]
        public Task MultiDeclarator_SplitsInOrder()
        {
            return VerifyAsync("""
                class C
                {
                    int M(int* p)
                    {
                        int a = {|CS9360:*|}p, b = a + {|CS9360:*|}p;
                        return a + b;
                    }
                }
                """, """
                class C
                {
                    int M(int* p)
                    {
                        int a, b;
                        // SAFETY: To be audited
                        unsafe
                        {
                            a = *p;
                            b = a + *p;
                        }
                        return a + b;
                    }
                }
                """);
        }

        [Fact]
        public Task SplitDeclaration_KeepsInlineComment()
        {
            return VerifyAsync("""
                class C
                {
                    int M(int* p)
                    {
                        {|CS9360:*|}p = 0;
                        /* important */ int a = {|CS9360:*|}p;
                        return a;
                    }
                }
                """, """
                class C
                {
                    int M(int* p)
                    {
                        /* important */ int a;
                        // SAFETY: To be audited
                        unsafe
                        {
                            *p = 0;
                            a = *p;
                        }
                        return a;
                    }
                }
                """);
        }

        [Fact]
        public Task VarWithDirectivesInInitializer_GrowsInsteadOfSplitting()
        {
            // The inferred type is `long` in the inactive arm, so hoisting `int value;` would break that configuration.
            return VerifyAsync("""
                class C
                {
                    static unsafe int GetInt() => 0;
                    static long GetLong() => 0;

                    long M()
                    {
                        var value = (
                #if NEVER
                            GetLong()
                #else
                            {|CS9362:GetInt()|}
                #endif
                            );
                        return value;
                    }
                }
                """, """
                class C
                {
                    static unsafe int GetInt() => 0;
                    static long GetLong() => 0;

                    long M()
                    {
                        // SAFETY: To be audited
                        unsafe
                        {
                            var value = (
                #if NEVER
                                GetLong()
                #else
                                GetInt()
                #endif
                                );
                            return value;
                        }
                    }
                }
                """);
        }

        [Fact]
        public Task NullableVar_SplitKeepsAnnotation()
        {
            return VerifyAsync("""
                #nullable enable
                class C
                {
                    static unsafe string? Get() => null;

                    int M()
                    {
                        var value = {|CS9362:Get()|};
                        value = null;
                        return value?.Length ?? 0;
                    }
                }
                """, """
                #nullable enable
                class C
                {
                    static unsafe string? Get() => null;

                    int M()
                    {
                        string? value;
                        // SAFETY: To be audited
                        unsafe
                        {
                            value = Get();
                        }
                        value = null;
                        return value?.Length ?? 0;
                    }
                }
                """);
        }

        [Fact]
        public Task SwitchSection_SharedScope_SplitsDeclaration()
        {
            return VerifyAsync("""
                class C
                {
                    int M(int kind, int* p)
                    {
                        switch (kind)
                        {
                            case 0:
                                int value = {|CS9360:*|}p;
                                return value;
                            default:
                                value = 1;
                                return value;
                        }
                    }
                }
                """, """
                class C
                {
                    int M(int kind, int* p)
                    {
                        switch (kind)
                        {
                            case 0:
                                int value;
                                // SAFETY: To be audited
                                unsafe
                                {
                                    value = *p;
                                }
                                return value;
                            default:
                                value = 1;
                                return value;
                        }
                    }
                }
                """);
        }
    }
}
#endif
