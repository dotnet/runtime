// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134476
{
    [Theory]
    [InlineData(0, 20)]
    [InlineData(3, 50)]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static void TestEntryPoint(int index, int expected)
    {
        int[] array = { 10, 20, 30, 40 };
        Assert.Equal(expected, Recurse(array, 0, 0, 1, index, 0, 0));
    }

    // The padding arguments put index on the stack on Windows x64.
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static int Recurse(int[] array, int x, int y, int count, int index, int first, int second)
    {
        if (count == 0)
        {
            return first + second;
        }

        return Recurse(array, x, y, count - 1, index + 1, array[index], array[second]);
    }
}
