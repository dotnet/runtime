// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_LocallocB_N
{
    const int Pass = 100;
    const int Fail = -1;

    // Reduce all values to byte
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static unsafe bool CHECK(byte check, byte expected) {
        return check == expected;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static unsafe int LocallocB_N(int n)
    {
        byte* a = stackalloc byte[n];

        for (int i = 0; i < n; i++)
        {
            a[i] = (byte) i;
        }

        for (int i = 0; i < n; i++)
        {
            if (!CHECK(a[i], (byte) i)) return i;
        }

        return -1;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int ret;

        ret = LocallocB_N(1);
        if (ret != -1) {
            Console.WriteLine("LocallocB_N - Test 1: Failed on index: " + ret);
            return Fail;
        }

        ret = LocallocB_N(5);
        if (ret != -1) {
            Console.WriteLine("LocallocB_N - Test 2: Failed on index: " + ret);
            return Fail;
        }

        ret = LocallocB_N(117);
        if (ret != -1) {
            Console.WriteLine("LocallocB_N - Test 3: Failed on index: " + ret);
            return Fail;
        }

        ret = LocallocB_N(5001);
        if (ret != -1) {
            Console.WriteLine("LocallocB_N - Test 4: Failed on index: " + ret);
            return Fail;
        }

        return Pass;
    }
}
