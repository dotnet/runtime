// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        int expected = BitConverter.IsLittleEndian ? 0x78563412 : 0x12345678;
        int actual = Test();
        return actual == expected ? 100 : 1;
    }

    struct Bytes
    {
        public byte b1, b2, b3, b4;

        public Bytes(int dummy)
        {
            b1 = 0x12;
            b2 = 0x34;
            b3 = 0x56;
            b4 = 0x78;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test()
    {
        Bytes s = new Bytes(42);
        return Unsafe.As<byte, int>(ref s.b1);
    }
}
