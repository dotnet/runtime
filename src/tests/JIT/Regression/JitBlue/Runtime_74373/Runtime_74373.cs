// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class _74373
{
    [Fact]
    public static int TestEntryPoint()
    {
        Problem(10);
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe float Problem(long x)
    {
        var y = BitConverter.Int32BitsToSingle((int)x);
        Use(&x);
        JitUse(0);
        return y;
    }

    internal static unsafe void Use(long* arg) { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void JitUse<T>(T arg) { }
}
