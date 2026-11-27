// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

namespace Runtime_137897;

public class Runtime_137897
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static ulong Test(ref int location, short comparand)
    {
        return (uint)Interlocked.CompareExchange(ref location, 42, comparand);
    }

    [Fact]
    public static void TestEntryPoint()
    {
        int location = -1;
        Assert.Equal(4294967295UL, Test(ref location, -1));
        Assert.Equal(42, location);
    }
}
