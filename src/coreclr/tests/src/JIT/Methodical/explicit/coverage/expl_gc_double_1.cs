// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit)]
internal class AA
{
    [FieldOffset(9)]
    public ulong tmp1;
    [FieldOffset(1)]
    public sbyte tmp2;
    [FieldOffset(13)]
    public ushort tmp3;

    [FieldOffset(8)]
    public double q;

    [FieldOffset(48)]
    public int tmp4;
    [FieldOffset(41)]
    public float tmp5;

    public AA(double qq)
    {
        tmp1 = 0;
        tmp2 = 0;
        tmp3 = 0;
        tmp4 = 0;
        tmp5 = 0;
        q = qq;
    }

    public static AA[] a_init = new AA[101];
    public static AA[] a_zero = new AA[101];
    public static AA[,,] aa_init = new AA[1, 101, 2];
    public static AA[,,] aa_zero = new AA[1, 101, 2];
    public static object b_init = new AA(100);
    public static AA _init, _zero;

    public static double call_target(double arg) { return arg; }
    public static double call_target_ref(ref double arg) { return arg; }

    public void verify()
    {
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
}

internal struct BB
{
    public static AA f_init, f_zero;
}
