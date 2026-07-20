// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_130425;

public class Runtime_130425
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(1, Test(3));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(int iterationCount)
    {
        int result = 0;
        int iteration = 0;

        for (int i = -1; (i < 0) && (iteration < iterationCount); i *= 2)
        {
            if (i < -2)
            {
                result++;
            }

            iteration++;
        }

        return result;
    }
}
