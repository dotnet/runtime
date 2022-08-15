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
      
        Console.WriteLine("PASSED");
        return 100;
    }
}
