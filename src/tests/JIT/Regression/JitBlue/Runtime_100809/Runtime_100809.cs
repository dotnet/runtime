// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public static class Runtime_100809
{
    [Fact]
    public static int TestEntryPoint()
    {
        return AlwaysFalse(96) ? -1 : 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool AlwaysFalse(int x)
    {
        var result = new byte[x];
        int count = result.Length - 2;
        return (x < 0 || result.Length - count < 0);
    }
}
