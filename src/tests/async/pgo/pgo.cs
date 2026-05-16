// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class Async2Pgo
{
    [Fact]
    public static void Interface()
    {
        int[] arr = Enumerable.Range(0, TestLibrary.Utilities.IsCoreClrInterpreter ? 100 : 2_500).ToArray();

        int jCount = TestLibrary.Utilities.IsCoreClrInterpreter ? 10 : 100;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < jCount; j++)
                AggregateInterfaceAsync(arr, new AggregateSum(), 0).GetAwaiter().GetResult();

            Thread.Sleep(100);
        }
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
    private static async Task<T> AggregateInterfaceAsync<T>(T[] arr, I<T> aggregate, T seed)
    {
        foreach (T val in arr)
            seed = await aggregate.Aggregate(seed, val);

        return seed;
    }

    [Fact]
    public static void IntDelegate()
    {
        Func<int, int, Task<int>> del = async (cur, val) => cur + val * val;

        int[] arr = [.. Enumerable.Range(0, TestLibrary.Utilities.IsCoreClrInterpreter ? 100 : 2_500)];

        int jCount = TestLibrary.Utilities.IsCoreClrInterpreter ? 10 : 100;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < jCount; j++)
                AggregateIntDelegateAsync(arr, del, 0).GetAwaiter().GetResult();

            Thread.Sleep(100);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<T> AggregateIntDelegateAsync<T>(T[] arr, Func<T, T, Task<T>> aggregate, T seed)
    {
        foreach (T val in arr)
            seed = await aggregate(seed, val);

        return seed;
    }

    [Fact]
    public static void RetBufStructDelegate()
    {
        Func<RetBufStruct, int, Task<RetBufStruct>> del = async (cur, val) => new RetBufStruct { A = cur.A + val * val };

        int[] arr = Enumerable.Range(0, TestLibrary.Utilities.IsCoreClrInterpreter ? 100 : 2_500).ToArray();

        int jCount = TestLibrary.Utilities.IsCoreClrInterpreter ? 10 : 100;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < jCount; j++)
                AggregateRetBufStructDelegateAsync(arr, del, new RetBufStruct()).GetAwaiter().GetResult();

            Thread.Sleep(100);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<TVal> AggregateRetBufStructDelegateAsync<TElem, TVal>(TElem[] arr, Func<TVal, TElem, Task<TVal>> aggregate, TVal seed)
    {
        foreach (TElem val in arr)
            seed = await aggregate(seed, val);

        return seed;
    }

    private struct RetBufStruct
    {
        public long A, B, C, D, E, F;
    }

    [Fact]
    public static void MultiRegStructDelegate()
    {
        Func<MultiRegStruct, int, Task<MultiRegStruct>> del = async (cur, val) => new MultiRegStruct { A = cur.A + val * val };

        int[] arr = Enumerable.Range(0, TestLibrary.Utilities.IsCoreClrInterpreter ? 100 : 2_500).ToArray();

        int jCount = TestLibrary.Utilities.IsCoreClrInterpreter ? 10 : 100;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < jCount; j++)
                AggregateMultiRegStructDelegateAsync(arr, del, new MultiRegStruct()).GetAwaiter().GetResult();

            Thread.Sleep(100);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<TVal> AggregateMultiRegStructDelegateAsync<TElem, TVal>(TElem[] arr, Func<TVal, TElem, Task<TVal>> aggregate, TVal seed)
    {
        foreach (TElem val in arr)
            seed = await aggregate(seed, val);

        return seed;
    }

    private struct MultiRegStruct
    {
        public long A, B;
    }

    [Fact]
    public static void VoidDelegate()
    {
        int sum = 0;
        Func<int, Task> del = async val => sum += val * val;

        int[] arr = Enumerable.Range(0, TestLibrary.Utilities.IsCoreClrInterpreter ? 100 : 2_500).ToArray();

        int jCount = TestLibrary.Utilities.IsCoreClrInterpreter ? 10 : 100;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < jCount; j++)
                AggregateVoidDelegateAsync(arr, del).GetAwaiter().GetResult();

            Thread.Sleep(100);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task AggregateVoidDelegateAsync<T>(T[] arr, Func<T, Task> aggregate)
    {
        foreach (T val in arr)
            await aggregate(val);
    }
}
