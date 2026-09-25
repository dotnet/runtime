// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133862
{
    private struct S
    {
        public int A;
        public int B;
        public int C;
        public int D;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Index(long value) => checked((int)value);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static S Load(S[] values) => values[Index(long.MaxValue)];

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Throws<OverflowException>(() => Load(new S[4]));
    }
}
