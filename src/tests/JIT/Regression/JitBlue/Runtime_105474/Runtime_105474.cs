// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

public class Program
{
    public static Vector<double> s_3;

    [Fact]
    public static void TestMethod()
    {
        if (Sve.IsSupported)
        {
            var vr1 = Vector128.CreateScalar((double)10).AsVector();
            s_3 = Sve.FusedMultiplyAdd(vr1, s_3, s_3);
        }
    }
}
