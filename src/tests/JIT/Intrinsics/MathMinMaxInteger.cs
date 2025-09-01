// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class MathMinMaxIntegerTest
{
    [Fact]
    public static void TestLong()
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Min(long a, long b) => Math.Min(a, b);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Max(long a, long b) => Math.Max(a, b);

        const long big = long.MaxValue, small = long.MinValue;
        Assert.Equal(small, Min(big, small));
        Assert.Equal(small, Min(small, big));
        Assert.Equal(small, Math.Min(big, small));
        Assert.Equal(small, Math.Min(small, big));
        Assert.Equal(big, Max(big, small));
        Assert.Equal(big, Max(small, big));
        Assert.Equal(big, Math.Max(big, small));
        Assert.Equal(big, Math.Max(small, big));
    }
    
    [Fact]
    public static void TestUlong()
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong Min(ulong a, ulong b) => Math.Min(a, b);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong Max(ulong a, ulong b) => Math.Max(a, b);

        const ulong big = ulong.MaxValue, small = ulong.MinValue;
        Assert.Equal(small, Min(big, small));
        Assert.Equal(small, Min(small, big));
        Assert.Equal(small, Math.Min(big, small));
        Assert.Equal(small, Math.Min(small, big));
        Assert.Equal(big, Max(big, small));
        Assert.Equal(big, Max(small, big));
        Assert.Equal(big, Math.Max(big, small));
        Assert.Equal(big, Math.Max(small, big));
    }
    
    [Fact]
    public static void TestInt()
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Min(int a, int b) => Math.Min(a, b);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Max(int a, int b) => Math.Max(a, b);

        const int big = int.MaxValue, small = int.MinValue;
        Assert.Equal(small, Min(big, small));
        Assert.Equal(small, Min(small, big));
        Assert.Equal(small, Math.Min(big, small));
        Assert.Equal(small, Math.Min(small, big));
        Assert.Equal(big, Max(big, small));
        Assert.Equal(big, Max(small, big));
        Assert.Equal(big, Math.Max(big, small));
        Assert.Equal(big, Math.Max(small, big));
    }
    
    [Fact]
    public static void TestUint()
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint Min(uint a, uint b) => Math.Min(a, b);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint Max(uint a, uint b) => Math.Max(a, b);

        const uint big = uint.MaxValue, small = uint.MinValue;
        Assert.Equal(small, Min(big, small));
        Assert.Equal(small, Min(small, big));
        Assert.Equal(small, Math.Min(big, small));
        Assert.Equal(small, Math.Min(small, big));
        Assert.Equal(big, Max(big, small));
        Assert.Equal(big, Max(small, big));
        Assert.Equal(big, Math.Max(big, small));
        Assert.Equal(big, Math.Max(small, big));
    }
    
    [Fact]
    public static void TestShort()
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static short Min(short a, short b) => Math.Min(a, b);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static short Max(short a, short b) => Math.Max(a, b);

        const short big = short.MaxValue, small = short.MinValue;
        Assert.Equal(small, Min(big, small));
        Assert.Equal(small, Min(small, big));
        Assert.Equal(small, Math.Min(big, small));
        Assert.Equal(small, Math.Min(small, big));
        Assert.Equal(big, Max(big, small));
        Assert.Equal(big, Max(small, big));
        Assert.Equal(big, Math.Max(big, small));
        Assert.Equal(big, Math.Max(small, big));
    }
    
    [Fact]
    public static void TestUshort()
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static ushort Min(ushort a, ushort b) => Math.Min(a, b);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ushort Max(ushort a, ushort b) => Math.Max(a, b);

        const ushort big = ushort.MaxValue, small = ushort.MinValue;
        Assert.Equal(small, Min(big, small));
        Assert.Equal(small, Min(small, big));
        Assert.Equal(small, Math.Min(big, small));
        Assert.Equal(small, Math.Min(small, big));
        Assert.Equal(big, Max(big, small));
        Assert.Equal(big, Max(small, big));
        Assert.Equal(big, Math.Max(big, small));
        Assert.Equal(big, Math.Max(small, big));
    }
    
    [Fact]
    public static void TestSbyte()
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static sbyte Min(sbyte a, sbyte b) => Math.Min(a, b);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static sbyte Max(sbyte a, sbyte b) => Math.Max(a, b);

        const sbyte big = sbyte.MaxValue, small = sbyte.MinValue;
        Assert.Equal(small, Min(big, small));
        Assert.Equal(small, Min(small, big));
        Assert.Equal(small, Math.Min(big, small));
        Assert.Equal(small, Math.Min(small, big));
        Assert.Equal(big, Max(big, small));
        Assert.Equal(big, Max(small, big));
        Assert.Equal(big, Math.Max(big, small));
        Assert.Equal(big, Math.Max(small, big));
    }
    
    [Fact]
    public static void TestByte()
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte Min(byte a, byte b) => Math.Min(a, b);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte Max(byte a, byte b) => Math.Max(a, b);

        const byte big = byte.MaxValue, small = byte.MinValue;
        Assert.Equal(small, Min(big, small));
        Assert.Equal(small, Min(small, big));
        Assert.Equal(small, Math.Min(big, small));
        Assert.Equal(small, Math.Min(small, big));
        Assert.Equal(big, Max(big, small));
        Assert.Equal(big, Max(small, big));
        Assert.Equal(big, Math.Max(big, small));
        Assert.Equal(big, Math.Max(small, big));
    }

    [Fact]
    public static void TestLongValueNumbering()
    {
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        static (long, long, long, long, ulong, ulong, ulong, ulong) MinMaxes(long a, long b) => (
            Math.Min(a, b),
            Math.Min(b, a),
            Math.Max(a, b),
            Math.Max(b, a),
            Math.Min(unchecked((ulong)a), unchecked((ulong)b)),
            Math.Min(unchecked((ulong)b), unchecked((ulong)a)),
            Math.Max(unchecked((ulong)a), unchecked((ulong)b)),
            Math.Max(unchecked((ulong)b), unchecked((ulong)a))
        );

        var m = MinMaxes(long.MinValue, long.MaxValue);
        Assert.Equal(long.MinValue, m.Item1);
        Assert.Equal(long.MinValue, m.Item2);
        Assert.Equal(long.MaxValue, m.Item3);
        Assert.Equal(long.MaxValue, m.Item4);
        Assert.Equal(unchecked((ulong)long.MaxValue), m.Item5);
        Assert.Equal(unchecked((ulong)long.MaxValue), m.Item6);
        Assert.Equal(unchecked((ulong)long.MinValue), m.Item7);
        Assert.Equal(unchecked((ulong)long.MinValue), m.Item8);
    }

    [Fact]
    public static void TestIntValueNumbering()
    {
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        static (int, int, int, int, uint, uint, uint, uint) MinMaxes(int a, int b) => (
            Math.Min(a, b),
            Math.Min(b, a),
            Math.Max(a, b),
            Math.Max(b, a),
            Math.Min(unchecked((uint)a), unchecked((uint)b)),
            Math.Min(unchecked((uint)b), unchecked((uint)a)),
            Math.Max(unchecked((uint)a), unchecked((uint)b)),
            Math.Max(unchecked((uint)b), unchecked((uint)a))
        );

        var m = MinMaxes(int.MinValue, int.MaxValue);
        Assert.Equal(int.MinValue, m.Item1);
        Assert.Equal(int.MinValue, m.Item2);
        Assert.Equal(int.MaxValue, m.Item3);
        Assert.Equal(int.MaxValue, m.Item4);
        Assert.Equal(unchecked((uint)int.MaxValue), m.Item5);
        Assert.Equal(unchecked((uint)int.MaxValue), m.Item6);
        Assert.Equal(unchecked((uint)int.MinValue), m.Item7);
        Assert.Equal(unchecked((uint)int.MinValue), m.Item8);
    }
    
    [Fact]
    public static void TestShortValueNumbering()
    {
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        static (short, short, short, short, ushort, ushort, ushort, ushort) MinMaxes(short a, short b) => (
            Math.Min(a, b),
            Math.Min(b, a),
            Math.Max(a, b),
            Math.Max(b, a),
            Math.Min(unchecked((ushort)a), unchecked((ushort)b)),
            Math.Min(unchecked((ushort)b), unchecked((ushort)a)),
            Math.Max(unchecked((ushort)a), unchecked((ushort)b)),
            Math.Max(unchecked((ushort)b), unchecked((ushort)a))
        );

        var m = MinMaxes(short.MinValue, short.MaxValue);
        Assert.Equal(short.MinValue, m.Item1);
        Assert.Equal(short.MinValue, m.Item2);
        Assert.Equal(short.MaxValue, m.Item3);
        Assert.Equal(short.MaxValue, m.Item4);
        Assert.Equal(unchecked((ushort)short.MaxValue), m.Item5);
        Assert.Equal(unchecked((ushort)short.MaxValue), m.Item6);
        Assert.Equal(unchecked((ushort)short.MinValue), m.Item7);
        Assert.Equal(unchecked((ushort)short.MinValue), m.Item8);
    }
    
    [Fact]
    public static void TestByteValueNumbering()
    {
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        static (sbyte, sbyte, sbyte, sbyte, byte, byte, byte, byte) MinMaxes(sbyte a, sbyte b) => (
            Math.Min(a, b),
            Math.Min(b, a),
            Math.Max(a, b),
            Math.Max(b, a),
            Math.Min(unchecked((byte)a), unchecked((byte)b)),
            Math.Min(unchecked((byte)b), unchecked((byte)a)),
            Math.Max(unchecked((byte)a), unchecked((byte)b)),
            Math.Max(unchecked((byte)b), unchecked((byte)a))
        );

        var m = MinMaxes(sbyte.MinValue, sbyte.MaxValue);
        Assert.Equal(sbyte.MinValue, m.Item1);
        Assert.Equal(sbyte.MinValue, m.Item2);
        Assert.Equal(sbyte.MaxValue, m.Item3);
        Assert.Equal(sbyte.MaxValue, m.Item4);
        Assert.Equal(unchecked((byte)sbyte.MaxValue), m.Item5);
        Assert.Equal(unchecked((byte)sbyte.MaxValue), m.Item6);
        Assert.Equal(unchecked((byte)sbyte.MinValue), m.Item7);
        Assert.Equal(unchecked((byte)sbyte.MinValue), m.Item8);
    }

    [Fact]
    public static void TestUnsignedIntLongValueNumbering()
    {
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        static (uint, uint, uint, uint, ulong, ulong, ulong, ulong) MinMaxes(uint a, uint b) => (
            Math.Min(a, b),
            Math.Min(b, a),
            Math.Max(a, b),
            Math.Max(b, a),
            Math.Min((ulong)a, (ulong)b),
            Math.Min((ulong)b, (ulong)a),
            Math.Max((ulong)a, (ulong)b),
            Math.Max((ulong)b, (ulong)a)
        );

        var m = MinMaxes(uint.MinValue, uint.MaxValue);
        Assert.Equal(uint.MinValue, m.Item1);
        Assert.Equal(uint.MinValue, m.Item2);
        Assert.Equal(uint.MaxValue, m.Item3);
        Assert.Equal(uint.MaxValue, m.Item4);
        Assert.Equal((ulong)uint.MinValue, m.Item5);
        Assert.Equal((ulong)uint.MinValue, m.Item6);
        Assert.Equal((ulong)uint.MaxValue, m.Item7);
        Assert.Equal((ulong)uint.MaxValue, m.Item8);
    }
}
