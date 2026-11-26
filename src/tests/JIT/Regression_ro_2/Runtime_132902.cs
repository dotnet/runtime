// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

namespace Runtime_132902;

public class Runtime_132902
{
    public enum Classification
    {
        IsFinite,
        IsInfinity,
        IsInteger,
        IsNaN,
        IsNegative,
        IsNegativeInfinity,
        IsPositive,
        IsPositiveInfinity,
        IsSubnormal,
    }

    [Theory]
    [InlineData(Classification.IsFinite)]
    [InlineData(Classification.IsInfinity)]
    [InlineData(Classification.IsInteger)]
    [InlineData(Classification.IsNaN)]
    [InlineData(Classification.IsNegative)]
    [InlineData(Classification.IsNegativeInfinity)]
    [InlineData(Classification.IsPositive)]
    [InlineData(Classification.IsPositiveInfinity)]
    [InlineData(Classification.IsSubnormal)]
    public static void TestEntryPoint(Classification classification)
    {
        Action action = classification switch
        {
            Classification.IsFinite => () => IsFinite(Vector128<int>.Count),
            Classification.IsInfinity => () => IsInfinity(Vector128<int>.Count),
            Classification.IsInteger => () => IsInteger(Vector128<int>.Count),
            Classification.IsNaN => () => IsNaN(Vector128<int>.Count),
            Classification.IsNegative => () => IsNegative(Vector128<uint>.Count),
            Classification.IsNegativeInfinity => () => IsNegativeInfinity(Vector128<int>.Count),
            Classification.IsPositive => () => IsPositive(Vector128<uint>.Count),
            Classification.IsPositiveInfinity => () => IsPositiveInfinity(Vector128<int>.Count),
            Classification.IsSubnormal => () => IsSubnormal(Vector128<int>.Count),
            _ => throw new ArgumentOutOfRangeException(nameof(classification)),
        };

        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<int> IsFinite(int index) =>
        Vector128.IsFinite(Vector128.WithElement(Vector128<int>.Zero, index, 0));

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<int> IsInfinity(int index) =>
        Vector128.IsInfinity(Vector128.WithElement(Vector128<int>.Zero, index, 0));

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<int> IsInteger(int index) =>
        Vector128.IsInteger(Vector128.WithElement(Vector128<int>.Zero, index, 0));

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<int> IsNaN(int index) =>
        Vector128.IsNaN(Vector128.WithElement(Vector128<int>.Zero, index, 0));

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<uint> IsNegative(int index) =>
        Vector128.IsNegative(Vector128.WithElement(Vector128<uint>.Zero, index, 0u));

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<int> IsNegativeInfinity(int index) =>
        Vector128.IsNegativeInfinity(Vector128.WithElement(Vector128<int>.Zero, index, 0));

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<uint> IsPositive(int index) =>
        Vector128.IsPositive(Vector128.WithElement(Vector128<uint>.Zero, index, 0u));

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<int> IsPositiveInfinity(int index) =>
        Vector128.IsPositiveInfinity(Vector128.WithElement(Vector128<int>.Zero, index, 0));

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<int> IsSubnormal(int index) =>
        Vector128.IsSubnormal(Vector128.WithElement(Vector128<int>.Zero, index, 0));
}
