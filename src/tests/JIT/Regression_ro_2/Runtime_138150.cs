// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_138150;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public static class Runtime_138150
{
    [Fact]
    public static void TestEntryPoint()
    {
        Vector128<sbyte> value = Vector128.Create((sbyte)1);

        Assert.Equal(value, AndNotOperator(value));
        Assert.Equal(value, AndNotIntrinsic(value));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static Vector128<sbyte> AndNotOperator(Vector128<sbyte> x) => x + (Vector128<sbyte>.Zero & ~Vector128<sbyte>.Zero);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static Vector128<sbyte> AndNotIntrinsic(Vector128<sbyte> x) => x + Vector128.AndNot(Vector128<sbyte>.Zero, Vector128<sbyte>.Zero);
}
