// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
    public static unsafe int LocallocCnstB1_PSP()
    {
        byte* a = stackalloc byte[1];
        int i;
        for (i = 0; i < 1; i++)
        {
            a[i] = (byte) i;
        }

        i = 0;
        try
        {
            for (; i < 1; i++)
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

    public static int Main()
    {
        int ret;

        ret = LocallocCnstB1_PSP();
        if (ret != -1) {
            Console.WriteLine("LocallocCnstB1_PSP: Failed on index: " + ret);
            return Fail;
        }

        return Pass;
    }
}
