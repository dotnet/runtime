// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134934
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(10, Test(new int[] { 1, 2, 3, 4 }));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(int[] array)
    {
        int sum = 0;
        int i = 0;
        bool condition;
        do
        {
            sum += array[i];
            i++;
            condition = i < 4;
        }
        while (condition);

        return condition ? -1 : sum;
    }
}
