// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Runtime_91839
{
    public static I0[] s_1;
    public static I2 s_5;

    [Fact]
    public static void TestEntryPoint()
    {
        S1 vr2 = new S1(new S0(0));
        if (vr2.F5)
        {
            s_5 = s_5;
        }

        S1 vr3 = vr2;
        vr3.F4 = vr3.F4;
        for (int vr4 = 0; vr4 < 1; vr4++)
        {
            var vr5 = vr3.F4;
            M2(vr5);
        }

        Assert.True(vr3.F4.F2 == 0);
    }

    private static void M2(S0 arg0)
    {
        s_1 = new I0[] { new C0() };
    }

    public interface I0
    {
    }

    public interface I2
    {
    }

    public struct S0
    {
        public ulong F0;
        public long F2;
        public short F3;
        public S0(long f2) : this()
        {
            F2 = f2;
        }
    }

    public class C0 : I0
    {
    }

    public struct S1
    {
        public S0 F4;
        public bool F5;
        public S1(S0 f4) : this()
        {
            F4 = f4;
        }
    }
}
