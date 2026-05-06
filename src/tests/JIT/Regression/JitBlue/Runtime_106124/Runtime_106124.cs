// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Runtime_106124;

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

public class Runtime_106124
{
    [ConditionalFact(typeof(Sve), nameof(Sve.IsSupported))]
    public static void TestEntryPoint()
    {
        var vr19 = Vector128.CreateScalar(0L).AsVector();
        var vr25 = Sve.CreateBreakPropagateMask(vr19, vr19);
        Consume(vr25);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(Vector<long> v)
    {
    ;
    }

}
