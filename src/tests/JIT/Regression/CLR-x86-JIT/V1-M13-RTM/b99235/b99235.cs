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
}

public class TestApp
{
    private static unsafe int test_26(uint ub)
    {
        return 0;
    }
    [Fact]
    public static unsafe int TestEntryPoint()
    {
        AA loc_x = new AA(0, 100);
        test_26((uint)&loc_x.m_b);
        return 100;
    }
}
