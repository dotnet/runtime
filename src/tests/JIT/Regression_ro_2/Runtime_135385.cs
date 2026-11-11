// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class Runtime_135385
{
    private static long s_val;

    [Fact]
    public static void TestEntryPoint()
    {
        s_val = -1;
        Assert.Equal(4294967295UL, Test(ref s_val));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static ulong Test(ref long loc) => (uint)(Interlocked.Add(ref loc, 5) - 5);
}
