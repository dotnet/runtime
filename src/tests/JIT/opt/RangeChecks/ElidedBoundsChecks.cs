// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
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
        ReadOnlySpan<byte> log2ToPow10 =
        [
            1,  1,  1,  2,  2,  2,  3,  3,  3,  4,  4,  4,  4,  5,  5,  5,
            6,  6,  6,  7,  7,  7,  7,  8,  8,  8,  9,  9,  9,  10, 10, 10,
            10, 11, 11, 11, 12, 12, 12, 13, 13, 13, 13, 14, 14, 14, 15, 15,
            15, 16, 16, 16, 16, 17, 17, 17, 18, 18, 18, 19, 19, 19, 19, 20
        ];
        return log2ToPow10[(int)ulong.Log2(value)];
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

        if (CountDigits(10000000000000000000UL) != 20)
            return 0;

        if (AndByConst(0) != 1)
            return 0;

        if (AndByConst(255) != 3)
            return 0;

        if (AndByLength(0) != 1)
            return 0;

        if (AndByLength(255) != 4)
            return 0;

        return 100;
    }
}
