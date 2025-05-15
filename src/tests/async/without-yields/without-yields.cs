// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Xunit;

public class Async2FibonacceWithoutYields
{
    //This async method lacks 'await'
#pragma warning disable 1998

    internal static async2 Task<int> B(int n)
    {
        return 100;
    }

    internal static async2 Task<int> A(int n)
    {
        int num = n;
        for (int num2 = 0; num2 < n; num2++)
        {
            num = await B(num);
        }

        return num;
    }

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
