// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;

public struct Rational
{
    public int num;
    public int den;
}

public struct RationalPolynomial
{
    public Rational a;
    public Rational b;
}

public class BringUpTest_StructFldAddr
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int StructFldAddr(RationalPolynomial rp)
    {
        return rp.a.num + rp.b.num;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Rational a = new Rational();
        Rational b = new Rational();
        a.num = 3;
        a.den = 4;
        b.num = 2;
        b.den = 3;
        RationalPolynomial rp = new RationalPolynomial();
        rp.a = a;
        rp.b = b;
        int y = StructFldAddr(rp);
        if (y == 5) return Pass;
        else return Fail;
    }
}
