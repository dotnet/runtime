// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_10337;

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Tests for the Lowering::OptimizeConstCompare TYP_BYTE narrowing path.
// Each method exercises a compare against a constant that fits in INT8 with
// an operand whose effective type is TYP_BYTE (the result of `(sbyte)x`).
// The narrowing must produce the same result as the canonical
// sign-extend-and-compare path for every input.

public class Runtime_10337
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Lt_M64(byte x) => ((sbyte)x) < -64;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Le_M64(byte x) => ((sbyte)x) <= -64;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Gt_M64(byte x) => ((sbyte)x) > -64;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Ge_M64(byte x) => ((sbyte)x) >= -64;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Eq_M1(byte x) => ((sbyte)x) == -1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Ne_M1(byte x) => ((sbyte)x) != -1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Lt_Max(byte x) => ((sbyte)x) < 127;

    // op2 == 0 with LT/GE must keep using the sign-bit-shift codegen.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Lt_Zero(byte x) => ((sbyte)x) < 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Ge_Zero(byte x) => ((sbyte)x) >= 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Eq_Zero(byte x) => ((sbyte)x) == 0;

    // Int source so the cast is doing real truncation, not just reinterpretation.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Lt_IntSrc(int x) => ((sbyte)x) < -64;

    // CAST(BYTE) over AND -- exercises the OR/XOR/AND narrowing branch.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Lt_And(int x) => ((sbyte)(x & 0xF0)) < -16;

    // Memory operand: the comparison should contain the load.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe bool Lt_Mem(byte* p) => ((sbyte)*p) < -64;

    [Fact]
    public static int TestEntryPoint()
    {
        bool ok = true;

        for (int x = 0; x < 256; x++)
        {
            byte b = (byte)x;
            sbyte sb = (sbyte)b;

            ok &= Lt_M64(b)  == (sb < -64);
            ok &= Le_M64(b)  == (sb <= -64);
            ok &= Gt_M64(b)  == (sb > -64);
            ok &= Ge_M64(b)  == (sb >= -64);
            ok &= Eq_M1(b)   == (sb == -1);
            ok &= Ne_M1(b)   == (sb != -1);
            ok &= Lt_Max(b)  == (sb < 127);
            ok &= Lt_Zero(b) == (sb < 0);
            ok &= Ge_Zero(b) == (sb >= 0);
            ok &= Eq_Zero(b) == (sb == 0);

            // Wider int sources, including patterns with garbage in the
            // upper bits so we exercise truncation semantics.
            int[] ints = { x, x ^ unchecked((int)0xFFFFFF00), (x << 8) | x, ~x };
            foreach (int xs in ints)
            {
                ok &= Lt_IntSrc(xs) == (((sbyte)xs) < -64);
                ok &= Lt_And(xs)    == (((sbyte)(xs & 0xF0)) < -16);
            }

            unsafe
            {
                byte bb = b;
                ok &= Lt_Mem(&bb) == (sb < -64);
            }
        }

        return ok ? 100 : 1;
    }
}
