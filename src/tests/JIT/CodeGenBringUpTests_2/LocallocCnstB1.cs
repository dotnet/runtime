// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_LocallocCnstB1
{
    const int Pass = 100;
    const int Fail = -1;

    // Reduce all values to byte
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static unsafe bool CHECK(byte check, byte expected) {
        return check == expected;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static unsafe int LocallocCnstB1()
    {
        byte* a = stackalloc byte[1];
        for (int i = 0; i < 1; i++)
        {
            a[i] = (byte) i;
        }

        for (int i = 0; i < 1; i++)
        {
            if (!CHECK(a[i], (byte) i)) return i;
        }

        return -1;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int ret;

        ret = LocallocCnstB1();
        if (ret != -1) {
            Console.WriteLine("LocallocCnstB1: Failed on index: " + ret);
            return Fail;
        }

        return Pass;
    }
}
