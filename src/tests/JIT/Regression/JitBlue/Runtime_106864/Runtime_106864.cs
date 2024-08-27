// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

public class C1
{
    public Vector<short> F1;
}

public class Runtime_106864
{
    public static C1 s_2 = new C1();

    [Fact]
    public static void TestEntryPoint()
    {
        if (Sve.IsSupported)
        {
            C1 vr2 = s_2;
            var vr3 = vr2.F1;
            var vr4 = vr2.F1;
            vr2.F1 = Sve.Max(vr3, vr4);
        }
    }
}