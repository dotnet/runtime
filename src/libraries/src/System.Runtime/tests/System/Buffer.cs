// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;

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

        Assert.Throws<ArgumentException>(() => Buffer.BlockCopy(new string[3], 0, new int[3], 0, 0));
        Assert.Throws<ArgumentException>(() => Buffer.BlockCopy(new byte[3], 3, new byte[3], 0, 1));
        Assert.Throws<ArgumentException>(() => Buffer.BlockCopy(new byte[3], 0, new byte[3], 3, 1));
        Assert.Throws<ArgumentException>(() => Buffer.BlockCopy(new byte[3], 0, new byte[3], 4, 0));
    }

    public static IEnumerable<object[]> ByteLengthTestData
    {
        get
        {
           return new[]
           {
               new object[] {typeof(byte), sizeof(byte) },
               new object[] {typeof(sbyte), sizeof(sbyte) },
               new object[] {typeof(short), sizeof(short) },
               new object[] {typeof(ushort), sizeof(ushort) },
               new object[] {typeof(int), sizeof(int) },
               new object[] {typeof(uint), sizeof(uint) },
               new object[] {typeof(long), sizeof(long) },
               new object[] {typeof(ulong), sizeof(ulong) },
               new object[] {typeof(IntPtr), sizeof(IntPtr) },
               new object[] {typeof(UIntPtr), sizeof(UIntPtr) },
               new object[] {typeof(double), sizeof(double) },
               new object[] {typeof(float), sizeof(float) },
               new object[] {typeof(bool), sizeof(bool) },
               new object[] {typeof(char), sizeof(char) },
               new object[] {typeof(decimal), sizeof(decimal) },
               new object[] {typeof(DateTime), sizeof(DateTime) },
               new object[] {typeof(string), -1 },
           };
        }
    }

    [Theory, MemberData(nameof(ByteLengthTestData))]
    public static void TestByteLength(Type type, int size)
    {
        const int length = 25;
        Array array = Array.CreateInstance(type, length);
        if (type.GetTypeInfo().IsPrimitive)
        {
            Assert.Equal(length * size, Buffer.ByteLength(array));
        }
        else
        {
            Assert.Throws<ArgumentException>(() => Buffer.ByteLength(array));
        }
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
