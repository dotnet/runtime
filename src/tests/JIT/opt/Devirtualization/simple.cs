// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

// Some simple interface call devirtualization cases

interface Ix
{
    int F();
}

interface Iy
{
    int G();
}

interface Iz
{
    int H();
    int I();
}

public class B : Iy, Ix, Iz
{
    public int F() { return 3; }
    virtual public int G() { return 5; }
    int Iz.H() { return 7; }
    int Iz.I() { return 11; }
}

public class Z : B, Iz
{
    new public int F() { return 13; }
    override public int G() { return 17; }
    int Iz.H() { return 19; }

    static int Fx(Ix x) { return x.F(); }
    static int Gy(Iy y) { return y.G(); }
    static int Hz(Iz z) { return z.H(); }
    static int Hi(Iz z) { return z.I(); }

    [Fact]
    public static int TestEntryPoint()
    {
        int callsBF = Fx(new Z()) + Fx(new B()) + ((Ix) new Z()).F() + ((Ix) new B()).F();
        int callsBG = Gy(new B()) + ((Iy) new B()).G() + (new B()).G();
        int callsBH = Hz(new B()) + ((Iz) new B()).H();
        int callsBI = Hi(new Z()) + Hi(new B()) + ((Iz) new Z()).I() + ((Iz) new B()).I();
        int callsZG = Gy(new Z()) + ((Iy) new Z()).G() + (new Z()).G();
        int callsZH = Hz(new Z()) + ((Iz) new Z()).H();

        int expected = 4 * 3 + 3 * 5 + 2 * 7 + 4 * 11 + 3 * 17 + 2 * 19;

        return callsBF + callsBG + callsBI + callsBH + callsZG + callsZH - expected + 100;
    }
}

