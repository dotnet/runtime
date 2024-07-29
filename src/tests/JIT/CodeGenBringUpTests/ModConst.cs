// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

static class ModConst
{
    // I4

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int I4_Mod_0(int i4)
    {
        return i4 % 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int I4_Mod_1(int i4)
    {
        return i4 % 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int I4_Mod_Minus1(int i4)
    {
        return i4 % -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int I4_Mod_3(int i4)
    {
        return i4 % 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int I4_ModRef_5(ref int i4)
    {
        return i4 % 5;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int I4_Mod_7(int i4)
    {
        return i4 % 7;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int I4_Mod_Minus3(int i4)
    {
        return i4 % -3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int I4_ModPow2_2(int i4)
    {
        return i4 % 2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int I4_ModPow2_Minus2(int i4)
    {
        return i4 % -2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int I4_ModPow2_8(ref int i4)
    {
        return i4 % 8;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int I4_ModPow2_Minus4(int i4)
    {
        return i4 % -4;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int I4_ModPow2_I4Min(ref int i4)
    {
        return i4 % int.MinValue;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int I4_ModPow2Embedded_4(int x, int y)
    {
        return y * 2 + (x + 2) % 4 + (x * y >> 31);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int I4_ModPow2Call_8(int i4)
    {
        return I4_ModPow2_2(i4 % 8) + I4_ModRef_5(ref i4) % 8;
    }

    // I8

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long I8_Mod_0(long i8)
    {
        return i8 % 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long I8_Mod_1(long i8)
    {
        return i8 % 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long I8_Mod_Minus1(long i8)
    {
        return i8 % -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long I8_Mod_3(long i8)
    {
        return i8 % 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long I8_Mod_5(long i8)
    {
        return i8 % 5;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long I8_Mod_7(long i8)
    {
        return i8 % 7;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long I8_Mod_Minus3(long i8)
    {
        return i8 % -3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long I8_ModPow2_4(long i8)
    {
        return i8 % 4;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long I8_ModPow2_Minus8(long i8)
    {
        return i8 % -8;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long I8_ModUncontainedPow2_1Shl32(long i8)
    {
        return i8 % (1L << 32);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long I8_ModUncontainedPow2_I8Min(long i8)
    {
        return i8 % long.MinValue;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long I8_ModPow2Embedded_4(long x, long y)
    {
        return y * 2 + (x + 2) % 4 + (x * y >> 31);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long I8_ModPow2Call_8(long i8)
    {
        return I8_ModPow2_4(i8 % 8) + I8_Mod_5(i8) % 8;
    }
}

public static class ModProgram
{
    [Fact]
    public static int TestEntryPoint()
    {
        const int Pass = 100;
        const int Fail = -1;

        // I4

        try
        {
            ModConst.I4_Mod_0(42);
            return Fail;
        }
        catch (DivideByZeroException)
        {
        }
        catch (Exception)
        {
            return Fail;
        }

        if (ModConst.I4_Mod_1(42) != 0)
        {
            return Fail;
        }

        if (ModConst.I4_Mod_1(1) != 0)
        {
            return Fail;
        }

        if (ModConst.I4_Mod_Minus1(42) != 0)
        {
            return Fail;
        }

        try
        {
            ModConst.I4_Mod_Minus1(int.MinValue);
            return Fail;
        }
        catch (OverflowException)
        {
        }
        catch (Exception)
        {
            return Fail;
        }

        if (ModConst.I4_Mod_3(41) != 2)
        {
            return Fail;
        }

        {
            int dividend = 42;

            if (ModConst.I4_ModRef_5(ref dividend) != 2)
            {
                return Fail;
            }
        }

        if (ModConst.I4_Mod_7(42) != 0)
        {
            return Fail;
        }

        if (ModConst.I4_Mod_Minus3(41) != 2)
        {
            return Fail;
        }

        if (ModConst.I4_ModPow2_2(43) != 1)
        {
            return Fail;
        }

        if (ModConst.I4_ModPow2_2(42) != 0)
        {
            return Fail;
        }

        if (ModConst.I4_ModPow2_2(-43) != -1)
        {
            return Fail;
        }

        if (ModConst.I4_ModPow2_2(-42) != 0)
        {
            return Fail;
        }

        if (ModConst.I4_ModPow2_Minus2(43) != 1)
        {
            return Fail;
        }

        {
            int dividend = 42;

            if (ModConst.I4_ModPow2_8(ref dividend) != 2)
            {
                return Fail;
            }
        }

        {
            int dividend = -42;

            if (ModConst.I4_ModPow2_8(ref dividend) != -2)
            {
                return Fail;
            }
        }

        if (ModConst.I4_ModPow2_Minus4(42) != 2)
        {
            return Fail;
        }

        if (ModConst.I4_ModPow2_Minus4(-42) != -2)
        {
            return Fail;
        }

        {
            int dividend = -42;

            if (ModConst.I4_ModPow2_I4Min(ref dividend) != -42)
            {
                return Fail;
            }
        }

        {
            int dividend = int.MinValue;

            if (ModConst.I4_ModPow2_I4Min(ref dividend) != 0)
            {
                return Fail;
            }
        }

        if (ModConst.I4_ModPow2Embedded_4(420, 938) != 1878)
        {
            return Fail;
        }

        if (ModConst.I4_ModPow2Call_8(3674) != 4)
        {
            return Fail;
        }

        // I8

        try
        {
            ModConst.I8_Mod_0(42);
            return Fail;
        }
        catch (DivideByZeroException)
        {
        }
        catch (Exception)
        {
            return Pass;
        }

        if (ModConst.I8_Mod_1(42) != 0)
        {
            return Fail;
        }

        if (ModConst.I8_Mod_Minus1(42) != 0)
        {
            return Fail;
        }

        try
        {
            ModConst.I8_Mod_Minus1(long.MinValue);
            return Fail;
        }
        catch (OverflowException)
        {
        }
        catch (Exception)
        {
            return Fail;
        }

        if (ModConst.I8_Mod_3(43) != 1)
        {
            return Fail;
        }

        if (ModConst.I8_Mod_5(42) != 2)
        {
            return Fail;
        }

        if (ModConst.I8_Mod_7(45) != 3)
        {
            return Fail;
        }

        if (ModConst.I8_Mod_Minus3(-43) != -1)
        {
            return Fail;
        }

        if (ModConst.I8_ModPow2_4(42) != 2)
        {
            return Fail;
        }

        if (ModConst.I8_ModPow2_Minus8(42) != 2)
        {
            return Fail;
        }

        if (ModConst.I8_ModPow2_Minus8(-42) != -2)
        {
            return Fail;
        }

        if (ModConst.I8_ModUncontainedPow2_1Shl32((1L << 33) + 42L) != 42)
        {
            return Fail;
        }

        if (ModConst.I8_ModUncontainedPow2_I8Min(42) != 42)
        {
            return Fail;
        }

        if (ModConst.I8_ModUncontainedPow2_I8Min(long.MinValue) != 0)
        {
            return Fail;
        }

        if (ModConst.I8_ModPow2Embedded_4(420, 938) != 1878)
        {
            return Fail;
        }

        if (ModConst.I8_ModPow2Call_8(3674) != 6)
        {
            return Fail;
        }

        return Pass;
    }
}
