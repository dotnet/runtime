// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class Async2Void
{
    [Fact]
    public static void TestEntryPoint()
    {
        int[] arr = new int[1];
        AsyncTestEntryPoint(arr, 0).Wait();
        Assert.Equal(199_990_000, arr[0]);
    }

    private static async Task AsyncTestEntryPoint(int[] arr, int index)
    {
        await HoistedByref(arr, index);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async2 Task HoistedByref(int[] arr, int index)
    {
        for (int i = 0; i < 20000; i++)
        {
            arr[index] += i;
            await Task.Yield();
        }
    }
}
