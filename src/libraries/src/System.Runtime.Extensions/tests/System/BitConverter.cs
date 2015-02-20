// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

public static unsafe class BitConverterTests
{
    [Fact]
    public static void TestBitConverter()
    {
        if (!BitConverter.IsLittleEndian)
        {
            Console.WriteLine("Detected BigEndian platform. Tests not certified for big-endian systems.");
            return;
        }

        int i;

        double d = 123456.3234;
        Int64 i64;

        i64 = BitConverter.DoubleToInt64Bits(d);
        Assert.Equal(i64, 4683220267154373240L);
        double d2 = BitConverter.Int64BitsToDouble(i64);
        Assert.Equal(d, d2);

        byte[] b;
        bool bol;

        b = BitConverter.GetBytes(true);
        Assert.Equal(b.Length, 1);
        Assert.Equal(b[0], 1);
        bol = BitConverter.ToBoolean(b, 0);
        Assert.True(bol);

        b = BitConverter.GetBytes(false);
        Assert.Equal(b.Length, 1);
        Assert.Equal(b[0], 0);
        bol = BitConverter.ToBoolean(b, 0);
        Assert.False(bol);

        b = BitConverter.GetBytes('A');
        Assert.Equal(b.Length, 2);
        Assert.Equal(b[0], 0x41);
        Assert.Equal(b[1], 0);

        char c = BitConverter.ToChar(b, 0);
        Assert.Equal(c, 'A');

        d = 123456.3234;
        b = BitConverter.GetBytes(d);
        Assert.Equal(b.Length, 8);
        Assert.Equal(b[0], 0x78);
        Assert.Equal(b[1], 0x7a);
        Assert.Equal(b[2], 0xa5);
        Assert.Equal(b[3], 0x2c);
        Assert.Equal(b[4], 0x05);
        Assert.Equal(b[5], 0x24);
        Assert.Equal(b[6], 0xfe);
        Assert.Equal(b[7], 0x40);
        d = BitConverter.ToDouble(b, 0);
        Assert.Equal(d, 123456.3234);

        float f = 8392.34F;
        b = BitConverter.GetBytes(f);
        Assert.Equal(b.Length, 4);
        Assert.Equal(b[0], 0x5c);
        Assert.Equal(b[1], 0x21);
        Assert.Equal(b[2], 0x03);
        Assert.Equal(b[3], 0x46);

        f = BitConverter.ToSingle(b, 0);
        Assert.Equal(f, 8392.34F);

        short sh = 0x1234;
        b = BitConverter.GetBytes(sh);
        Assert.Equal(b.Length, 2);
        Assert.Equal(b[0], 0x34);
        Assert.Equal(b[1], 0x12);
        sh = BitConverter.ToInt16(b, 0);
        Assert.Equal(sh, 0x1234);

        ushort ush = 0x1234;
        b = BitConverter.GetBytes(ush);
        Assert.Equal(b.Length, 2);
        Assert.Equal(b[0], 0x34);
        Assert.Equal(b[1], 0x12);
        ush = BitConverter.ToUInt16(b, 0);
        Assert.Equal(ush, 0x1234);

        i = 0x12345678;
        b = BitConverter.GetBytes(i);
        Assert.Equal(b.Length, 4);
        Assert.Equal(b[0], 0x78);
        Assert.Equal(b[1], 0x56);
        Assert.Equal(b[2], 0x34);
        Assert.Equal(b[3], 0x12);

        i = BitConverter.ToInt32(b, 0);
        Assert.Equal(i, 0x12345678);

        uint u = 0x12345678u;
        b = BitConverter.GetBytes(u);
        Assert.Equal(b.Length, 4);
        Assert.Equal(b[0], 0x78);
        Assert.Equal(b[1], 0x56);
        Assert.Equal(b[2], 0x34);
        Assert.Equal(b[3], 0x12);

        u = BitConverter.ToUInt32(b, 0);
        Assert.Equal(u, 0x12345678u);

        long l = 0x0123456789abcdef;
        b = BitConverter.GetBytes(l);
        Assert.Equal(b.Length, 8);
        Assert.Equal(b[0], 0xef);
        Assert.Equal(b[1], 0xcd);
        Assert.Equal(b[2], 0xab);
        Assert.Equal(b[3], 0x89);
        Assert.Equal(b[4], 0x67);
        Assert.Equal(b[5], 0x45);
        Assert.Equal(b[6], 0x23);
        Assert.Equal(b[7], 0x01);
        l = BitConverter.ToInt64(b, 0);
        Assert.Equal(l, 0x0123456789abcdef);

        ulong ul = 0x0123456789abcdefu;
        b = BitConverter.GetBytes(ul);
        Assert.Equal(b.Length, 8);
        Assert.Equal(b[0], 0xef);
        Assert.Equal(b[1], 0xcd);
        Assert.Equal(b[2], 0xab);
        Assert.Equal(b[3], 0x89);
        Assert.Equal(b[4], 0x67);
        Assert.Equal(b[5], 0x45);
        Assert.Equal(b[6], 0x23);
        Assert.Equal(b[7], 0x01);
        ul = BitConverter.ToUInt64(b, 0);
        Assert.Equal(ul, 0x0123456789abcdefu);

        b = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9a };
        String s = BitConverter.ToString(b);
        Assert.Equal(s, "12-34-56-78-9A");

        s = BitConverter.ToString(b, 2);
        Assert.Equal(s, "56-78-9A");

        s = BitConverter.ToString(b, 2, 1);
        Assert.Equal(s, "56");

        s = BitConverter.ToString(new byte[0]);
        Assert.Equal(s, String.Empty);
    }
}

