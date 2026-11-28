// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// If-conversion of guarded loads (APX CFCMOV on x64): "c ? p.f : y" becomes a
// conditional move whose memory operand is only accessed when c holds. Each case is
// run with the guard both true and false, including a null or otherwise invalid
// address on the false path, and with a faulting address on the true path, which
// must still throw NullReferenceException.
//
// The condition code is encoded in the low bits of the CFCMOV opcode, so every integer
// condition is covered at both operand sizes; an inverted condition returns the wrong
// value or faults on the null address.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public unsafe class ApxCfcmovGuardedLoad
{
    class Node
    {
        public int Value;
        public long Wide;
        public int Last;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int NullOrValue(Node n) => n == null ? 0 : n.Value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ValueOrDefault(Node n, int d) => n != null ? n.Value : d;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int LastOrMinusOne(Node n)
    {
        int x = -1;
        if (n != null)
        {
            x = n.Last;
        }
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long WideOrDefault(Node n, long d)
    {
        long x = d;
        if (n != null)
        {
            x = n.Wide;
        }
        return x;
    }

    // The guard does not imply the address is valid, so the true path can fault.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ValueIfFlag(bool flag, Node n) => flag ? n.Value : 7;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ValueIfPositive(int k, Node n)
    {
        int x = k;
        if (k > 0)
        {
            x = n.Value;
        }
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int DerefOrZero(int* p) => p != null ? *p : 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long DerefOffsetOrZero(long* p) => p == null ? 0 : p[1];

    // An element address that is only valid when the guard holds.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int FifthOrMinusOne(int[] a) => a.Length > 5 ? a[5] : -1;

    // The Else value is a store in the condition's block. The guarded load can throw
    // into the handler, so that store must still be visible there. x starts non-zero so
    // that "x = 7" is not the local's explicit init, which if-conversion would not move.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ValueIfFlagInTry(bool flag, Node n)
    {
        int x = 3;
        try
        {
            x = 7;
            if (flag)
            {
                x = n.Value;
            }
        }
        catch (NullReferenceException)
        {
            return x;
        }
        return x;
    }

    // One method per condition, with a 32-bit and a 64-bit load.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ValueIfEqual(int k, int m, Node n, int d) => k == m ? n.Value : d;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ValueIfLess(int k, int m, Node n, int d) => k < m ? n.Value : d;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ValueIfGreaterOrEqual(int k, int m, Node n, int d) => k >= m ? n.Value : d;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ValueIfLessOrEqual(int k, int m, Node n, int d) => k <= m ? n.Value : d;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ValueIfGreater(int k, int m, Node n, int d) => k > m ? n.Value : d;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ValueIfBelow(int k, int m, Node n, int d) => (uint)k < (uint)m ? n.Value : d;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ValueIfAboveOrEqual(int k, int m, Node n, int d) => (uint)k >= (uint)m ? n.Value : d;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ValueIfBelowOrEqual(int k, int m, Node n, int d) => (uint)k <= (uint)m ? n.Value : d;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ValueIfAbove(int k, int m, Node n, int d) => (uint)k > (uint)m ? n.Value : d;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ValueIfNegative(int k, int m, Node n, int d) => k < 0 ? n.Value : d;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ValueIfNotNegative(int k, int m, Node n, int d) => k >= 0 ? n.Value : d;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long WideIfEqual(int k, int m, Node n, long d) => k == m ? n.Wide : d;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long WideIfLess(int k, int m, Node n, long d) => k < m ? n.Wide : d;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long WideIfGreaterOrEqual(int k, int m, Node n, long d) => k >= m ? n.Wide : d;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long WideIfLessOrEqual(int k, int m, Node n, long d) => k <= m ? n.Wide : d;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long WideIfGreater(int k, int m, Node n, long d) => k > m ? n.Wide : d;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long WideIfBelow(int k, int m, Node n, long d) => (uint)k < (uint)m ? n.Wide : d;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long WideIfAboveOrEqual(int k, int m, Node n, long d) => (uint)k >= (uint)m ? n.Wide : d;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long WideIfBelowOrEqual(int k, int m, Node n, long d) => (uint)k <= (uint)m ? n.Wide : d;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long WideIfAbove(int k, int m, Node n, long d) => (uint)k > (uint)m ? n.Wide : d;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long WideIfNegative(int k, int m, Node n, long d) => k < 0 ? n.Wide : d;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long WideIfNotNegative(int k, int m, Node n, long d) => k >= 0 ? n.Wide : d;

    // Pairs that tell signed from unsigned and strict from non-strict comparisons apart.
    static readonly (int, int)[] s_operands = { (1, 2), (2, 1), (2, 2), (-1, 1), (1, -1), (0, 0), (-2, -2) };

    static void CheckCondition(Func<int, int, Node, int, int> f, Func<int, int, bool> condition, Node n)
    {
        foreach ((int k, int m) in s_operands)
        {
            if (condition(k, m))
            {
                Assert.Equal(42, f(k, m, n, 5));
                Assert.Throws<NullReferenceException>(() => f(k, m, null, 5));
            }
            else
            {
                Assert.Equal(5, f(k, m, n, 5));
                Assert.Equal(5, f(k, m, null, 5));
            }
        }
    }

    static void CheckCondition(Func<int, int, Node, long, long> f, Func<int, int, bool> condition, Node n)
    {
        foreach ((int k, int m) in s_operands)
        {
            if (condition(k, m))
            {
                Assert.Equal(0x1_0000_0002L, f(k, m, n, 3));
                Assert.Throws<NullReferenceException>(() => f(k, m, null, 3));
            }
            else
            {
                Assert.Equal(3L, f(k, m, n, 3));
                Assert.Equal(3L, f(k, m, null, 3));
            }
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Node n = new Node { Value = 42, Wide = 0x1_0000_0002, Last = 9 };

        Assert.Equal(0, NullOrValue(null));
        Assert.Equal(42, NullOrValue(n));
        Assert.Equal(5, ValueOrDefault(null, 5));
        Assert.Equal(42, ValueOrDefault(n, 5));
        Assert.Equal(-1, LastOrMinusOne(null));
        Assert.Equal(9, LastOrMinusOne(n));
        Assert.Equal(3L, WideOrDefault(null, 3));
        Assert.Equal(0x1_0000_0002L, WideOrDefault(n, 3));

        Assert.Equal(7, ValueIfFlag(false, null));
        Assert.Equal(42, ValueIfFlag(true, n));
        Assert.Throws<NullReferenceException>(() => ValueIfFlag(true, null));

        Assert.Equal(-3, ValueIfPositive(-3, null));
        Assert.Equal(42, ValueIfPositive(1, n));
        Assert.Throws<NullReferenceException>(() => ValueIfPositive(1, null));

        Assert.Equal(7, ValueIfFlagInTry(false, null));
        Assert.Equal(42, ValueIfFlagInTry(true, n));
        Assert.Equal(7, ValueIfFlagInTry(true, null));

        int i = 11;
        long* l = stackalloc long[2];
        l[1] = -8;
        Assert.Equal(0, DerefOrZero(null));
        Assert.Equal(11, DerefOrZero(&i));
        Assert.Equal(0L, DerefOffsetOrZero(null));
        Assert.Equal(-8L, DerefOffsetOrZero(l));

        Assert.Equal(-1, FifthOrMinusOne(new int[2]));
        Assert.Equal(6, FifthOrMinusOne(new[] { 1, 2, 3, 4, 5, 6 }));

        CheckCondition(ValueIfEqual, (k, m) => k == m, n);
        CheckCondition(ValueIfLess, (k, m) => k < m, n);
        CheckCondition(ValueIfGreaterOrEqual, (k, m) => k >= m, n);
        CheckCondition(ValueIfLessOrEqual, (k, m) => k <= m, n);
        CheckCondition(ValueIfGreater, (k, m) => k > m, n);
        CheckCondition(ValueIfBelow, (k, m) => (uint)k < (uint)m, n);
        CheckCondition(ValueIfAboveOrEqual, (k, m) => (uint)k >= (uint)m, n);
        CheckCondition(ValueIfBelowOrEqual, (k, m) => (uint)k <= (uint)m, n);
        CheckCondition(ValueIfAbove, (k, m) => (uint)k > (uint)m, n);
        CheckCondition(ValueIfNegative, (k, m) => k < 0, n);
        CheckCondition(ValueIfNotNegative, (k, m) => k >= 0, n);

        CheckCondition(WideIfEqual, (k, m) => k == m, n);
        CheckCondition(WideIfLess, (k, m) => k < m, n);
        CheckCondition(WideIfGreaterOrEqual, (k, m) => k >= m, n);
        CheckCondition(WideIfLessOrEqual, (k, m) => k <= m, n);
        CheckCondition(WideIfGreater, (k, m) => k > m, n);
        CheckCondition(WideIfBelow, (k, m) => (uint)k < (uint)m, n);
        CheckCondition(WideIfAboveOrEqual, (k, m) => (uint)k >= (uint)m, n);
        CheckCondition(WideIfBelowOrEqual, (k, m) => (uint)k <= (uint)m, n);
        CheckCondition(WideIfAbove, (k, m) => (uint)k > (uint)m, n);
        CheckCondition(WideIfNegative, (k, m) => k < 0, n);
        CheckCondition(WideIfNotNegative, (k, m) => k >= 0, n);
    }
}
