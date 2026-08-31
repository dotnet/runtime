// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Xunit;

public class Async2FibonacceWithYields
{
    internal static async Task<int> B(int n)
    {
        int num = 1;
        await Task.Yield();

        num *= 10;
        await Task.Yield();

        num *= 10;
        await Task.Yield();

        return num;
    }

    internal static async Task<int> A(int n)
    {
        int num = n;
        for (int num2 = 0; num2 < n; num2++)
        {
            num = await B(num);
        }

        return num;
    }

    [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(false)]
    private static async Task<int> AsyncEntry()
    {
        int result = 0;
        for (int i = 0; i < 10; i++)
        {
            result = await A(100);
        }

        return result;
    }

    [Fact]
    public static int Test()
    {
        return AsyncEntry().Result;
    }
}
