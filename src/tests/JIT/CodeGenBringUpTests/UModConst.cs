// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

static class UModConst
{
    // U4

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint U4_Mod_0(uint u4)
    {
        return u4 % 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint U4_Mod_1(uint u4)
    {
        return u4 % 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint U4_Mod_3(uint u4)
    {
        return u4 % 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint U4_Mod_5(uint u4)
    {
        return u4 % 5;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint U4_Mod_7(uint u4)
    {
        return u4 % 7;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint U4_ModPow2_16(uint u4)
    {
        return u4 % 16;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint U4_ModPow2_0x80000000(uint u4)
    {
        return u4 % 0x80000000u;
    }

    // U8

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ulong U8_Mod_0(ulong u8)
    {
        return u8 % 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ulong U8_Mod_1(ulong u8)
    {
        return u8 % 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ulong U8_Mod_3(ulong u8)
    {
        return u8 % 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ulong U8_Mod_5(ulong u8)
    {
        return u8 % 5;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ulong U8_Mod_7(ulong u8)
    {
        return u8 % 7;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ulong U8_ModUncontained_I8Max(ulong u8)
    {
        return u8 % ulong.MaxValue;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ulong U8_ModPow2_8(ulong u8)
    {
        return u8 % 8;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ulong U8_ModUncontainedPow2_1Shl32(ulong u8)
    {
        return u8 % (1UL << 32);
    }
}

public static class UModProgram
{
    [Fact]
    public static int TestEntryPoint()
    {
        const int Pass = 100;
        const int Fail = -1;

        // U4

        try
        {
            UModConst.U4_Mod_0(42);
            return Fail;
        }
        catch (DivideByZeroException)
        {
        }
        catch (Exception)
        {
            return Fail;
        }

        if (UModConst.U4_Mod_1(42) != 0)
        {
            return Fail;
        }

        if (UModConst.U4_Mod_3(43) != 1)
        {
            return Fail;
        }

        if (UModConst.U4_Mod_5(42) != 2)
        {
            return Fail;
        }

        if (UModConst.U4_Mod_7(43) != 1)
        {
            return Fail;
        }

        if (UModConst.U4_ModPow2_16(42) != 10)
        {
            return Fail;
        }

        if (UModConst.U4_ModPow2_0x80000000(3) != 3)
        {
            return Fail;
        }

        if (UModConst.U4_ModPow2_0x80000000(0x80000001u) != 1)
        {
            return Fail;
        }

        // U8

        try
        {
            UModConst.U8_Mod_0(42);
            return Fail;
        }
        catch (DivideByZeroException)
        {
        }
        catch (Exception)
        {
            return Fail;
        }

        if (UModConst.U8_Mod_1(42) != 0)
        {
            return Fail;
        }

        if (UModConst.U8_Mod_3(43) != 1)
        {
            return Fail;
        }

        if (UModConst.U8_Mod_5(42) != 2)
        {
            return Fail;
        }

        if (UModConst.U8_Mod_7(420) != 0)
        {
            return Fail;
        }

        if (UModConst.U8_ModUncontained_I8Max(ulong.MaxValue - 1) != ulong.MaxValue - 1)
        {
            return Fail;
        }

        if (UModConst.U8_ModUncontained_I8Max(ulong.MaxValue) != 0)
        {
            return Fail;
        }

        if (UModConst.U8_ModPow2_8(42) != 2)
        {
            return Fail;
        }

        if (UModConst.U8_ModPow2_8(43) != 3)
        {
            return Fail;
        }

        if (UModConst.U8_ModUncontainedPow2_1Shl32((1UL << 33) + 42) != 42)
        {
            return Fail;
        }

        return Pass;
    }
}
