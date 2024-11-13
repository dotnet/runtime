// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public static class Runtime_102149
{
    [Fact]
    public static void Test()
    {
        if (BitConverter.DoubleToUInt64Bits(U8_to_R8(Const(9303915604039947368UL))) != 4890948523238023291UL)
            throw new InvalidOperationException("Case 1 failed");

        if (BitConverter.DoubleToUInt64Bits((double)Const(9303915604039947368UL)) != 4890948523238023291UL)
            throw new InvalidOperationException("Case 2 failed");

        if (NonConst(10648738977740919977d) != NonConst(10648738977740919977UL))
            throw new InvalidOperationException("Case 3 failed");

        if (Const(10648738977740919977d) != Const(10648738977740919977UL))
            throw new InvalidOperationException("Case 4 failed");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T Const<T>(T val) => val;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T NonConst<T>(T val) => val;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double U8_to_R8(ulong x) => x;
}
