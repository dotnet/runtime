// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

[StructLayout(LayoutKind.Sequential)]
internal struct AA
{
    public long tmp1;
    public float tmp2;
    public long tmp3;

    public long q;

    public ushort tmp4;
    public long tmp5;
    public ushort tmp6;

    public AA(long qq)
    {
        tmp1 = 106;
        tmp2 = 107;
        tmp3 = 108;
        tmp4 = 109;
        tmp5 = 110;
        tmp6 = 111;
        q = qq;
    }

    public static AA[] a_init = new AA[101];
    public static AA[] a_zero = new AA[101];
    public static AA[,,] aa_init = new AA[1, 101, 2];
    public static AA[,,] aa_zero = new AA[1, 101, 2];
    public static object b_init = new AA(100);
    public static AA _init, _zero;

    public static long call_target(long arg) { return arg; }
    public static long call_target_ref(ref long arg) { return arg; }

    public void verify()
    {
        if (tmp1 != 106) throw new Exception("tmp1 corrupted");
        if (tmp2 != 107) throw new Exception("tmp2 corrupted");
        if (tmp3 != 108) throw new Exception("tmp3 corrupted");
        if (tmp4 != 109) throw new Exception("tmp4 corrupted");
        if (tmp5 != 110) throw new Exception("tmp5 corrupted");
        if (tmp6 != 111) throw new Exception("tmp6 corrupted");
    }

    public static void verify_all()
    {
        a_init[100].verify();
        a_zero[100].verify();
        aa_init[0, 99, 1].verify();
        aa_zero[0, 99, 1].verify();
        _init.verify();
        _zero.verify();
        BB.f_init.verify();
        BB.f_zero.verify();
    }

    public static void reset()
    {
        a_init[100] = new AA(100);
        a_zero[100] = new AA(0);
        aa_init[0, 99, 1] = new AA(100);
        aa_zero[0, 99, 1] = new AA(0);
        _init = new AA(100);
        _zero = new AA(0);
        BB.f_init = new AA(100);
        BB.f_zero = new AA(0);
    }

    [Fact]
    public static int TestEntrypoint()
    {
        return TestApp.RunAllTests();
    }
}

internal struct BB
{
    public static AA f_init, f_zero;
}
