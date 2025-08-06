// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class Async2Small
{
    [Fact]
    public static void TestEntryPoint()
    {
        SmallType(123).Wait();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task SmallType(byte arg)
    {
        await Task.Yield();
        Assert.Equal(123, arg);
    }
}
