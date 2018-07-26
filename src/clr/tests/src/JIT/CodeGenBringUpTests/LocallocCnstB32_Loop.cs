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
    public static unsafe int LocallocCnstB32_Loop(int count)
    {
        for (int j = 0; j < count; j++)
        {
            byte* a = stackalloc byte[32];
            for (int i = 0; i < 5; i++)
            {
                a[i] = (byte) (i + j);
            }

            for (int i = 0; i < 5; i++)
            {
                if (!CHECK(a[i], (byte) (i + j))) return i + j * 100;
            }
        }
        return -1;
    }

    public static int Main()
    {
        int ret;

        ret = LocallocCnstB32_Loop(4);
        if (ret != -1) {
            Console.WriteLine("LocallocCnstB32_Loop: Failed on index: " + ret);
            return Fail;
        }

        return Pass;
    }
}
