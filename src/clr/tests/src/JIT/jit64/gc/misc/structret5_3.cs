// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;

struct Pad
{
#pragma warning disable 0414
    public double d1;
    public double d2;
    public double d3;
    public double d4;
    public double d5;
    public double d6;
    public double d7;
    public double d8;
    public double d9;
    public double d10;
    public double d11;
    public double d12;
    public double d13;
    public double d14;
    public double d15;
    public double d16;
    public double d17;
    public double d18;
    public double d19;
    public double d20;
    public double d21;
    public double d22;
    public double d23;
    public double d24;
    public double d25;
    public double d26;
    public double d27;
    public double d28;
    public double d29;
    public double d30;
#pragma warning restore 0414
}

struct S
{
    public String str;
    public Pad pad;
#pragma warning disable 0414
    public String str2;
#pragma warning restore 0414

    public S(String s)
    {
        str = s;
        str2 = s + str;
        pad.d1 =
        pad.d2 =
        pad.d3 =
        pad.d4 =
        pad.d5 =
        pad.d6 =
        pad.d7 =
        pad.d8 =
        pad.d9 =
        pad.d10 =
        pad.d11 =
        pad.d12 =
        pad.d13 =
        pad.d14 =
        pad.d15 =
        pad.d16 =
        pad.d17 =
        pad.d18 =
        pad.d19 =
        pad.d20 =
        pad.d21 =
        pad.d22 =
        pad.d23 =
        pad.d24 =
        pad.d25 =
        pad.d26 =
        pad.d27 =
        pad.d28 =
        pad.d29 =
        pad.d30 = 3.3;
    }
}

class Test
{
    public static S c(S s1, S s2, S s3, S s4)
    {
        S r;
        r = s1;
        r.str = s1.str + s2.str + s3.str + s4.str;
        return r;
    }

    public static int Main()
    {
        S sM = new S("test");
        S sM2 = new S("test2");
        S sM3 = new S("test3");
        S sM4 = new S("test4");

        Console.WriteLine(c(sM, sM2, sM3, sM4));
        return 100;
    }
}
