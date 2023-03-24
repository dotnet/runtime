// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public static class FloatingPointHelper<TSelf>
    where TSelf : IFloatingPoint<TSelf>
{
    public static int GetExponentShortestBitLength(TSelf value)
        => value.GetExponentShortestBitLength();
}

public class Runtime_81460
{
    [Fact]
    public static int TestEntryPoint()
    {
        return (Test() == 0) ? 100 : 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Test() =>
        FloatingPointHelper<double>.GetExponentShortestBitLength(1.0);
}
