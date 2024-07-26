// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_105518
{
    [Fact]
    public static void TestEntryPoint()
    {
        Problem<decimal?, decimal>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T GetValue<T>() => (T)(object)100M;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static TTo Problem<TTo, TFrom>() => (TTo)(object)GetValue<TFrom>();
}
