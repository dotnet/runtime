// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_LocallocCnstB5001
{
    const int Pass = 100;
    const int Fail = -1;

    // Reduce all values to byte
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static unsafe bool CHECK(byte check, byte expected) {
        return check == expected;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static unsafe int LocallocCnstB5001()
    {
        byte* a = stackalloc byte[5001];
        for (int i = 0; i < 5001; i++)
        {
            a[i] = (byte) i;
        }

        for (int i = 0; i < 5001; i++)
        {
            if (!CHECK(a[i], (byte) i)) return i;
        }

        return -1;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int ret;

        ret = LocallocCnstB5001();
        if (ret != -1) {
            Console.WriteLine("LocallocCnstB5001: Failed on index: " + ret);
            return Fail;
        }

        return Pass;
    }
}
