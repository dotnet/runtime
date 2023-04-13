// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

struct Struct_32bytes
{
    int m_i0;
    int m_i1;
    int m_i2;
    int m_i3;
    int m_i4;
    int m_i5;
    int m_i6;
    int m_i7;

    public int Sum()
    {
        return m_i0 + m_i1 + m_i2 + m_i3 +
            m_i4 + m_i5 + m_i6 + m_i7;
    }
}

struct Struct_256bytes
{
    Struct_32bytes m_i0;
    Struct_32bytes m_i1;
    Struct_32bytes m_i2;
    Struct_32bytes m_i3;
    Struct_32bytes m_i4;
    Struct_32bytes m_i5;
    Struct_32bytes m_i6;
    Struct_32bytes m_i7;

    public int Sum()
    {
        return m_i0.Sum() + m_i1.Sum() + m_i2.Sum() + m_i3.Sum() +
            m_i4.Sum() + m_i5.Sum() + m_i6.Sum() + m_i7.Sum();
    }
}

struct Struct_2Kbytes
{
    Struct_256bytes m_i0;
    Struct_256bytes m_i1;
    Struct_256bytes m_i2;
    Struct_256bytes m_i3;
    Struct_256bytes m_i4;
    Struct_256bytes m_i5;
    Struct_256bytes m_i6;
    Struct_256bytes m_i7;

    public int Sum()
    {
        return m_i0.Sum() + m_i1.Sum() + m_i2.Sum() + m_i3.Sum() +
            m_i4.Sum() + m_i5.Sum() + m_i6.Sum() + m_i7.Sum();
    }
}

struct Struct_16Kbytes
{
    Struct_2Kbytes m_i0;
    Struct_2Kbytes m_i1;
    Struct_2Kbytes m_i2;
    Struct_2Kbytes m_i3;
    Struct_2Kbytes m_i4;
    Struct_2Kbytes m_i5;
    Struct_2Kbytes m_i6;
    Struct_2Kbytes m_i7;

    public int Sum()
    {
        return m_i0.Sum() + m_i1.Sum() + m_i2.Sum() + m_i3.Sum() +
            m_i4.Sum() + m_i5.Sum() + m_i6.Sum() + m_i7.Sum();
    }

}

struct Struct_128Kbytes
{
    Struct_16Kbytes m_i0;
    Struct_16Kbytes m_i1;
    Struct_16Kbytes m_i2;
    Struct_16Kbytes m_i3;
    Struct_16Kbytes m_i4;
    Struct_16Kbytes m_i5;
    Struct_16Kbytes m_i6;
    Struct_16Kbytes m_i7;

    public int Sum()
    {
        return m_i0.Sum() + m_i1.Sum() + m_i2.Sum() + m_i3.Sum() +
            m_i4.Sum() + m_i5.Sum() + m_i6.Sum() + m_i7.Sum();
    }

}

public class bug178119
{
    public static int foo1()
    {
        Struct_128Kbytes s0 = new Struct_128Kbytes();
        Struct_128Kbytes s1 = new Struct_128Kbytes();
        Struct_128Kbytes s2 = new Struct_128Kbytes();
        Struct_128Kbytes s3 = new Struct_128Kbytes();
        Struct_128Kbytes s4 = new Struct_128Kbytes();
        Struct_128Kbytes s5 = new Struct_128Kbytes();

        int result = s0.Sum() + s1.Sum() + s2.Sum() + s3.Sum() +
            s4.Sum() + s5.Sum();

        GC.Collect();
        return result;
    }

    public static int foo2()
    {
        Struct_128Kbytes s0 = new Struct_128Kbytes();
        Struct_128Kbytes s1 = new Struct_128Kbytes();
        Struct_128Kbytes s2 = new Struct_128Kbytes();
        Struct_128Kbytes s3 = new Struct_128Kbytes();
        Struct_128Kbytes s4 = new Struct_128Kbytes();
        Struct_128Kbytes s5 = new Struct_128Kbytes();
        Struct_128Kbytes s6 = new Struct_128Kbytes();
        Struct_128Kbytes s7 = new Struct_128Kbytes();

        int result = s0.Sum() + s1.Sum() + s2.Sum() + s3.Sum() +
            s4.Sum() + s5.Sum() + s6.Sum() + s7.Sum();

        GC.Collect();
        return result;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Console.WriteLine(foo1());
        return 100;
    }
}
