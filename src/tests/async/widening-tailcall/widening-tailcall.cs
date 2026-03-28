// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class Async2WideningTailcall
{
    [Fact]
    public static void TestEntryPoint()
    {
        uint vr0 = (uint)M29().GetAwaiter().GetResult();
        Assert.Equal(uint.MaxValue, vr0);
    }

    private static async Task<short> M29()
    {
        return await M40();
    }

    private static sbyte s_38 = -1;
    private static async Task<sbyte> M40()
    {
        await Task.Yield();
        return s_38;
    }
}
