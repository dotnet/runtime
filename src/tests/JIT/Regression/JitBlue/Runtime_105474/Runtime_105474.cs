// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

public class Runtime_105474
{
    private static Vector<double> s_3;

    [Fact]
    public static void TestEntryPoint()
    {
        if (Sve.IsSupported)
        {
            TestMethod1();
            TestMethod2(Vector<double>.Zero);
            TestMethod3(Vector<double>.Zero);
            TestMethod4(Vector<double>.Zero);
            TestMethod5(Vector<double>.Zero);
            TestMethod6(Vector<double>.Zero);
        }
    }

    [method: MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestMethod1()
    {
        // ARM64-FULL-LINE: fmad z17.d, p0/m, z17.d, z16.d
        var vr1 = Vector128.CreateScalar((double)10).AsVector();
        s_3 = Sve.FusedMultiplyAdd(vr1, s_3, s_3);
    }

    [method: MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestMethod2(Vector<double> mask)
    {
        // ARM64-FULL-LINE: fmla z16.d, p0/m, z17.d, z17.d
        var vr1 = Vector128.CreateScalar((double)10).AsVector();
        s_3 = Sve.ConditionalSelect(mask, Sve.FusedMultiplyAdd(vr1, s_3, s_3), s_3);
    }

    [method: MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestMethod3(Vector<double> mask)
    {
        // ARM64-FULL-LINE: fmad z16.d, p0/m, z16.d, z16.d
        s_3 = Sve.ConditionalSelect(mask, Sve.FusedMultiplyAdd(s_3, s_3, s_3), s_3);
    }

    [method: MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestMethod4(Vector<double> mask)
    {
        // ARM64-FULL-LINE: fmad z16.d, p0/m, z17.d, z16.d
        var vr1 = Vector128.CreateScalar((double)10).AsVector();
        s_3 = Sve.ConditionalSelect(mask, Sve.FusedMultiplyAdd(s_3, vr1, s_3), s_3);
    }

    [method: MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestMethod5(Vector<double> mask)
    {
        // ARM64-FULL-LINE: fmad z16.d, p0/m, z16.d, z17.d
        var vr1 = Vector128.CreateScalar((double)10).AsVector();
        s_3 = Sve.ConditionalSelect(mask, Sve.FusedMultiplyAdd(s_3, vr1, vr1), s_3);
    }

    [method: MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestMethod6(Vector<double> mask)
    {
        // ARM64-FULL-LINE: fmad z16.d, p0/m, z16.d, z16.d
        var vr1 = Vector128.CreateScalar((double)10).AsVector();
        s_3 = Sve.ConditionalSelect(mask, Sve.FusedMultiplyAdd(vr1, vr1, vr1), s_3);
    }
}
