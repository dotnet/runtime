// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// When BitOperations.RotateRight is constant-folded with an unsigned
// result that uses the full 32-bit unsigned range (e.g. 0xFFFFFFFF),
// BashToConst would hit an assertion that only checked the signed
// int32_t range. The fix allows unsigned types to use their full range.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_129099
{
    // Use a sink to prevent the JIT from folding the entire expression
    // at import time — we need the constant to survive to the morph/BashToConst path.
    private static uint s_sink;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint RotateRightMax()
    {
        // RotateRight(0xFFFFFFFFu, 1) = 0xFFFFFFFF
        // This is a TYP_UINT constant with value 0xFFFFFFFF which is
        // valid as uint32 but not as int32.
        uint v1 = BitOperations.RotateRight(0xFFFFFFFFu, 1);
        s_sink = v1;
        return v1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint RotateLeftMax()
    {
        // RotateLeft(0x80000000u, 1) = 0x00000001 (but let's also test
        // RotateLeft(0xFFFFFFFFu, 1) = 0xFFFFFFFE which also exceeds int32 max)
        uint v1 = BitOperations.RotateLeft(0xFFFFFFFFu, 1);
        s_sink = v1;
        return v1;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        uint result = RotateRightMax();
        // RotateRight(0xFFFFFFFF, 1) = 0xFFFFFFFF
        // (0xFFFFFFFF >> 1) | (0xFFFFFFFF << 31) = 0x7FFFFFFF | 0x80000000 = 0xFFFFFFFF
        Assert.Equal(0xFFFFFFFFu, result);

        uint result2 = RotateLeftMax();
        // RotateLeft(0xFFFFFFFF, 1) = 0xFFFFFFFF
        // (0xFFFFFFFF << 1) | (0xFFFFFFFF >> 31) = 0xFFFFFFFE | 0x00000001 = 0xFFFFFFFF
        Assert.Equal(0xFFFFFFFFu, result2);
    }
}
