// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class ElidedBoundsChecks
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ComplexBinaryOperators(byte inData)
    {
        // X64-NOT: CORINFO_HELP_RNGCHKFAIL
        // ARM64-NOT: CORINFO_HELP_RNGCHKFAIL
        ReadOnlySpan<byte> base64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/="u8;
        return base64[((inData & 0x03) << 4) | ((inData & 0xf0) >> 4)];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool LastCharCheck(string prefix, string path)
    {
        // X64-NOT: CORINFO_HELP_RNGCHKFAIL
        // ARM64-NOT: CORINFO_HELP_RNGCHKFAIL
        if (prefix.Length < path.Length)
            return (path[prefix.Length] == '/');
        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static nint CountDigits(ulong value)
    {
        // X64-NOT: CORINFO_HELP_RNGCHKFAIL
        // ARM64-NOT: CORINFO_HELP_RNGCHKFAIL
        if (Lzcnt.X64.IsSupported || ArmBase.Arm64.IsSupported)
        {
            ReadOnlySpan<byte> log2ToPow10 =
            [
                1,  1,  1,  2,  2,  2,  3,  3,  3,  4,  4,  4,  4,  5,  5,  5,
                6,  6,  6,  7,  7,  7,  7,  8,  8,  8,  9,  9,  9,  10, 10, 10,
                10, 11, 11, 11, 12, 12, 12, 13, 13, 13, 13, 14, 14, 14, 15, 15,
                15, 16, 16, 16, 16, 17, 17, 17, 18, 18, 18, 19, 19, 19, 19, 20
            ];
            return log2ToPow10[(int)ulong.Log2(value)];
        }
        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte AndByConst(int i)
    {
        // X64-NOT: CORINFO_HELP_RNGCHKFAIL
        // ARM64-NOT: CORINFO_HELP_RNGCHKFAIL
        ReadOnlySpan<byte> span = new byte[] { 1, 2, 3, 4 };
        return span[i & 2];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte AndByLength(int i)
    {
        // X64-NOT: CORINFO_HELP_RNGCHKFAIL
        // ARM64-NOT: CORINFO_HELP_RNGCHKFAIL
        ReadOnlySpan<byte> span = new byte[] { 1, 2, 3, 4 };
        return span[i & (span.Length - 1)];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IndexPlusConstLessThanLen(ReadOnlySpan<char> span)
    {
        // X64-NOT: CORINFO_HELP_RNGCHKFAIL
        // ARM64-NOT: CORINFO_HELP_RNGCHKFAIL
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i] == '%' && (uint)(i + 2) < (uint)span.Length)
            {
                if (span[i + 1] == 'F' && span[i + 2] == 'F')
                {
                    return true;
                }
            }
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TryStripFirstChar(ref ReadOnlySpan<char> span, char value)
    {
        // X64-NOT: CORINFO_HELP_RNGCHKFAIL
        // ARM64-NOT: CORINFO_HELP_RNGCHKFAIL
        if (!span.IsEmpty && span[0] == value)
        {
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Sum4Increasing(int[] a) => a[0] + a[1] + a[2] + a[3];

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Sum4Span(ReadOnlySpan<int> s) => s[0] + s[1] + s[2] + s[3];

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Sum4MixedOrder(int[] a) => a[2] + a[3] + a[0] + a[1];

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int DivBetweenBCs(int[] a, int divisor)
    {
        // The divide must not be reordered with a[5]: when divisor == 0 we
        // must observe DivideByZeroException, not IndexOutOfRangeException.
        int x = a[3];
        int y = 100 / divisor;
        return x + y + a[5];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int NreBetweenBCs(int[] a, int[] b)
    {
        // First touch of b may throw NRE; that must not be reordered with
        // a[5]: when b == null we must observe NullReferenceException.
        int x = a[3];
        int y = b.Length;
        return x + y + a[5];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int LocalLiveInCatch(int[] a)
    {
        // The store `x = 99` is between two BCs in a try block whose local is
        // live in the catch. It must act as a barrier: if a[3]'s BC were
        // strengthened to length 6, the IOOB would fire before x=99 and the
        // catch would observe x == -1 instead of 99.
        int x = -1;
        try
        {
            int t = a[3];
            x = 99;
            return t + a[5];
        }
        catch (IndexOutOfRangeException)
        {
            return x;
        }
    }

    static int s_finallyObserved;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int LocalLiveInFinally(int[] a)
    {
        // Same idea, but the local is live into a finally rather than a catch.
        int x = -1;
        try
        {
            int t = a[3];
            x = 99;
            return t + a[5];
        }
        finally
        {
            s_finallyObserved = x;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        if (ComplexBinaryOperators(0xFF) != (byte)'/')
            return 0;

        if (LastCharCheck("abc", "abcd") != false)
            return 0;

        if (LastCharCheck("abc", "abc/def") != true)
            return 0;

        if (CountDigits(1) != 1)
            return 0;

        if (AndByConst(0) != 1)
            return 0;

        if (AndByConst(255) != 3)
            return 0;

        if (AndByLength(0) != 1)
            return 0;

        if (AndByLength(255) != 4)
            return 0;

        if (IndexPlusConstLessThanLen("%FF".AsSpan()) != true)
            return 0;

        if (IndexPlusConstLessThanLen("%F".AsSpan()) != false)
            return 0;

        if (IndexPlusConstLessThanLen("hello".AsSpan()) != false)
            return 0;

        ReadOnlySpan<char> chars = "hello".AsSpan();
        if (TryStripFirstChar(ref chars, 'h') != true)
            return 0;

        chars = ReadOnlySpan<char>.Empty;
        if (TryStripFirstChar(ref chars, 'h') != false)
            return 0;

        // Bounds-check coalescing: 4 constant indices, same length VN.
        int[] arr4 = new int[] { 10, 20, 30, 40 };
        if (Sum4Increasing(arr4) != 100)
            return 0;
        if (Sum4Span(arr4) != 100)
            return 0;
        if (Sum4MixedOrder(arr4) != 100)
            return 0;

        // Short array: must throw IndexOutOfRangeException.
        Assert.Throws<IndexOutOfRangeException>(() => Sum4Increasing(new int[3]));
        Assert.Throws<IndexOutOfRangeException>(() => Sum4MixedOrder(new int[3]));

        // Exception ordering must be preserved across non-IOOB throwers.
        int[] arr6 = new int[] { 1, 2, 3, 4, 5, 6 };
        if (DivBetweenBCs(arr6, 5) != (arr6[3] + 100 / 5 + arr6[5]))
            return 0;

        // divisor == 0 with a too short for a[5]: must be DivideByZero, not IOOB.
        Assert.Throws<DivideByZeroException>(() => DivBetweenBCs(new int[4], 0));

        // b == null with a too short for a[5]: must be NRE, not IOOB.
        Assert.Throws<NullReferenceException>(() => NreBetweenBCs(new int[4], null));

        // Local live in catch handler: a[3]'s BC must not be strengthened to
        // a[5] across the `x = 99` store, otherwise the catch would see -1.
        if (LocalLiveInCatch(new int[4]) != 99)
            return 0;

        // Local live in finally: same constraint, observed via static field.
        s_finallyObserved = 0;
        try
        {
            LocalLiveInFinally(new int[4]);
            return 0;
        }
        catch (IndexOutOfRangeException) { }
        if (s_finallyObserved != 99)
            return 0;

        return 100;
    }
}
