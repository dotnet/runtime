// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// unit test for the full range comparison optimization

using System;
using System.Runtime.CompilerServices;

public class FullRangeComparisonTest
{
    // Class for testing side effects promotion
    public class SideEffects
    { 
        public byte B;
    } 

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool EqualsOrGreaterThan_MinValue_RHSConst_Byte(byte b) => b >= 0;
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool EqualsOrGreaterThan_MinValue_RHSConst_Short(short s) => s >= short.MinValue;
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool EqualsOrGreaterThan_MinValue_RHSConst_Int(int i) => i >= int.MinValue;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool EqualsOrGreaterThan_MinValue_RHSConst_Long(long l) => l >= long.MinValue;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool EqualsOrLessThan_MaxValue_RHSConst_Byte(byte b) => b <= byte.MaxValue;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool EqualsOrLessThan_MaxValue_RHSConst_Short(short s) => s <= short.MaxValue;
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool EqualsOrLessThan_MaxValue_RHSConst_Int(int i) => i <= int.MaxValue;
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool EqualsOrLessThan_MaxValue_RHSConst_Long(long l) => l <= long.MaxValue;
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool EqualsOrLessThan_MaxValue_RHSConst_UShort(ushort us) => us <= ushort.MaxValue;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool EqualsOrLessThan_MaxValue_RHSConst_UInt(uint ui) => ui <= uint.MaxValue;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool EqualsOrLessThan_MaxValue_RHSConst_ULong(ulong ul) => ul <= uint.MaxValue;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool EqualsOrLessThan_MinValue_LHSConst_Byte(byte b) => 0 <= b;
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool EqualsOrGreaterThan_MinValue_LHSConst_Short(short i) => short.MinValue <= i;
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool EqualsOrLessThan_MinValue_LHSConst_Int(int i) => int.MinValue <= i;
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool EqualsOrLessThan_MinValue_LHSConst_Long(long l) => long.MinValue <= l;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool EqualsOrGreaterThan_MaxValue_LHSConst_Byte(byte b) => byte.MaxValue >= b;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool EqualsOrGreaterThan_MaxValue_LHSConst_Short(short s) => short.MaxValue >= s;
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool EqualsOrGreaterThan_MaxValue_LHSConst_Int(int i) => int.MaxValue >= i;
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool EqualsOrGreaterThan_MaxValue_LHSConst_Long(long l) => long.MaxValue >= l;
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool EqualsOrGreaterThan_MaxValue_LHSConst_SideEffects(SideEffects c) => c.B <= 255;


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void consume<T>(T a1, T a2) {}

