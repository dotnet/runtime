// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System;
using System.Runtime.CompilerServices;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

public class Runtime_105723
{
    [Fact]
    public static void TestEntryPoint()
    {
        if (Sve.IsSupported)
        {
            Vector<long> vr2 = Vector128.CreateScalar(-2L).AsVector();
            var vr3 = new sbyte[]
            {
                1
            };
            var vr4 = Vector128.CreateScalar(9223372036854775806L).AsVector();
            Vector<long> vr5 = Sve.MultiplySubtract(vr2, vr2, vr4);
            Consume(vr5);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(Vector<long> v)
    {
        ;
    }
}
