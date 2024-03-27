// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_92590
{
    [Fact]
    public static void TestEntryPoint()
    {
        Span<byte> bytes = stackalloc byte[4];
        bytes.Fill(0xff);
        TestByteByte(ref bytes[0], 0, Vector256.Create((byte)1));

        Assert.True(bytes.SequenceEqual(stackalloc byte[] { 0x2, 0xff, 0xff, 0xff }));

        bytes.Fill(0xff);
        TestByteInt(ref bytes[0], 0, Vector256.Create(1));

        Assert.True(bytes.SequenceEqual(stackalloc byte[] { 0x2, 0xff, 0xff, 0xff }));

        int i = int.MaxValue;
        TestIntByte(ref i, 0, Vector256.Create((byte)1));

        Assert.Equal(2, i);

        i = int.MaxValue;
        TestIntInt(ref i, 0, Vector256.Create(1));

        Assert.Equal(2, i);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestByteByte(ref byte b, int x, Vector256<byte> vin)
    {
        Vector256<byte> v = vin + vin;
        Unsafe.Add(ref b, x) = v[3];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestByteInt(ref byte b, int x, Vector256<int> vin)
    {
        Vector256<int> v = vin + vin;
        Unsafe.Add(ref b, x) = (byte)v[3];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestIntByte(ref int b, int x, Vector256<byte> vin)
    {
        Vector256<byte> v = vin + vin;
        Unsafe.Add(ref b, x) = v[3];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestIntInt(ref int b, int x, Vector256<int> vin)
    {
        Vector256<int> v = vin + vin;
        Unsafe.Add(ref b, x) = v[3];
    }
}
