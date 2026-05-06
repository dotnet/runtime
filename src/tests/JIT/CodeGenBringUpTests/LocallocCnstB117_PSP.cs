// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_LocallocCnstB117_PSP
{
    const int Pass = 100;
    const int Fail = -1;

    // Reduce all values to byte
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static unsafe bool CHECK(byte check, byte expected) {
        return check == expected;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static unsafe int LocallocCnstB117_PSP()
    {
        byte* a = stackalloc byte[117];
        int i;
        for (i = 0; i < 117; i++)
        {
            a[i] = (byte) i;
        }

        i = 0;
        try
        {
            for (; i < 117; i++)
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

        ret = LocallocCnstB117_PSP();
        if (ret != -1) {
            Console.WriteLine("LocallocCnstB117_PSP: Failed on index: " + ret);
            return Fail;
        }

        return Pass;
    }
}
