// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_135817
{
    private class Holder
    {
        public static readonly int? Value = Init();

        private static int? Init() => 42;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(42, Test(1, 1));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(int n, int denom)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += i / denom;
            object res = Holder.Value;
            if (res is not null)
                sum += 42;
        }
        return sum;
    }
}
