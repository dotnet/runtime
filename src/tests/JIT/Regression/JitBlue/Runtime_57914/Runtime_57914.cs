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
        return (Test1() && Test2()) ? 100 : 101;
    }

    private static bool Test1()
    {
        byte[] array = new byte[2];
        array[0] = 1;
        array[1] = 2;

        // a1, a2 and a3 all have different values here
        byte a1  = Unsafe.ReadUnaligned<byte>(ref array[0]);
        short a2 = Unsafe.ReadUnaligned<short>(ref array[0]);
        array[1] = 42;
        short a3 = Unsafe.ReadUnaligned<short>(ref array[0]);

        return a1 != a2 && a1 != a3 && a2 != a3;
    }

    private static bool Test2()
    {
        bool result = true;
        byte[] buffer = new byte[4];
        buffer[0] = 0x1;
        buffer[1] = 0x2;
        buffer[2] = 0x3;
        buffer[3] = 0x4;

        if (buffer.Length > 0)
        {
            int n = Unsafe.ReadUnaligned<int>(ref buffer[0]);
            if (n != 0x4030201)
                result = false;
            Consume(n);
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Consume(int n) {}
}
