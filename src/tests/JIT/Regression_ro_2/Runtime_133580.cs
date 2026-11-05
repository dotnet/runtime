// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public unsafe class Runtime_133580
{
    [StructLayout(LayoutKind.Explicit)]
    private struct Overlapped
    {
        [FieldOffset(0)]
        public int I;

        [FieldOffset(0)]
        public byte B;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static uint FromByte(byte* p) =>
        BitConverter.SingleToUInt32Bits(BitConverter.Int32BitsToSingle(*p));

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static uint FromUShort(ushort* p) =>
        BitConverter.SingleToUInt32Bits(BitConverter.Int32BitsToSingle(*p));

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static uint FromByteViaBitCast(byte* p) =>
        BitConverter.SingleToUInt32Bits(Unsafe.BitCast<int, float>(*p));

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static uint FromLclFld(int value)
    {
        Overlapped o = default;
        o.I = value;
        return BitConverter.SingleToUInt32Bits(BitConverter.Int32BitsToSingle(o.B));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static uint FromInt(int* p) =>
        BitConverter.SingleToUInt32Bits(BitConverter.Int32BitsToSingle(*p));

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static ulong FromLong(long* p) =>
        BitConverter.DoubleToUInt64Bits(BitConverter.Int64BitsToDouble(*p));

    [Fact]
    public static void TestEntryPoint()
    {
        byte* buf = stackalloc byte[8];
        buf[0] = 0x01;
        buf[1] = 0x02;
        buf[2] = 0x03;
        buf[3] = 0x04;
        buf[4] = 0x05;
        buf[5] = 0x06;
        buf[6] = 0x07;
        buf[7] = 0x08;

        Assert.Equal(0x00000001u, FromByte(buf));
        Assert.Equal(0x00000201u, FromUShort((ushort*)buf));
        Assert.Equal(0x00000001u, FromByteViaBitCast(buf));
        Assert.Equal(0x00000001u, FromLclFld(0x04030201));

        Assert.Equal(0x04030201u, FromInt((int*)buf));
        Assert.Equal(0x0807060504030201ul, FromLong((long*)buf));
    }
}