    /* If conditions that are consumed. */

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Eq_byte_consume(byte a1, byte a2) {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}
        //ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, eq
        if (a1 == a2) { a1 = 10; }
        consume<byte>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Ne_short_consume(short a1, short a2)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}
        //ARM64-NEXT-FULL-LINE: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, ne
        if (a1 != a2) { a1 = 11; }
        consume<short>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Lt_int_consume(int a1, int a2)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}
        //ARM64-NEXT-FULL-LINE: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, lt
        if (a1 < a2) { a1 = 12; }
        consume<int>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Le_long_consume(long a1, long a2)
    {
        //ARM64-FULL-LINE: cmp {{x[0-9]+}}, {{x[0-9]+}}
        //ARM64-NEXT-FULL-LINE: csel {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, le
        if (a1 <= a2) { a1 = 13; }
        consume<long>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Gt_ushort_consume(ushort a1, ushort a2)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}
        //ARM64-NEXT-FULL-LINE: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, gt
        if (a1 > a2) { a1 = 14; }
        consume<ushort>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Ge_uint_consume(uint a1, uint a2)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}
        //ARM64-NEXT-FULL-LINE: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, ge
        if (a1 >= a2) { a1 = 15; }
        consume<uint>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Eq_ulong_consume(ulong a1, ulong a2)
    {
        //ARM64-FULL-LINE: cmp {{x[0-9]+}}, {{x[0-9]+}}
        //ARM64-NEXT-FULL-LINE: csel {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, eq
        if (a1 == a2) { a1 = 16; }
        consume<ulong>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Ne_float_int_consume(float f1, float f2, int a1, int a2)
    {
        //ARM64-FULL-LINE: fcmp {{s[0-9]+}}, {{s[0-9]+}}
        //ARM64-NEXT-FULL-LINE: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, ne
        if (f1 != f2) { a1 = 17; }
        consume<float>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Lt_double_long_consume(double f1, double f2, long a1, long a2)
    {
        //ARM64-FULL-LINE: fcmp {{d[0-9]+}}, {{d[0-9]+}}
        //ARM64-NEXT-FULL-LINE: csel {{x[0-31]}}, {{x[0-31]}}, {{x[0-31]}}, lt
        if (f1 < f2) { a1 = 18; }
        consume<double>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Eq_double_long_consume(double f1, double f2, long a1, long a2)
    {
        //ARM64-FULL-LINE: fcmp {{d[0-9]+}}, {{d[0-9]+}}
        //ARM64-NEXT-FULL-LINE: csel {{x[0-31]}}, {{x[0-31]}}, {{x[0-31]}}, eq
        if (f1 == f2) { a1 = 18; }
        consume<double>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Ne_double_int_consume(double f1, double f2, int a1, int a2)
    {
        //ARM64-FULL-LINE: fcmp {{d[0-9]+}}, {{d[0-9]+}}
        //ARM64-NEXT-FULL-LINE: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, ne
        if (f1 != f2) { a1 = 18; }
        consume<double>(a1, a2);
    }

    public static int Main()
    {
        // Optimize comparison with full range values
        // RHS Const Optimization
        if (!EqualsOrGreaterThan_MinValue_RHSConst_Byte(10))
        {
            Console.WriteLine("FullRangeComparisonTest:EqualsOrGreaterThan_MinValue_RHSConst_Byte(10) failed");
            return 101;
        }

        if (!EqualsOrGreaterThan_MinValue_RHSConst_Short(-10))
        {
            Console.WriteLine("FullRangeComparisonTest:EqualsOrGreaterThan_MinValue_RHSConst_Short(10) failed");
            return 101;
        }

        if (!EqualsOrGreaterThan_MinValue_RHSConst_Int(-10))
        {
            Console.WriteLine("FullRangeComparisonTest:EqualsOrGreaterThan_MinValue_RHSConst_Int(10) failed");
            return 101;
        }

        if (!EqualsOrGreaterThan_MinValue_RHSConst_Long(-10))
        {
            Console.WriteLine("FullRangeComparisonTest:EqualsOrGreaterThan_MinValue_RHSConst_Long(10) failed");
            return 101;
        }

        if (!EqualsOrLessThan_MaxValue_RHSConst_Byte(10))
        {
            Console.WriteLine("FullRangeComparisonTest:EqualsOrLessThan_MaxValue_RHSConst_Byte(10) failed");
            return 101;
        }

        if (!EqualsOrLessThan_MaxValue_RHSConst_Short(10))
        {
            Console.WriteLine("FullRangeComparisonTest:EqualsOrLessThan_MaxValue_RHSConst_Short(10) failed");
            return 101;
        }

        if (!EqualsOrLessThan_MaxValue_RHSConst_Int(10))
        {
            Console.WriteLine("FullRangeComparisonTest:EqualsOrLessThan_MaxValue_RHSConst_Int(10) failed");
            return 101;
        }

        if (!EqualsOrLessThan_MaxValue_RHSConst_Long(10))
        {
            Console.WriteLine("FullRangeComparisonTest:EqualsOrLessThan_MaxValue_RHSConst_Long(10) failed");
            return 101;
        }

        // LHS Const Optimization
        if (!EqualsOrLessThan_MinValue_LHSConst_Byte(10))
        {
            Console.WriteLine("FullRangeComparisonTest:EqualsOrLessThan_MinValue_LHSConst_Byte(10) failed");
            return 101;
        }

        if (!EqualsOrLessThan_MinValue_LHSConst_Int(-10))
        {
            Console.WriteLine("FullRangeComparisonTest:EqualsOrLessThan_MinValue_LHSConst_Int(10) failed");
            return 101;
        }

        if (!EqualsOrLessThan_MinValue_LHSConst_Long(-10))
        {
            Console.WriteLine("FullRangeComparisonTest:EqualsOrLessThan_MinValue_LHSConst_Long(10) failed");
            return 101;
        }

        if (!EqualsOrGreaterThan_MaxValue_LHSConst_Byte(10))
        {
            Console.WriteLine("FullRangeComparisonTest:EqualsOrGreaterThan_MaxValue_LHSConst_Byte(10) failed");
            return 101;
        }

        if (!EqualsOrGreaterThan_MaxValue_LHSConst_Int(10))
        {
            Console.WriteLine("FullRangeComparisonTest:EqualsOrGreaterThan_MaxValue_LHSConst_Int(10) failed");
            return 101;
        }

        if (!EqualsOrGreaterThan_MaxValue_LHSConst_Long(10))
        {
            Console.WriteLine("FullRangeComparisonTest:EqualsOrGreaterThan_MaxValue_LHSConst_Long(10) failed");
            return 101;
        }


        // Unsigned values
        if (!EqualsOrLessThan_MaxValue_RHSConst_UShort(10))
        {
            Console.WriteLine("FullRangeComparisonTest:EqualsOrLessThan_MaxValue_RHSConst_UShort(10) failed");
            return 101;
        }

        if (!EqualsOrLessThan_MaxValue_RHSConst_UInt(10))
        {
            Console.WriteLine("FullRangeComparisonTest:EqualsOrLessThan_MaxValue_RHSConst_UInt(10) failed");
            return 101;
        }

        if (!EqualsOrLessThan_MaxValue_RHSConst_ULong(10))
        {
            Console.WriteLine("FullRangeComparisonTest:EqualsOrLessThan_MaxValue_RHSConst_ULong(10) failed");
            return 101;
        }

        // Side effects persist
        try
        {
            EqualsOrGreaterThan_MaxValue_LHSConst_SideEffects(null);
            Console.WriteLine("FullRangeComparisonTest:EqualsOrGreaterThan_MaxValue_LHSConst_SideEffects(null) failed");
            return 101;
        }
        catch (NullReferenceException ex)
        {

        }
        catch (Exception ex)
        {
            Console.WriteLine("FullRangeComparisonTest:EqualsOrGreaterThan_MaxValue_LHSConst_SideEffects(null) failed");
            return 101;
        }

        Eq_byte_consume(10, 11);
        Ne_short_consume(10, 11);
        Lt_int_consume(10, 11);
        Le_long_consume(10, 11);
        Gt_ushort_consume(10, 11);
        Ge_uint_consume(10, 11);
        Eq_ulong_consume(10, 11);
        Ne_float_int_consume(10.1F, 11.1F, 12, 13);
        Lt_double_long_consume(10.1, 11.1, 12, 13);
        Eq_double_long_consume(10.1, 11.1, 12, 13);
        Ne_double_int_consume(10.1, 11.1, 12, 13);

        Console.WriteLine("PASSED");
        return 100;
    }
}
