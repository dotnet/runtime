// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class Runtime_137607
{
    private static int s_count;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task F()
    {
        await Task.Yield();
        s_count++;
    }

    // More than 256 suspension points place resume info entries beyond the first
    // 4 KB of the method's data, which needs the full page offset to address on ARM64.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task Test()
    {
        await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F();
        await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F();
        await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F();
        await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F();
        await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F();
        await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F();
        await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F();
        await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F();
        await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F();
        await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F();
        await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F();
        await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F();
        await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F(); await F();
    }

    [Fact]
    public static async Task TestEntryPoint()
    {
        s_count = 0;
        await Test();
        Assert.Equal(257, s_count);
    }
}
