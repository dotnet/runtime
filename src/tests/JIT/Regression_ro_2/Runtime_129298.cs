// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Testcase exposed an ROR node with an out of range operand on arm64,
// asserted in lowering

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_129298
{
    private static volatile uint Input_p0 = 1;
    private static volatile uint Input_p1 = 1;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static uint Fn(uint p0, uint p1)
    {
        unchecked
        {
            uint v1, v3, v5, v6, v8, v11, v15, v16, v22, v26, v28;
            uint v33 = 0, v40, v44;
            uint v21 = 0;
            int v2, v4, v7, v9, v10, v29;
            v1 = p0 % p1;
            v2 = BitOperations.LeadingZeroCount(v1); v3 = (uint)v2;
            v4 = BitOperations.LeadingZeroCount(v1); v5 = (uint)v4;
            v6 = BitOperations.RotateLeft(p0, 0);
            v7 = BitOperations.IsPow2(0x80000000u) ? 1 : 0; v8 = (uint)v7;
            v9 = (p1 < p0) ? 1 : 0;
            if (v9 == 0)
            {
                v22 = Math.Min(0xFFFFFFFEu, 0xFFFFFFFFu);
                return v22;
            }
            v10 = (v1 > p1) ? 1 : 0; v11 = (uint)v10;
            v15 = v6 + 0x12345u; v16 = v11 + p1;
            v26 = BitOperations.RotateLeft(0xFFFFFFFFu, (int)v21);
            v28 = p1 ^ 3u;
            v29 = (v15 <= v16) ? 1 : 0;
            if (v29 != 0)
            {
                v33 = (uint)BitOperations.TrailingZeroCount(v16);
                return v33 ^ v5;
            }
            // unreached at runtime; v33 = 0 (default-init) reaches Lowering.
            v40 = BitOperations.RotateLeft(p0, (int)(0x7FFFFFFEu % v33));
            v44 = v40 ^ v5;
            return v44;
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        // The JIT must compile Fn (including the dead rotate block) under
        // FullOpts without asserting. For the given inputs Fn takes the first
        // early return, so the result is Math.Min(0xFFFFFFFE, 0xFFFFFFFF).
        Assert.Equal(0xFFFFFFFEu, Fn(Input_p0, Input_p1));
    }
}
