// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_130830;

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public static class Runtime_130830
{
    [Fact]
    public static void TestEntryPoint()
    {
        Vector128<int> result = Test(Vector128.Create(10), Vector128.Create(100));
        Assert.Equal(Vector128.Create(90, 91, 92, 93), result);
    }

    // The low lane of the constant is zero but the constant is not all-zero, so the
    // subtract must not be treated as a negate; otherwise the constant is dropped and
    // the result collapses to <90, 90, 90, 90>.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<int> Test(Vector128<int> v1, Vector128<int> v2)
    {
        return (Vector128.Create(0, 1, 2, 3) - v1) + v2;
    }
}
