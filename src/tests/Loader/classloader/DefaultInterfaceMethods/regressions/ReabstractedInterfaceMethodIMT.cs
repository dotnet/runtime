// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    [Fact]
    public static void TestEntryPoint()
    {
        IDerived instance = new Implementation();

        for (var i = 0; i < 50; i++)
            Invoke(instance, i);

        for (var i = 0; i < 50; i++)
            Assert.Equal(i, Invoke(instance, i));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Invoke(IDerived instance, int method) => method switch
    {
        0 => instance.M00(),
        1 => instance.M01(),
        2 => instance.M02(),
        3 => instance.M03(),
        4 => instance.M04(),
        5 => instance.M05(),
        6 => instance.M06(),
        7 => instance.M07(),
        8 => instance.M08(),
        9 => instance.M09(),
        10 => instance.M10(),
        11 => instance.M11(),
        12 => instance.M12(),
        13 => instance.M13(),
        14 => instance.M14(),
        15 => instance.M15(),
        16 => instance.M16(),
        17 => instance.M17(),
        18 => instance.M18(),
        19 => instance.M19(),
        20 => instance.M20(),
        21 => instance.M21(),
        22 => instance.M22(),
        23 => instance.M23(),
        24 => instance.M24(),
        25 => instance.M25(),
        26 => instance.M26(),
        27 => instance.M27(),
        28 => instance.M28(),
        29 => instance.M29(),
        30 => instance.M30(),
        31 => instance.M31(),
        32 => instance.M32(),
        33 => instance.M33(),
        34 => instance.M34(),
        35 => instance.M35(),
        36 => instance.M36(),
        37 => instance.M37(),
        38 => instance.M38(),
        39 => instance.M39(),
        40 => instance.M40(),
        41 => instance.M41(),
        42 => instance.M42(),
        43 => instance.M43(),
        44 => instance.M44(),
        45 => instance.M45(),
        46 => instance.M46(),
        47 => instance.M47(),
        48 => instance.M48(),
        49 => instance.M49(),
        _ => throw new ArgumentOutOfRangeException(nameof(method)),
    };
}

interface IBase
{
    int R00 { get; }
    int R01 { get; }
    int R02 { get; }
    int R03 { get; }
    int R04 { get; }
    int R05 { get; }
    int R06 { get; }
    int R07 { get; }
    int R08 { get; }
    int R09 { get; }
    int R10 { get; }
    int R11 { get; }
    int R12 { get; }
    int R13 { get; }
    int R14 { get; }
    int R15 { get; }
    int R16 { get; }
    int R17 { get; }
    int R18 { get; }
    int R19 { get; }
    int R20 { get; }
    int R21 { get; }
    int R22 { get; }
    int R23 { get; }
    int R24 { get; }
    int R25 { get; }
    int R26 { get; }
    int R27 { get; }
    int R28 { get; }
    int R29 { get; }
    int R30 { get; }
    int R31 { get; }
    int R32 { get; }
    int R33 { get; }
    int R34 { get; }
    int R35 { get; }
    int R36 { get; }
    int R37 { get; }
    int R38 { get; }
    int R39 { get; }
    int R40 { get; }
    int R41 { get; }
    int R42 { get; }
    int R43 { get; }
    int R44 { get; }
    int R45 { get; }
    int R46 { get; }
    int R47 { get; }
    int R48 { get; }
    int R49 { get; }
}

interface IDerived : IBase
{
    abstract int IBase.R00 { get; }
    abstract int IBase.R01 { get; }
    abstract int IBase.R02 { get; }
    abstract int IBase.R03 { get; }
    abstract int IBase.R04 { get; }
    abstract int IBase.R05 { get; }
    abstract int IBase.R06 { get; }
    abstract int IBase.R07 { get; }
    abstract int IBase.R08 { get; }
    abstract int IBase.R09 { get; }
    abstract int IBase.R10 { get; }
    abstract int IBase.R11 { get; }
    abstract int IBase.R12 { get; }
    abstract int IBase.R13 { get; }
    abstract int IBase.R14 { get; }
    abstract int IBase.R15 { get; }
    abstract int IBase.R16 { get; }
    abstract int IBase.R17 { get; }
    abstract int IBase.R18 { get; }
    abstract int IBase.R19 { get; }
    abstract int IBase.R20 { get; }
    abstract int IBase.R21 { get; }
    abstract int IBase.R22 { get; }
    abstract int IBase.R23 { get; }
    abstract int IBase.R24 { get; }
    abstract int IBase.R25 { get; }
    abstract int IBase.R26 { get; }
    abstract int IBase.R27 { get; }
    abstract int IBase.R28 { get; }
    abstract int IBase.R29 { get; }
    abstract int IBase.R30 { get; }
    abstract int IBase.R31 { get; }
    abstract int IBase.R32 { get; }
    abstract int IBase.R33 { get; }
    abstract int IBase.R34 { get; }
    abstract int IBase.R35 { get; }
    abstract int IBase.R36 { get; }
    abstract int IBase.R37 { get; }
    abstract int IBase.R38 { get; }
    abstract int IBase.R39 { get; }
    abstract int IBase.R40 { get; }
    abstract int IBase.R41 { get; }
    abstract int IBase.R42 { get; }
    abstract int IBase.R43 { get; }
    abstract int IBase.R44 { get; }
    abstract int IBase.R45 { get; }
    abstract int IBase.R46 { get; }
    abstract int IBase.R47 { get; }
    abstract int IBase.R48 { get; }
    abstract int IBase.R49 { get; }

