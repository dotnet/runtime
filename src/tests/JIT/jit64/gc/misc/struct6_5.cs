// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

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
#pragma warning disable 0414
    public Pad pad;
    public Pad pad2;
    public Pad pad3;
    public Pad pad4;
    public Pad pad5;
    public String str2;
#pragma warning restore 0414

    Pad initPad()
    {
        Pad p;

        p.d1 =
        p.d2 =
        p.d3 =
        p.d4 =
        p.d5 =
        p.d6 =
        p.d7 =
        p.d8 =
        p.d9 =
        p.d10 =
        p.d11 =
        p.d12 =
        p.d13 =
        p.d14 =
        p.d15 =
        p.d16 =
        p.d17 =
        p.d18 =
        p.d19 =
        p.d20 =
        p.d21 =
        p.d22 =
        p.d23 =
        p.d24 =
        p.d25 =
        p.d26 =
        p.d27 =
        p.d28 =
        p.d29 =
        p.d30 = 3.3;

        return p;
    }

    public S(String s)
    {
        str = s;
        str2 = s + str;

        //pad = initPad();
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

        pad5 = pad4 = pad3 = pad2 = pad;
    }
}

public class Test_struct6_5
{
    private static void c(S s1, S s2, S s3, S s4, S s5)
    {
        Console.WriteLine(s1.str + s2.str + s3.str + s4.str + s5.str);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        S sM = new S("test");
        S sM2 = new S("test2");
        S sM3 = new S("test3");
        S sM4 = new S("test4");
        S sM5 = new S("test5");

        c(sM, sM2, sM3, sM4, sM5);
        return 100;
    }
}
