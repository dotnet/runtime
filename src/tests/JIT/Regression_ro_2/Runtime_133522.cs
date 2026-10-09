// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_133522
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(Vector128.Create(0xC03FFFFFu), ComplementAddOne(Vector128.Create(1.0f)).AsUInt32());
        Assert.Equal(Vector128.Create(0xC007FFFFFFFFFFFFul), ComplementAddOne(Vector128.Create(1.0)).AsUInt64());
        Assert.Equal(Vector128.Create(-1), ComplementAddOne(Vector128.Create(1)));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<T> ComplementAddOne<T>(Vector128<T> value) where T : unmanaged
    {
        return ~value + Vector128<T>.One;
    }
}
