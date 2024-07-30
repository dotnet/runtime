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
    public static void TestMethod1()
    {
        if (Sve.IsSupported)
        {
            var vr1 = Vector128.CreateScalar((double)10).AsVector();
            s_3 = Sve.FusedMultiplyAdd(vr1, s_3, s_3);
        }
    }

    [Fact]
    public static void MoreTestMethods()
    {
        if (Sve.IsSupported)
        {
            TestMethod2(Vector<double>.Zero);
            TestMethod3(Vector<double>.Zero);
            TestMethod4(Vector<double>.Zero);
            TestMethod5(Vector<double>.Zero);
            TestMethod6(Vector<double>.Zero);
        }
    }

    [method: MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestMethod2(Vector<double> mask)
    {
        var vr1 = Vector128.CreateScalar((double)10).AsVector();
        s_3 = Sve.ConditionalSelect(mask, Sve.FusedMultiplyAdd(vr1, s_3, s_3), s_3);
    }

    [method: MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestMethod3(Vector<double> mask)
    {
        s_3 = Sve.ConditionalSelect(mask, Sve.FusedMultiplyAdd(s_3, s_3, s_3), s_3);
    }

    [method: MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestMethod4(Vector<double> mask)
    {
        var vr1 = Vector128.CreateScalar((double)10).AsVector();
        s_3 = Sve.ConditionalSelect(mask, Sve.FusedMultiplyAdd(s_3, vr1, s_3), s_3);
    }

    [method: MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestMethod5(Vector<double> mask)
    {
        var vr1 = Vector128.CreateScalar((double)10).AsVector();
        s_3 = Sve.ConditionalSelect(mask, Sve.FusedMultiplyAdd(s_3, vr1, vr1), s_3);
    }

    [method: MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestMethod6(Vector<double> mask)
    {
        var vr1 = Vector128.CreateScalar((double)10).AsVector();
        s_3 = Sve.ConditionalSelect(mask, Sve.FusedMultiplyAdd(vr1, vr1, vr1), s_3);
    }
}
