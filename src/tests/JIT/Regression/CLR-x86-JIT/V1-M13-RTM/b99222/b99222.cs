// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public struct A
{
    public int m_aval;
};

public struct B
{
    public int m_bval;
};

public struct AA
{
    public A m_a;
    public B m_b;
    public AA(int a, int b)
    {
        m_a.m_aval = a;
        m_b.m_bval = b;
    }

    public unsafe static B* get_pb(AA* px) { return &px->m_b; }
}

public class TestApp
{
    private static unsafe int test_3_0_0(AA* px)
    {
        return AA.get_pb(px)->m_bval;
    }
    [Fact]
    public static unsafe int TestEntryPoint()
    {
        AA loc_x = new AA(0, 100);
        test_3_0_0(&loc_x);
        return 100;
    }
}
