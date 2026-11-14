// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class StrengthReductionTest
{
    [Fact]
    public static async Task<int> TestEntryPoint()
    {
        return (await StrengthReduction(Enumerable.Range(0, 1000).ToArray())) - 499400;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<int> StrengthReduction(int[] arr)
    {
        int sum = 0;
        foreach (int x in arr)
        {
            sum += x;
            await Task.Yield();
        }
        return sum;
    }
}
