// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// impArrayAccessIntrinsic used to treat a sealed element type as proof that an MD-array
// Set/Address needs no covariance check. Array and variant types are sealed yet still
// covariant, so the check is required for them.
public class Runtime_133975
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void SetJagged(object[,][] a, object[] v) => a[0, 0] = v;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static ref object[] AddressJagged(object[,][] a) => ref a[0, 0];

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void SetVariant(Func<object>[,] a, Func<object> v) => a[0, 0] = v;

    [Fact]
    public static void TestEntryPoint()
    {
        string[,][] jagged = new string[1, 1][];
        Assert.Throws<ArrayTypeMismatchException>(() => SetJagged(jagged, new object[] { 42 }));
        Assert.Throws<ArrayTypeMismatchException>(() => AddressJagged(jagged) = new object[] { 42 });
        Assert.Null(jagged[0, 0]);

        Func<string>[,] variant = new Func<string>[1, 1];
        Assert.Throws<ArrayTypeMismatchException>(() => SetVariant(variant, () => new object()));
        Assert.Null(variant[0, 0]);

        // The intrinsic expansion must still be correct for a genuinely exact element type.
        string[,] exact = new string[1, 1];
        SetExact(exact, "ok");
        Assert.Equal("ok", exact[0, 0]);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void SetExact(string[,] a, string v) => a[0, 0] = v;
}
