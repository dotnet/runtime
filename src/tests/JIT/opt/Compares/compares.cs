// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// unit test for the full range comparison optimization

using System;
using System.Runtime.CompilerServices;
using Xunit;

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
    internal static void consume<T>(T a1, T a2) {}

    /* If conditions that are consumed. */

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(10, 11)]
    public static void Eq_byte_consume(byte a1, byte a2) {
        // ARM64-FULL-LINE:      cmp {{w[0-9]+}}, {{w[0-9]+}}
        // ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{eq|ne}}
        //
        // X64-FULL-LINE:        cmov{{ne|e}} {{[a-z0-9]+}}, {{.*}}

        if (a1 == a2) { a1 = 10; }
        consume<byte>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(10, 11)]
    public static void Ne_short_consume(short a1, short a2)
    {
        // ARM64-FULL-LINE:      cmp {{w[0-9]+}}, {{w[0-9]+}}
        // ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{ne|eq}}
        //
        // X64-FULL-LINE:        cmov{{ne|e}} {{[a-z0-9]+}}, {{.*}}

        if (a1 != a2) { a1 = 11; }
        consume<short>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(10, 11)]
    public static void Lt_int_consume(int a1, int a2)
    {
        // ARM64-FULL-LINE:      cmp {{w[0-9]+}}, {{w[0-9]+}}
        // ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{lt|ge}}
        //
        // X64-FULL-LINE:        cmov{{l|ge}} {{[a-z0-9]+}}, {{.*}}

        if (a1 < a2) { a1 = 12; }
        consume<int>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(10, 11)]
    public static void Le_long_consume(long a1, long a2)
    {
        // ARM64-FULL-LINE:      cmp {{x[0-9]+}}, {{x[0-9]+}}
        // ARM64-FULL-LINE-NEXT: csel {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, {{le|gt}}
        //
        // X64-FULL-LINE:        cmov{{le|g}} {{[a-z0-9]+}}, {{.*}}

        if (a1 <= a2) { a1 = 13; }
        consume<long>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(10, 11)]
    public static void Gt_ushort_consume(ushort a1, ushort a2)
    {
        // ARM64-FULL-LINE:      cmp {{w[0-9]+}}, {{w[0-9]+}}
        // ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        //
        // X64-FULL-LINE:        cmov{{g|le}} {{[a-z0-9]+}}, {{.*}}

        if (a1 > a2) { a1 = 14; }
        consume<ushort>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(10, 11)]
    public static void Ge_uint_consume(uint a1, uint a2)
    {
        // ARM64-FULL-LINE:      cmp {{w[0-9]+}}, {{w[0-9]+}}
        // ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{hs|lo}}
        //
        // X64-FULL-LINE:        cmov{{ae|b}} {{[a-z0-9]+}}, {{.*}}

        if (a1 >= a2) { a1 = 15; }
        consume<uint>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(10, 11)]
    public static void Eq_ulong_consume(ulong a1, ulong a2)
    {
        // ARM64-FULL-LINE:      cmp {{x[0-9]+}}, {{x[0-9]+}}
        // ARM64-FULL-LINE-NEXT: csel {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, {{eq|ne}}
        //
        // X64-FULL-LINE:        cmov{{e|ne}} {{[a-z0-9]+}}, {{.*}}

        if (a1 == a2) { a1 = 16; }
        consume<ulong>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(10.1F, 11.1F, 12, 13)]
    public static void Ne_float_int_consume(float f1, float f2, int a1, int a2)
    {
        // ARM64-FULL-LINE:      fcmp {{s[0-9]+}}, {{s[0-9]+}}
        // ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{ne|eq}}
        //
        // X64-FULL-LINE:        cmov{{p|np|ne|e}} {{[a-z0-9]+}}, {{.*}}
        // X64-FULL-LINE-NEXT:   cmov{{p|np|ne|e}} {{[a-z0-9]+}}, {{.*}}

        if (f1 != f2) { a1 = 17; }
        consume<float>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(10.1, 11.1, 12, 13)]
    public static void Lt_double_long_consume(double f1, double f2, long a1, long a2)
    {
        // ARM64-FULL-LINE:      fcmp {{d[0-9]+}}, {{d[0-9]+}}
        // ARM64-FULL-LINE-NEXT: csel {{x[0-31]}}, {{x[0-31]}}, {{x[0-31]}}, {{hs|lo}}
        //
        // X64-FULL-LINE:        cmov{{be|a}} {{[a-z0-9]+}}, {{.*}}

        if (f1 < f2) { a1 = 18; }
        consume<double>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(10.1, 11.1, 12, 13)]
    public static void Eq_double_long_consume(double f1, double f2, long a1, long a2)
    {
        // ARM64-FULL-LINE:      fcmp {{d[0-9]+}}, {{d[0-9]+}}
        // ARM64-FULL-LINE-NEXT: csel {{x[0-31]}}, {{x[0-31]}}, {{x[0-31]}}, {{eq|ne}}
        //
        // X64-FULL-LINE:        cmov{{p|np|ne|e}} {{[a-z0-9]+}}, {{.*}}
        // X64-FULL-LINE-NEXT:   cmov{{p|np|ne|e}} {{[a-z0-9]+}}, {{.*}}

        if (f1 == f2) { a1 = 18; }
        consume<double>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(10.1, 11.1, 12, 13)]
    public static void Ne_double_int_consume(double f1, double f2, int a1, int a2)
    {
        // ARM64-FULL-LINE:      fcmp {{d[0-9]+}}, {{d[0-9]+}}
        // ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{ne|eq}}
        //
        // X64-FULL-LINE:        cmov{{p|np|ne|e}} {{[a-z0-9]+}}, {{.*}}
        // X64-FULL-LINE-NEXT:   cmov{{p|np|ne|e}} {{[a-z0-9]+}}, {{.*}}

        if (f1 != f2) { a1 = 18; }
        consume<double>(a1, a2);
    }

    /* If/Else conditions that consume. */

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(20, 21)]
    public static void Ne_else_byte_consume(byte a1, byte a2)
    {
        // ARM64-FULL-LINE:      cmp {{w[0-9]+}}, {{w[0-9]+}}
        // ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{ne|eq}}
        //
        // X64-FULL-LINE:        cmov{{ne|e}} {{[a-z0-9]+}}, {{.*}}

        if (a1 != a2) { a1 = 10; } else { a1 = 100; }
        consume<byte>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(10, 11)]
    public static void Lt_else_short_consume(short a1, short a2)
    {
        // ARM64-FULL-LINE:      cmp {{w[0-9]+}}, {{w[0-9]+}}
        // ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{lt|ge}}
        //
        // X64-FULL-LINE:        cmov{{l|ge}} {{[a-z0-9]+}}, {{.*}}

        if (a1 < a2) { a1 = 11; } else { a1 = 101; }
        consume<short>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(10, 11)]
    public static void Le_else_int_consume(int a1, int a2)
    {
        // ARM64-FULL-LINE:      cmp {{w[0-9]+}}, {{w[0-9]+}}
        // ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{le|gt}}
        //
        // X64-FULL-LINE:        cmov{{le|g}} {{[a-z0-9]+}}, {{.*}}

        if (a1 <= a2) { a1 = 12; } else { a1 = 102; }
        consume<int>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(10, 11)]
    public static void Gt_else_long_consume(long a1, long a2)
    {
        // ARM64-FULL-LINE:      cmp {{x[0-9]+}}, {{x[0-9]+}}
        // ARM64-FULL-LINE-NEXT: csel {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, {{gt|le}}
        //
        // X64-FULL-LINE:        cmov{{g|le}} {{[a-z0-9]+}}, {{.*}}

        if (a1 > a2) { a1 = 13; } else { a1 = 103; }
        consume<long>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(10, 11)]
    public static void Ge_else_ushort_consume(ushort a1, ushort a2)
    {
        // ARM64-FULL-LINE:      cmp {{w[0-9]+}}, {{w[0-9]+}}
        // ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{ge|lt}}
        //
        // X64-FULL-LINE:        cmov{{ge|l}} {{[a-z0-9]+}}, {{.*}}

        if (a1 >= a2) { a1 = 14; } else { a1 = 104; }
        consume<ushort>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(10, 11)]
    public static void Eq_else_uint_consume(uint a1, uint a2)
    {
        // ARM64-FULL-LINE:      cmp {{w[0-9]+}}, {{w[0-9]+}}
        // ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{eq|ne}}
        //
        // X64-FULL-LINE:        cmov{{e|ne}} {{[a-z0-9]+}}, {{.*}}

        if (a1 == a2) { a1 = 15; } else { a1 = 105; }
        consume<uint>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(10, 11)]
    public static void Ne_else_ulong_consume(ulong a1, ulong a2)
    {
        // ARM64-FULL-LINE:      cmp {{x[0-9]+}}, {{x[0-9]+}}
        // ARM64-FULL-LINE-NEXT: csel {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, {{ne|eq}}
        //
        // X64-FULL-LINE:        cmov{{ne|e}} {{[a-z0-9]+}}, {{.*}}

        if (a1 != a2) { a1 = 16; } else { a1 = 106; }
        consume<ulong>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(10.1F, 11.1F, 12, 13)]
    public static void Lt_else_float_int_consume(float f1, float f2, int a1, int a2)
    {
        // ARM64-FULL-LINE:      fcmp {{s[0-9]+}}, {{s[0-9]+}}
        // ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{hs|lo}}
        //
        // X64-FULL-LINE:        cmov{{be|a}} {{[a-z0-9]+}}, {{.*}}

        if (f1 < f2) { a1 = 17; } else { a1 = 107; }
        consume<float>(a1, a2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Theory]
    [InlineData(10.1, 11.1, 12, 13)]
    public static void Le_else_double_int_consume(double f1, double f2, int a1, int a2)
    {
        // ARM64-FULL-LINE:      fcmp {{d[0-9]+}}, {{d[0-9]+}}
        // ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, hi
        //
        // X64-FULL-LINE:        cmov{{b|ae}} {{[a-z0-9]+}}, {{.*}}

        if (f1 <= f2) { a1 = 18; } else { a1 = 108; }
        consume<double>(a1, a2);
    }

    /* If/Else conditions that return. */

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static byte Lt_else_byte_return(byte a1, byte a2)
    {
        // ARM64-FULL-LINE:      cmp {{w[0-9]+}}, {{w[0-9]+}}
        // ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{lt|ge}}
        //
        // X64-FULL-LINE:        cmov{{l|ge}} {{[a-z0-9]+}}, {{.*}}

        return (a1 < a2) ? (byte)10 : (byte)100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static short Le_else_short_return(short a1, short a2)
    {
        // ARM64-FULL-LINE:      cmp {{w[0-9]+}}, {{w[0-9]+}}
        // ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{le|gt}}
        //
        // X64-FULL-LINE:        cmov{{le|g}} {{[a-z0-9]+}}, {{.*}}

        return (a1 <= a2) ? (short)11 : (short)101;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Gt_else_int_return(int a1, int a2)
    {
        // ARM64-FULL-LINE:      cmp {{w[0-9]+}}, {{w[0-9]+}}
        // ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        //
        // X64-FULL-LINE:        cmov{{g|le}} {{[a-z0-9]+}}, {{.*}}

        return (a1 > a2) ? (int)12 : (int)102;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Ge_else_long_return(long a1, long a2)
    {
        // ARM64-FULL-LINE:      cmp {{x[0-9]+}}, {{x[0-9]+}}
        // ARM64-FULL-LINE-NEXT: csel {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, {{ge|lt}}
        //
        // X64-FULL-LINE:        cmov{{ge|l}} {{[a-z0-9]+}}, {{.*}}

        return (a1 >= a2) ? (long)13 : (long)103;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ushort Eq_else_ushort_return(ushort a1, ushort a2)
    {
        // ARM64-FULL-LINE:      cmp {{w[0-9]+}}, {{w[0-9]+}}
        // ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{eq|ne}}
        //
        // X64-FULL-LINE:        cmov{{e|ne}} {{[a-z0-9]+}}, {{.*}}

        return (a1 == a2) ? (ushort)14 : (ushort)104;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint Ne_else_uint_return(uint a1, uint a2)
    {
        // ARM64-FULL-LINE:      cmp {{w[0-9]+}}, {{w[0-9]+}}
        // ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{ne|eq}}
        //
        // X64-FULL-LINE:        cmov{{e|ne}} {{[a-z0-9]+}}, {{.*}}

        return (a1 != a2) ? (uint)15 : (uint)105;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ulong Lt_else_ulong_return(ulong a1, ulong a2)
    {
        // ARM64-FULL-LINE:      cmp {{x[0-9]+}}, {{x[0-9]+}}
        // ARM64-FULL-LINE-NEXT: csel {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, {{hs|lo}}
        //
        // X64-FULL-LINE:        cmov{{b|ae}} {{[a-z0-9]+}}, {{.*}}

        return (a1 < a2) ? (ulong)16 : (ulong)106;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Le_else_float_int_return(float a1, float a2)
    {
        // ARM64-FULL-LINE:      fcmp {{s[0-9]+}}, {{s[0-9]+}}
        // ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, ls
        //
        // X64-FULL-LINE:        cmov{{b|ae}} {{[a-z0-9]+}}, {{.*}}

        return (a1 <= a2) ? 17 : 107;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Gt_else_double_int_return(double a1, double a2)
    {
        // ARM64-FULL-LINE:      fcmp {{d[0-9]+}}, {{d[0-9]+}}
        // ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        //
        // X64-FULL-LINE:        cmov{{be|a}} {{[a-z0-9]+}}, {{.*}}

        return (a1 > a2) ? 18 : 108;
    }


    [Fact]
    public static int TestEntryPoint()
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

        if (Lt_else_byte_return(10,11) != 10)
        {
            Console.WriteLine("FullRangeComparisonTest:Lt_else_byte_return() failed");
            return 101;
        }
        if (Le_else_short_return(10, 11) != 11)
        {
            Console.WriteLine("FullRangeComparisonTest:Le_else_short_return() failed");
            return 101;
        }
        if (Gt_else_int_return(10, 11) != 102)
        {
            Console.WriteLine("FullRangeComparisonTest:Gt_else_int_return() failed");
            return 101;
        }
        if (Ge_else_long_return(10, 11) != 103)
        {
            Console.WriteLine("FullRangeComparisonTest:Ge_else_long_return() failed");
            return 101;
        }
        if (Eq_else_ushort_return(10, 11) != 104)
        {
            Console.WriteLine("FullRangeComparisonTest:Eq_else_ushort_return() failed");
            return 101;
        }
        if (Ne_else_uint_return(10, 11) != 15)
        {
            Console.WriteLine("FullRangeComparisonTest:Ne_else_uint_return() failed");
            return 101;
        }
        if (Lt_else_ulong_return(10, 11) != 16)
        {
            Console.WriteLine("FullRangeComparisonTest:Lt_else_ulong_return() failed");
            return 101;
        }
        if (Le_else_float_int_return(10.1F, 11.1F) != 17)
        {
            Console.WriteLine("FullRangeComparisonTest:Le_else_float_int_return() failed");
            return 101;
        }
        if (Gt_else_double_int_return(10.1, 11.1) != 108)
        {
            Console.WriteLine("FullRangeComparisonTest:Gt_else_double_int_return() failed");
            return 101;
        }

        Console.WriteLine("PASSED");
        return 100;
    }
}
