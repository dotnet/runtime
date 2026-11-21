// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134897
{
    private struct S
    {
        public int A;
        public int B;
    }

    private class Holder
    {
        public static S Value = new S { A = 10, B = 20 };
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(92, Test(new[] { false, true, true }));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(bool[] cond)
    {
        int sum = 0;
        for (int i = 0; i < cond.Length; i++)
        {
            if (cond[i])
            {
                sum++;
            }

            S s = Holder.Value;
            sum += s.A + s.B;
        }

        return sum;
    }
}
