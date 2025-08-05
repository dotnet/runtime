// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_106838
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<ulong> Problem(Vector128<ulong> vector) => vector * 5UL;

    [Fact]
    public static void TestEntryPoint()
    {
        Vector128<ulong> result = Problem(Vector128.Create<ulong>(5));
        Assert.Equal(Vector128.Create<ulong>(25), result);
    }
}
