// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public struct A
{
    public long m_aval;
};

public struct B
{
    public long m_bval;
};

public struct AA
{
    public A m_a;
    public B m_b;

    public AA(long a, long b)
    {
        m_a.m_aval = a;
        m_b.m_bval = b;
    }

    public static unsafe void init_all(long mode)
    {
        s_x = new AA(0, 100);
    }

    public static AA s_x;

    public unsafe static B* get_pb(AA* px) { return &px->m_b; }
    public unsafe static B* get_pb_1(AA* px) { return &px->m_b - 1; }
    public unsafe static long get_pb_i(AA* px) { return (long)&px->m_b; }
    public unsafe static long get_bv1(B* pb) { return pb->m_bval; }
    public unsafe static long get_bv2(B b) { return b.m_bval; }
    public unsafe static long get_bv3(ref B rb) { return rb.m_bval; }
    public unsafe static long get_i1(long* pi) { return *pi; }
    public unsafe static long get_i2(long i) { return i; }
    public unsafe static long get_i3(ref long ri) { return ri; }
}
