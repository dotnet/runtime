// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_LocallocCnstB5_PSP
{
    const int Pass = 100;
    const int Fail = -1;

    // Reduce all values to byte
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static unsafe bool CHECK(byte check, byte expected) {
        return check == expected;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static unsafe int LocallocCnstB5_PSP()
    {
        byte* a = stackalloc byte[5];
        int i;
        for (i = 0; i < 5; i++)
        {
            a[i] = (byte) i;
        }

        i = 0;
        try
        {
            for (; i < 5; i++)
            {
                if (!CHECK(a[i], (byte) i)) return i;
            }
        }
        catch
        {
            Console.WriteLine("ERROR!!!");
            return i;
        }

        return -1;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int ret;

        ret = LocallocCnstB5_PSP();
        if (ret != -1) {
            Console.WriteLine("LocallocCnstB5_PSP: Failed on index: " + ret);
            return Fail;
        }

        return Pass;
    }
}
