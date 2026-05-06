// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// This test is to ensure that an assertion does not occur in the JIT.
public class Runtime_78891
{
    class C0
    {
        public long F1;
    }

    struct S5
    {
        public bool F1;
        public int F2;
        public C0 F4;
        public short F5;
        public ulong F6;
        public uint F7;
    }

    static S5 s_48;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Consume(short x)
    {

    }

    static void M59(S5 arg0, S5 arg1)
    {
        try
        {
            arg0 = arg1;
            arg0 = arg1;
            short var3 = arg1.F5;
            Consume(var3);
        }
        finally
        {
            if (s_48.F4.F1 > arg0.F7)
            {
                arg0.F1 |= false;
            }
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        var vr2 = new S5();
        var vr3 = new S5();
        Assert.Throws<NullReferenceException>(() => M59(vr2, vr3));
    }
}
