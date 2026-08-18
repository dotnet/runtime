// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public static class CheckedContext
{
    static int s_intField;
    static uint s_uintField;
    static long s_longField;
    static short s_shortField;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static T OpaqueVal<T>(T v) => v;

    // =====================================================================
    //  ADD — safe (overflow flag should be removed)
    // =====================================================================

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int AddArrayLengthSmallConst(int[] arr)
    {
        // Array.Length is in [0..0x7FFFFFC7], so adding 10 cannot overflow.
        return checked(arr.Length + 10);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int AddTwoPositiveRanges(int a, int b)
    {
        if (a > 0 && a < 1000)
            if (b > 0 && b < 1000)
                return checked(a + b);
        return -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int AddNegativeRanges(int a, int b)
    {
        if (a > -500 && a < 0)
            if (b > -500 && b < 0)
                return checked(a + b);
        return -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int AddMixedSignRanges(int a, int b)
    {
        // a in [-100..100], b in [-100..100], sum in [-200..200] — safe
        if (a > -100 && a < 100)
            if (b > -100 && b < 100)
                return checked(a + b);
        return -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int AddSpanLengthSmallConst(Span<byte> span)
    {
        // Span.Length is in [0..0x7FFFFFFF] but after the guard it's [0..99]
        if (span.Length >= 100)
            return -1;
        return checked(span.Length + 50);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int AddFieldWithGuard()
    {
        int v = s_intField;
        if (v < 0 || v > 10_000)
            return -1;
        return checked(v + 10_000);
    }

    // =====================================================================
    //  ADD — must overflow
    // =====================================================================

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int AddOverflow_MaxPlusOne(int a)
    {
        // Even with range analysis, if a is near int.MaxValue, addition must overflow.
        return checked(a + 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int AddOverflow_TwoLargeRanges(int a, int b)
    {
        if (a > 1_000_000_000 && a < int.MaxValue)
            if (b > 1_000_000_000 && b < int.MaxValue)
                return checked(a + b);
        return -1;
    }

    // =====================================================================
    //  SUB — safe (overflow flag should be removed)
    // =====================================================================

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int SubTwoSmallPositives(int a, int b)
    {
        if (a > 10 && a < 100)
            if (b > 20 && b < 200)
                return checked(a - b);
        return -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int SubArrayLengths(int[] arr1, int[] arr2)
    {
        // Both lengths in [0..0x7FFFFFC7], so difference is always in int range.
        return checked(arr1.Length - arr2.Length);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int SubNarrowNegativeRange(int a, int b)
    {
        // a in [-50..-1], b in [-50..-1], difference in [-49..49] — safe
        if (a >= -50 && a <= -1)
            if (b >= -50 && b <= -1)
                return checked(a - b);
        return -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int SubFieldWithGuard()
    {
        int v = s_intField;
        if (v < -10_000 || v > 10_000)
            return -1;
        return checked(v - 5_000);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int SubSpanLengths(Span<int> s1, Span<int> s2)
    {
        if (s1.Length > 1000 || s2.Length > 1000)
            return -1;
        return checked(s1.Length - s2.Length);
    }

    // =====================================================================
    //  SUB — must overflow
    // =====================================================================

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int SubOverflow_MinMinusOne(int a)
    {
        return checked(a - 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int SubOverflow_MaxMinusNeg(int a, int b)
    {
        // a large positive, b large negative => a - b overflows
        if (a > 1_000_000_000)
            if (b < -1_000_000_000)
                return checked(a - b);
        return -1;
    }

    // =====================================================================
    //  MUL — safe (overflow flag should be removed)
    // =====================================================================

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int MulSpanLengthSmall(Span<int> span)
    {
        if (span.Length >= 100)
            return -1;
        return checked(span.Length * 10);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int MulTwoSmallPositives(int a, int b)
    {
        if (a > 0 && a < 100)
            if (b > 0 && b < 100)
                return checked(a * b);
        return -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int MulSmallNegatives(int a, int b)
    {
        // a in [-10..-1], b in [-10..-1], product in [1..100] — safe
        if (a >= -10 && a <= -1)
            if (b >= -10 && b <= -1)
                return checked(a * b);
        return -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int MulMixedSign(int a, int b)
    {
        // a in [-50..50], b in [1..10], product in [-500..500] — safe
        if (a > -50 && a < 50)
            if (b > 0 && b < 10)
                return checked(a * b);
        return -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int MulArrayLengthBySmall(int[] arr)
    {
        if (arr.Length > 1000)
            return -1;
        return checked(arr.Length * 4);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int MulFieldWithGuard()
    {
        int v = s_intField;
        if (v < -100 || v > 100)
            return -1;
        return checked(v * 100);
    }

    // =====================================================================
    //  MUL — must overflow
    // =====================================================================

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int MulOverflow_LargeRanges(int a, int b)
    {
        if (a > 50_000)
            if (b > 50_000)
                return checked(a * b);
        return -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int MulOverflow_NegMaxRange(int a, int b)
    {
        // Negative * large positive can overflow
        if (a < -50_000)
            if (b > 50_000)
                return checked(a * b);
        return -1;
    }

    // =====================================================================
    //  Chained operations — range narrows through multiple ops
    // =====================================================================

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ChainedAddMul(int a, int b)
    {
        if (a > 0 && a < 50)
            if (b > 0 && b < 50)
            {
                int sum = checked(a + b);  // [2..98]
                return checked(sum * 10);  // [20..980] — safe
            }
        return -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ChainedSubAdd(int a, int b, int c)
    {
        if (a > 10 && a < 100)
            if (b > 10 && b < 100)
                if (c > 0 && c < 50)
                {
                    int diff = checked(a - b);  // [-88..88]
                    return checked(diff + c);    // [-88..137] — safe
                }
        return -1;
    }

    // =====================================================================
    //  Operations with array / span length (natural range [0..MaxArrayLen])
    // =====================================================================

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ArrayLengthAddAndMul(int[] arr)
    {
        if (arr.Length > 100)
            return -1;
        int len = arr.Length;           // [0..100]
        int sum = checked(len + 5);     // [5..105]
        return checked(sum * 2);        // [10..210] — safe
    }

    // =====================================================================
    //  Multiple fields — combining narrowed field ranges
    // =====================================================================

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TwoFieldsAdd()
    {
        int a = s_intField;
        int b = OpaqueVal(42);
        if (a < 0 || a > 1000)
            return -1;
        if (b < 0 || b > 1000)
            return -1;
        return checked(a + b);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TwoFieldsSub()
    {
        int a = s_intField;
        int b = OpaqueVal(42);
        if (a < 0 || a > 1000)
            return -1;
        if (b < 0 || b > 1000)
            return -1;
        return checked(a - b);
    }

    // =====================================================================
    //  Guard via cast (unsigned to signed) — range inferred from type
    // =====================================================================

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int AddWithByteOperands(byte[] data, int idx)
    {
        if ((uint)idx >= (uint)data.Length)
            return -1;
        // Each byte in [0..255], so sum in [0..510] — safe
        byte a = data[idx];
        byte b = data[0];
        return checked(a + b);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int MulWithShortOperands(short x, short y)
    {
        if (x < -100 || x > 100)
            return -1;
        if (y < -100 || y > 100)
            return -1;
        // Product in [-10000..10000] — safe for int
        return checked(x * y);
    }

    // =====================================================================
    //  Zero and identity edge cases
    // =====================================================================

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int AddZero(int[] arr)
    {
        return checked(arr.Length + 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int SubZero(int[] arr)
    {
        return checked(arr.Length - 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int MulOne(int[] arr)
    {
        if (arr.Length > 1000) return -1;
        return checked(arr.Length * 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int MulZero(int[] arr)
    {
        return checked(arr.Length * 0);
    }

    // =====================================================================
    //  Boundary-tight ranges — just barely safe
    // =====================================================================

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int AddBarelyFits(int a, int b)
    {
        // a in [0..1_073_741_823], b in [0..1_073_741_823]
        // sum max = 2_147_483_646 = int.MaxValue - 1 — safe
        if (a < 0 || a > 1_073_741_823)
            return -1;
        if (b < 0 || b > 1_073_741_823)
            return -1;
        return checked(a + b);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int SubBarelyFits(int a, int b)
    {
        // a = int.MinValue + 500, b in [0..500], sub min = int.MinValue — safe
        if (a < int.MinValue + 500 || a > 0)
            return -1;
        if (b < 0 || b > 500)
            return -1;
        return checked(a - b);
    }

    // =====================================================================
    //  Multiple operands via helper methods — ensure the JIT tracks ranges
    //  through calls that return narrowed values.
    // =====================================================================

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ClampedValue(int v)
    {
        if (v < 0) return 0;
        if (v > 1000) return 1000;
        return v;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int AddClampedValues(int a, int b)
    {
        int ca = ClampedValue(a);
        int cb = ClampedValue(b);
        return checked(ca + cb);
    }

    // =====================================================================
    //  Loop-carried range — index variable stays in range
    // =====================================================================

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int LoopAddSmallIndex(int[] arr)
    {
        int sum = 0;
        for (int i = 0; i < arr.Length && i < 100; i++)
        {
            sum += checked(i + arr.Length);
        }
        return sum;
    }

    // =====================================================================
    //  Unsigned ADD — safe
    // =====================================================================

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint UnsignedAddSmall(uint a, uint b)
    {
        if (a > 1000u) return 0;
        if (b > 1000u) return 0;
        return checked(a + b);
    }

    // =====================================================================
    //  Unsigned ADD — must overflow
    // =====================================================================

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint UnsignedAddOverflow(uint a, uint b)
    {
        return checked(a + b);
    }

    // =====================================================================
    //  Unsigned MUL — safe
    // =====================================================================

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint UnsignedMulSmall(uint a, uint b)
    {
        if (a > 1000u) return 0;
        if (b > 1000u) return 0;
        return checked(a * b);
    }

    // =====================================================================
    //  Unsigned MUL — must overflow
    // =====================================================================

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint UnsignedMulOverflow(uint a)
    {
        return checked(a * a);
    }

    // =====================================================================
    //  Unsigned SUB — safe
    // =====================================================================

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint UnsignedSubSafe(uint a, uint b)
    {
        if (a < b) return 0;
        if (a > 1000u) return 0;
        return checked(a - b);
    }

    // =====================================================================
    //  Unsigned SUB — must overflow (underflow)
    // =====================================================================

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint UnsignedSubOverflow(uint a, uint b)
    {
        return checked(a - b);
    }

    // =====================================================================
    //  Tests exercising field loads and opaque helpers so the JIT
    //  must rely on assertion-propagated range info.
    // =====================================================================

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int FieldAddThenMul()
    {
        int v = s_intField;
        if (v < 0 || v > 100)
            return -1;
        int sum = checked(v + 50);  // [50..150]
        return checked(sum * 10);   // [500..1500] — safe
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int FieldSubThenAdd()
    {
        int v = s_intField;
        if (v < -200 || v > 200)
            return -1;
        int diff = checked(v - 100);  // [-300..100]
        return checked(diff + 500);   // [200..600] — safe
    }

    // =====================================================================
    //  Conditional paths — overflow only on one branch
    // =====================================================================

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ConditionalSafeAdd(int a, int b, bool useLarge)
    {
        if (useLarge)
        {
            // No guard → might overflow
            return checked(a + b);
        }
        else
        {
            if (a > 0 && a < 500)
                if (b > 0 && b < 500)
                    return checked(a + b); // safe
            return -1;
        }
    }

    // =====================================================================
    //  Operations with a single range-narrowed operand and a constant
    // =====================================================================

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int AddNarrowedPlusConst(int a)
    {
        if (a < 0 || a > 100)
            return -1;
        return checked(a + 2_000_000_000);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int SubNarrowedMinusConst(int a)
    {
        if (a < 0 || a > 100)
            return -1;
        return checked(a - 2_000_000_000);
    }

    // =====================================================================
    //  CAST — safe (checked context should be removed)
    // =====================================================================

    // --- Signed int → smaller signed types ---

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte CastIntToByte_Guarded(int a)
    {
        if (a < 0 || a > 255)
            return 0;
        return checked((byte)a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static sbyte CastIntToSByte_Guarded(int a)
    {
        if (a < -128 || a > 127)
            return 0;
        return checked((sbyte)a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static short CastIntToShort_Guarded(int a)
    {
        if (a < -32768 || a > 32767)
            return 0;
        return checked((short)a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ushort CastIntToUShort_Guarded(int a)
    {
        if (a < 0 || a > 65535)
            return 0;
        return checked((ushort)a);
    }

    // --- Unsigned int → smaller types ---

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte CastUIntToByte_Guarded(uint a)
    {
        if (a > 255)
            return 0;
        return checked((byte)a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ushort CastUIntToUShort_Guarded(uint a)
    {
        if (a > 65535)
            return 0;
        return checked((ushort)a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static short CastUIntToShort_Guarded(uint a)
    {
        if (a > 32767)
            return 0;
        return checked((short)a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static sbyte CastUIntToSByte_Guarded(uint a)
    {
        if (a > 127)
            return 0;
        return checked((sbyte)a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int CastUIntToInt_Guarded(uint a)
    {
        if (a > (uint)int.MaxValue)
            return 0;
        return checked((int)a);
    }

    // --- Tighter range guards (range well inside target type) ---

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte CastIntToByte_TightRange(int a)
    {
        if (a < 10 || a > 100)
            return 0;
        return checked((byte)a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static sbyte CastIntToSByte_PositiveOnly(int a)
    {
        if (a < 0 || a > 50)
            return 0;
        return checked((sbyte)a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static sbyte CastIntToSByte_NegativeRange(int a)
    {
        if (a < -100 || a > -1)
            return 0;
        return checked((sbyte)a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static short CastIntToShort_NarrowRange(int a)
    {
        if (a < -1000 || a > 1000)
            return 0;
        return checked((short)a);
    }

    // --- Cast from non-LCL_VAR (field load) — overflow flag cleared, cast kept ---

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte CastFieldToByte()
    {
        int v = s_intField;
        if (v < 0 || v > 200)
            return 0;
        return checked((byte)v);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static short CastFieldToShort()
    {
        int v = s_intField;
        if (v < -1000 || v > 1000)
            return 0;
        return checked((short)v);
    }

    // --- Cast with guard using array length ---

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte CastArrayLengthToByte(int[] arr)
    {
        if (arr.Length > 200)
            return 0;
        return checked((byte)arr.Length);
    }

    // --- Signed cast after arithmetic narrowing ---

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte CastAfterAdd_Safe(int a, int b)
    {
        if (a < 0 || a > 100)
            return 0;
        if (b < 0 || b > 100)
            return 0;
        int sum = a + b; // [0..200]
        return checked((byte)sum);
    }

    // --- LE/GE style guards ---

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte CastIntToByte_LEGuard(int a)
    {
        if (a >= 0 && a <= 255)
            return checked((byte)a);
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static sbyte CastIntToSByte_LEGuard(int a)
    {
        if (a >= -128 && a <= 127)
            return checked((sbyte)a);
        return 0;
    }

    // --- Boundary-exact: value exactly at target type boundary ---

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte CastIntToByte_ExactBoundary(int a)
    {
        if (a < 0 || a > 255)
            return 0;
        return checked((byte)a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static short CastIntToShort_ExactBoundary(int a)
    {
        if (a < short.MinValue || a > short.MaxValue)
            return 0;
        return checked((short)a);
    }

    // =====================================================================
    //  CAST — must overflow (checked must NOT be removed)
    // =====================================================================

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte CastIntToByte_NoGuard(int a)
    {
        return checked((byte)a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static sbyte CastIntToSByte_NoGuard(int a)
    {
        return checked((sbyte)a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static short CastIntToShort_NoGuard(int a)
    {
        return checked((short)a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ushort CastIntToUShort_NoGuard(int a)
    {
        return checked((ushort)a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte CastUIntToByte_NoGuard(uint a)
    {
        return checked((byte)a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int CastUIntToInt_NoGuard(uint a)
    {
        return checked((int)a);
    }

    // --- Guard too loose — range exceeds target ---

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte CastIntToByte_GuardTooLoose(int a)
    {
        if (a < 0 || a > 300)
            return 0;
        return checked((byte)a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static sbyte CastIntToSByte_GuardTooLoose(int a)
    {
        if (a < -200 || a > 200)
            return 0;
        return checked((sbyte)a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static short CastIntToShort_GuardTooLoose(int a)
    {
        if (a < -50000 || a > 50000)
            return 0;
        return checked((short)a);
    }

    // --- Negative value cast to unsigned ---

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte CastNegativeIntToByte(int a)
    {
        if (a < -10 || a > 10)
            return 0;
        return checked((byte)a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ushort CastNegativeIntToUShort(int a)
    {
        if (a < -100 || a > 100)
            return 0;
        return checked((ushort)a);
    }

    // =====================================================================
    //  XUnit entry point
    // =====================================================================

    [Fact]
    public static int TestEntryPoint()
    {
        int[] arr10 = new int[OpaqueVal(10)];
        int[] arr5 = new int[OpaqueVal(5)];
        int[] arr200 = new int[OpaqueVal(200)];
        Span<byte> byteSpan = new byte[OpaqueVal(50)];
        Span<int> intSpan1 = new int[OpaqueVal(30)];
        Span<int> intSpan2 = new int[OpaqueVal(20)];
        byte[] byteArr = new byte[] { (byte)OpaqueVal(100), (byte)OpaqueVal(200) };

        // ----- ADD safe cases -----

        if (AddArrayLengthSmallConst(arr10) != 20)
            return 0;

        if (AddTwoPositiveRanges(OpaqueVal(500), OpaqueVal(400)) != 900)
            return 0;

        if (AddNegativeRanges(OpaqueVal(-100), OpaqueVal(-200)) != -300)
            return 0;

        if (AddMixedSignRanges(OpaqueVal(-50), OpaqueVal(80)) != 30)
            return 0;

        if (AddSpanLengthSmallConst(byteSpan) != 100)
            return 0;

        s_intField = OpaqueVal(5000);
        if (AddFieldWithGuard() != 15000)
            return 0;

        // ----- ADD overflow cases -----

        bool threw = false;
        try { AddOverflow_MaxPlusOne(OpaqueVal(int.MaxValue)); }
        catch (OverflowException) { threw = true; }
        if (!threw)
            return 0;

        threw = false;
        try { AddOverflow_TwoLargeRanges(OpaqueVal(1_500_000_000), OpaqueVal(1_500_000_000)); }
        catch (OverflowException) { threw = true; }
        if (!threw)
            return 0;

        // ----- SUB safe cases -----

        if (SubTwoSmallPositives(OpaqueVal(50), OpaqueVal(30)) != 20)
            return 0;

        if (SubArrayLengths(arr10, arr5) != 5)
            return 0;

        if (SubNarrowNegativeRange(OpaqueVal(-10), OpaqueVal(-30)) != 20)
            return 0;

        s_intField = OpaqueVal(3000);
        if (SubFieldWithGuard() != -2000)
            return 0;

        if (SubSpanLengths(intSpan1, intSpan2) != 10)
            return 0;

        // ----- SUB overflow cases -----

        threw = false;
        try { SubOverflow_MinMinusOne(OpaqueVal(int.MinValue)); }
        catch (OverflowException) { threw = true; }
        if (!threw)
            return 0;

        threw = false;
        try { SubOverflow_MaxMinusNeg(OpaqueVal(1_500_000_000), OpaqueVal(-1_500_000_000)); }
        catch (OverflowException) { threw = true; }
        if (!threw)
            return 0;

        // ----- MUL safe cases -----

        Span<int> mulSpan = new int[OpaqueVal(50)];
        if (MulSpanLengthSmall(mulSpan) != 500)
            return 0;

        if (MulTwoSmallPositives(OpaqueVal(7), OpaqueVal(8)) != 56)
            return 0;

        if (MulSmallNegatives(OpaqueVal(-3), OpaqueVal(-4)) != 12)
            return 0;

        if (MulMixedSign(OpaqueVal(-10), OpaqueVal(5)) != -50)
            return 0;

        if (MulArrayLengthBySmall(arr10) != 40)
            return 0;

        s_intField = OpaqueVal(50);
        if (MulFieldWithGuard() != 5000)
            return 0;

        // ----- MUL overflow cases -----

        threw = false;
        try { MulOverflow_LargeRanges(OpaqueVal(100_000), OpaqueVal(100_000)); }
        catch (OverflowException) { threw = true; }
        if (!threw)
            return 0;

        threw = false;
        try { MulOverflow_NegMaxRange(OpaqueVal(-100_000), OpaqueVal(100_000)); }
        catch (OverflowException) { threw = true; }
        if (!threw)
            return 0;

        // ----- Chained operations -----

        if (ChainedAddMul(OpaqueVal(10), OpaqueVal(20)) != 300)
            return 0;

        if (ChainedSubAdd(OpaqueVal(50), OpaqueVal(30), OpaqueVal(10)) != 30)
            return 0;

        // ----- Array length chained -----

        int[] arrChain = new int[OpaqueVal(50)];
        if (ArrayLengthAddAndMul(arrChain) != 110)
            return 0;

        // ----- Two fields -----

        s_intField = OpaqueVal(100);
        if (TwoFieldsAdd() != 142)
            return 0;

        if (TwoFieldsSub() != 58)
            return 0;

        // ----- Byte/short operands -----

        if (AddWithByteOperands(byteArr, OpaqueVal(1)) != 300)
            return 0;

        if (MulWithShortOperands(OpaqueVal((short)-10), OpaqueVal((short)10)) != -100)
            return 0;

        // ----- Identity / zero -----

        if (AddZero(arr10) != 10)
            return 0;

        if (SubZero(arr10) != 10)
            return 0;

        if (MulOne(arr10) != 10)
            return 0;

        if (MulZero(arr10) != 0)
            return 0;

        // ----- Boundary-tight -----

        if (AddBarelyFits(OpaqueVal(1_073_741_823), OpaqueVal(1_073_741_823)) != 2_147_483_646)
            return 0;

        if (SubBarelyFits(OpaqueVal(int.MinValue + 500), OpaqueVal(500)) != int.MinValue)
            return 0;

        // ----- Clamped values -----

        if (AddClampedValues(OpaqueVal(100), OpaqueVal(200)) != 300)
            return 0;

        // ----- Loop -----

        int[] loopArr = new int[OpaqueVal(10)];
        // sum = sum(i + 10) for i in [0..9] = (0+10)+(1+10)+...+(9+10) = 45+100 = 145
        if (LoopAddSmallIndex(loopArr) != 145)
            return 0;

        // ----- Unsigned safe -----

        if (UnsignedAddSmall(OpaqueVal((uint)500), OpaqueVal((uint)400)) != 900u)
            return 0;

        if (UnsignedMulSmall(OpaqueVal((uint)30), OpaqueVal((uint)20)) != 600u)
            return 0;

        if (UnsignedSubSafe(OpaqueVal((uint)100), OpaqueVal((uint)50)) != 50u)
            return 0;

        // ----- Unsigned overflow -----

        threw = false;
        try { UnsignedAddOverflow(OpaqueVal(uint.MaxValue), OpaqueVal((uint)1)); }
        catch (OverflowException) { threw = true; }
        if (!threw)
            return 0;

        threw = false;
        try { UnsignedMulOverflow(OpaqueVal((uint)100_000)); }
        catch (OverflowException) { threw = true; }
        if (!threw)
            return 0;

        threw = false;
        try { UnsignedSubOverflow(OpaqueVal((uint)0), OpaqueVal((uint)1)); }
        catch (OverflowException) { threw = true; }
        if (!threw)
            return 0;

        // ----- Field chained -----

        s_intField = OpaqueVal(50);
        if (FieldAddThenMul() != 1000)
            return 0;

        s_intField = OpaqueVal(100);
        if (FieldSubThenAdd() != 500)
            return 0;

        // ----- Conditional path (safe branch) -----

        if (ConditionalSafeAdd(OpaqueVal(100), OpaqueVal(200), false) != 300)
            return 0;

        // ----- Conditional path (overflow branch) -----

        threw = false;
        try { ConditionalSafeAdd(OpaqueVal(int.MaxValue), OpaqueVal(1), true); }
        catch (OverflowException) { threw = true; }
        if (!threw)
            return 0;

        // ----- Narrowed + large constant -----

        if (AddNarrowedPlusConst(OpaqueVal(50)) != 2_000_000_050)
            return 0;

        if (SubNarrowedMinusConst(OpaqueVal(50)) != -1_999_999_950)
            return 0;

        // ----- Large array to ensure no accidental overflow -----

        if (AddArrayLengthSmallConst(arr200) != 210)
            return 0;

        if (SubArrayLengths(arr200, arr10) != 190)
            return 0;

        // ----- CAST safe cases -----

        if (CastIntToByte_Guarded(OpaqueVal(200)) != 200)
            return 0;

        if (CastIntToSByte_Guarded(OpaqueVal(-50)) != -50)
            return 0;

        if (CastIntToShort_Guarded(OpaqueVal(-10000)) != -10000)
            return 0;

        if (CastIntToUShort_Guarded(OpaqueVal(50000)) != 50000)
            return 0;

        if (CastUIntToByte_Guarded(OpaqueVal((uint)100)) != 100)
            return 0;

        if (CastUIntToUShort_Guarded(OpaqueVal((uint)40000)) != 40000)
            return 0;

        if (CastUIntToShort_Guarded(OpaqueVal((uint)30000)) != 30000)
            return 0;

        if (CastUIntToSByte_Guarded(OpaqueVal((uint)100)) != 100)
            return 0;

        if (CastUIntToInt_Guarded(OpaqueVal((uint)1_000_000)) != 1_000_000)
            return 0;

        if (CastIntToByte_TightRange(OpaqueVal(50)) != 50)
            return 0;

        if (CastIntToSByte_PositiveOnly(OpaqueVal(25)) != 25)
            return 0;

        if (CastIntToSByte_NegativeRange(OpaqueVal(-50)) != -50)
            return 0;

        if (CastIntToShort_NarrowRange(OpaqueVal(-500)) != -500)
            return 0;

        s_intField = OpaqueVal(150);
        if (CastFieldToByte() != 150)
            return 0;

        s_intField = OpaqueVal(-500);
        if (CastFieldToShort() != -500)
            return 0;

        int[] arr150 = new int[OpaqueVal(150)];
        if (CastArrayLengthToByte(arr150) != 150)
            return 0;

        if (CastAfterAdd_Safe(OpaqueVal(80), OpaqueVal(80)) != 160)
            return 0;

        if (CastIntToByte_LEGuard(OpaqueVal(128)) != 128)
            return 0;

        if (CastIntToSByte_LEGuard(OpaqueVal(-100)) != -100)
            return 0;

        if (CastIntToByte_ExactBoundary(OpaqueVal(255)) != 255)
            return 0;

        if (CastIntToShort_ExactBoundary(OpaqueVal(short.MaxValue)) != short.MaxValue)
            return 0;

        // ----- CAST overflow cases -----

        threw = false;
        try { CastIntToByte_NoGuard(OpaqueVal(256)); }
        catch (OverflowException) { threw = true; }
        if (!threw)
            return 0;

        threw = false;
        try { CastIntToSByte_NoGuard(OpaqueVal(128)); }
        catch (OverflowException) { threw = true; }
        if (!threw)
            return 0;

        threw = false;
        try { CastIntToShort_NoGuard(OpaqueVal(40000)); }
        catch (OverflowException) { threw = true; }
        if (!threw)
            return 0;

        threw = false;
        try { CastIntToUShort_NoGuard(OpaqueVal(-1)); }
        catch (OverflowException) { threw = true; }
        if (!threw)
            return 0;

        threw = false;
        try { CastUIntToByte_NoGuard(OpaqueVal((uint)300)); }
        catch (OverflowException) { threw = true; }
        if (!threw)
            return 0;

        threw = false;
        try { CastUIntToInt_NoGuard(OpaqueVal(uint.MaxValue)); }
        catch (OverflowException) { threw = true; }
        if (!threw)
            return 0;

        // guard too loose — value in guard range but overflows cast
        threw = false;
        try { CastIntToByte_GuardTooLoose(OpaqueVal(260)); }
        catch (OverflowException) { threw = true; }
        if (!threw)
            return 0;

        threw = false;
        try { CastIntToSByte_GuardTooLoose(OpaqueVal(130)); }
        catch (OverflowException) { threw = true; }
        if (!threw)
            return 0;

        threw = false;
        try { CastIntToShort_GuardTooLoose(OpaqueVal(40000)); }
        catch (OverflowException) { threw = true; }
        if (!threw)
            return 0;

        // negative value to unsigned — must throw
        threw = false;
        try { CastNegativeIntToByte(OpaqueVal(-5)); }
        catch (OverflowException) { threw = true; }
        if (!threw)
            return 0;

        threw = false;
        try { CastNegativeIntToUShort(OpaqueVal(-50)); }
        catch (OverflowException) { threw = true; }
        if (!threw)
            return 0;

        return 100;
    }
}
