// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

public static unsafe class BufferTests
{
    [Fact]
    public static void TestBlockCopy()
    {
        byte[] b1 = { 0x1a, 0x2b, 0x3c, 0x4d };
        byte[] b2 = new byte[6];
        for (int i = 0; i < b2.Length; i++)
            b2[i] = 0x6f;

        Buffer.BlockCopy(b1, 0, b2, 1, 4);
        Assert.Equal(b2[0], 0x6f);
        Assert.Equal(b2[1], 0x1a);
        Assert.Equal(b2[2], 0x2b);
        Assert.Equal(b2[3], 0x3c);
        Assert.Equal(b2[4], 0x4d);
        Assert.Equal(b2[5], 0x6f);

        b2 = new byte[] { 0x1a, 0x2b, 0x3c, 0x4d, 0x5e };
        Buffer.BlockCopy(b2, 1, b2, 2, 2);
        Assert.Equal(b2[0], 0x1a);
        Assert.Equal(b2[1], 0x2b);
        Assert.Equal(b2[2], 0x2b);
        Assert.Equal(b2[3], 0x3c);
        Assert.Equal(b2[4], 0x5e);

        try
        {
            Buffer.BlockCopy(new String[3], 0, new int[3], 0, 0);
            Assert.True(false, "BlockCopy should have thrown.");  //Array not primitive
        }
        catch (ArgumentException)
        {
        }

        try
        {
            Buffer.BlockCopy(new byte[3], 3, new byte[3], 0, 1);
            Assert.True(false, "BlockCopy should have thrown.");  // Buffer overrun
        }
        catch (ArgumentException)
        {
        }

        try
        {
            Buffer.BlockCopy(new byte[3], 0, new byte[3], 3, 1);
            Assert.True(false, "BlockCopy should have thrown.");  // Buffer overrun
        }
        catch (ArgumentException)
        {
        }

        try
        {
            Buffer.BlockCopy(new byte[3], 0, new byte[3], 4, 0);  // Buffer overrun
            Assert.True(false, "BlockCopy should have thrown.");
        }
        catch (ArgumentException)
        {
        }
    }

    [Fact]
    public static void TestByteLength()
    {
        int i;

        i = Buffer.ByteLength(new int[7]);
        Assert.Equal(i, 7 * sizeof(int));

        i = Buffer.ByteLength(new double[33]);
        Assert.Equal(i, 33 * sizeof(double));
    }

    [Fact]
    public static void TestGetByteAndSetByte()
    {
        uint[] a = { 0x01234567, 0x89abcdef };
        byte v;

        v = Buffer.GetByte(a, 0);
        Assert.Equal(v, 0x67);

        v = Buffer.GetByte(a, 7);
        Assert.Equal(v, 0x89);

        Assert.Throws<ArgumentOutOfRangeException>(() => { v = Buffer.GetByte(a, -1); });

        Assert.Throws<ArgumentOutOfRangeException>(() => { v = Buffer.GetByte(a, 8); });

        Assert.Throws<ArgumentOutOfRangeException>(() => Buffer.SetByte(a, -1, 0xff));

        Assert.Throws<ArgumentOutOfRangeException>(() => Buffer.SetByte(a, 8, 0xff));

        Assert.Equal<uint>(a[0], 0x01234567);
        Assert.Equal<uint>(a[1], 0x89abcdef);
        Buffer.SetByte(a, 0, 0x42);
        Assert.Equal<uint>(a[0], 0x01234542);
        Assert.Equal<uint>(a[1], 0x89abcdef);
        Buffer.SetByte(a, 7, 0xa2);
        Assert.Equal<uint>(a[0], 0x01234542);
        Assert.Equal<uint>(a[1], 0xa2abcdef);
    }
}

