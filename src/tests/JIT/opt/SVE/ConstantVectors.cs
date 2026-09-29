// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

public class ConstanVectors
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    [Fact]
    public static int TestConstanVectors()
    {
        bool fail = false;

        if (Sve.IsSupported)
        {
            var r1 = SaturatingDecrementByActiveElementCountConst();
            Console.WriteLine(r1);
            if (r1 != 18446744073709551615)
            {
                fail = true;
            }
        }

        if (fail)
        {
            return 101;
        }
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ulong SaturatingDecrementByActiveElementCountConst()
    {
        var vr5 = Vector128.CreateScalar(14610804860246336108UL).AsVector();
        return Sve.SaturatingDecrementByActiveElementCount(0UL, vr5);
    }
}
