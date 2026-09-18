// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_126060;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

// gtFoldExprHWIntrinsic didn't account for a range check attached to WithElement
public class Runtime_126060
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WithElementOutOfRange());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector256<double> WithElementOutOfRange()
    {
        return Vector256.WithElement(Vector256<double>.AllBitsSet, 4, 0.0);
    }
}
