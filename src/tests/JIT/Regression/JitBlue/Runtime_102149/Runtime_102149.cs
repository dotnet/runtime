// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public static class Runtime_102149
{
    private static ulong GetVal() => 9303915604039947368;

    [Fact]
    public static void Test()
    {
        // Case 1: non-constant input
        if (BitConverter.DoubleToUInt64Bits(U8_to_R8(GetVal())) != 4890948523238023291)
            throw new InvalidOperationException("Case 1 failed");

        // Case 2: constant folding
        if (BitConverter.DoubleToUInt64Bits((double)GetVal()) != 4890948523238023291)
            throw new InvalidOperationException("Case 2 failed");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double U8_to_R8(ulong x) => x;
}
