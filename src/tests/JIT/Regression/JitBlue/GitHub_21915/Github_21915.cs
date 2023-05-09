// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Some tests for removing bounds checks based
// on byte and sbyte-based indices

public class GitHub_21915
{
    private static ReadOnlySpan<byte> A => new byte[256] {
    0, 0, 0, 7, 8, 9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    1, 1, 0, 1, 1, 1, 0, 0, 1, 1, 1, 0, 1, 1, 1, 1,
    1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 0, 1,
    1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
    1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1,
    0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
    1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0,
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte NeedsEscapingByte256(int value) => A[value];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte NeedsEscapingByte255(int value) => A.Slice(0, 255)[value];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte ByteRemoveBoundsCheck(ReadOnlySpan<byte> data, int i)
    {
        return NeedsEscapingByte256(data[i]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte ByteKeepBoundsCheck1(ReadOnlySpan<byte> data, int i)
    {
        return NeedsEscapingByte255(data[i]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte ByteKeepBoundsCheck2(ReadOnlySpan<byte> data, int i)
    {
        return NeedsEscapingByte256(data[i] + 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte SByteRemoveBoundsCheck(ReadOnlySpan<sbyte> data, int i)
    {
        return NeedsEscapingByte256(data[i] + 128);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte SByteKeepBoundsCheck1(ReadOnlySpan<sbyte> data, int i)
    {
        return NeedsEscapingByte255(data[i] + 128);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte SByteKeepBoundsCheck2(ReadOnlySpan<sbyte> data, int i)
    {
        return NeedsEscapingByte256(data[i] + 127);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        ReadOnlySpan<byte> bytes = new byte[] { 2, 3 };

        byte brbc = ByteRemoveBoundsCheck(bytes, 1);
        byte bkbc1 = ByteKeepBoundsCheck1(bytes, 1);
        byte bkbc2 = ByteKeepBoundsCheck2(bytes, 0);

        Console.WriteLine($"byte cases: {brbc} {bkbc1} {bkbc2} (expected 7 7 7)");

        ReadOnlySpan<sbyte> sbytes = new sbyte[] { -124, -125, -128 };

        byte sbrbc = SByteRemoveBoundsCheck(sbytes, 1);
        byte sbkbc1 = SByteKeepBoundsCheck1(sbytes, 1);
        byte sbkbc2 = SByteKeepBoundsCheck2(sbytes, 0);

        Console.WriteLine($"sbyte cases: {sbrbc} {sbkbc1} {sbkbc2} (expected 7 7 7)");

        bool ok = (brbc == 7) && (bkbc1 == 7) && (bkbc2 == 7) && (sbrbc == 7) && (sbkbc1 == 7) && (sbkbc2 == 7);

        try
        {
            // Check for buggy case in initial PR 21857
            // -128 + 127 should index with -1 and throw.
            SByteKeepBoundsCheck2(sbytes, 2);
            Console.WriteLine($"error: did not throw as expected");
            ok = false;
        }
        catch (IndexOutOfRangeException)
        {
            Console.WriteLine($"threw exception, as expected");
        }

        return ok ? 100 : -1;
    }
}
