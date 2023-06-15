// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class StringEquals
{
    [Fact]
    public static int TestEntryPoint()
    {
        int testCount = 0;
        foreach (var method in typeof(Tests).GetMethods())
        {
            if (!method.Name.StartsWith("Equals_"))
                continue;

            foreach (string testStr in Tests.s_TestData)
            {
                testCount++;
                method.Invoke(null, new object[] { testStr });
            }
        }

        Console.WriteLine(testCount);
        return testCount == 27888 ? 100 : 0;
    }
}

public static class Tests
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ValidateEquals(bool result, string left, string right)
    {
        if (result != (left == right))
            throw new Exception($"{result} != ({left} == {right})");
    }

    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_0(string s) => ValidateEquals(s == "", s, "");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_1(string s) => ValidateEquals(s == "3", s, "3");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_2(string s) => ValidateEquals(s == "\0", s, "\0");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_3(string s) => ValidateEquals(s == "\u0436", s, "\u0436");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_4(string s) => ValidateEquals(s == "1", s, "1");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_5(string s) => ValidateEquals(s == "33", s, "33");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_6(string s) => ValidateEquals(s == "31", s, "31");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_7(string s) => ValidateEquals(s == "a1", s, "a1");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_8(string s) => ValidateEquals(s == "12", s, "12");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_9(string s) => ValidateEquals(s == "1\0", s, "1\0");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_10(string s) => ValidateEquals(s == "b12", s, "b12");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_11(string s) => ValidateEquals(s == "\u043623", s, "\u043623");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_12(string s) => ValidateEquals(s == "2a2", s, "2a2");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_13(string s) => ValidateEquals(s == "222", s, "222");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_14(string s) => ValidateEquals(s == "0\u044C3", s, "0\u044C3");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_15(string s) => ValidateEquals(s == "b\u043631", s, "b\u043631");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_16(string s) => ValidateEquals(s == "\u044C\u0419b\u0419", s, "\u044C\u0419b\u0419");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_17(string s) => ValidateEquals(s == "b033", s, "b033");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_18(string s) => ValidateEquals(s == "311\u044C", s, "311\u044C");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_19(string s) => ValidateEquals(s == "\u0436\u041912", s, "\u0436\u041912");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_20(string s) => ValidateEquals(s == "2011b", s, "2011b");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_21(string s) => ValidateEquals(s == "222b2", s, "222b2");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_22(string s) => ValidateEquals(s == "a\u0419213", s, "a\u0419213");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_23(string s) => ValidateEquals(s == "1a131", s, "1a131");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_24(string s) => ValidateEquals(s == "3232\u0419", s, "3232\u0419");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_25(string s) => ValidateEquals(s == "3b0\u044C\u0436\u044C", s, "3b0\u044C\u0436\u044C");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_26(string s) => ValidateEquals(s == "213b2\u0419", s, "213b2\u0419");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_27(string s) => ValidateEquals(s == "b31210", s, "b31210");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_28(string s) => ValidateEquals(s == "1\u04360021", s, "1\u04360021");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_29(string s) => ValidateEquals(s == "3\u044C3112", s, "3\u044C3112");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_30(string s) => ValidateEquals(s == "122b231", s, "122b231");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_31(string s) => ValidateEquals(s == "03\u043632\u04363", s, "03\u043632\u04363");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_32(string s) => ValidateEquals(s == "bb31\u04362\u0419", s, "bb31\u04362\u0419");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_33(string s) => ValidateEquals(s == "023b\u044C\u0436\u0419", s, "023b\u044C\u0436\u0419");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_34(string s) => ValidateEquals(s == "\0232a12", s, "\0232a12");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_35(string s) => ValidateEquals(s == "\u043613\u044C11\u0419\u044C", s, "\u043613\u044C11\u0419\u044C");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_36(string s) => ValidateEquals(s == "11\u044Cbb32\u044C", s, "11\u044Cbb32\u044C");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_37(string s) => ValidateEquals(s == "222\u0419\u04363\u04363", s, "222\u0419\u04363\u04363");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_38(string s) => ValidateEquals(s == "\u0436303a\u041912", s, "\u0436303a\u041912");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_39(string s) => ValidateEquals(s == "\u044Cb22322b", s, "\u044Cb22322b");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_40(string s) => ValidateEquals(s == "a22b10b1\u0419", s, "a22b10b1\u0419");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_41(string s) => ValidateEquals(s == "3ba2221\u044C3", s, "3ba2221\u044C3");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_42(string s) => ValidateEquals(s == "\u0436a1\u04190b1\u04191", s, "\u0436a1\u04190b1\u04191");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_43(string s) => ValidateEquals(s == "a20\u0419\u0436\u04361\u044C\u044C", s, "a20\u0419\u0436\u04361\u044C\u044C");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_44(string s) => ValidateEquals(s == "\u044Ca\u043632132\u044C", s, "\u044Ca\u043632132\u044C");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_45(string s) => ValidateEquals(s == "11111\u04193\u041912", s, "11111\u04193\u041912");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_46(string s) => ValidateEquals(s == "11\u0419\02\u0419b3\u0436\u0436", s, "11\u0419\02\u0419b3\u0436\u0436");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_47(string s) => ValidateEquals(s == "21b\u0436\u0436\u04360103", s, "21b\u0436\u0436\u04360103");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_48(string s) => ValidateEquals(s == "333332a\u041911", s, "333332a\u041911");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_49(string s) => ValidateEquals(s == "\u0419123112313", s, "\u0419123112313");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_50(string s) => ValidateEquals(s == "12\u0419\u044C\u0419a\u041911\u044Cb", s, "12\u0419\u044C\u0419a\u041911\u044Cb");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_51(string s) => ValidateEquals(s == "\u0436\u043622221\u04193\u04192", s, "\u0436\u043622221\u04193\u04192");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_52(string s) => ValidateEquals(s == "\u044C\u04191bb\u04363202\u0436", s, "\u044C\u04191bb\u04363202\u0436");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_53(string s) => ValidateEquals(s == "1bb\u04192\u041933\u04192\u0436", s, "1bb\u04192\u041933\u04192\u0436");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_54(string s) => ValidateEquals(s == "2013133\u044C1b\u0436", s, "2013133\u044C1b\u0436");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_55(string s) => ValidateEquals(s == "23a2\02\u0436a2a13", s, "23a2\02\u0436a2a13");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_56(string s) => ValidateEquals(s == "23\u0419210\u04193a3\u04361", s, "23\u0419210\u04193a3\u04361");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_57(string s) => ValidateEquals(s == "32\u04192133bb2\u04193", s, "32\u04192133bb2\u04193");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_58(string s) => ValidateEquals(s == "\u04193bb1\u044C3bb\u044Cb3", s, "\u04193bb1\u044C3bb\u044Cb3");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_59(string s) => ValidateEquals(s == "a0\u0419bab\u04362\u0419133", s, "a0\u0419bab\u04362\u0419133");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_60(string s) => ValidateEquals(s == "320\u0436a22a11\u04361b", s, "320\u0436a22a11\u04361b");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_61(string s) => ValidateEquals(s == "\u044C321b3\u044C\u0419\u041913\u04192", s, "\u044C321b3\u044C\u0419\u041913\u04192");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_62(string s) => ValidateEquals(s == "a3\u044C1\u04362a\022a1a", s, "a3\u044C1\u04362a\022a1a");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_63(string s) => ValidateEquals(s == "3\u0419b30b33231b\u044C", s, "3\u0419b30b33231b\u044C");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_64(string s) => ValidateEquals(s == "2210121\u043613231", s, "2210121\u043613231");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_65(string s) => ValidateEquals(s == "013311aa3203\u04191", s, "013311aa3203\u04191");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_66(string s) => ValidateEquals(s == "12\u0419\u04191\u04192a\u04192\u044Cb\u0419a", s, "12\u0419\u04191\u04192a\u04192\u044Cb\u0419a");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_67(string s) => ValidateEquals(s == "2b1\u041911130221b\u044C", s, "2b1\u041911130221b\u044C");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_68(string s) => ValidateEquals(s == "230110\u04190b3112\u0436", s, "230110\u04190b3112\u0436");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_69(string s) => ValidateEquals(s == "a213\u044Cab121b332", s, "a213\u044Cab121b332");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_70(string s) => ValidateEquals(s == "111a01\u04363121b123", s, "111a01\u04363121b123");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_71(string s) => ValidateEquals(s == "13a322\u04192\u04193b\u0436b0\u0419", s, "13a322\u04192\u04193b\u0436b0\u0419");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_72(string s) => ValidateEquals(s == "\021232b1\u0419aa1032", s, "\021232b1\u0419aa1032");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_73(string s) => ValidateEquals(s == "\u0436\u0419112\u044Cb12\u0419\u044C3b2\u0436", s, "\u0436\u0419112\u044Cb12\u0419\u044C3b2\u0436");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_74(string s) => ValidateEquals(s == "2b\u044C\u044C331bb\023122", s, "2b\u044C\u044C331bb\023122");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_75(string s) => ValidateEquals(s == "a\u043622\u04192203b023b\u044C3", s, "a\u043622\u04192203b023b\u044C3");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_76(string s) => ValidateEquals(s == "a\u0419033\u04363a220\u044C3331", s, "a\u0419033\u04363a220\u044C3331");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_77(string s) => ValidateEquals(s == "20\u0419\u0436a1b1313\u0436\u0419b2a", s, "20\u0419\u0436a1b1313\u0436\u0419b2a");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_78(string s) => ValidateEquals(s == "131\u04191\022\u04362322123", s, "131\u04191\022\u04362322123");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_79(string s) => ValidateEquals(s == "23323b21\u044C11b\u0419321", s, "23323b21\u044C11b\u0419321");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_80(string s) => ValidateEquals(s == "302a\u044C\u0436a3213\u0436a\u04193\u0419\u0436", s, "302a\u044C\u0436a3213\u0436a\u04193\u0419\u0436");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_81(string s) => ValidateEquals(s == "\u043613b00210b1212102", s, "\u043613b00210b1212102");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_82(string s) => ValidateEquals(s == "20320\u04193\u04193\u044C\u04363\u04192122", s, "20320\u04193\u04193\u044C\u04363\u04192122");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_83(string s) => ValidateEquals(s == "0bb23a30ba\u0419b2333\u044C", s, "0bb23a30ba\u0419b2333\u044C");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_84(string s) => ValidateEquals(s == "22122\u044C130230103a2", s, "22122\u044C130230103a2");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_85(string s) => ValidateEquals(s == "\u044C\u044Cba20\u04361\u0436\u044C\u0419\u044Cb\u041931b\u0436", s, "\u044C\u044Cba20\u04361\u0436\u044C\u0419\u044Cb\u041931b\u0436");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_86(string s) => ValidateEquals(s == "bb1\u044C1033b\u04363011b\u043610", s, "bb1\u044C1033b\u04363011b\u043610");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_87(string s) => ValidateEquals(s == "1\u044C320a3a22b3333b13", s, "1\u044C320a3a22b3333b13");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_88(string s) => ValidateEquals(s == "0a22a\u0419\u0436a2222\u043623\u041913", s, "0a22a\u0419\u0436a2222\u043623\u041913");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_89(string s) => ValidateEquals(s == "\u041911\u0419213212\u04361233b23", s, "\u041911\u0419213212\u04361233b23");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_90(string s) => ValidateEquals(s == "32\u044C1\u041903123\u044C011332ab", s, "32\u044C1\u041903123\u044C011332ab");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_91(string s) => ValidateEquals(s == "222\u04362311b133b3\u04363223", s, "222\u04362311b133b3\u04363223");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_92(string s) => ValidateEquals(s == "0111\u044C3002222a3aaaa3", s, "0111\u044C3002222a3aaaa3");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_93(string s) => ValidateEquals(s == "313\u0419213a\u043601a12231a2", s, "313\u0419213a\u043601a12231a2");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_94(string s) => ValidateEquals(s == "1\u0436022\u044C1323b3b3\u0436222\u044C", s, "1\u0436022\u044C1323b3b3\u0436222\u044C");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_95(string s) => ValidateEquals(s == "\u044C023a3b213\u044C033\u043613231", s, "\u044C023a3b213\u044C033\u043613231");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_96(string s) => ValidateEquals(s == "ab2b0b\u044C322300\u04362220\u04362", s, "ab2b0b\u044C322300\u04362220\u04362");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_97(string s) => ValidateEquals(s == "1133\u0419323223\u043631002123", s, "1133\u0419323223\u043631002123");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_98(string s) => ValidateEquals(s == "233\u04360b3\u0419023\u0419\u044Caa\u04363321", s, "233\u04360b3\u0419023\u0419\u044Caa\u04363321");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_99(string s) => ValidateEquals(s == "3\u041911b313323230a02\u041930", s, "3\u041911b313323230a02\u041930");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_100(string s) => ValidateEquals(s == "1\u04362\u0419\u04360131a2\u04362a\u0419\u04193\u044Cb11", s, "1\u04362\u0419\u04360131a2\u04362a\u0419\u04193\u044Cb11");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_101(string s) => ValidateEquals(s == "\u041913303ba3\u044C\u043631a1102222", s, "\u041913303ba3\u044C\u043631a1102222");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_102(string s) => ValidateEquals(s == "32331221\u044C3\u044Cb103212132", s, "32331221\u044C3\u044Cb103212132");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_103(string s) => ValidateEquals(s == "133\u04190332210231331100\u0419", s, "133\u04190332210231331100\u0419");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_104(string s) => ValidateEquals(s == "22221322\u04191133bb0\u04193222", s, "22221322\u04191133bb0\u04193222");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_105(string s) => ValidateEquals(s == "12b011\u04363a1\u04363\u0419\u0419a12\u04190\u044C3\u044C", s, "12b011\u04363a1\u04363\u0419\u0419a12\u04190\u044C3\u044C");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_106(string s) => ValidateEquals(s == "0333\u044C12113\u044C11331\u0436323\u0419\u0436", s, "0333\u044C12113\u044C11331\u0436323\u0419\u0436");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_107(string s) => ValidateEquals(s == "0\u041913a310\u044C12\02\u044C\02320331", s, "0\u041913a310\u044C12\02\u044C\02320331");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_108(string s) => ValidateEquals(s == "022b2\u044C\u04360302b33\u041921\u04361112", s, "022b2\u044C\u04360302b33\u041921\u04361112");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_109(string s) => ValidateEquals(s == "3322\u04362133133b3032\u0419aa12", s, "3322\u04362133133b3032\u0419aa12");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_110(string s) => ValidateEquals(s == "\u0419132\u0419a\u044Cb33a3\u041933\u0419b21a2b2", s, "\u0419132\u0419a\u044Cb33a3\u041933\u0419b21a2b2");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_111(string s) => ValidateEquals(s == "31102113\u041911\u0436b31b\u041912b133", s, "31102113\u041911\u0436b31b\u041912b133");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_112(string s) => ValidateEquals(s == "\u0419\u044C\u0419\u04190\u041903a\023\u044C3311\u044C\u04191323", s, "\u0419\u044C\u0419\u04190\u041903a\023\u044C3311\u044C\u04191323");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_113(string s) => ValidateEquals(s == "212323\u0436a23203bb00\u0436a12\u04363", s, "212323\u0436a23203bb00\u0436a12\u04363");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_114(string s) => ValidateEquals(s == "\u0436\u041931130\u043632322313010aa13", s, "\u0436\u041931130\u043632322313010aa13");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_115(string s) => ValidateEquals(s == "123a\u04362221\022\u043622\u0419021b\u0419\u04190\u0419", s, "123a\u04362221\022\u043622\u0419021b\u0419\u04190\u0419");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_116(string s) => ValidateEquals(s == "211131\u04362213303b1b0231a11", s, "211131\u04362213303b1b0231a11");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_117(string s) => ValidateEquals(s == "\u044C1a\u0419\u0436\u044C0110\u04192b220\u0436\u04363\u044C\u04363\u04361", s, "\u044C1a\u0419\u0436\u044C0110\u04192b220\u0436\u04363\u044C\u04363\u04361");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_118(string s) => ValidateEquals(s == "3\u0436ab2221133331311\023\u0419\u04193\u0436", s, "3\u0436ab2221133331311\023\u0419\u04193\u0436");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_119(string s) => ValidateEquals(s == "21\u041920\02\u044C\u044C333\u044Cb332223\u0419\u04361\u0419", s, "21\u041920\02\u044C\u044C333\u044Cb332223\u0419\u04361\u0419");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_120(string s) => ValidateEquals(s == "1\u04192120a01110\u04191121003a3b33", s, "1\u04192120a01110\u04191121003a3b33");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_121(string s) => ValidateEquals(s == "3021a1\u04191aa1111b22\u0419112\u0419201", s, "3021a1\u04191aa1111b22\u0419112\u0419201");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_122(string s) => ValidateEquals(s == "2b21\u044Ca\u044Cb\023\u041933301\u04193123\u0419\u04361", s, "2b21\u044Ca\u044Cb\023\u041933301\u04193123\u0419\u04361");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_123(string s) => ValidateEquals(s == "2\u04361ba\u0419\u04191a\021\u043623323\u0436b\u0436331\u0436", s, "2\u04361ba\u0419\u04191a\021\u043623323\u0436b\u0436331\u0436");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_124(string s) => ValidateEquals(s == "bab12332\u043631130\u04193230\u044C1011a", s, "bab12332\u043631130\u04193230\u044C1011a");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_125(string s) => ValidateEquals(s == "110a\u044C31\u043633\u044C\u044C33333a2b32\u044C12\u044C", s, "110a\u044C31\u043633\u044C\u044C33333a2b32\u044C12\u044C");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_126(string s) => ValidateEquals(s == "a2\u0436\u041911\u044C1b\u044C312a11a\u044Ca\u044Cb02\u0419b0", s, "a2\u0436\u041911\u044C1b\u044C312a11a\u044Ca\u044Cb02\u0419b0");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_127(string s) => ValidateEquals(s == "32b2\u043612a32a3\u043623\u04361\u044C\u0419bb22213", s, "32b2\u043612a32a3\u043623\u04361\u044C\u0419bb22213");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_128(string s) => ValidateEquals(s == "30\u0436111\u044C11120\u0436\u0436b10212\u0436b\u044C33\u0419", s, "30\u0436111\u044C11120\u0436\u0436b10212\u0436b\u044C33\u0419");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_129(string s) => ValidateEquals(s == "33b1311\u04361\023b\u0436020\u041910b0302\u0436", s, "33b1311\u04361\023b\u0436020\u041910b0302\u0436");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_130(string s) => ValidateEquals(s == "b3122\u0436a12\021123a3130100113\u044C", s, "b3122\u0436a12\021123a3130100113\u044C");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_131(string s) => ValidateEquals(s == "302a1\u0436322\021221\02a1331b2\u0436\u044C1", s, "302a1\u0436322\021221\02a1331b2\u0436\u044C1");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_132(string s) => ValidateEquals(s == "31201322\u0436\u0436\0221\u0419\021\u0419\u044C\u044C32\u041911\u0436", s, "31201322\u0436\u0436\0221\u0419\021\u0419\u044C\u044C32\u041911\u0436");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_133(string s) => ValidateEquals(s == "3b\u0436a132a13ba1311\u04361\u041922\u0419b\u0419a33", s, "3b\u0436a132a13ba1311\u04361\u041922\u0419b\u0419a33");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_134(string s) => ValidateEquals(s == "b33\u0419113\u0419\u0419ab1b332211222\u041932\02", s, "b33\u0419113\u0419\u0419ab1b332211222\u041932\02");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_135(string s) => ValidateEquals(s == "0\u0436333b31b212121b1a\u043602\u0436133111", s, "0\u0436333b31b212121b1a\u043602\u0436133111");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_136(string s) => ValidateEquals(s == "0101\u0419220\u04190\u0436\u04193\u04192abba0b1223aab", s, "0101\u0419220\u04190\u0436\u04193\u04192abba0b1223aab");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_137(string s) => ValidateEquals(s == "2\u0419330b\u0419123\u04362\u043602\u0436212\u044C112111\u04191", s, "2\u0419330b\u0419123\u04362\u043602\u0436212\u044C112111\u04191");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_138(string s) => ValidateEquals(s == "22a\0212a\u044C3b1303\u04193bb2b313\u0419222", s, "22a\0212a\u044C3b1303\u04193bb2b313\u0419222");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_139(string s) => ValidateEquals(s == "2\u044C\u0436133332102222\020\u0419\u0436bb2\022\02", s, "2\u044C\u0436133332102222\020\u0419\u0436bb2\022\02");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_140(string s) => ValidateEquals(s == "2\02321b31123231b2\u0419\u0419122ab\u04192131", s, "2\02321b31123231b2\u0419\u0419122ab\u04192131");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_141(string s) => ValidateEquals(s == "1b021\u044C\u041930a2332\u044C3\u041912231\u0436\u04361a\u0436\u044C1", s, "1b021\u044C\u041930a2332\u044C3\u041912231\u0436\u04361a\u0436\u044C1");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_142(string s) => ValidateEquals(s == "1\u04363\u044C\u044C3\u044C1\u044C1\u044C0\u04361\u0419122132a2\u044C\u044Ca\u04193b", s, "1\u04363\u044C\u044C3\u044C1\u044C1\u044C0\u04361\u0419122132a2\u044C\u044Ca\u04193b");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_143(string s) => ValidateEquals(s == "21bbb31301\u044C3\u0436aa\u04360\u04193323b33\u044C1\u044C1", s, "21bbb31301\u044C3\u0436aa\u04360\u04193323b33\u044C1\u044C1");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_144(string s) => ValidateEquals(s == "a00\u0419\u044C11\u0436\u0436aa321\u044C\u04191\u041931\u0436a21\u04363223", s, "a00\u0419\u044C11\u0436\u0436aa321\u044C\u04191\u041931\u0436a21\u04363223");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_145(string s) => ValidateEquals(s == "3132b0\u0419b3110ab\0201\u04191\u043632222a33\u0436", s, "3132b0\u0419b3110ab\0201\u04191\u043632222a33\u0436");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_146(string s) => ValidateEquals(s == "32b110bb312\u044C02\u04191b2\u041923232\u041912\u044C33", s, "32b110bb312\u044C02\u04191b2\u041923232\u041912\u044C33");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_147(string s) => ValidateEquals(s == "\u0436121bbb\u04192b1\u043612222\u0419\u044C1\u0419b02013\u0436\u044C1", s, "\u0436121bbb\u04192b1\u043612222\u0419\u044C1\u0419b02013\u0436\u044C1");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_148(string s) => ValidateEquals(s == "\u044C1b00a3310231001b1a1\u044C33\u0436\u0436b130\u044C", s, "\u044C1b00a3310231001b1a1\u044C33\u0436\u0436b130\u044C");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_149(string s) => ValidateEquals(s == "\u04363b211b121\u043623b\u044C12a1\u04192\u0419\u043612313a\u0436", s, "\u04363b211b121\u043623b\u044C12a1\u04192\u0419\u043612313a\u0436");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_150(string s) => ValidateEquals(s == "1a3\u0436b31311322\u0436\u044C33213\u04193\u044C13330\u0436a3", s, "1a3\u0436b31311322\u0436\u044C33213\u04193\u044C13330\u0436a3");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_151(string s) => ValidateEquals(s == "b33\u0419b\u04363333233101a33\u04363b231221\u044C11", s, "b33\u0419b\u04363333233101a33\u04363b231221\u044C11");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_152(string s) => ValidateEquals(s == "1\u0419212\u04193\u0436112a31a\u044C\u0436\u044C\u041932\u0436233a32\u04191\u0436", s, "1\u0419212\u04193\u0436112a31a\u044C\u0436\u044C\u041932\u0436233a32\u04191\u0436");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_153(string s) => ValidateEquals(s == "133\u044C02a\u044Ca0\u04193\u0419ab3\u044C1\u04193\u04192a21121210", s, "133\u044C02a\u044Ca0\u04193\u0419ab3\u044C1\u04193\u04192a21121210");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_154(string s) => ValidateEquals(s == "1320ba\u043631b3\u04192\u04191322b113212212331", s, "1320ba\u043631b3\u04192\u04191322b113212212331");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_155(string s) => ValidateEquals(s == "1\u0419a332132\u0436b31\u041933\u041932321\u043631b120\u043603", s, "1\u0419a332132\u0436b31\u041933\u041932321\u043631b120\u043603");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_156(string s) => ValidateEquals(s == "213321a1b3\u0436\u044C3111\u0436\u04192b2\u04193101221\u044C33", s, "213321a1b3\u0436\u044C3111\u0436\u04192b2\u04193101221\u044C33");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_157(string s) => ValidateEquals(s == "2\u04361311a23b2212a\u041921\u041911\u0436b3233bb3a1", s, "2\u04361311a23b2212a\u041921\u041911\u0436b3233bb3a1");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_158(string s) => ValidateEquals(s == "01\u041911113\u044C3\u041932a3\u044C\u0419\u04193\u041932b2ab221310", s, "01\u041911113\u044C3\u041932a3\u044C\u0419\u04193\u041932b2ab221310");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_159(string s) => ValidateEquals(s == "a120213b11211\0223223312\u044C\u044C1\u04193222\u0419", s, "a120213b11211\0223223312\u044C\u044C1\u04193222\u0419");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_160(string s) => ValidateEquals(s == "\u9244", s, "\u9244");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_161(string s) => ValidateEquals(s == "\u9244\u9244", s, "\u9244\u9244");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_162(string s) => ValidateEquals(s == "\u9244\u9244\u9244", s, "\u9244\u9244\u9244");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_163(string s) => ValidateEquals(s == "\uFFFF", s, "\uFFFF");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_164(string s) => ValidateEquals(s == "\uFFFF\uFFFF", s, "\uFFFF\uFFFF");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_165(string s) => ValidateEquals(s == "\uFFFF\uFFFF\uFFFF", s, "\uFFFF\uFFFF\uFFFF");

    public static readonly string[] s_TestData =
    {
        null,
        "\u9244",
        "\u9244\u9244",
        "\u9244\u9244\u9244",
        "\uFFFF",
        "\uFFFF\uFFFF",
        "\uFFFF\uFFFF\uFFFF",
        "",
        "\0",
        "a",
        "0",
        "\u0436",
        "3",
        "33",
        "31",
        "a\0",
        "12",
        "13",
        "b12",
        "\u043623",
        "2a2",
        "222",
        "0\u044C3",
        "b\u043631",
        "\u044C\u0419b\u0419",
        "b033",
        "311\u044C",
        "\u0436\u041912",
        "2011b",
        "222b2",
        "a\u0419213",
        "1a131",
        "3232\u0419",
        "3b0\u044C\u0436\u044C",
        "213b2\u0419",
        "b31210",
        "1\u04360021",
        "3\u044C3112",
        "122b231",
        "03\u043632\u04363",
        "bb31\u04362\u0419",
        "023b\u044C\u0436\u0419",
        "\0232a12",
        "\u043613\u044C11\u0419\u044C",
        "11\u044Cbb32\u044C",
        "222\u0419\u04363\u04363",
        "\u0436303a\u041912",
        "\u044Cb22322b",
        "a22b10b1\u0419",
        "3ba2221\u044C3",
        "\u0436a1\u04190b1\u04191",
        "a20\u0419\u0436\u04361\u044C\u044C",
        "\u044Ca\u043632132\u044C",
        "11111\u04193\u041912",
        "11\u0419\02\u0419b3\u0436\u0436",
        "21b\u0436\u0436\u04360103",
        "333332a\u041911",
        "\u0419123112313",
        "12\u0419\u044C\u0419a\u041911\u044Cb",
        "\u0436\u043622221\u04193\u04192",
        "\u044C\u04191bb\u04363202\u0436",
        "1bb\u04192\u041933\u04192\u0436",
        "2013133\u044C1b\u0436",
        "23a2\02\u0436a2a13",
        "23\u0419210\u04193a3\u04361",
        "32\u04192133bb2\u04193",
        "\u04193bb1\u044C3bb\u044Cb3",
        "a0\u0419bab\u04362\u0419133",
        "320\u0436a22a11\u04361b",
        "\u044C321b3\u044C\u0419\u041913\u04192",
        "a3\u044C1\u04362a\022a1a",
        "3\u0419b30b33231b\u044C",
        "2210121\u043613231",
        "013311aa3203\u04191",
        "12\u0419\u04191\u04192a\u04192\u044Cb\u0419a",
        "2b1\u041911130221b\u044C",
        "230110\u04190b3112\u0436",
        "a213\u044Cab121b332",
        "111a01\u04363121b123",
        "13a322\u04192\u04193b\u0436b0\u0419",
        "\021232b1\u0419aa1032",
        "\u0436\u0419112\u044Cb12\u0419\u044C3b2\u0436",
        "2b\u044C\u044C331bb\023122",
        "a\u043622\u04192203b023b\u044C3",
        "a\u0419033\u04363a220\u044C3331",
        "20\u0419\u0436a1b1313\u0436\u0419b2a",
        "131\u04191\022\u04362322123",
        "23323b21\u044C11b\u0419321",
        "302a\u044C\u0436a3213\u0436a\u04193\u0419\u0436",
        "\u043613b00210b1212102",
        "20320\u04193\u04193\u044C\u04363\u04192122",
        "0bb23a30ba\u0419b2333\u044C",
        "22122\u044C130230103a2",
        "\u044C\u044Cba20\u04361\u0436\u044C\u0419\u044Cb\u041931b\u0436",
        "bb1\u044C1033b\u04363011b\u043610",
        "1\u044C320a3a22b3333b13",
        "0a22a\u0419\u0436a2222\u043623\u041913",
        "\u041911\u0419213212\u04361233b23",
        "32\u044C1\u041903123\u044C011332ab",
        "222\u04362311b133b3\u04363223",
        "0111\u044C3002222a3aaaa3",
        "313\u0419213a\u043601a12231a2",
        "1\u0436022\u044C1323b3b3\u0436222\u044C",
        "\u044C023a3b213\u044C033\u043613231",
        "ab2b0b\u044C322300\u04362220\u04362",
        "1133\u0419323223\u043631002123",
        "233\u04360b3\u0419023\u0419\u044Caa\u04363321",
        "3\u041911b313323230a02\u041930",
        "1\u04362\u0419\u04360131a2\u04362a\u0419\u04193\u044Cb11",
        "\u041913303ba3\u044C\u043631a1102222",
        "32331221\u044C3\u044Cb103212132",
        "133\u04190332210231331100\u0419",
        "22221322\u04191133bb0\u04193222",
        "12b011\u04363a1\u04363\u0419\u0419a12\u04190\u044C3\u044C",
        "0333\u044C12113\u044C11331\u0436323\u0419\u0436",
        "0\u041913a310\u044C12\02\u044C\02\02320331",
        "022b2\u044C\u04360302b33\u041921\u04361112",
        "3322\u04362133133b3032\u0419aa12",
        "\u0419132\u0419a\u044Cb33a3\u041933\u0419b21a2b2",
        "31102113\u041911\u0436b31b\u041912b133",
        "\u0419\u044C\u0419\u04190\u041903a\023\u044C3311\u044C\u04191323",
        "212323\u0436a23203bb00\u0436a12\u04363",
        "\u0436\u041931130\u043632322313010aa13",
        "123a\u04362221\022\u043622\u0419021b\u0419\u04190\u0419",
        "211131\u04362213303b1b0231a11",
        "\u044C1a\u0419\u0436\u044C0110\u04192b220\u0436\u04363\u044C\u04363\u04361",
        "3\u0436ab2221133331311\023\u0419\u04193\u0436",
        "21\u041920\02\u044C\u044C333\u044Cb332223\u0419\u04361\u0419",
        "1\u04192120a01110\u04191121003a3b33",
        "3021a1\u04191aa1111b22\u0419112\u0419201",
        "2b21\u044Ca\u044Cb\023\u041933301\u04193123\u0419\u04361",
        "2\u04361ba\u0419\u04191a\021\u043623323\u0436b\u0436331\u0436",
        "bab12332\u043631130\u04193230\u044C1011a",
        "110a\u044C31\u043633\u044C\u044C33333a2b32\u044C12\u044C",
        "a2\u0436\u041911\u044C1b\u044C312a11a\u044Ca\u044Cb02\u0419b0",
        "32b2\u043612a32a3\u043623\u04361\u044C\u0419bb22213",
        "30\u0436111\u044C11120\u0436\u0436b10212\u0436b\u044C33\u0419",
        "33b1311\u04361\023b\u0436020\u041910b0302\u0436",
        "b3122\u0436a12\021123a3130100113\u044C",
        "302a1\u0436322\021221z2a1331b2\u0436\u044C1",
        "31201322\u0436\u0436\0221\u0419z21\u0419\u044C\u044C32\u041911\u0436",
        "3b\u0436a132a13ba1311\u04361\u041922\u0419b\u0419a33",
        "b33\u0419113\u0419\u0419ab1b332211222\u041932\02",
        "0\u0436333b31b212121b1a\u043602\u0436133111",
        "0101\u0419220\u04190\u0436\u04193\u04192abba0b1223aa\0",
        "2\u0419330b\u0419123\u04362\u043602\u0436212\u044C112111\u04191",
        "22a\0212a\u044C3b1303\u04193bb2b313\u0419222",
        "2\u044C\u0436133332102222\020\u0419\u0436bb2\022\02",
        "2\02321b31123231b2\u0419\u0419122ab\u04192131",
        "1b021\u044C\u041930a2332\u044C3\u041912231\u0436\u04361a\u0436\u044C1",
        "1\u04363\u044C\u044C3\u044C1\u044C1\u044C0\u04361\u0419122132a2\u044C\u044Ca\u04193b",
        "21bbb31301\u044C3\u0436aa\u04360\u04193323b33\u044C1\u044C1",
        "a00\u0419\u044C11\u0436\u0436aa321\u044C\u04191\u041931\u0436a21\u04363223",
        "3132b0\u0419b3110ab\0201\u04191\u043632222a33\u0436",
        "32b110bb312\u044C02\u04191b2\u041923232\u041912\u044C33",
        "\u0436121bbb\u04192b1\u043612222\u0419\u044C1\u0419b02013\u0436\u044C1",
        "\u044C1b00a3310231001b1a1\u044C33\u0436\u0436b130\u044C",
        "\u04363b211b121\u043623b\u044C12a1\u04192\u0419\u043612313a\u0436",
        "1a3\u0436b31311322\u0436\u044C33213\u04193\u044C13330\u0436a3",
        "b33\u0419b\u04363333233101a33\u04363b231221\u044C11",
        "1\u0419212\u04193\u0436112a31a\u044C\u0436\u044C\u041932\u0436233a32\u04191\u0436",
        "133\u044C02a\u044Ca0\u04193\u0419ab3\u044C1\u04193\u04192a21121210",
        "1320ba\u043631b3\u04192\u04191322b113212212331",
        "1\u0419a332132\u0436b31\u041933\u041932321\u043631b120\u043603",
        "213321a1b3\u0436\u044C3111\u0436\u04192b2\u04193101221\u044C33",
        "2\u04361311a23b2212a\u041921\u041911\u0436b3233bb3a1",
        "01\u041911113\u044C3\u041932a3\u044C\u0419\u04193\u041932b2ab221310",
        "a120213b11211\0223223312\u044C\u044C1\u04193222\u0419",
    };
}
