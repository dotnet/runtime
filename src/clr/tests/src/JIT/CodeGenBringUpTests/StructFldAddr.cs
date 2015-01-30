// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Runtime.CompilerServices;

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

public class BringUpTest
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int StructFldAddr(RationalPolynomial rp)
    {
        return rp.a.num + rp.b.num;
    }

    public static int Main()
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
