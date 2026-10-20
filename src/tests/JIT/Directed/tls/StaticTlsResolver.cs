// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public static class StaticTlsResolver
{
    [ThreadStatic]
    private static int t_value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int AccessThreadStatic()
    {
        // ARM64-LINUX: mrs {{x[0-9]+}}, tpidr_el0

        t_value = 100;
        return t_value;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        return AccessThreadStatic();
    }
}
