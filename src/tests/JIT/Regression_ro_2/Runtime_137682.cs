// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_137682
{
    private static int s_a, s_b;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Call(int k)
    {
        if (k > 5)
        {
            throw new Exception();
        }
    }

    // The only path back to the loop header is through the catch, and the
    // IV test block has both normal successors outside the loop.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(int k)
    {
        int i = 0;
        while (true)
        {
            try
            {
                Call(k);
                i++;
                if (i >= 3) goto L1;
                goto L2;
            }
            catch (Exception)
            {
                k--;
            }
        }
    L1:
        s_a += i;
        return s_a;
    L2:
        s_b += i * 2;
        return s_b;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(2, Test(0));
    }
}
