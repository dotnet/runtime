// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public class StringEquals
{
    public static int Main()
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
        return testCount == 25920 ? 100 : 0;
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
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_3(string s) => ValidateEquals(s == "ж", s, "ж");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_4(string s) => ValidateEquals(s == "1", s, "1");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_5(string s) => ValidateEquals(s == "33", s, "33");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_6(string s) => ValidateEquals(s == "31", s, "31");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_7(string s) => ValidateEquals(s == "a1", s, "a1");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_8(string s) => ValidateEquals(s == "12", s, "12");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_9(string s) => ValidateEquals(s == "1\0", s, "1\0");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_10(string s) => ValidateEquals(s == "b12", s, "b12");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_11(string s) => ValidateEquals(s == "ж23", s, "ж23");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_12(string s) => ValidateEquals(s == "2a2", s, "2a2");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_13(string s) => ValidateEquals(s == "222", s, "222");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_14(string s) => ValidateEquals(s == "0ь3", s, "0ь3");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_15(string s) => ValidateEquals(s == "bж31", s, "bж31");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_16(string s) => ValidateEquals(s == "ьЙbЙ", s, "ьЙbЙ");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_17(string s) => ValidateEquals(s == "b033", s, "b033");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_18(string s) => ValidateEquals(s == "311ь", s, "311ь");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_19(string s) => ValidateEquals(s == "жЙ12", s, "жЙ12");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_20(string s) => ValidateEquals(s == "2011b", s, "2011b");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_21(string s) => ValidateEquals(s == "222b2", s, "222b2");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_22(string s) => ValidateEquals(s == "aЙ213", s, "aЙ213");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_23(string s) => ValidateEquals(s == "1a131", s, "1a131");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_24(string s) => ValidateEquals(s == "3232Й", s, "3232Й");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_25(string s) => ValidateEquals(s == "3b0ьжь", s, "3b0ьжь");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_26(string s) => ValidateEquals(s == "213b2Й", s, "213b2Й");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_27(string s) => ValidateEquals(s == "b31210", s, "b31210");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_28(string s) => ValidateEquals(s == "1ж0021", s, "1ж0021");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_29(string s) => ValidateEquals(s == "3ь3112", s, "3ь3112");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_30(string s) => ValidateEquals(s == "122b231", s, "122b231");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_31(string s) => ValidateEquals(s == "03ж32ж3", s, "03ж32ж3");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_32(string s) => ValidateEquals(s == "bb31ж2Й", s, "bb31ж2Й");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_33(string s) => ValidateEquals(s == "023bьжЙ", s, "023bьжЙ");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_34(string s) => ValidateEquals(s == "\0232a12", s, "\0232a12");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_35(string s) => ValidateEquals(s == "ж13ь11Йь", s, "ж13ь11Йь");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_36(string s) => ValidateEquals(s == "11ьbb32ь", s, "11ьbb32ь");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_37(string s) => ValidateEquals(s == "222Йж3ж3", s, "222Йж3ж3");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_38(string s) => ValidateEquals(s == "ж303aЙ12", s, "ж303aЙ12");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_39(string s) => ValidateEquals(s == "ьb22322b", s, "ьb22322b");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_40(string s) => ValidateEquals(s == "a22b10b1Й", s, "a22b10b1Й");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_41(string s) => ValidateEquals(s == "3ba2221ь3", s, "3ba2221ь3");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_42(string s) => ValidateEquals(s == "жa1Й0b1Й1", s, "жa1Й0b1Й1");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_43(string s) => ValidateEquals(s == "a20Йжж1ьь", s, "a20Йжж1ьь");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_44(string s) => ValidateEquals(s == "ьaж32132ь", s, "ьaж32132ь");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_45(string s) => ValidateEquals(s == "11111Й3Й12", s, "11111Й3Й12");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_46(string s) => ValidateEquals(s == "11Й\02Йb3жж", s, "11Й\02Йb3жж");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_47(string s) => ValidateEquals(s == "21bжжж0103", s, "21bжжж0103");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_48(string s) => ValidateEquals(s == "333332aЙ11", s, "333332aЙ11");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_49(string s) => ValidateEquals(s == "Й123112313", s, "Й123112313");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_50(string s) => ValidateEquals(s == "12ЙьЙaЙ11ьb", s, "12ЙьЙaЙ11ьb");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_51(string s) => ValidateEquals(s == "жж22221Й3Й2", s, "жж22221Й3Й2");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_52(string s) => ValidateEquals(s == "ьЙ1bbж3202ж", s, "ьЙ1bbж3202ж");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_53(string s) => ValidateEquals(s == "1bbЙ2Й33Й2ж", s, "1bbЙ2Й33Й2ж");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_54(string s) => ValidateEquals(s == "2013133ь1bж", s, "2013133ь1bж");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_55(string s) => ValidateEquals(s == "23a2\02жa2a13", s, "23a2\02жa2a13");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_56(string s) => ValidateEquals(s == "23Й210Й3a3ж1", s, "23Й210Й3a3ж1");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_57(string s) => ValidateEquals(s == "32Й2133bb2Й3", s, "32Й2133bb2Й3");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_58(string s) => ValidateEquals(s == "Й3bb1ь3bbьb3", s, "Й3bb1ь3bbьb3");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_59(string s) => ValidateEquals(s == "a0Йbabж2Й133", s, "a0Йbabж2Й133");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_60(string s) => ValidateEquals(s == "320жa22a11ж1b", s, "320жa22a11ж1b");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_61(string s) => ValidateEquals(s == "ь321b3ьЙЙ13Й2", s, "ь321b3ьЙЙ13Й2");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_62(string s) => ValidateEquals(s == "a3ь1ж2a\022a1a", s, "a3ь1ж2a\022a1a");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_63(string s) => ValidateEquals(s == "3Йb30b33231bь", s, "3Йb30b33231bь");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_64(string s) => ValidateEquals(s == "2210121ж13231", s, "2210121ж13231");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_65(string s) => ValidateEquals(s == "013311aa3203Й1", s, "013311aa3203Й1");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_66(string s) => ValidateEquals(s == "12ЙЙ1Й2aЙ2ьbЙa", s, "12ЙЙ1Й2aЙ2ьbЙa");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_67(string s) => ValidateEquals(s == "2b1Й11130221bь", s, "2b1Й11130221bь");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_68(string s) => ValidateEquals(s == "230110Й0b3112ж", s, "230110Й0b3112ж");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_69(string s) => ValidateEquals(s == "a213ьab121b332", s, "a213ьab121b332");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_70(string s) => ValidateEquals(s == "111a01ж3121b123", s, "111a01ж3121b123");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_71(string s) => ValidateEquals(s == "13a322Й2Й3bжb0Й", s, "13a322Й2Й3bжb0Й");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_72(string s) => ValidateEquals(s == "\021232b1Йaa1032", s, "\021232b1Йaa1032");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_73(string s) => ValidateEquals(s == "жЙ112ьb12Йь3b2ж", s, "жЙ112ьb12Йь3b2ж");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_74(string s) => ValidateEquals(s == "2bьь331bb\023122", s, "2bьь331bb\023122");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_75(string s) => ValidateEquals(s == "aж22Й2203b023bь3", s, "aж22Й2203b023bь3");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_76(string s) => ValidateEquals(s == "aЙ033ж3a220ь3331", s, "aЙ033ж3a220ь3331");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_77(string s) => ValidateEquals(s == "20Йжa1b1313жЙb2a", s, "20Йжa1b1313жЙb2a");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_78(string s) => ValidateEquals(s == "131Й1\022ж2322123", s, "131Й1\022ж2322123");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_79(string s) => ValidateEquals(s == "23323b21ь11bЙ321", s, "23323b21ь11bЙ321");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_80(string s) => ValidateEquals(s == "302aьжa3213жaЙ3Йж", s, "302aьжa3213жaЙ3Йж");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_81(string s) => ValidateEquals(s == "ж13b00210b1212102", s, "ж13b00210b1212102");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_82(string s) => ValidateEquals(s == "20320Й3Й3ьж3Й2122", s, "20320Й3Й3ьж3Й2122");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_83(string s) => ValidateEquals(s == "0bb23a30baЙb2333ь", s, "0bb23a30baЙb2333ь");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_84(string s) => ValidateEquals(s == "22122ь130230103a2", s, "22122ь130230103a2");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_85(string s) => ValidateEquals(s == "ььba20ж1жьЙьbЙ31bж", s, "ььba20ж1жьЙьbЙ31bж");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_86(string s) => ValidateEquals(s == "bb1ь1033bж3011bж10", s, "bb1ь1033bж3011bж10");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_87(string s) => ValidateEquals(s == "1ь320a3a22b3333b13", s, "1ь320a3a22b3333b13");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_88(string s) => ValidateEquals(s == "0a22aЙжa2222ж23Й13", s, "0a22aЙжa2222ж23Й13");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_89(string s) => ValidateEquals(s == "Й11Й213212ж1233b23", s, "Й11Й213212ж1233b23");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_90(string s) => ValidateEquals(s == "32ь1Й03123ь011332ab", s, "32ь1Й03123ь011332ab");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_91(string s) => ValidateEquals(s == "222ж2311b133b3ж3223", s, "222ж2311b133b3ж3223");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_92(string s) => ValidateEquals(s == "0111ь3002222a3aaaa3", s, "0111ь3002222a3aaaa3");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_93(string s) => ValidateEquals(s == "313Й213aж01a12231a2", s, "313Й213aж01a12231a2");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_94(string s) => ValidateEquals(s == "1ж022ь1323b3b3ж222ь", s, "1ж022ь1323b3b3ж222ь");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_95(string s) => ValidateEquals(s == "ь023a3b213ь033ж13231", s, "ь023a3b213ь033ж13231");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_96(string s) => ValidateEquals(s == "ab2b0bь322300ж2220ж2", s, "ab2b0bь322300ж2220ж2");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_97(string s) => ValidateEquals(s == "1133Й323223ж31002123", s, "1133Й323223ж31002123");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_98(string s) => ValidateEquals(s == "233ж0b3Й023Йьaaж3321", s, "233ж0b3Й023Йьaaж3321");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_99(string s) => ValidateEquals(s == "3Й11b313323230a02Й30", s, "3Й11b313323230a02Й30");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_100(string s) => ValidateEquals(s == "1ж2Йж0131a2ж2aЙЙ3ьb11", s, "1ж2Йж0131a2ж2aЙЙ3ьb11");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_101(string s) => ValidateEquals(s == "Й13303ba3ьж31a1102222", s, "Й13303ba3ьж31a1102222");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_102(string s) => ValidateEquals(s == "32331221ь3ьb103212132", s, "32331221ь3ьb103212132");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_103(string s) => ValidateEquals(s == "133Й0332210231331100Й", s, "133Й0332210231331100Й");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_104(string s) => ValidateEquals(s == "22221322Й1133bb0Й3222", s, "22221322Й1133bb0Й3222");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_105(string s) => ValidateEquals(s == "12b011ж3a1ж3ЙЙa12Й0ь3ь", s, "12b011ж3a1ж3ЙЙa12Й0ь3ь");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_106(string s) => ValidateEquals(s == "0333ь12113ь11331ж323Йж", s, "0333ь12113ь11331ж323Йж");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_107(string s) => ValidateEquals(s == "0Й13a310ь12\02ь\02320331", s, "0Й13a310ь12\02ь\02320331");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_108(string s) => ValidateEquals(s == "022b2ьж0302b33Й21ж1112", s, "022b2ьж0302b33Й21ж1112");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_109(string s) => ValidateEquals(s == "3322ж2133133b3032Йaa12", s, "3322ж2133133b3032Йaa12");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_110(string s) => ValidateEquals(s == "Й132Йaьb33a3Й33Йb21a2b2", s, "Й132Йaьb33a3Й33Йb21a2b2");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_111(string s) => ValidateEquals(s == "31102113Й11жb31bЙ12b133", s, "31102113Й11жb31bЙ12b133");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_112(string s) => ValidateEquals(s == "ЙьЙЙ0Й03a\023ь3311ьЙ1323", s, "ЙьЙЙ0Й03a\023ь3311ьЙ1323");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_113(string s) => ValidateEquals(s == "212323жa23203bb00жa12ж3", s, "212323жa23203bb00жa12ж3");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_114(string s) => ValidateEquals(s == "жЙ31130ж32322313010aa13", s, "жЙ31130ж32322313010aa13");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_115(string s) => ValidateEquals(s == "123aж2221\022ж22Й021bЙЙ0Й", s, "123aж2221\022ж22Й021bЙЙ0Й");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_116(string s) => ValidateEquals(s == "211131ж2213303b1b0231a11", s, "211131ж2213303b1b0231a11");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_117(string s) => ValidateEquals(s == "ь1aЙжь0110Й2b220жж3ьж3ж1", s, "ь1aЙжь0110Й2b220жж3ьж3ж1");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_118(string s) => ValidateEquals(s == "3жab2221133331311\023ЙЙ3ж", s, "3жab2221133331311\023ЙЙ3ж");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_119(string s) => ValidateEquals(s == "21Й20\02ьь333ьb332223Йж1Й", s, "21Й20\02ьь333ьb332223Йж1Й");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_120(string s) => ValidateEquals(s == "1Й2120a01110Й1121003a3b33", s, "1Й2120a01110Й1121003a3b33");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_121(string s) => ValidateEquals(s == "3021a1Й1aa1111b22Й112Й201", s, "3021a1Й1aa1111b22Й112Й201");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_122(string s) => ValidateEquals(s == "2b21ьaьb\023Й33301Й3123Йж1", s, "2b21ьaьb\023Й33301Й3123Йж1");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_123(string s) => ValidateEquals(s == "2ж1baЙЙ1a\021ж23323жbж331ж", s, "2ж1baЙЙ1a\021ж23323жbж331ж");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_124(string s) => ValidateEquals(s == "bab12332ж31130Й3230ь1011a", s, "bab12332ж31130Й3230ь1011a");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_125(string s) => ValidateEquals(s == "110aь31ж33ьь33333a2b32ь12ь", s, "110aь31ж33ьь33333a2b32ь12ь");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_126(string s) => ValidateEquals(s == "a2жЙ11ь1bь312a11aьaьb02Йb0", s, "a2жЙ11ь1bь312a11aьaьb02Йb0");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_127(string s) => ValidateEquals(s == "32b2ж12a32a3ж23ж1ьЙbb22213", s, "32b2ж12a32a3ж23ж1ьЙbb22213");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_128(string s) => ValidateEquals(s == "30ж111ь11120жжb10212жbь33Й", s, "30ж111ь11120жжb10212жbь33Й");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_129(string s) => ValidateEquals(s == "33b1311ж1\023bж020Й10b0302ж", s, "33b1311ж1\023bж020Й10b0302ж");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_130(string s) => ValidateEquals(s == "b3122жa12\021123a3130100113ь", s, "b3122жa12\021123a3130100113ь");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_131(string s) => ValidateEquals(s == "302a1ж322\021221\02a1331b2жь1", s, "302a1ж322\021221\02a1331b2жь1");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_132(string s) => ValidateEquals(s == "31201322жж\0221Й\021Йьь32Й11ж", s, "31201322жж\0221Й\021Йьь32Й11ж");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_133(string s) => ValidateEquals(s == "3bжa132a13ba1311ж1Й22ЙbЙa33", s, "3bжa132a13ba1311ж1Й22ЙbЙa33");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_134(string s) => ValidateEquals(s == "b33Й113ЙЙab1b332211222Й32\02", s, "b33Й113ЙЙab1b332211222Й32\02");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_135(string s) => ValidateEquals(s == "0ж333b31b212121b1aж02ж133111", s, "0ж333b31b212121b1aж02ж133111");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_136(string s) => ValidateEquals(s == "0101Й220Й0жЙ3Й2abba0b1223aab", s, "0101Й220Й0жЙ3Й2abba0b1223aab");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_137(string s) => ValidateEquals(s == "2Й330bЙ123ж2ж02ж212ь112111Й1", s, "2Й330bЙ123ж2ж02ж212ь112111Й1");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_138(string s) => ValidateEquals(s == "22a\0212aь3b1303Й3bb2b313Й222", s, "22a\0212aь3b1303Й3bb2b313Й222");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_139(string s) => ValidateEquals(s == "2ьж133332102222\020Йжbb2\022\02", s, "2ьж133332102222\020Йжbb2\022\02");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_140(string s) => ValidateEquals(s == "2\02321b31123231b2ЙЙ122abЙ2131", s, "2\02321b31123231b2ЙЙ122abЙ2131");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_141(string s) => ValidateEquals(s == "1b021ьЙ30a2332ь3Й12231жж1aжь1", s, "1b021ьЙ30a2332ь3Й12231жж1aжь1");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_142(string s) => ValidateEquals(s == "1ж3ьь3ь1ь1ь0ж1Й122132a2ььaЙ3b", s, "1ж3ьь3ь1ь1ь0ж1Й122132a2ььaЙ3b");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_143(string s) => ValidateEquals(s == "21bbb31301ь3жaaж0Й3323b33ь1ь1", s, "21bbb31301ь3жaaж0Й3323b33ь1ь1");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_144(string s) => ValidateEquals(s == "a00Йь11жжaa321ьЙ1Й31жa21ж3223", s, "a00Йь11жжaa321ьЙ1Й31жa21ж3223");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_145(string s) => ValidateEquals(s == "3132b0Йb3110ab\0201Й1ж32222a33ж", s, "3132b0Йb3110ab\0201Й1ж32222a33ж");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_146(string s) => ValidateEquals(s == "32b110bb312ь02Й1b2Й23232Й12ь33", s, "32b110bb312ь02Й1b2Й23232Й12ь33");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_147(string s) => ValidateEquals(s == "ж121bbbЙ2b1ж12222Йь1Йb02013жь1", s, "ж121bbbЙ2b1ж12222Йь1Йb02013жь1");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_148(string s) => ValidateEquals(s == "ь1b00a3310231001b1a1ь33жжb130ь", s, "ь1b00a3310231001b1a1ь33жжb130ь");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_149(string s) => ValidateEquals(s == "ж3b211b121ж23bь12a1Й2Йж12313aж", s, "ж3b211b121ж23bь12a1Й2Йж12313aж");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_150(string s) => ValidateEquals(s == "1a3жb31311322жь33213Й3ь13330жa3", s, "1a3жb31311322жь33213Й3ь13330жa3");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_151(string s) => ValidateEquals(s == "b33Йbж3333233101a33ж3b231221ь11", s, "b33Йbж3333233101a33ж3b231221ь11");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_152(string s) => ValidateEquals(s == "1Й212Й3ж112a31aьжьЙ32ж233a32Й1ж", s, "1Й212Й3ж112a31aьжьЙ32ж233a32Й1ж");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_153(string s) => ValidateEquals(s == "133ь02aьa0Й3Йab3ь1Й3Й2a21121210", s, "133ь02aьa0Й3Йab3ь1Й3Й2a21121210");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_154(string s) => ValidateEquals(s == "1320baж31b3Й2Й1322b113212212331", s, "1320baж31b3Й2Й1322b113212212331");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_155(string s) => ValidateEquals(s == "1Йa332132жb31Й33Й32321ж31b120ж03", s, "1Йa332132жb31Й33Й32321ж31b120ж03");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_156(string s) => ValidateEquals(s == "213321a1b3жь3111жЙ2b2Й3101221ь33", s, "213321a1b3жь3111жЙ2b2Й3101221ь33");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_157(string s) => ValidateEquals(s == "2ж1311a23b2212aЙ21Й11жb3233bb3a1", s, "2ж1311a23b2212aЙ21Й11жb3233bb3a1");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_158(string s) => ValidateEquals(s == "01Й11113ь3Й32a3ьЙЙ3Й32b2ab221310", s, "01Й11113ь3Й32a3ьЙЙ3Й32b2ab221310");
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Equals_159(string s) => ValidateEquals(s == "a120213b11211\0223223312ьь1Й3222Й", s, "a120213b11211\0223223312ьь1Й3222Й");

    public static readonly string[] s_TestData =
    {
        null,
        "",
        "\0",
        "a",
        "0",
        "ж",
        "3",
        "33",
        "31",
        "a\0",
        "12",
        "13",
        "b12",
        "ж23",
        "2a2",
        "222",
        "0ь3",
        "bж31",
        "ьЙbЙ",
        "b033",
        "311ь",
        "жЙ12",
        "2011b",
        "222b2",
        "aЙ213",
        "1a131",
        "3232Й",
        "3b0ьжь",
        "213b2Й",
        "b31210",
        "1ж0021",
        "3ь3112",
        "122b231",
        "03ж32ж3",
        "bb31ж2Й",
        "023bьжЙ",
        "\0232a12",
        "ж13ь11Йь",
        "11ьbb32ь",
        "222Йж3ж3",
        "ж303aЙ12",
        "ьb22322b",
        "a22b10b1Й",
        "3ba2221ь3",
        "жa1Й0b1Й1",
        "a20Йжж1ьь",
        "ьaж32132ь",
        "11111Й3Й12",
        "11Й\02Йb3жж",
        "21bжжж0103",
        "333332aЙ11",
        "Й123112313",
        "12ЙьЙaЙ11ьb",
        "жж22221Й3Й2",
        "ьЙ1bbж3202ж",
        "1bbЙ2Й33Й2ж",
        "2013133ь1bж",
        "23a2\02жa2a13",
        "23Й210Й3a3ж1",
        "32Й2133bb2Й3",
        "Й3bb1ь3bbьb3",
        "a0Йbabж2Й133",
        "320жa22a11ж1b",
        "ь321b3ьЙЙ13Й2",
        "a3ь1ж2a\022a1a",
        "3Йb30b33231bь",
        "2210121ж13231",
        "013311aa3203Й1",
        "12ЙЙ1Й2aЙ2ьbЙa",
        "2b1Й11130221bь",
        "230110Й0b3112ж",
        "a213ьab121b332",
        "111a01ж3121b123",
        "13a322Й2Й3bжb0Й",
        "\021232b1Йaa1032",
        "жЙ112ьb12Йь3b2ж",
        "2bьь331bb\023122",
        "aж22Й2203b023bь3",
        "aЙ033ж3a220ь3331",
        "20Йжa1b1313жЙb2a",
        "131Й1\022ж2322123",
        "23323b21ь11bЙ321",
        "302aьжa3213жaЙ3Йж",
        "ж13b00210b1212102",
        "20320Й3Й3ьж3Й2122",
        "0bb23a30baЙb2333ь",
        "22122ь130230103a2",
        "ььba20ж1жьЙьbЙ31bж",
        "bb1ь1033bж3011bж10",
        "1ь320a3a22b3333b13",
        "0a22aЙжa2222ж23Й13",
        "Й11Й213212ж1233b23",
        "32ь1Й03123ь011332ab",
        "222ж2311b133b3ж3223",
        "0111ь3002222a3aaaa3",
        "313Й213aж01a12231a2",
        "1ж022ь1323b3b3ж222ь",
        "ь023a3b213ь033ж13231",
        "ab2b0bь322300ж2220ж2",
        "1133Й323223ж31002123",
        "233ж0b3Й023Йьaaж3321",
        "3Й11b313323230a02Й30",
        "1ж2Йж0131a2ж2aЙЙ3ьb11",
        "Й13303ba3ьж31a1102222",
        "32331221ь3ьb103212132",
        "133Й0332210231331100Й",
        "22221322Й1133bb0Й3222",
        "12b011ж3a1ж3ЙЙa12Й0ь3ь",
        "0333ь12113ь11331ж323Йж",
        "0Й13a310ь12\02ь\02\02320331",
        "022b2ьж0302b33Й21ж1112",
        "3322ж2133133b3032Йaa12",
        "Й132Йaьb33a3Й33Йb21a2b2",
        "31102113Й11жb31bЙ12b133",
        "ЙьЙЙ0Й03a\023ь3311ьЙ1323",
        "212323жa23203bb00жa12ж3",
        "жЙ31130ж32322313010aa13",
        "123aж2221\022ж22Й021bЙЙ0Й",
        "211131ж2213303b1b0231a11",
        "ь1aЙжь0110Й2b220жж3ьж3ж1",
        "3жab2221133331311\023ЙЙ3ж",
        "21Й20\02ьь333ьb332223Йж1Й",
        "1Й2120a01110Й1121003a3b33",
        "3021a1Й1aa1111b22Й112Й201",
        "2b21ьaьb\023Й33301Й3123Йж1",
        "2ж1baЙЙ1a\021ж23323жbж331ж",
        "bab12332ж31130Й3230ь1011a",
        "110aь31ж33ьь33333a2b32ь12ь",
        "a2жЙ11ь1bь312a11aьaьb02Йb0",
        "32b2ж12a32a3ж23ж1ьЙbb22213",
        "30ж111ь11120жжb10212жbь33Й",
        "33b1311ж1\023bж020Й10b0302ж",
        "b3122жa12\021123a3130100113ь",
        "302a1ж322\021221z2a1331b2жь1",
        "31201322жж\0221Йz21Йьь32Й11ж",
        "3bжa132a13ba1311ж1Й22ЙbЙa33",
        "b33Й113ЙЙab1b332211222Й32\02",
        "0ж333b31b212121b1aж02ж133111",
        "0101Й220Й0жЙ3Й2abba0b1223aa\0",
        "2Й330bЙ123ж2ж02ж212ь112111Й1",
        "22a\0212aь3b1303Й3bb2b313Й222",
        "2ьж133332102222\020Йжbb2\022\02",
        "2\02321b31123231b2ЙЙ122abЙ2131",
        "1b021ьЙ30a2332ь3Й12231жж1aжь1",
        "1ж3ьь3ь1ь1ь0ж1Й122132a2ььaЙ3b",
        "21bbb31301ь3жaaж0Й3323b33ь1ь1",
        "a00Йь11жжaa321ьЙ1Й31жa21ж3223",
        "3132b0Йb3110ab\0201Й1ж32222a33ж",
        "32b110bb312ь02Й1b2Й23232Й12ь33",
        "ж121bbbЙ2b1ж12222Йь1Йb02013жь1",
        "ь1b00a3310231001b1a1ь33жжb130ь",
        "ж3b211b121ж23bь12a1Й2Йж12313aж",
        "1a3жb31311322жь33213Й3ь13330жa3",
        "b33Йbж3333233101a33ж3b231221ь11",
        "1Й212Й3ж112a31aьжьЙ32ж233a32Й1ж",
        "133ь02aьa0Й3Йab3ь1Й3Й2a21121210",
        "1320baж31b3Й2Й1322b113212212331",
        "1Йa332132жb31Й33Й32321ж31b120ж03",
        "213321a1b3жь3111жЙ2b2Й3101221ь33",
        "2ж1311a23b2212aЙ21Й11жb3233bb3a1",
        "01Й11113ь3Й32a3ьЙЙ3Й32b2ab221310",
        "a120213b11211\0223223312ьь1Й3222Й",
    };
}
