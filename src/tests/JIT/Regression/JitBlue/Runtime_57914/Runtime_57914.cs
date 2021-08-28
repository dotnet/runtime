// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public class Program
{
    public static int Main()
    {
        return (Test1() && Test2()) ? 100 : 101;
    }

    private static bool Test1()
    {
        byte[] arr = new byte[2];
        arr[0] = 1;
        arr[1] = 2;

        short a1 = Unsafe.ReadUnaligned<short>(ref arr[0]);
        arr[1] = 42;
        short a2 = Unsafe.ReadUnaligned<short>(ref arr[0]);

        return a1 != a2;
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
