// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

public class Program
{
    public const byte Value = 0x50;

    public static int Main()
    {
        int testCount = 0;
        foreach (MethodInfo mi in typeof(Program)
            .GetMethods()
            .Where(m => m.Name.StartsWith("Test")))
        {
            mi.Invoke(null, null);
            testCount++;
        }

        return testCount;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test0()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 0);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(0));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test1()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 1);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(1));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test2()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 2);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(2));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test3()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 3);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(3));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test4()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 4);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(4));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test5()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 5);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(5));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test6()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 6);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(6));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test7()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 7);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(7));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test8()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 8);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(8));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test9()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 9);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(9));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test10()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 10);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(10));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test11()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 11);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(11));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test12()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 12);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(12));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test13()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 13);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(13));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test14()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 14);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(14));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test15()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 15);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(15));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test16()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 16);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(16));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test17()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 17);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(17));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test18()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 18);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(18));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test19()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 19);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(19));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test20()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 20);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(20));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test21()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 21);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(21));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test22()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 22);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(22));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test23()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 23);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(23));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test24()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 24);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(24));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test25()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 25);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(25));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test26()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 26);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(26));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test27()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 27);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(27));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test28()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 28);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(28));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test29()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 29);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(29));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test30()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 30);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(30));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test31()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 31);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(31));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test32()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 32);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(32));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test33()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 33);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(33));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test34()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 34);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(34));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test35()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 35);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(35));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test36()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 36);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(36));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test37()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 37);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(37));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test38()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 38);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(38));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test39()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 39);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(39));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test40()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 40);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(40));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test41()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 41);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(41));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test42()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 42);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(42));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test43()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 43);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(43));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test44()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 44);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(44));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test45()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 45);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(45));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test46()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 46);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(46));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test47()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 47);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(47));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test48()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 48);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(48));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test49()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 49);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(49));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test50()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 50);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(50));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test51()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 51);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(51));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test52()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 52);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(52));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test53()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 53);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(53));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test54()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 54);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(54));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test55()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 55);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(55));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test56()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 56);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(56));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test57()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 57);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(57));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test58()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 58);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(58));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test59()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 59);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(59));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test60()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 60);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(60));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test61()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 61);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(61));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test62()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 62);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(62));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test63()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 63);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(63));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test64()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 64);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(64));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test65()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 65);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(65));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test66()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 66);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(66));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test67()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 67);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(67));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test68()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 68);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(68));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test69()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 69);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(69));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test70()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 70);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(70));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test71()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 71);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(71));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test72()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 72);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(72));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test73()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 73);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(73));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test74()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 74);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(74));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test75()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 75);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(75));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test76()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 76);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(76));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test77()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 77);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(77));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test78()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 78);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(78));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test79()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 79);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(79));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test80()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 80);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(80));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test81()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 81);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(81));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test82()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 82);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(82));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test83()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 83);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(83));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test84()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 84);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(84));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test85()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 85);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(85));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test86()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 86);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(86));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test87()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 87);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(87));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test88()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 88);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(88));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test89()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 89);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(89));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test90()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 90);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(90));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test91()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 91);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(91));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test92()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 92);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(92));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test93()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 93);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(93));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test94()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 94);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(94));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test95()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 95);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(95));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test96()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 96);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(96));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test97()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 97);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(97));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test98()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 98);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(98));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test99()
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, 99);
        AssertEquals(BitConverter.ToString(bytes), NonUnrolledVersion(99));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string NonUnrolledVersion(uint len)
    {
        byte[] bytes = TestData();
        Unsafe.InitBlockUnaligned(ref bytes[0], Value, len);
        return BitConverter.ToString(bytes);
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte[] TestData()
    {
        return Enumerable.Repeat<byte>(0xFF, 100).ToArray();
    }

    private static void AssertEquals(string actual, string expected)
    {
        if (actual != expected)
        {
            throw new InvalidOperationException($"ERROR: {actual} != {expected}");
        }
    }
}