// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool I1_BT_reg_reg(sbyte x, int y) => (x & (1 << y)) != 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool I1_BT_mem_reg(ref sbyte x, int y) => (x & (1 << y)) != 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool I2_BT_reg_reg(short x, int y) => (x & (1 << y)) != 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool I2_BT_mem_reg(ref short x, int y) => (x & (1 << y)) != 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool I4_BT_reg_reg(int x, int y) => (x & (1 << y)) != 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool I4_BT_reg_reg_EQ(int x, int y) => (x & (1 << y)) == 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int I4_BT_reg_reg_JCC(int x, int y) => (x & (1 << y)) == 0 ? (x + 1) : (x - 1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool I4_BT_mem_reg(ref int x, int y) => (x & (1 << y)) != 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool I8_BT_reg_reg(long x, int y) => (x & (1L << y)) != 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool I8_BT_mem_reg(ref long x, int y) => (x & (1L << y)) != 0;

    [Fact]
    public static int TestEntryPoint()
    {
        sbyte i1min = sbyte.MinValue;
        sbyte i1one = 1;
        sbyte i1two = 2;
        short i2min = short.MinValue;
        short i2one = 1;
        short i2two = 2;
        int i4one = 1;
        int i4two = 2;
        long i8one = 1;
        long i8two = 2;
        bool pass = true;

        pass &= I1_BT_reg_reg(i1min, 7);
        pass &= I1_BT_reg_reg(i1min, 8);
        pass &= I1_BT_reg_reg(i1one, 0);
        pass &= !I1_BT_reg_reg(i1one, 8);
        pass &= I1_BT_reg_reg(i1one, 32);
        pass &= !I1_BT_reg_reg(i1two, 0);

        pass &= I1_BT_mem_reg(ref i1min, 7);
        pass &= I1_BT_mem_reg(ref i1min, 8);
        pass &= I1_BT_mem_reg(ref i1one, 0);
        pass &= !I1_BT_mem_reg(ref i1one, 8);
        pass &= I1_BT_mem_reg(ref i1one, 32);
        pass &= !I1_BT_mem_reg(ref i1two, 0);

        pass &= I2_BT_reg_reg(i2min, 15);
        pass &= I2_BT_reg_reg(i2min, 16);
        pass &= I2_BT_reg_reg(i2one, 0);
        pass &= !I2_BT_reg_reg(i2one, 16);
        pass &= I2_BT_reg_reg(i2one, 32);
        pass &= !I2_BT_reg_reg(i2two, 0);

        pass &= I2_BT_mem_reg(ref i2min, 15);
        pass &= I2_BT_mem_reg(ref i2min, 16);
        pass &= I2_BT_mem_reg(ref i2one, 0);
        pass &= !I2_BT_mem_reg(ref i2one, 16);
        pass &= I2_BT_mem_reg(ref i2one, 32);
        pass &= !I2_BT_mem_reg(ref i2two, 0);

        pass &= I4_BT_reg_reg(i4one, 0);
        pass &= I4_BT_reg_reg(i4one, 32);
        pass &= !I4_BT_reg_reg(i4two, 0);

        pass &= !I4_BT_reg_reg_EQ(i4one, 0);
        pass &= !I4_BT_reg_reg_EQ(i4one, 32);
        pass &= I4_BT_reg_reg_EQ(i4two, 0);

        pass &= I4_BT_reg_reg_JCC(i4one, 0) == 0;
        pass &= I4_BT_reg_reg_JCC(i4one, 32) == 0;
        pass &= I4_BT_reg_reg_JCC(i4two, 0) == 3;

        pass &= I4_BT_mem_reg(ref i4one, 0);
        pass &= I4_BT_mem_reg(ref i4one, 32);
        pass &= !I4_BT_mem_reg(ref i4two, 0);

        pass &= I8_BT_reg_reg(i8one, 0);
        pass &= !I8_BT_reg_reg(i8one, 32);
        pass &= I8_BT_reg_reg(i8one, 64);
        pass &= !I8_BT_reg_reg(i8two, 0);

        pass &= I8_BT_mem_reg(ref i8one, 0);
        pass &= !I8_BT_mem_reg(ref i8one, 32);
        pass &= I8_BT_mem_reg(ref i8one, 64);
        pass &= !I8_BT_mem_reg(ref i8two, 0);

        if (pass)
        {
            Console.WriteLine("PASSED");
            return 100;
        }
        else
        {
            Console.WriteLine("FAILED");
            return 1;
        }
    }
}
