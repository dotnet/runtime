// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_119061
{
    private static C1 s_6 = new C1();
    
    [Fact]
    public static void TestEntryPoint()
    {
        s_6.F8 = new S0(-1);
        var vr4 = s_6.F8.F0;
        Vector128<short> vr6 = default(Vector128<short>);
        M3(vr4, vr6);
    }

    private static ushort M3(int arg1, Vector128<short> arg2)
    {
        bool[] var0 = new bool[]
        {
            false
        };
        for (sbyte lvar1 = 10; lvar1 < 12; lvar1++)
        {
            if (var0[0])
            {
                var vr2 = (uint)(3329109910U / (-9223372036854775808L / (arg1 | 1)));
                var vr1 = (short)BitOperations.LeadingZeroCount(vr2);
                arg2 = Vector128.CreateScalar(vr1);
            }
        }

        C0 vr8 = new C0();
        return vr8.F1;
    }

    private struct S0
    {
        public int F0;
        public S0(int f0) : this()
        {
            F0 = f0;
        }
    }

    private class C0
    {
        public ushort F1;
    }

    private class C1
    {
        public S0 F8;
    }
}
