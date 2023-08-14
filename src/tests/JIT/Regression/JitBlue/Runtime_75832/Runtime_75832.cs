// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_75832
{
    [Fact]
    public static void TestEntryPoint()
        => Assert.Throws<DivideByZeroException>(() => Test(0));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Test(int i) => GetAction()(100 / i);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Action<int> GetAction() => null;
}
