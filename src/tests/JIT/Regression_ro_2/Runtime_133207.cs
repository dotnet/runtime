// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133207
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Same(typeof(bool), GetNullableType(true));
        Assert.Throws<NullReferenceException>(() => GetNullableType(null));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Type GetNullableType(bool? value) => value.GetType();
}
