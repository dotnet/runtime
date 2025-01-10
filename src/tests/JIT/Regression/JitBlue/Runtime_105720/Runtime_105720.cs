// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System;
using System.Runtime.CompilerServices;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

public class Runtime_105720
{
    [Fact]
    public static void TestEntryPoint()
    {
        if (Sve.IsSupported)
        {
            var vr20 = Vector128.CreateScalar(-205721710953328329L).AsVector();
            var vr18 = (short)0;
            var vr19 = Vector128.CreateScalar(vr18).AsVector();
            var vr21 = Vector.Create<long>(0);
            var vr24 = Vector.Create<short>(0);
            var vr25 = Sve.CreateBreakPropagateMask(vr19, vr24);
            var vr26 = Sve.AddAcross(vr25);
            var vr27 = Sve.ConditionalSelect(vr20, vr26, vr21);
            Consume(vr27);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(Vector<long> v)
    {
        ;
    }
}