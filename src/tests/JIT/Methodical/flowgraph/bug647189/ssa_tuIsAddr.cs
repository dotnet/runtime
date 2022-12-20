// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 * The SSA phase isn't counting TU_ISADDR leafs when determining how many hash nodes to allocate.
 * The repro can be generated as:
 * Basically a lot of static field direct accesses with nothing else in the methods.
 */

using System;
using Xunit;

namespace Test_ssa_tuIsAddr_cs
{
public class Repro
{
    private static int s_field0 = 0;
    private static int s_field1 = 1;
    private static int s_field2 = 2;
    private static int s_field3 = 3;
    private static int s_field4 = 4;
    private static int s_field5 = 5;
    private static int s_field6 = 6;
    private static int s_field7 = 7;
    private static int s_field8 = 8;
    private static int s_field9 = 9;
    private static int s_field10 = 10;
    private static int s_field11 = 11;
    private static int s_field12 = 12;
    private static int s_field13 = 13;
    private static int s_field14 = 14;
    private static int s_field15 = 15;
    private static int s_field16 = 16;
    private static int s_field17 = 17;
    private static int s_field18 = 18;
    private static int s_field19 = 19;
    private static int s_field20 = 20;
    private static int s_field21 = 21;
    private static int s_field22 = 22;
    private static int s_field23 = 23;
    private static int s_field24 = 24;
    private static int s_field25 = 25;
    private static int s_field26 = 26;
    private static int s_field27 = 27;
    private static int s_field28 = 28;
    private static int s_field29 = 29;
    private static int s_field30 = 30;
    private static int s_field31 = 31;
    private static int s_field32 = 32;
    private static int s_field33 = 33;
    private static int s_field34 = 34;
    private static int s_field35 = 35;
    private static int s_field36 = 36;
    private static int s_field37 = 37;
    private static int s_field38 = 38;
    private static int s_field39 = 39;
    private static int s_field40 = 40;
    private static int s_field41 = 41;
    private static int s_field42 = 42;
    private static int s_field43 = 43;
    private static int s_field44 = 44;
    private static int s_field45 = 45;
    private static int s_field46 = 46;
    private static int s_field47 = 47;
    private static int s_field48 = 48;
    private static int s_field49 = 49;
    private static int s_field50 = 50;
    private static int s_field51 = 51;
    private static int s_field52 = 52;
    private static int s_field53 = 53;
    private static int s_field54 = 54;
    private static int s_field55 = 55;
    private static int s_field56 = 56;
    private static int s_field57 = 57;
    private static int s_field58 = 58;
    private static int s_field59 = 59;

    [Fact]
    public static int TestEntryPoint()
    {
        s_field0 = 2;
        if (s_field0 == 2)
        {
            Console.WriteLine("Passed");
            return 100;
        }
        else
        {
            Console.WriteLine("Failed");
            return 101;
        }
    }
}
}
