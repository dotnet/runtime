// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal class TestApp
{
    private static int test_0_0(int num, AA init, AA zero)
    {
        return init.q.val;
    }
    private static int test_0_1(int num, AA init, AA zero)
    {
        zero.q.val = num;
        return zero.q.val;
    }
    private static int test_0_2(int num, AA init, AA zero)
    {
        if (init.q.val != zero.q.val)
            return 100;
        else
            return zero.q.val;
    }
    private static int test_0_3(int num, AA init, AA zero)
    {
        return init.q.val < num + 1 ? 100 : -1;
    }
    private static int test_0_4(int num, AA init, AA zero)
    {
        return (init.q.val > zero.q.val ? 1 : 0) + 99;
    }
    private static int test_0_5(int num, AA init, AA zero)
    {
        return (init.q.val ^ zero.q.val) | num;
    }
    private static int test_0_6(int num, AA init, AA zero)
    {
        zero.q.val |= init.q.val;
        return zero.q.val & num;
    }
    private static int test_0_7(int num, AA init, AA zero)
    {
        return init.q.val >> zero.q.val;
    }
    private static int test_0_8(int num, AA init, AA zero)
    {
        return AA.a_init[init.q.val].q.val;
    }
    private static int test_0_9(int num, AA init, AA zero)
    {
        return AA.aa_init[num - 100, (init.q.val | 1) - 2, 1 + zero.q.val].q.val;
    }
    private static int test_0_10(int num, AA init, AA zero)
    {
        object bb = init.q.val;
        return (int)bb;
    }
    private static int test_0_11(int num, AA init, AA zero)
    {
        double dbl = init.q.val;
        return (int)dbl;
    }
    private static int test_0_12(int num, AA init, AA zero)
    {
        return AA.call_target(init.q).val;
    }
    private static int test_0_13(int num, AA init, AA zero)
    {
        return AA.call_target_ref(ref init.q).val;
    }
    private static int test_0_14(int num, AA init, AA zero)
    {
        return init.q.ret_code();
    }
    private static int test_1_0(int num, ref AA r_init, ref AA r_zero)
    {
        return r_init.q.val;
    }
    private static int test_1_1(int num, ref AA r_init, ref AA r_zero)
    {
        r_zero.q.val = num;
        return r_zero.q.val;
    }
    private static int test_1_2(int num, ref AA r_init, ref AA r_zero)
    {
        if (r_init.q.val != r_zero.q.val)
            return 100;
        else
            return r_zero.q.val;
    }
    private static int test_1_3(int num, ref AA r_init, ref AA r_zero)
    {
        return r_init.q.val < num + 1 ? 100 : -1;
    }
    private static int test_1_4(int num, ref AA r_init, ref AA r_zero)
    {
        return (r_init.q.val > r_zero.q.val ? 1 : 0) + 99;
    }
    private static int test_1_5(int num, ref AA r_init, ref AA r_zero)
    {
        return (r_init.q.val ^ r_zero.q.val) | num;
    }
    private static int test_1_6(int num, ref AA r_init, ref AA r_zero)
    {
        r_zero.q.val |= r_init.q.val;
        return r_zero.q.val & num;
    }
    private static int test_1_7(int num, ref AA r_init, ref AA r_zero)
    {
        return r_init.q.val >> r_zero.q.val;
    }
    private static int test_1_8(int num, ref AA r_init, ref AA r_zero)
    {
        return AA.a_init[r_init.q.val].q.val;
    }
    private static int test_1_9(int num, ref AA r_init, ref AA r_zero)
    {
        return AA.aa_init[num - 100, (r_init.q.val | 1) - 2, 1 + r_zero.q.val].q.val;
    }
    private static int test_1_10(int num, ref AA r_init, ref AA r_zero)
    {
        object bb = r_init.q.val;
        return (int)bb;
    }
    private static int test_1_11(int num, ref AA r_init, ref AA r_zero)
    {
        double dbl = r_init.q.val;
        return (int)dbl;
    }
    private static int test_1_12(int num, ref AA r_init, ref AA r_zero)
    {
        return AA.call_target(r_init.q).val;
    }
    private static int test_1_13(int num, ref AA r_init, ref AA r_zero)
    {
        return AA.call_target_ref(ref r_init.q).val;
    }
    private static int test_1_14(int num, ref AA r_init, ref AA r_zero)
    {
        return r_init.q.ret_code();
    }
    private static int test_2_0(int num)
    {
        return AA.a_init[num].q.val;
    }
    private static int test_2_1(int num)
    {
        AA.a_zero[num].q.val = num;
        return AA.a_zero[num].q.val;
    }
    private static int test_2_2(int num)
    {
        if (AA.a_init[num].q.val != AA.a_zero[num].q.val)
            return 100;
        else
            return AA.a_zero[num].q.val;
    }
    private static int test_2_3(int num)
    {
        return AA.a_init[num].q.val < num + 1 ? 100 : -1;
    }
    private static int test_2_4(int num)
    {
        return (AA.a_init[num].q.val > AA.a_zero[num].q.val ? 1 : 0) + 99;
    }
    private static int test_2_5(int num)
    {
        return (AA.a_init[num].q.val ^ AA.a_zero[num].q.val) | num;
    }
    private static int test_2_6(int num)
    {
        AA.a_zero[num].q.val |= AA.a_init[num].q.val;
        return AA.a_zero[num].q.val & num;
    }
    private static int test_2_7(int num)
    {
        return AA.a_init[num].q.val >> AA.a_zero[num].q.val;
    }
    private static int test_2_8(int num)
    {
        return AA.a_init[AA.a_init[num].q.val].q.val;
    }
    private static int test_2_9(int num)
    {
        return AA.aa_init[num - 100, (AA.a_init[num].q.val | 1) - 2, 1 + AA.a_zero[num].q.val].q.val;
    }
    private static int test_2_10(int num)
    {
        object bb = AA.a_init[num].q.val;
        return (int)bb;
    }
    private static int test_2_11(int num)
    {
        double dbl = AA.a_init[num].q.val;
        return (int)dbl;
    }
    private static int test_2_12(int num)
    {
        return AA.call_target(AA.a_init[num].q).val;
    }
    private static int test_2_13(int num)
    {
        return AA.call_target_ref(ref AA.a_init[num].q).val;
    }
    private static int test_2_14(int num)
    {
        return AA.a_init[num].q.ret_code();
    }
    private static int test_3_0(int num)
    {
        return AA.aa_init[0, num - 1, num / 100].q.val;
    }
    private static int test_3_1(int num)
    {
        AA.aa_zero[0, num - 1, num / 100].q.val = num;
        return AA.aa_zero[0, num - 1, num / 100].q.val;
    }
    private static int test_3_2(int num)
    {
        if (AA.aa_init[0, num - 1, num / 100].q.val != AA.aa_zero[0, num - 1, num / 100].q.val)
            return 100;
        else
            return AA.aa_zero[0, num - 1, num / 100].q.val;
    }
    private static int test_3_3(int num)
    {
        return AA.aa_init[0, num - 1, num / 100].q.val < num + 1 ? 100 : -1;
    }
    private static int test_3_4(int num)
    {
        return (AA.aa_init[0, num - 1, num / 100].q.val > AA.aa_zero[0, num - 1, num / 100].q.val ? 1 : 0) + 99;
    }
    private static int test_3_5(int num)
    {
        return (AA.aa_init[0, num - 1, num / 100].q.val ^ AA.aa_zero[0, num - 1, num / 100].q.val) | num;
    }
    private static int test_3_6(int num)
    {
        AA.aa_zero[0, num - 1, num / 100].q.val |= AA.aa_init[0, num - 1, num / 100].q.val;
        return AA.aa_zero[0, num - 1, num / 100].q.val & num;
    }
    private static int test_3_7(int num)
    {
        return AA.aa_init[0, num - 1, num / 100].q.val >> AA.aa_zero[0, num - 1, num / 100].q.val;
    }
    private static int test_3_8(int num)
    {
        return AA.a_init[AA.aa_init[0, num - 1, num / 100].q.val].q.val;
    }
    private static int test_3_9(int num)
    {
        return AA.aa_init[num - 100, (AA.aa_init[0, num - 1, num / 100].q.val | 1) - 2, 1 + AA.aa_zero[0, num - 1, num / 100].q.val].q.val;
    }
    private static int test_3_10(int num)
    {
        object bb = AA.aa_init[0, num - 1, num / 100].q.val;
        return (int)bb;
    }
    private static int test_3_11(int num)
    {
        double dbl = AA.aa_init[0, num - 1, num / 100].q.val;
        return (int)dbl;
    }
    private static int test_3_12(int num)
    {
        return AA.call_target(AA.aa_init[0, num - 1, num / 100].q).val;
    }
    private static int test_3_13(int num)
    {
        return AA.call_target_ref(ref AA.aa_init[0, num - 1, num / 100].q).val;
    }
    private static int test_3_14(int num)
    {
        return AA.aa_init[0, num - 1, num / 100].q.ret_code();
    }
    private static int test_4_0(int num)
    {
        return BB.f_init.q.val;
    }
    private static int test_4_1(int num)
    {
        BB.f_zero.q.val = num;
        return BB.f_zero.q.val;
    }
    private static int test_4_2(int num)
    {
        if (BB.f_init.q.val != BB.f_zero.q.val)
            return 100;
        else
            return BB.f_zero.q.val;
    }
    private static int test_4_3(int num)
    {
        return BB.f_init.q.val < num + 1 ? 100 : -1;
    }
    private static int test_4_4(int num)
    {
        return (BB.f_init.q.val > BB.f_zero.q.val ? 1 : 0) + 99;
    }
    private static int test_4_5(int num)
    {
        return (BB.f_init.q.val ^ BB.f_zero.q.val) | num;
    }
    private static int test_4_6(int num)
    {
        BB.f_zero.q.val |= BB.f_init.q.val;
        return BB.f_zero.q.val & num;
    }
    private static int test_4_7(int num)
    {
        return BB.f_init.q.val >> BB.f_zero.q.val;
    }
    private static int test_4_8(int num)
    {
        return AA.a_init[BB.f_init.q.val].q.val;
    }
    private static int test_4_9(int num)
    {
        return AA.aa_init[num - 100, (BB.f_init.q.val | 1) - 2, 1 + BB.f_zero.q.val].q.val;
    }
    private static int test_4_10(int num)
    {
        object bb = BB.f_init.q.val;
        return (int)bb;
    }
    private static int test_4_11(int num)
    {
        double dbl = BB.f_init.q.val;
        return (int)dbl;
    }
    private static int test_4_12(int num)
    {
        return AA.call_target(BB.f_init.q).val;
    }
    private static int test_4_13(int num)
    {
        return AA.call_target_ref(ref BB.f_init.q).val;
    }
    private static int test_4_14(int num)
    {
        return BB.f_init.q.ret_code();
    }
    private static int test_5_0(int num)
    {
        return ((AA)AA.b_init).q.val;
    }
    private static int test_6_0(int num, TypedReference tr_init)
    {
        return __refvalue(tr_init, AA).q.val;
    }

    internal static unsafe int RunAllTests()
    {
        AA.reset();
        if (test_0_0(100, new AA(100), new AA(0)) != 100)
        {
            Console.WriteLine("test_0_0() failed.");
            return 101;
        }
        AA.verify_all(); AA.reset();
        if (test_0_1(100, new AA(100), new AA(0)) != 100)
        {
            Console.WriteLine("test_0_1() failed.");
            return 102;
        }
        AA.verify_all(); AA.reset();
        if (test_0_2(100, new AA(100), new AA(0)) != 100)
        {
            Console.WriteLine("test_0_2() failed.");
            return 103;
        }
        AA.verify_all(); AA.reset();
        if (test_0_3(100, new AA(100), new AA(0)) != 100)
        {
            Console.WriteLine("test_0_3() failed.");
            return 104;
        }
        AA.verify_all(); AA.reset();
        if (test_0_4(100, new AA(100), new AA(0)) != 100)
        {
            Console.WriteLine("test_0_4() failed.");
            return 105;
        }
        AA.verify_all(); AA.reset();
        if (test_0_5(100, new AA(100), new AA(0)) != 100)
        {
            Console.WriteLine("test_0_5() failed.");
            return 106;
        }
        AA.verify_all(); AA.reset();
        if (test_0_6(100, new AA(100), new AA(0)) != 100)
        {
            Console.WriteLine("test_0_6() failed.");
            return 107;
        }
        AA.verify_all(); AA.reset();
        if (test_0_7(100, new AA(100), new AA(0)) != 100)
        {
            Console.WriteLine("test_0_7() failed.");
            return 108;
        }
        AA.verify_all(); AA.reset();
        if (test_0_8(100, new AA(100), new AA(0)) != 100)
        {
            Console.WriteLine("test_0_8() failed.");
            return 109;
        }
        AA.verify_all(); AA.reset();
        if (test_0_9(100, new AA(100), new AA(0)) != 100)
        {
            Console.WriteLine("test_0_9() failed.");
            return 110;
        }
        AA.verify_all(); AA.reset();
        if (test_0_10(100, new AA(100), new AA(0)) != 100)
        {
            Console.WriteLine("test_0_10() failed.");
            return 111;
        }
        AA.verify_all(); AA.reset();
        if (test_0_11(100, new AA(100), new AA(0)) != 100)
        {
            Console.WriteLine("test_0_11() failed.");
            return 112;
        }
        AA.verify_all(); AA.reset();
        if (test_0_12(100, new AA(100), new AA(0)) != 100)
        {
            Console.WriteLine("test_0_12() failed.");
            return 113;
        }
        AA.verify_all(); AA.reset();
        if (test_0_13(100, new AA(100), new AA(0)) != 100)
        {
            Console.WriteLine("test_0_13() failed.");
            return 114;
        }
        AA.verify_all(); AA.reset();
        if (test_0_14(100, new AA(100), new AA(0)) != 100)
        {
            Console.WriteLine("test_0_14() failed.");
            return 115;
        }
        AA.verify_all(); AA.reset();
        if (test_1_0(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_0() failed.");
            return 116;
        }
        AA.verify_all(); AA.reset();
        if (test_1_1(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_1() failed.");
            return 117;
        }
        AA.verify_all(); AA.reset();
        if (test_1_2(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_2() failed.");
            return 118;
        }
        AA.verify_all(); AA.reset();
        if (test_1_3(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_3() failed.");
            return 119;
        }
        AA.verify_all(); AA.reset();
        if (test_1_4(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_4() failed.");
            return 120;
        }
        AA.verify_all(); AA.reset();
        if (test_1_5(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_5() failed.");
            return 121;
        }
        AA.verify_all(); AA.reset();
        if (test_1_6(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_6() failed.");
            return 122;
        }
        AA.verify_all(); AA.reset();
        if (test_1_7(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_7() failed.");
            return 123;
        }
        AA.verify_all(); AA.reset();
        if (test_1_8(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_8() failed.");
            return 124;
        }
        AA.verify_all(); AA.reset();
        if (test_1_9(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_9() failed.");
            return 125;
        }
        AA.verify_all(); AA.reset();
        if (test_1_10(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_10() failed.");
            return 126;
        }
        AA.verify_all(); AA.reset();
        if (test_1_11(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_11() failed.");
            return 127;
        }
        AA.verify_all(); AA.reset();
        if (test_1_12(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_12() failed.");
            return 128;
        }
        AA.verify_all(); AA.reset();
        if (test_1_13(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_13() failed.");
            return 129;
        }
        AA.verify_all(); AA.reset();
        if (test_1_14(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_14() failed.");
            return 130;
        }
        AA.verify_all(); AA.reset();
        if (test_2_0(100) != 100)
        {
            Console.WriteLine("test_2_0() failed.");
            return 131;
        }
        AA.verify_all(); AA.reset();
        if (test_2_1(100) != 100)
        {
            Console.WriteLine("test_2_1() failed.");
            return 132;
        }
        AA.verify_all(); AA.reset();
        if (test_2_2(100) != 100)
        {
            Console.WriteLine("test_2_2() failed.");
            return 133;
        }
        AA.verify_all(); AA.reset();
        if (test_2_3(100) != 100)
        {
            Console.WriteLine("test_2_3() failed.");
            return 134;
        }
        AA.verify_all(); AA.reset();
        if (test_2_4(100) != 100)
        {
            Console.WriteLine("test_2_4() failed.");
            return 135;
        }
        AA.verify_all(); AA.reset();
        if (test_2_5(100) != 100)
        {
            Console.WriteLine("test_2_5() failed.");
            return 136;
        }
        AA.verify_all(); AA.reset();
        if (test_2_6(100) != 100)
        {
            Console.WriteLine("test_2_6() failed.");
            return 137;
        }
        AA.verify_all(); AA.reset();
        if (test_2_7(100) != 100)
        {
            Console.WriteLine("test_2_7() failed.");
            return 138;
        }
        AA.verify_all(); AA.reset();
        if (test_2_8(100) != 100)
        {
            Console.WriteLine("test_2_8() failed.");
            return 139;
        }
        AA.verify_all(); AA.reset();
        if (test_2_9(100) != 100)
        {
            Console.WriteLine("test_2_9() failed.");
            return 140;
        }
        AA.verify_all(); AA.reset();
        if (test_2_10(100) != 100)
        {
            Console.WriteLine("test_2_10() failed.");
            return 141;
        }
        AA.verify_all(); AA.reset();
        if (test_2_11(100) != 100)
        {
            Console.WriteLine("test_2_11() failed.");
            return 142;
        }
        AA.verify_all(); AA.reset();
        if (test_2_12(100) != 100)
        {
            Console.WriteLine("test_2_12() failed.");
            return 143;
        }
        AA.verify_all(); AA.reset();
        if (test_2_13(100) != 100)
        {
            Console.WriteLine("test_2_13() failed.");
            return 144;
        }
        AA.verify_all(); AA.reset();
        if (test_2_14(100) != 100)
        {
            Console.WriteLine("test_2_14() failed.");
            return 145;
        }
        AA.verify_all(); AA.reset();
        if (test_3_0(100) != 100)
        {
            Console.WriteLine("test_3_0() failed.");
            return 146;
        }
        AA.verify_all(); AA.reset();
        if (test_3_1(100) != 100)
        {
            Console.WriteLine("test_3_1() failed.");
            return 147;
        }
        AA.verify_all(); AA.reset();
        if (test_3_2(100) != 100)
        {
            Console.WriteLine("test_3_2() failed.");
            return 148;
        }
        AA.verify_all(); AA.reset();
        if (test_3_3(100) != 100)
        {
            Console.WriteLine("test_3_3() failed.");
            return 149;
        }
        AA.verify_all(); AA.reset();
        if (test_3_4(100) != 100)
        {
            Console.WriteLine("test_3_4() failed.");
            return 150;
        }
        AA.verify_all(); AA.reset();
        if (test_3_5(100) != 100)
        {
            Console.WriteLine("test_3_5() failed.");
            return 151;
        }
        AA.verify_all(); AA.reset();
        if (test_3_6(100) != 100)
        {
            Console.WriteLine("test_3_6() failed.");
            return 152;
        }
        AA.verify_all(); AA.reset();
        if (test_3_7(100) != 100)
        {
            Console.WriteLine("test_3_7() failed.");
            return 153;
        }
        AA.verify_all(); AA.reset();
        if (test_3_8(100) != 100)
        {
            Console.WriteLine("test_3_8() failed.");
            return 154;
        }
        AA.verify_all(); AA.reset();
        if (test_3_9(100) != 100)
        {
            Console.WriteLine("test_3_9() failed.");
            return 155;
        }
        AA.verify_all(); AA.reset();
        if (test_3_10(100) != 100)
        {
            Console.WriteLine("test_3_10() failed.");
            return 156;
        }
        AA.verify_all(); AA.reset();
        if (test_3_11(100) != 100)
        {
            Console.WriteLine("test_3_11() failed.");
            return 157;
        }
        AA.verify_all(); AA.reset();
        if (test_3_12(100) != 100)
        {
            Console.WriteLine("test_3_12() failed.");
            return 158;
        }
        AA.verify_all(); AA.reset();
        if (test_3_13(100) != 100)
        {
            Console.WriteLine("test_3_13() failed.");
            return 159;
        }
        AA.verify_all(); AA.reset();
        if (test_3_14(100) != 100)
        {
            Console.WriteLine("test_3_14() failed.");
            return 160;
        }
        AA.verify_all(); AA.reset();
        if (test_4_0(100) != 100)
        {
            Console.WriteLine("test_4_0() failed.");
            return 161;
        }
        AA.verify_all(); AA.reset();
        if (test_4_1(100) != 100)
        {
            Console.WriteLine("test_4_1() failed.");
            return 162;
        }
        AA.verify_all(); AA.reset();
        if (test_4_2(100) != 100)
        {
            Console.WriteLine("test_4_2() failed.");
            return 163;
        }
        AA.verify_all(); AA.reset();
        if (test_4_3(100) != 100)
        {
            Console.WriteLine("test_4_3() failed.");
            return 164;
        }
        AA.verify_all(); AA.reset();
        if (test_4_4(100) != 100)
        {
            Console.WriteLine("test_4_4() failed.");
            return 165;
        }
        AA.verify_all(); AA.reset();
        if (test_4_5(100) != 100)
        {
            Console.WriteLine("test_4_5() failed.");
            return 166;
        }
        AA.verify_all(); AA.reset();
        if (test_4_6(100) != 100)
        {
            Console.WriteLine("test_4_6() failed.");
            return 167;
        }
        AA.verify_all(); AA.reset();
        if (test_4_7(100) != 100)
        {
            Console.WriteLine("test_4_7() failed.");
            return 168;
        }
        AA.verify_all(); AA.reset();
        if (test_4_8(100) != 100)
        {
            Console.WriteLine("test_4_8() failed.");
            return 169;
        }
        AA.verify_all(); AA.reset();
        if (test_4_9(100) != 100)
        {
            Console.WriteLine("test_4_9() failed.");
            return 170;
        }
        AA.verify_all(); AA.reset();
        if (test_4_10(100) != 100)
        {
            Console.WriteLine("test_4_10() failed.");
            return 171;
        }
        AA.verify_all(); AA.reset();
        if (test_4_11(100) != 100)
        {
            Console.WriteLine("test_4_11() failed.");
            return 172;
        }
        AA.verify_all(); AA.reset();
        if (test_4_12(100) != 100)
        {
            Console.WriteLine("test_4_12() failed.");
            return 173;
        }
        AA.verify_all(); AA.reset();
        if (test_4_13(100) != 100)
        {
            Console.WriteLine("test_4_13() failed.");
            return 174;
        }
        AA.verify_all(); AA.reset();
        if (test_4_14(100) != 100)
        {
            Console.WriteLine("test_4_14() failed.");
            return 175;
        }
        AA.verify_all(); AA.reset();
        if (test_5_0(100) != 100)
        {
            Console.WriteLine("test_5_0() failed.");
            return 176;
        }
        AA.verify_all(); AA.reset();
        if (test_6_0(100, __makeref(AA._init)) != 100)
        {
            Console.WriteLine("test_6_0() failed.");
            return 177;
        }
        AA.verify_all(); Console.WriteLine("All tests passed.");
        return 100;
    }
}
