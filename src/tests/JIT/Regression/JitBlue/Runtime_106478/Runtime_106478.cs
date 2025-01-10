// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Found by Antigen
// Reduced from 30.05 KB to 2.35 KB.
// Further redued by hand

using System;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_106478
{
    static Vector512<double> s_v512_double_46 = Vector512.Create(4, -4.971830985915493, -0.9789473684210527, -1.956043956043956, 2, 74.25, 1.0533333333333332, 4.033898305084746);
    Vector512<int> v512_int_101 = Vector512<int>.Zero;
    Vector512<int> p_v512_int_125 = Vector512<int>.Zero;

    void Problem()
    {
        p_v512_int_125 = v512_int_101& Vector512.AsInt32(s_v512_double_46 ^ Vector512<double>.AllBitsSet);
    }

    [Fact]
    public static void Test()
    {
        new Runtime_106478().Problem();
    }
}
