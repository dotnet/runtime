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


    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool I1_BT_reg_min(sbyte x) => (x & (1 << 7)) != 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static sbyte I1_BT_reg_min_JCC(sbyte x) => (sbyte)((x & (1 << 7)) == 0 ? (x + 1) : (x - 1));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool I2_BT_reg_min(short x) => (x & (1 << 15)) != 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool I4_BT_reg_min(int x) => (x & (1 << 31)) != 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool I4_BT_reg_min_EQ(int x) => (x & (1 << 31)) == 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int I4_BT_reg_min_JCC(int x) => (x & (1 << 31)) == 0 ? (x + 1) : (x - 1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool I8_BT_reg_min(long x) => (x & (1L << 63)) != 0;


    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool I1_BT_reg_min_1(sbyte x) => (x & (1 << 6)) != 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static sbyte I1_BT_reg_min_1_JCC(sbyte x) => (sbyte)((x & (1 << 6)) == 0 ? (x + 1) : (x - 1));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool I2_BT_reg_min_1(short x) => (x & (1 << 14)) != 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool I4_BT_reg_min_1(int x) => (x & (1 << 30)) != 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool I4_BT_reg_min_1_EQ(int x) => (x & (1 << 30)) == 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int I4_BT_reg_min_1_JCC(int x) => (x & (1 << 30)) == 0 ? (x + 1) : (x - 1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool I8_BT_reg_min_1(long x) => (x & (1L << 62)) != 0;


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

        pass &= I1_BT_reg_min(sbyte.MinValue);
        pass &= !I1_BT_reg_min(sbyte.MaxValue);
        pass &= !I1_BT_reg_min_1(sbyte.MinValue);
        pass &= I1_BT_reg_min_1(sbyte.MaxValue);

        pass &= I1_BT_reg_min_JCC(sbyte.MinValue) == sbyte.MaxValue;
        pass &= I1_BT_reg_min_JCC(sbyte.MaxValue) == sbyte.MinValue;
        pass &= I1_BT_reg_min_1_JCC(sbyte.MinValue) == (sbyte.MinValue + 1);
        pass &= I1_BT_reg_min_1_JCC(sbyte.MaxValue) == (sbyte.MaxValue - 1);

        pass &= I2_BT_reg_min(short.MinValue);
        pass &= !I2_BT_reg_min(short.MaxValue);
        pass &= !I2_BT_reg_min_1(short.MinValue);
        pass &= I2_BT_reg_min_1(short.MaxValue);

        pass &= I4_BT_reg_min(int.MinValue);
        pass &= !I4_BT_reg_min(int.MaxValue);
        pass &= !I4_BT_reg_min_1(int.MinValue);
        pass &= I4_BT_reg_min_1(int.MaxValue);

        pass &= !I4_BT_reg_min_EQ(int.MinValue);
        pass &= I4_BT_reg_min_EQ(int.MaxValue);
        pass &= I4_BT_reg_min_1_EQ(int.MinValue);
        pass &= !I4_BT_reg_min_1_EQ(int.MaxValue);

        pass &= I4_BT_reg_min_JCC(int.MinValue) == int.MaxValue;
        pass &= I4_BT_reg_min_JCC(int.MaxValue) == int.MinValue;
        pass &= I4_BT_reg_min_1_JCC(int.MinValue) == (int.MinValue + 1);
        pass &= I4_BT_reg_min_1_JCC(int.MaxValue) == (int.MaxValue - 1);

        pass &= I8_BT_reg_min(long.MinValue);
        pass &= !I8_BT_reg_min(long.MaxValue);
        pass &= !I8_BT_reg_min_1(long.MinValue);
        pass &= I8_BT_reg_min_1(long.MaxValue);

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
