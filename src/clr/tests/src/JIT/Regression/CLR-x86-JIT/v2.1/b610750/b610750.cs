// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

// This is a test case created by briansul. it compares
// the result of a constant against the result of a variable shift operation. 
// It never repro'ed the actual issue, but it tests some of
// the changes done by brian to the product code. 
// Well, and it runs fast. 

using System;

class Program
{
    static bool failed = false;

    static void check(long x, long y, string msg)
    {
        if (x != y)
        {
            Console.WriteLine("Failed " + msg);
            failed = true;
        }
    }

    static void check(ulong x, ulong y, string msg)
    {
        if (x != y)
        {
            Console.WriteLine("Failed " + msg);
            failed = true;
        }
    }

    static long VSHL(long x, int s)
    {
        return x << s;
    }

    static long SHL01(long x)
    {
        return x << 01;
    }

    static long SHL02(long x)
    {
        return x << 02;
    }

    static long SHL03(long x)
    {
        return x << 03;
    }

    static long SHL04(long x)
    {
        return x << 04;
    }

    static long SHL05(long x)
    {
        return x << 05;
    }

    static long SHL06(long x)
    {
        return x << 06;
    }

    static long SHL07(long x)
    {
        return x << 07;
    }

    static long SHL08(long x)
    {
        return x << 08;
    }

    static long SHL09(long x)
    {
        return x << 09;
    }

    static long SHL10(long x)
    {
        return x << 10;
    }

    static long SHL11(long x)
    {
        return x << 11;
    }

    static long SHL12(long x)
    {
        return x << 12;
    }

    static long SHL13(long x)
    {
        return x << 13;
    }

    static long SHL14(long x)
    {
        return x << 14;
    }

    static long SHL15(long x)
    {
        return x << 15;
    }

    static long SHL16(long x)
    {
        return x << 16;
    }

    static long SHL17(long x)
    {
        return x << 17;
    }

    static long SHL18(long x)
    {
        return x << 18;
    }

    static long SHL19(long x)
    {
        return x << 19;
    }

    static long SHL20(long x)
    {
        return x << 20;
    }

    static long SHL21(long x)
    {
        return x << 21;
    }

    static long SHL22(long x)
    {
        return x << 22;
    }

    static long SHL23(long x)
    {
        return x << 23;
    }

    static long SHL24(long x)
    {
        return x << 24;
    }

    static long SHL25(long x)
    {
        return x << 25;
    }

    static long SHL26(long x)
    {
        return x << 26;
    }

    static long SHL27(long x)
    {
        return x << 27;
    }

    static long SHL28(long x)
    {
        return x << 28;
    }

    static long SHL29(long x)
    {
        return x << 29;
    }

    static long SHL30(long x)
    {
        return x << 30;
    }

    static long SHL31(long x)
    {
        return x << 31;
    }

    static long SHL32(long x)
    {
        return x << 32;
    }

    static long SHL33(long x)
    {
        return x << 33;
    }

    static long SHL34(long x)
    {
        return x << 34;
    }

    static long SHL35(long x)
    {
        return x << 35;
    }

    static long SHL36(long x)
    {
        return x << 36;
    }

    static long SHL37(long x)
    {
        return x << 37;
    }

    static long SHL38(long x)
    {
        return x << 38;
    }

    static long SHL39(long x)
    {
        return x << 39;
    }

    static long SHL40(long x)
    {
        return x << 40;
    }

    static long SHL41(long x)
    {
        return x << 41;
    }

    static long SHL42(long x)
    {
        return x << 42;
    }

    static long SHL43(long x)
    {
        return x << 43;
    }

    static long SHL44(long x)
    {
        return x << 44;
    }

    static long SHL45(long x)
    {
        return x << 45;
    }

    static long SHL46(long x)
    {
        return x << 46;
    }

    static long SHL47(long x)
    {
        return x << 47;
    }

    static long SHL48(long x)
    {
        return x << 48;
    }

    static long SHL49(long x)
    {
        return x << 49;
    }

    static long SHL50(long x)
    {
        return x << 50;
    }

    static long SHL51(long x)
    {
        return x << 51;
    }

    static long SHL52(long x)
    {
        return x << 52;
    }

    static long SHL53(long x)
    {
        return x << 53;
    }

    static long SHL54(long x)
    {
        return x << 54;
    }

