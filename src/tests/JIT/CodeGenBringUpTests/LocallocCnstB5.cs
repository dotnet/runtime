// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
public class BringUpTest
{
    const int Pass = 100;
    const int Fail = -1;

    // Reduce all values to byte
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static unsafe bool CHECK(byte check, byte expected) {
        return check == expected;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static unsafe int LocallocCnstB5()
    {
        byte* a = stackalloc byte[5];
        for (int i = 0; i < 5; i++)
        {
            a[i] = (byte) i;
        }

        for (int i = 0; i < 5; i++)
        {
            if (!CHECK(a[i], (byte) i)) return i;
        }

        return -1;
    }

    public static int Main()
    {
        int ret;

        ret = LocallocCnstB5();
        if (ret != -1) {
            Console.WriteLine("LocallocCnstB5: Failed on index: " + ret);
            return Fail;
        }

        return Pass;
    }
}
