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
            var vr3 = Sve.CreateTrueMaskInt32();
            var vr4 = Vector.Create<int>(1);
            var vr5 = Vector128.CreateScalar(0).AsVector();
            Vector<int> vr6 = Sve.ConditionalSelect(vr3, vr4, vr5);
            Consume(vr6);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(Vector<int> v)
    {
        ;
    }
}
