// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

static class UDivConst
{
    // U4

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint U4_Div_0(uint u4)
    {
        return u4 / 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint U4_Div_1(uint u4)
    {
        return u4 / 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint U4_Div_3(uint u4)
    {
        return u4 / 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint U4_Div_5(uint u4)
    {
        return u4 / 5;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint U4_Div_7(uint u4)
    {
        return u4 / 7;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint U4_DivPow2_16(uint u4)
    {
        return u4 / 16;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint U4_DivPow2_I4Min(uint u4)
    {
        return u4 / 0x80000000u;
    }

    // U8

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ulong U8_Div_0(ulong u8)
    {
        return u8 / 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ulong U8_Div_1(ulong u8)
    {
        return u8 / 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ulong U8_Div_3(ulong u8)
    {
        return u8 / 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ulong U8_Div_5(ulong u8)
    {
        return u8 / 5;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ulong U8_Div_7(ulong u8)
    {
        return u8 / 7;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ulong U8_DivUncontained_I8Max(ulong u8)
    {
        return u8 / ulong.MaxValue;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ulong U8_DivPow2_2(ulong u8)
    {
        return u8 / 2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ulong U8_DivUncontainedPow2_1Shl32(ulong u8)
    {
        return u8 / (1UL << 32);
    }
}

public static class UDivProgram
{
    [Fact]
    public static int TestEntryPoint()
    {
        const int Pass = 100;
        const int Fail = -1;

        // U4

        try
        {
            UDivConst.U4_Div_0(42);
            return Fail;
        }
        catch (DivideByZeroException)
        {
        }
        catch (Exception)
        {
            return Fail;
        }

        if (UDivConst.U4_Div_1(42) != 42)
        {
            return Fail;
        }

        if (UDivConst.U4_Div_3(42) != 14)
        {
            return Fail;
        }

        if (UDivConst.U4_Div_5(42) != 8)
        {
            return Fail;
        }

        if (UDivConst.U4_Div_7(43) != 6)
        {
            return Fail;
        }

        if (UDivConst.U4_DivPow2_16(42) != 2)
        {
            return Fail;
        }

        if (UDivConst.U4_DivPow2_I4Min(3) != 0)
        {
            return Fail;
        }

        if (UDivConst.U4_DivPow2_I4Min(0x80000001u) != 1)
        {
            return Fail;
        }

        // U8

        try
        {
            UDivConst.U8_Div_0(42);
            return Fail;
        }
        catch (DivideByZeroException)
        {
        }
        catch (Exception)
        {
            return Fail;
        }

        if (UDivConst.U8_Div_1(42) != 42)
        {
            return Fail;
        }

        if (UDivConst.U8_Div_3(42) != 14)
        {
            return Fail;
        }

        if (UDivConst.U8_Div_5(42) != 8)
        {
            return Fail;
        }

        if (UDivConst.U8_Div_7(420) != 60)
        {
            return Fail;
        }

        if (UDivConst.U8_DivUncontained_I8Max(ulong.MaxValue - 1) != 0)
        {
            return Fail;
        }

        if (UDivConst.U8_DivUncontained_I8Max(ulong.MaxValue) != 1)
        {
            return Fail;
        }

        if (UDivConst.U8_DivPow2_2(42) != 21)
        {
            return Fail;
        }

        if (UDivConst.U8_DivUncontainedPow2_1Shl32(1UL << 33) != 2)
        {
            return Fail;
        }

        return Pass;
    }
}