    static long SHL55(long x)
    {
        return x << 55;
    }

    static long SHL56(long x)
    {
        return x << 56;
    }

    static long SHL57(long x)
    {
        return x << 57;
    }

    static long SHL58(long x)
    {
        return x << 58;
    }

    static long SHL59(long x)
    {
        return x << 59;
    }

    static long SHL60(long x)
    {
        return x << 60;
    }

    static long SHL61(long x)
    {
        return x << 61;
    }

    static long SHL62(long x)
    {
        return x << 62;
    }

    static long SHL63(long x)
    {
        return x << 63;
    }

    static long SHL64(long x)
    {
        return x << 64;
    }


    static void TestSHL()
    {
        long x = 1;
        long resK;
        long resV;
        int s;

        for (int i = 0; i < 32; i++)
        {
            s = 1;
            resK = SHL01(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL01");
            s += 1;

            resK = SHL02(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL02");
            s += 1;

            resK = SHL03(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL03");
            s += 1;

            resK = SHL04(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL04");
            s += 1;

            resK = SHL05(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL05");
            s += 1;

            resK = SHL06(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL06");
            s += 1;

            resK = SHL07(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL07");
            s += 1;

            resK = SHL08(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL08");
            s += 1;

            resK = SHL09(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL09");
            s += 1;

            resK = SHL10(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL10");
            s += 1;

            resK = SHL11(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL11");
            s += 1;

            resK = SHL12(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL12");
            s += 1;

            resK = SHL13(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL13");
            s += 1;

            resK = SHL14(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL14");
            s += 1;

            resK = SHL15(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL15");
            s += 1;

            resK = SHL16(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL16");
            s += 1;

            resK = SHL17(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL17");
            s += 1;

            resK = SHL18(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL18");
            s += 1;

            resK = SHL19(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL19");
            s += 1;

            resK = SHL20(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL20");
            s += 1;

            resK = SHL21(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL21");
            s += 1;

            resK = SHL22(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL22");
            s += 1;

            resK = SHL23(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL23");
            s += 1;

            resK = SHL24(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL24");
            s += 1;

            resK = SHL25(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL25");
            s += 1;

            resK = SHL26(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL26");
            s += 1;

            resK = SHL27(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL27");
            s += 1;

            resK = SHL28(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL28");
            s += 1;

            resK = SHL29(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL29");
            s += 1;

            resK = SHL30(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL30");
            s += 1;

            resK = SHL31(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL31");
            s += 1;

            resK = SHL32(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL32");
            s += 1;

            resK = SHL33(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL33");
            s += 1;

            resK = SHL34(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL34");
            s += 1;

            resK = SHL35(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL35");
            s += 1;

            resK = SHL36(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL36");
            s += 1;

            resK = SHL37(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL37");
            s += 1;

            resK = SHL38(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL38");
            s += 1;

            resK = SHL39(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL39");
            s += 1;

            resK = SHL40(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL40");
            s += 1;

            resK = SHL41(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL41");
            s += 1;

            resK = SHL42(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL42");
            s += 1;

            resK = SHL43(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL43");
            s += 1;

            resK = SHL44(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL44");
            s += 1;

            resK = SHL45(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL45");
            s += 1;

            resK = SHL46(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL46");
            s += 1;

            resK = SHL47(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL47");
            s += 1;

            resK = SHL48(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL48");
            s += 1;

            resK = SHL49(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL49");
            s += 1;

            resK = SHL50(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL50");
            s += 1;

            resK = SHL51(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL51");
            s += 1;

            resK = SHL52(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL52");
            s += 1;

            resK = SHL53(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL53");
            s += 1;

            resK = SHL54(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL54");
            s += 1;

            resK = SHL55(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL55");
            s += 1;

            resK = SHL56(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL56");
            s += 1;

            resK = SHL57(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL57");
            s += 1;

            resK = SHL58(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL58");
            s += 1;

            resK = SHL59(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL59");
            s += 1;

            resK = SHL60(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL60");
            s += 1;

            resK = SHL61(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL61");
            s += 1;

            resK = SHL62(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL62");
            s += 1;

            resK = SHL63(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL63");
            s += 1;

            resK = SHL64(x);
            resV = VSHL(x, s);
            check(resK, resV, "SHL64");
            s += 1;

            x *= 5;
        }

    }

    static long VSHR(long x, int s)
    {
        return x >> s;
    }

    static long SHR01(long x)
    {
        return x >> 01;
    }

    static long SHR02(long x)
    {
        return x >> 02;
    }

    static long SHR03(long x)
    {
        return x >> 03;
    }

    static long SHR04(long x)
    {
        return x >> 04;
    }

    static long SHR05(long x)
    {
        return x >> 05;
    }

    static long SHR06(long x)
    {
        return x >> 06;
    }

    static long SHR07(long x)
    {
        return x >> 07;
    }

    static long SHR08(long x)
    {
        return x >> 08;
    }

    static long SHR09(long x)
    {
        return x >> 09;
    }

    static long SHR10(long x)
    {
        return x >> 10;
    }

    static long SHR11(long x)
    {
        return x >> 11;
    }

    static long SHR12(long x)
    {
        return x >> 12;
    }

    static long SHR13(long x)
    {
        return x >> 13;
    }

    static long SHR14(long x)
    {
        return x >> 14;
    }

    static long SHR15(long x)
    {
        return x >> 15;
    }

    static long SHR16(long x)
    {
        return x >> 16;
    }

    static long SHR17(long x)
    {
        return x >> 17;
    }

    static long SHR18(long x)
    {
        return x >> 18;
    }

    static long SHR19(long x)
    {
        return x >> 19;
    }

    static long SHR20(long x)
    {
        return x >> 20;
    }

    static long SHR21(long x)
    {
        return x >> 21;
    }

    static long SHR22(long x)
    {
        return x >> 22;
    }

    static long SHR23(long x)
    {
        return x >> 23;
    }

    static long SHR24(long x)
    {
        return x >> 24;
    }

    static long SHR25(long x)
    {
        return x >> 25;
    }

    static long SHR26(long x)
    {
        return x >> 26;
    }

    static long SHR27(long x)
    {
        return x >> 27;
    }

    static long SHR28(long x)
    {
        return x >> 28;
    }

    static long SHR29(long x)
    {
        return x >> 29;
    }

    static long SHR30(long x)
    {
        return x >> 30;
    }

    static long SHR31(long x)
    {
        return x >> 31;
    }

    static long SHR32(long x)
    {
        return x >> 32;
    }

    static long SHR33(long x)
    {
        return x >> 33;
    }

    static long SHR34(long x)
    {
        return x >> 34;
    }

    static long SHR35(long x)
    {
        return x >> 35;
    }

    static long SHR36(long x)
    {
        return x >> 36;
    }

    static long SHR37(long x)
    {
        return x >> 37;
    }

    static long SHR38(long x)
    {
        return x >> 38;
    }

    static long SHR39(long x)
    {
        return x >> 39;
    }

    static long SHR40(long x)
    {
        return x >> 40;
    }

    static long SHR41(long x)
    {
        return x >> 41;
    }

    static long SHR42(long x)
    {
        return x >> 42;
    }

    static long SHR43(long x)
    {
        return x >> 43;
    }

    static long SHR44(long x)
    {
        return x >> 44;
    }

    static long SHR45(long x)
    {
        return x >> 45;
    }

    static long SHR46(long x)
    {
        return x >> 46;
    }

    static long SHR47(long x)
    {
        return x >> 47;
    }

    static long SHR48(long x)
    {
        return x >> 48;
    }

    static long SHR49(long x)
    {
        return x >> 49;
    }

    static long SHR50(long x)
    {
        return x >> 50;
    }

    static long SHR51(long x)
    {
        return x >> 51;
    }

    static long SHR52(long x)
    {
        return x >> 52;
    }

    static long SHR53(long x)
    {
        return x >> 53;
    }

    static long SHR54(long x)
    {
        return x >> 54;
    }

    static long SHR55(long x)
    {
        return x >> 55;
    }

    static long SHR56(long x)
    {
        return x >> 56;
    }

    static long SHR57(long x)
    {
        return x >> 57;
    }

    static long SHR58(long x)
    {
        return x >> 58;
    }

    static long SHR59(long x)
    {
        return x >> 59;
    }

    static long SHR60(long x)
    {
        return x >> 60;
    }

    static long SHR61(long x)
    {
        return x >> 61;
    }

    static long SHR62(long x)
    {
        return x >> 62;
    }

    static long SHR63(long x)
    {
        return x >> 63;
    }

    static long SHR64(long x)
    {
        return x >> 64;
    }


    static void TestSHR()
    {
        long x = 1;
        long resK;
        long resV;
        int s;

        for (int i = 0; i < 32; i++)
        {
            s = 1;
            resK = SHR01(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR01");
            s += 1;

            resK = SHR02(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR02");
            s += 1;

            resK = SHR03(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR03");
            s += 1;

            resK = SHR04(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR04");
            s += 1;

            resK = SHR05(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR05");
            s += 1;

            resK = SHR06(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR06");
            s += 1;

            resK = SHR07(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR07");
            s += 1;

            resK = SHR08(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR08");
            s += 1;

            resK = SHR09(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR09");
            s += 1;

            resK = SHR10(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR10");
            s += 1;

            resK = SHR11(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR11");
            s += 1;

            resK = SHR12(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR12");
            s += 1;

            resK = SHR13(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR13");
            s += 1;

            resK = SHR14(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR14");
            s += 1;

            resK = SHR15(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR15");
            s += 1;

            resK = SHR16(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR16");
            s += 1;

            resK = SHR17(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR17");
            s += 1;

            resK = SHR18(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR18");
            s += 1;

            resK = SHR19(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR19");
            s += 1;

            resK = SHR20(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR20");
            s += 1;

            resK = SHR21(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR21");
            s += 1;

            resK = SHR22(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR22");
            s += 1;

            resK = SHR23(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR23");
            s += 1;

            resK = SHR24(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR24");
            s += 1;

            resK = SHR25(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR25");
            s += 1;

            resK = SHR26(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR26");
            s += 1;

            resK = SHR27(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR27");
            s += 1;

            resK = SHR28(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR28");
            s += 1;

            resK = SHR29(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR29");
            s += 1;

            resK = SHR30(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR30");
            s += 1;

            resK = SHR31(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR31");
            s += 1;

            resK = SHR32(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR32");
            s += 1;

            resK = SHR33(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR33");
            s += 1;

            resK = SHR34(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR34");
            s += 1;

            resK = SHR35(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR35");
            s += 1;

            resK = SHR36(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR36");
            s += 1;

            resK = SHR37(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR37");
            s += 1;

            resK = SHR38(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR38");
            s += 1;

            resK = SHR39(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR39");
            s += 1;

            resK = SHR40(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR40");
            s += 1;

            resK = SHR41(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR41");
            s += 1;

            resK = SHR42(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR42");
            s += 1;

            resK = SHR43(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR43");
            s += 1;

            resK = SHR44(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR44");
            s += 1;

            resK = SHR45(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR45");
            s += 1;

            resK = SHR46(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR46");
            s += 1;

            resK = SHR47(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR47");
            s += 1;

            resK = SHR48(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR48");
            s += 1;

            resK = SHR49(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR49");
            s += 1;

            resK = SHR50(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR50");
            s += 1;

            resK = SHR51(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR51");
            s += 1;

            resK = SHR52(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR52");
            s += 1;

            resK = SHR53(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR53");
            s += 1;

            resK = SHR54(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR54");
            s += 1;

            resK = SHR55(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR55");
            s += 1;

            resK = SHR56(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR56");
            s += 1;

            resK = SHR57(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR57");
            s += 1;

            resK = SHR58(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR58");
            s += 1;

            resK = SHR59(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR59");
            s += 1;

            resK = SHR60(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR60");
            s += 1;

            resK = SHR61(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR61");
            s += 1;

            resK = SHR62(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR62");
            s += 1;

            resK = SHR63(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR63");
            s += 1;

            resK = SHR64(x);
            resV = VSHR(x, s);
            check(resK, resV, "SHR64");
            s += 1;

            x *= 5;
        }

    }

    static ulong VSZR(ulong x, int s)
    {
        return x >> s;
    }

    static ulong SZR01(ulong x)
    {
        return x >> 01;
    }

    static ulong SZR02(ulong x)
    {
        return x >> 02;
    }

    static ulong SZR03(ulong x)
    {
        return x >> 03;
    }

    static ulong SZR04(ulong x)
    {
        return x >> 04;
    }

    static ulong SZR05(ulong x)
    {
        return x >> 05;
    }

    static ulong SZR06(ulong x)
    {
        return x >> 06;
    }

    static ulong SZR07(ulong x)
    {
        return x >> 07;
    }

    static ulong SZR08(ulong x)
    {
        return x >> 08;
    }

    static ulong SZR09(ulong x)
    {
        return x >> 09;
    }

    static ulong SZR10(ulong x)
    {
        return x >> 10;
    }

    static ulong SZR11(ulong x)
    {
        return x >> 11;
    }

    static ulong SZR12(ulong x)
    {
        return x >> 12;
    }

    static ulong SZR13(ulong x)
    {
        return x >> 13;
    }

    static ulong SZR14(ulong x)
    {
        return x >> 14;
    }

    static ulong SZR15(ulong x)
    {
        return x >> 15;
    }

    static ulong SZR16(ulong x)
    {
        return x >> 16;
    }

    static ulong SZR17(ulong x)
    {
        return x >> 17;
    }

    static ulong SZR18(ulong x)
    {
        return x >> 18;
    }

    static ulong SZR19(ulong x)
    {
        return x >> 19;
    }

    static ulong SZR20(ulong x)
    {
        return x >> 20;
    }

    static ulong SZR21(ulong x)
    {
        return x >> 21;
    }

    static ulong SZR22(ulong x)
    {
        return x >> 22;
    }

    static ulong SZR23(ulong x)
    {
        return x >> 23;
    }

    static ulong SZR24(ulong x)
    {
        return x >> 24;
    }

    static ulong SZR25(ulong x)
    {
        return x >> 25;
    }

    static ulong SZR26(ulong x)
    {
        return x >> 26;
    }

    static ulong SZR27(ulong x)
    {
        return x >> 27;
    }

    static ulong SZR28(ulong x)
    {
        return x >> 28;
    }

    static ulong SZR29(ulong x)
    {
        return x >> 29;
    }

    static ulong SZR30(ulong x)
    {
        return x >> 30;
    }

    static ulong SZR31(ulong x)
    {
        return x >> 31;
    }

    static ulong SZR32(ulong x)
    {
        return x >> 32;
    }

    static ulong SZR33(ulong x)
    {
        return x >> 33;
    }

    static ulong SZR34(ulong x)
    {
        return x >> 34;
    }

    static ulong SZR35(ulong x)
    {
        return x >> 35;
    }

    static ulong SZR36(ulong x)
    {
        return x >> 36;
    }

    static ulong SZR37(ulong x)
    {
        return x >> 37;
    }

    static ulong SZR38(ulong x)
    {
        return x >> 38;
    }

    static ulong SZR39(ulong x)
    {
        return x >> 39;
    }

    static ulong SZR40(ulong x)
    {
        return x >> 40;
    }

    static ulong SZR41(ulong x)
    {
        return x >> 41;
    }

    static ulong SZR42(ulong x)
    {
        return x >> 42;
    }

    static ulong SZR43(ulong x)
    {
        return x >> 43;
    }

    static ulong SZR44(ulong x)
    {
        return x >> 44;
    }

    static ulong SZR45(ulong x)
    {
        return x >> 45;
    }

    static ulong SZR46(ulong x)
    {
        return x >> 46;
    }

    static ulong SZR47(ulong x)
    {
        return x >> 47;
    }

    static ulong SZR48(ulong x)
    {
        return x >> 48;
    }

    static ulong SZR49(ulong x)
    {
        return x >> 49;
    }

    static ulong SZR50(ulong x)
    {
        return x >> 50;
    }

    static ulong SZR51(ulong x)
    {
        return x >> 51;
    }

    static ulong SZR52(ulong x)
    {
        return x >> 52;
    }

    static ulong SZR53(ulong x)
    {
        return x >> 53;
    }

    static ulong SZR54(ulong x)
    {
        return x >> 54;
    }

    static ulong SZR55(ulong x)
    {
        return x >> 55;
    }

    static ulong SZR56(ulong x)
    {
        return x >> 56;
    }

    static ulong SZR57(ulong x)
    {
        return x >> 57;
    }

    static ulong SZR58(ulong x)
    {
        return x >> 58;
    }

    static ulong SZR59(ulong x)
    {
        return x >> 59;
    }

    static ulong SZR60(ulong x)
    {
        return x >> 60;
    }

    static ulong SZR61(ulong x)
    {
        return x >> 61;
    }

    static ulong SZR62(ulong x)
    {
        return x >> 62;
    }

    static ulong SZR63(ulong x)
    {
        return x >> 63;
    }

    static ulong SZR64(ulong x)
    {
        return x >> 64;
    }


    static void TestSZR()
    {
        ulong x = 1;
        ulong resK;
        ulong resV;
        int s;

        for (int i = 0; i < 32; i++)
        {
            s = 1;
            resK = SZR01(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR01");
            s += 1;

            resK = SZR02(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR02");
            s += 1;

            resK = SZR03(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR03");
            s += 1;

            resK = SZR04(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR04");
            s += 1;

            resK = SZR05(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR05");
            s += 1;

            resK = SZR06(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR06");
            s += 1;

            resK = SZR07(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR07");
            s += 1;

            resK = SZR08(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR08");
            s += 1;

            resK = SZR09(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR09");
            s += 1;

            resK = SZR10(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR10");
            s += 1;

            resK = SZR11(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR11");
            s += 1;

            resK = SZR12(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR12");
            s += 1;

            resK = SZR13(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR13");
            s += 1;

            resK = SZR14(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR14");
            s += 1;

            resK = SZR15(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR15");
            s += 1;

            resK = SZR16(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR16");
            s += 1;

            resK = SZR17(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR17");
            s += 1;

            resK = SZR18(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR18");
            s += 1;

            resK = SZR19(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR19");
            s += 1;

            resK = SZR20(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR20");
            s += 1;

            resK = SZR21(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR21");
            s += 1;

            resK = SZR22(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR22");
            s += 1;

            resK = SZR23(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR23");
            s += 1;

            resK = SZR24(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR24");
            s += 1;

            resK = SZR25(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR25");
            s += 1;

            resK = SZR26(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR26");
            s += 1;

            resK = SZR27(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR27");
            s += 1;

            resK = SZR28(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR28");
            s += 1;

            resK = SZR29(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR29");
            s += 1;

            resK = SZR30(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR30");
            s += 1;

            resK = SZR31(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR31");
            s += 1;

            resK = SZR32(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR32");
            s += 1;

            resK = SZR33(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR33");
            s += 1;

            resK = SZR34(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR34");
            s += 1;

            resK = SZR35(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR35");
            s += 1;

            resK = SZR36(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR36");
            s += 1;

            resK = SZR37(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR37");
            s += 1;

            resK = SZR38(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR38");
            s += 1;

            resK = SZR39(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR39");
            s += 1;

            resK = SZR40(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR40");
            s += 1;

            resK = SZR41(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR41");
            s += 1;

            resK = SZR42(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR42");
            s += 1;

            resK = SZR43(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR43");
            s += 1;

            resK = SZR44(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR44");
            s += 1;

            resK = SZR45(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR45");
            s += 1;

            resK = SZR46(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR46");
            s += 1;

            resK = SZR47(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR47");
            s += 1;

            resK = SZR48(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR48");
            s += 1;

            resK = SZR49(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR49");
            s += 1;

            resK = SZR50(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR50");
            s += 1;

            resK = SZR51(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR51");
            s += 1;

            resK = SZR52(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR52");
            s += 1;

            resK = SZR53(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR53");
            s += 1;

            resK = SZR54(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR54");
            s += 1;

            resK = SZR55(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR55");
            s += 1;

            resK = SZR56(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR56");
            s += 1;

            resK = SZR57(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR57");
            s += 1;

            resK = SZR58(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR58");
            s += 1;

            resK = SZR59(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR59");
            s += 1;

            resK = SZR60(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR60");
            s += 1;

            resK = SZR61(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR61");
            s += 1;

            resK = SZR62(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR62");
            s += 1;

            resK = SZR63(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR63");
            s += 1;

            resK = SZR64(x);
            resV = VSZR(x, s);
            check(resK, resV, "SZR64");
            s += 1;

            x *= 5;
        }

    }

    static int Main(string[] args)
    {
        TestSHL();

        TestSHR();

        TestSZR();

        if (!failed)
        {
            Console.WriteLine("!!!!!!!!! PASSED !!!!!!!!!!!!");
            return 100;
        }
        else
        {
            Console.WriteLine("!!!!!!!!! FAILED !!!!!!!!!!!!");
            return 666;
        }


    }
}

