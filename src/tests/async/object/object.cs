// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class Async2Object
{
    [Fact]
    public static int TestEntryPoint()
    {
        return (int)AsyncTestEntryPoint(100).Result;
    }

    [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(false)]
    private static async Task<object> AsyncTestEntryPoint(int arg)
    {
        return await ObjMethod(arg);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<object> ObjMethod(int arg)
    {
        await Task.Yield();
        return arg;
    }
}
