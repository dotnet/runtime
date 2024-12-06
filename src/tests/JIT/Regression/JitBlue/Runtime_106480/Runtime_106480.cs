// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Found by Antigen
// Reduced from 121.36 KB to 3.38 
// Further redued by hand

using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class Runtime_106480
{
    Vector512<ushort> v512_ushort_102 = Vector512<ushort>.AllBitsSet;

    void Problem()
    {
        if (Avx512F.IsSupported)
        {
            byte byte_126 = 5;
            Avx512F.TernaryLogic(v512_ushort_102, v512_ushort_102, v512_ushort_102, byte_126);
        }
    }

    [Fact]
    public static void Test()
    {
        new Runtime_106480().Problem();
    }
}