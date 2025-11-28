// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class Async2Pgo
{
    [Fact]
    public static void EntryPoint()
    {
        AsyncEntryPoint().Wait();
    }

    internal static async Task<int> AsyncEntryPoint()
    {
        int[] arr = Enumerable.Range(0, 100_000).ToArray();

        int sum = 0;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 100; j++)
                sum += await AggregateDelegateAsync(arr, new AggregateSum(), 0);

            await Task.Delay(100);
        }

        return sum;
    }

    private class AggregateSum : I<int>
    {
        public async Task<int> Aggregate(int a, int b) => a + b;
    }

    public interface I<T>
    {
        public Task<T> Aggregate(T seed, T val);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<T> AggregateDelegateAsync<T>(T[] arr, I<T> aggregate, T seed)
    {
        foreach (T val in arr)
            seed = await aggregate.Aggregate(seed, val);

        return seed;
    }
}
