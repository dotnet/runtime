// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

static class DivConst
{
    // I4

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int I4_Div_0(int i4)
    {
        return i4 / 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int I4_Div_1(int i4)
    {
        return i4 / 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int I4_Div_Minus1(int i4)
    {
        return i4 / -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int I4_Div_3(int i4)
    {
        return i4 / 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int I4_DivRef_5(ref int i4)
    {
        return i4 / 5;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int I4_Div_7(int i4)
    {
        return i4 / 7;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int I4_Div_Minus3(int i4)
    {
        return i4 / -3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int I4_DivPow2_2(int i4)
    {
        return i4 / 2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int I4_DivPow2_Minus2(int i4)
    {
        return i4 / -2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int I4_DivPow2_8(ref int i4)
    {
        return i4 / 8;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int I4_DivPow2_Minus4(int i4)
    {
        return i4 / -4;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int I4_DivPow2_I4Min(int i4)
    {
        return i4 / int.MinValue;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int I4_DivPow2Embedded_4(int x, int y)
    {
        return y * 2 + (x + 2) / 4 + (x * y >> 31);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int I4_DivPow2Embdedded_Point(Point p)
    {
        int a = p.X + 4;
        int b = (a - p.Y) / 2;
        return p.Y > p.X ? a : b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int I4_DivPow2Call_8(int i4)
    {
        return I4_DivPow2_2(i4 / 8) + I4_DivRef_5(ref i4) / 8;
    }

    // I8

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long I8_Div_0(long i8)
    {
        return i8 / 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long I8_Div_1(long i8)
    {
        return i8 / 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long I8_Div_Minus1(long i8)
    {
        return i8 / -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long I8_Div_3(long i8)
    {
        return i8 / 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long I8_Div_5(long i8)
    {
        return i8 / 5;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long I8_Div_7(ref long i8)
    {
        return i8 / 7;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long I8_Div_Minus3(long i8)
    {
        return i8 / -3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long I8_DivPow2_4(long i8)
    {
        return i8 / 4;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long I8_DivPow2_Minus8(long i8)
    {
        return i8 / -8;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long I8_DivUncontainedPow2_1Shl32(long i8)
    {
        return i8 / (1L << 32);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long I8_DivUncontainedPow2_I8Min(long i8)
    {
        return i8 / long.MinValue;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long I8_DivPow2Embedded_4(long x, long y)
    {
        return y * 2 + (x + 2) / 4 + (x * y >> 31);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long I8_DivPow2Call_8(long i8)
    {
        return I8_DivPow2_4(i8 / 8) + I8_Div_5(i8) / 8;
    }
}

class Point
{
    public int X;
    public int Y;
}

public static class DivProgram
{
    [Fact]
    public static int TestEntryPoint()
    {
        const int Pass = 100;
        const int Fail = -1;

        // I4

        try
        {
            DivConst.I4_Div_0(42);
            return Fail;
        }
        catch (DivideByZeroException)
        {
        }
        catch (Exception)
        {
            return Fail;
        }

        if (DivConst.I4_Div_1(42) != 42)
        {
            return Fail;
        }

        if (DivConst.I4_Div_Minus1(42) != -42)
        {
            return Fail;
        }

        try
        {
            DivConst.I4_Div_Minus1(int.MinValue);
            return Fail;
        }
        catch (OverflowException)
        {
        }
        catch (Exception)
        {
            return Fail;
        }

        if (DivConst.I4_Div_3(42) != 14)
        {
            return Fail;
        }

        {
            int dividend = 42;

            if (DivConst.I4_DivRef_5(ref dividend) != 8)
            {
                return Fail;
            }
        }

        if (DivConst.I4_Div_7(42) != 6)
        {
            return Fail;
        }

        if (DivConst.I4_Div_Minus3(42) != -14)
        {
            return Fail;
        }

        if (DivConst.I4_DivPow2_2(42) != 21)
        {
            return Fail;
        }

        if (DivConst.I4_DivPow2_2(43) != 21)
        {
            return Fail;
        }

        if (DivConst.I4_DivPow2_2(-42) != -21)
        {
            return Fail;
        }

        if (DivConst.I4_DivPow2_2(-43) != -21)
        {
            return Fail;
        }

        if (DivConst.I4_DivPow2_Minus2(43) != -21)
        {
            return Fail;
        }

        {
            int dividend = 42;

            if (DivConst.I4_DivPow2_8(ref dividend) != 5)
            {
                return Fail;
            }
        }

        {
            int dividend = -42;

            if (DivConst.I4_DivPow2_8(ref dividend) != -5)
            {
                return Fail;
            }
        }

        if (DivConst.I4_DivPow2_Minus4(42) != -10)
        {
            return Fail;
        }

        if (DivConst.I4_DivPow2_Minus4(-42) != 10)
        {
            return Fail;
        }

        if (DivConst.I4_DivPow2_I4Min(-42) != 0)
        {
            return Fail;
        }

        if (DivConst.I4_DivPow2_I4Min(int.MinValue) != 1)
        {
            return Fail;
        }

        if (DivConst.I4_DivPow2Embedded_4(420, 938) != 1981)
        {
            return Fail;
        }

        if (DivConst.I4_DivPow2Embdedded_Point(new Point { X = 513, Y = 412 }) != 52)
        {
            return Fail;
        }

        if (DivConst.I4_DivPow2Call_8(420) != 36)
        {
            return Fail;
        }

        // I8

        try
        {
            DivConst.I8_Div_0(42);
            return Fail;
        }
        catch (DivideByZeroException)
        {
        }
        catch (Exception)
        {
            return Fail;
        }

        if (DivConst.I8_Div_1(42) != 42)
        {
            return Fail;
        }

        if (DivConst.I8_Div_Minus1(42) != -42)
        {
            return Fail;
        }

        try
        {
            DivConst.I8_Div_Minus1(long.MinValue);
            return Fail;
        }
        catch (OverflowException)
        {
        }
        catch (Exception)
        {
            return Fail;
        }

        if (DivConst.I8_Div_3(42) != 14)
        {
            return Fail;
        }

        if (DivConst.I8_Div_5(42) != 8)
        {
            return Fail;
        }

        {
            long dividend = 45;

            if (DivConst.I8_Div_7(ref dividend) != 6)
            {
                return Fail;
            }
        }

        if (DivConst.I8_Div_Minus3(42) != -14)
        {
            return Fail;
        }

        if (DivConst.I8_DivPow2_4(42) != 10)
        {
            return Fail;
        }

        if (DivConst.I8_DivPow2_Minus8(42) != -5)
        {
            return Fail;
        }

        if (DivConst.I8_DivPow2_Minus8(-42) != 5)
        {
            return Fail;
        }

        if (DivConst.I8_DivUncontainedPow2_1Shl32(1L << 33) != 2)
        {
            return Fail;
        }

        if (DivConst.I8_DivUncontainedPow2_I8Min(42) != 0)
        {
            return Fail;
        }

        if (DivConst.I8_DivUncontainedPow2_I8Min(long.MinValue) != 1)
        {
            return Fail;
        }

        if (DivConst.I8_DivPow2Embedded_4(420, 938) != 1981)
        {
            return Fail;
        }

        if (DivConst.I8_DivPow2Call_8(420) != 23)
        {
            return Fail;
        }

        return Pass;
    }
}
