// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Xunit;

public class Async2ObjectsWithYields
{
    internal static async Task<int> A(object n)
    {
        // use string equality so that JIT would not think of hoisting "(int)n"
        // also to produce some amout of garbage
        if (n.ToString() != 0.ToString())
        {
            return await A((int)n - 1) + (int)n;
        }

        await Task.Yield();
        return 0;
    }

    [RuntimeAsyncMethodGeneration(false)]
    private static async Task<int> AsyncEntry()
    {
        object result = 0;
        for (int i = 0; i < 20; i++)
        {
            var tsk = A(i);
            await Task.Yield();
            GC.Collect();
            result = await tsk;
        }

        // the result should be 20 * (20 - 1) => 190
        return (int)result - 90;
    }

    [Fact]
    public static int Test()
    {
        return (int)AsyncEntry().Result;
    }
}
