// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Tests for consecutive shift folding optimization in morph:
//   (x shift c1) shift c2 -> x shift (c1 + c2)
// This covers issue #74020 where repeated integer division by powers of two
// generates redundant shift instructions.

public class ShiftCombining
{
    [Fact]
    public static int TestEntryPoint()
    {
        int result = 100;

        // === RSH (>>) combining - int32 ===
        if (!TestRshCombine_Int32()) result--;
        if (!TestRshOvershift_Int32()) result--;

        // === RSZ (>>>) combining - int32 ===
        if (!TestRszCombine_Int32()) result--;
        if (!TestRszOvershift_Int32()) result--;

        // === RSH (>>) combining - int64 ===
        if (!TestRshCombine_Int64()) result--;
        if (!TestRshOvershift_Int64()) result--;

        // === RSZ (>>>) combining - int64 ===
        if (!TestRszCombine_Int64()) result--;
        if (!TestRszOvershift_Int64()) result--;

        // === Boundary cases ===
        if (!TestBoundaryCases()) result--;

        // === Division pattern (original issue #74020) ===
        if (!TestDivisionPattern()) result--;

        return result;
    }

    // --- RSH int32 ---

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestRshCombine_Int32()
    {
        // (x >> 3) >> 4  ->  x >> 7
        bool pass = true;
        pass &= Check((Shr32(100, 3) >> 4), 100 >> 7, "RSH combine 3+4 int32");
        pass &= Check((Shr32(-100, 3) >> 4), -100 >> 7, "RSH combine 3+4 int32 negative");
        pass &= Check((Shr32(int.MaxValue, 5) >> 10), int.MaxValue >> 15, "RSH combine 5+10 int32");
        pass &= Check((Shr32(int.MinValue, 2) >> 3), int.MinValue >> 5, "RSH combine 2+3 int32 MinValue");
        return pass;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestRshOvershift_Int32()
    {
        // (x >> 30) >> 5 -> x >> 31 (saturates for signed)
        bool pass = true;
        pass &= Check((Shr32(100, 20) >> 15), 100 >> 31, "RSH overshift int32 positive");
        pass &= Check((Shr32(-100, 20) >> 15), -100 >> 31, "RSH overshift int32 negative");
        pass &= Check((Shr32(int.MaxValue, 16) >> 16), int.MaxValue >> 31, "RSH overshift int32 MaxValue");
        pass &= Check((Shr32(int.MinValue, 16) >> 16), int.MinValue >> 31, "RSH overshift int32 MinValue");
        return pass;
    }

    // --- RSZ int32 ---

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestRszCombine_Int32()
    {
        // (x >>> 3) >>> 4  ->  x >>> 7
        bool pass = true;
        pass &= CheckU((Shr32U(100u, 3) >>> 4), 100u >>> 7, "RSZ combine 3+4 uint32");
        pass &= CheckU((Shr32U(uint.MaxValue, 5) >>> 10), uint.MaxValue >>> 15, "RSZ combine 5+10 uint32");
        return pass;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestRszOvershift_Int32()
    {
        // (x >>> 20) >>> 15 -> 0 (overshift for unsigned)
        bool pass = true;
        pass &= CheckU((Shr32U(100u, 20) >>> 15), 0u, "RSZ overshift uint32");
        pass &= CheckU((Shr32U(uint.MaxValue, 20) >>> 15), 0u, "RSZ overshift uint32 MaxValue");
        return pass;
    }

    // --- RSH int64 ---

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestRshCombine_Int64()
    {
        // (x >> 10) >> 20  ->  x >> 30
        bool pass = true;
        pass &= CheckL((Shr64(100L, 10) >> 20), 100L >> 30, "RSH combine 10+20 int64");
        pass &= CheckL((Shr64(-100L, 10) >> 20), -100L >> 30, "RSH combine 10+20 int64 negative");
        pass &= CheckL((Shr64(long.MaxValue, 15) >> 15), long.MaxValue >> 30, "RSH combine 15+15 int64");
        return pass;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestRshOvershift_Int64()
    {
        // (x >> 40) >> 30 -> x >> 63 (saturates for signed)
        bool pass = true;
        pass &= CheckL((Shr64(100L, 40) >> 30), 100L >> 63, "RSH overshift int64 positive");
        pass &= CheckL((Shr64(-100L, 40) >> 30), -100L >> 63, "RSH overshift int64 negative");
        return pass;
    }

    // --- RSZ int64 ---

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestRszCombine_Int64()
    {
        // (x >>> 10) >>> 20  ->  x >>> 30
        bool pass = true;
        pass &= CheckUL((Shr64U(100uL, 10) >>> 20), 100uL >>> 30, "RSZ combine 10+20 uint64");
        pass &= CheckUL((Shr64U(ulong.MaxValue, 15) >>> 15), ulong.MaxValue >>> 30, "RSZ combine 15+15 uint64");
        return pass;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestRszOvershift_Int64()
    {
        // (x >>> 40) >>> 30 -> 0 (overshift for unsigned)
        bool pass = true;
        pass &= CheckUL((Shr64U(100uL, 40) >>> 30), 0uL, "RSZ overshift uint64");
        pass &= CheckUL((Shr64U(ulong.MaxValue, 40) >>> 30), 0uL, "RSZ overshift uint64 MaxValue");
        return pass;
    }

    // --- Boundary cases ---

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestBoundaryCases()
    {
        bool pass = true;

        // Shift by 1+1 = 2
        pass &= Check((Shr32(256, 1) >> 1), 256 >> 2, "RSH 1+1 int32");

        // Shift by 15+16 = 31 (exact bitwidth - 1 for int32)
        pass &= Check((Shr32(int.MaxValue, 15) >> 16), int.MaxValue >> 31, "RSH 15+16=31 int32");

        // Shift by 31+31 = 62 (for int64, still within range)
        pass &= CheckL((Shr64(long.MaxValue, 31) >> 31), long.MaxValue >> 62, "RSH 31+31=62 int64");

        return pass;
    }

    // --- Division pattern (issue #74020) ---
    // x / 4 / 8 lowers to consecutive shifts: (x >> 2) >> 3

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestDivisionPattern()
    {
        bool pass = true;

        // x / 4 / 8 should produce same result as x / 32
        int x1 = GetValue(1024);
        pass &= Check(x1 / 4 / 8, x1 / 32, "div 4 then 8 == div 32");

        int x2 = GetValue(-1024);
        pass &= Check(x2 / 4 / 8, x2 / 32, "div 4 then 8 == div 32 negative");

        int x3 = GetValue(int.MaxValue);
        pass &= Check(x3 / 2 / 2, x3 / 4, "div 2 then 2 == div 4 MaxValue");

        return pass;
    }

    // --- Helpers ---

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Shr32(int x, int c) => x >> c;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint Shr32U(uint x, int c) => x >>> c;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Shr64(long x, int c) => x >> c;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ulong Shr64U(ulong x, int c) => x >>> c;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int GetValue(int x) => x;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Check(int actual, int expected, string label)
    {
        if (actual != expected)
        {
            Console.WriteLine($"FAIL {label}: expected {expected}, got {actual}");
            return false;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool CheckU(uint actual, uint expected, string label)
    {
        if (actual != expected)
        {
            Console.WriteLine($"FAIL {label}: expected {expected}, got {actual}");
            return false;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool CheckL(long actual, long expected, string label)
    {
        if (actual != expected)
        {
            Console.WriteLine($"FAIL {label}: expected {expected}, got {actual}");
            return false;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool CheckUL(ulong actual, ulong expected, string label)
    {
        if (actual != expected)
        {
            Console.WriteLine($"FAIL {label}: expected {expected}, got {actual}");
            return false;
        }
        return true;
    }
}