    int M00();
    int M01();
    int M02();
    int M03();
    int M04();
    int M05();
    int M06();
    int M07();
    int M08();
    int M09();
    int M10();
    int M11();
    int M12();
    int M13();
    int M14();
    int M15();
    int M16();
    int M17();
    int M18();
    int M19();
    int M20();
    int M21();
    int M22();
    int M23();
    int M24();
    int M25();
    int M26();
    int M27();
    int M28();
    int M29();
    int M30();
    int M31();
    int M32();
    int M33();
    int M34();
    int M35();
    int M36();
    int M37();
    int M38();
    int M39();
    int M40();
    int M41();
    int M42();
    int M43();
    int M44();
    int M45();
    int M46();
    int M47();
    int M48();
    int M49();
}

class Implementation : IDerived
{
    int IBase.R00 => -1;
    int IBase.R01 => -1;
    int IBase.R02 => -1;
    int IBase.R03 => -1;
    int IBase.R04 => -1;
    int IBase.R05 => -1;
    int IBase.R06 => -1;
    int IBase.R07 => -1;
    int IBase.R08 => -1;
    int IBase.R09 => -1;
    int IBase.R10 => -1;
    int IBase.R11 => -1;
    int IBase.R12 => -1;
    int IBase.R13 => -1;
    int IBase.R14 => -1;
    int IBase.R15 => -1;
    int IBase.R16 => -1;
    int IBase.R17 => -1;
    int IBase.R18 => -1;
    int IBase.R19 => -1;
    int IBase.R20 => -1;
    int IBase.R21 => -1;
    int IBase.R22 => -1;
    int IBase.R23 => -1;
    int IBase.R24 => -1;
    int IBase.R25 => -1;
    int IBase.R26 => -1;
    int IBase.R27 => -1;
    int IBase.R28 => -1;
    int IBase.R29 => -1;
    int IBase.R30 => -1;
    int IBase.R31 => -1;
    int IBase.R32 => -1;
    int IBase.R33 => -1;
    int IBase.R34 => -1;
    int IBase.R35 => -1;
    int IBase.R36 => -1;
    int IBase.R37 => -1;
    int IBase.R38 => -1;
    int IBase.R39 => -1;
    int IBase.R40 => -1;
    int IBase.R41 => -1;
    int IBase.R42 => -1;
    int IBase.R43 => -1;
    int IBase.R44 => -1;
    int IBase.R45 => -1;
    int IBase.R46 => -1;
    int IBase.R47 => -1;
    int IBase.R48 => -1;
    int IBase.R49 => -1;

    public int M00() => 0;
    public int M01() => 1;
    public int M02() => 2;
    public int M03() => 3;
    public int M04() => 4;
    public int M05() => 5;
    public int M06() => 6;
    public int M07() => 7;
    public int M08() => 8;
    public int M09() => 9;
    public int M10() => 10;
    public int M11() => 11;
    public int M12() => 12;
    public int M13() => 13;
    public int M14() => 14;
    public int M15() => 15;
    public int M16() => 16;
    public int M17() => 17;
    public int M18() => 18;
    public int M19() => 19;
    public int M20() => 20;
    public int M21() => 21;
    public int M22() => 22;
    public int M23() => 23;
    public int M24() => 24;
    public int M25() => 25;
    public int M26() => 26;
    public int M27() => 27;
    public int M28() => 28;
    public int M29() => 29;
    public int M30() => 30;
    public int M31() => 31;
    public int M32() => 32;
    public int M33() => 33;
    public int M34() => 34;
    public int M35() => 35;
    public int M36() => 36;
    public int M37() => 37;
    public int M38() => 38;
    public int M39() => 39;
    public int M40() => 40;
    public int M41() => 41;
    public int M42() => 42;
    public int M43() => 43;
    public int M44() => 44;
    public int M45() => 45;
    public int M46() => 46;
    public int M47() => 47;
    public int M48() => 48;
    public int M49() => 49;
}
