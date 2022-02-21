// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal class TestApp
{
    private static short test_0_0(short num, AA init, AA zero)
    {
        return init.q;
    }
    private static short test_0_1(short num, AA init, AA zero)
    {
        zero.q = num;
        return zero.q;
    }
    private static short test_0_2(short num, AA init, AA zero)
    {
        return (short)(init.q + zero.q);
    }
    private static short test_0_3(short num, AA init, AA zero)
    {
        return (short)checked(init.q - zero.q);
    }
    private static short test_0_4(short num, AA init, AA zero)
    {
        zero.q += num; return zero.q;
    }
    private static short test_0_5(short num, AA init, AA zero)
    {
        zero.q += init.q; return zero.q;
    }
    private static short test_0_6(short num, AA init, AA zero)
    {
        if (init.q == num)
            return 100;
        else
            return zero.q;
    }
    private static short test_0_7(short num, AA init, AA zero)
    {
        return (short)(init.q < num + 1 ? 100 : -1);
    }
    private static short test_0_8(short num, AA init, AA zero)
    {
        return (short)((init.q > zero.q ? 1 : 0) + 99);
    }
    private static short test_0_9(short num, AA init, AA zero)
    {
        return (short)((init.q ^ zero.q) | num);
    }
    private static short test_0_10(short num, AA init, AA zero)
    {
        zero.q |= init.q;
        return (short)(zero.q & num);
    }
    private static short test_0_11(short num, AA init, AA zero)
    {
        return (short)(init.q >> zero.q);
    }
    private static short test_0_12(short num, AA init, AA zero)
    {
        return AA.a_init[init.q].q;
    }
    private static short test_0_13(short num, AA init, AA zero)
    {
        return AA.aa_init[num - 100, (init.q | 1) - 2, 1 + zero.q].q;
    }
    private static short test_0_14(short num, AA init, AA zero)
    {
        object bb = init.q;
        return (short)bb;
    }
    private static short test_0_15(short num, AA init, AA zero)
    {
        double dbl = init.q;
        return (short)dbl;
    }
    private static short test_0_16(short num, AA init, AA zero)
    {
        return AA.call_target(init.q);
    }
    private static short test_0_17(short num, AA init, AA zero)
    {
        return AA.call_target_ref(ref init.q);
    }
    private static short test_1_0(short num, ref AA r_init, ref AA r_zero)
    {
        return r_init.q;
    }
    private static short test_1_1(short num, ref AA r_init, ref AA r_zero)
    {
        r_zero.q = num;
        return r_zero.q;
    }
    private static short test_1_2(short num, ref AA r_init, ref AA r_zero)
    {
        return (short)(r_init.q + r_zero.q);
    }
    private static short test_1_3(short num, ref AA r_init, ref AA r_zero)
    {
        return (short)checked(r_init.q - r_zero.q);
    }
    private static short test_1_4(short num, ref AA r_init, ref AA r_zero)
    {
        r_zero.q += num; return r_zero.q;
    }
    private static short test_1_5(short num, ref AA r_init, ref AA r_zero)
    {
        r_zero.q += r_init.q; return r_zero.q;
    }
    private static short test_1_6(short num, ref AA r_init, ref AA r_zero)
    {
        if (r_init.q == num)
            return 100;
        else
            return r_zero.q;
    }
    private static short test_1_7(short num, ref AA r_init, ref AA r_zero)
    {
        return (short)(r_init.q < num + 1 ? 100 : -1);
    }
    private static short test_1_8(short num, ref AA r_init, ref AA r_zero)
    {
        return (short)((r_init.q > r_zero.q ? 1 : 0) + 99);
    }
    private static short test_1_9(short num, ref AA r_init, ref AA r_zero)
    {
        return (short)((r_init.q ^ r_zero.q) | num);
    }
    private static short test_1_10(short num, ref AA r_init, ref AA r_zero)
    {
        r_zero.q |= r_init.q;
        return (short)(r_zero.q & num);
    }
    private static short test_1_11(short num, ref AA r_init, ref AA r_zero)
    {
        return (short)(r_init.q >> r_zero.q);
    }
    private static short test_1_12(short num, ref AA r_init, ref AA r_zero)
    {
        return AA.a_init[r_init.q].q;
    }
    private static short test_1_13(short num, ref AA r_init, ref AA r_zero)
    {
        return AA.aa_init[num - 100, (r_init.q | 1) - 2, 1 + r_zero.q].q;
    }
    private static short test_1_14(short num, ref AA r_init, ref AA r_zero)
    {
        object bb = r_init.q;
        return (short)bb;
    }
    private static short test_1_15(short num, ref AA r_init, ref AA r_zero)
    {
        double dbl = r_init.q;
        return (short)dbl;
    }
    private static short test_1_16(short num, ref AA r_init, ref AA r_zero)
    {
        return AA.call_target(r_init.q);
    }
    private static short test_1_17(short num, ref AA r_init, ref AA r_zero)
    {
        return AA.call_target_ref(ref r_init.q);
    }
    private static short test_2_0(short num)
    {
        return AA.a_init[num].q;
    }
    private static short test_2_1(short num)
    {
        AA.a_zero[num].q = num;
        return AA.a_zero[num].q;
    }
    private static short test_2_2(short num)
    {
        return (short)(AA.a_init[num].q + AA.a_zero[num].q);
    }
    private static short test_2_3(short num)
    {
        return (short)checked(AA.a_init[num].q - AA.a_zero[num].q);
    }
    private static short test_2_4(short num)
    {
        AA.a_zero[num].q += num; return AA.a_zero[num].q;
    }
    private static short test_2_5(short num)
    {
        AA.a_zero[num].q += AA.a_init[num].q; return AA.a_zero[num].q;
    }
    private static short test_2_6(short num)
    {
        if (AA.a_init[num].q == num)
            return 100;
        else
            return AA.a_zero[num].q;
    }
    private static short test_2_7(short num)
    {
        return (short)(AA.a_init[num].q < num + 1 ? 100 : -1);
    }
    private static short test_2_8(short num)
    {
        return (short)((AA.a_init[num].q > AA.a_zero[num].q ? 1 : 0) + 99);
    }
    private static short test_2_9(short num)
    {
        return (short)((AA.a_init[num].q ^ AA.a_zero[num].q) | num);
    }
    private static short test_2_10(short num)
    {
        AA.a_zero[num].q |= AA.a_init[num].q;
        return (short)(AA.a_zero[num].q & num);
    }
    private static short test_2_11(short num)
    {
        return (short)(AA.a_init[num].q >> AA.a_zero[num].q);
    }
    private static short test_2_12(short num)
    {
        return AA.a_init[AA.a_init[num].q].q;
    }
    private static short test_2_13(short num)
    {
        return AA.aa_init[num - 100, (AA.a_init[num].q | 1) - 2, 1 + AA.a_zero[num].q].q;
    }
    private static short test_2_14(short num)
    {
        object bb = AA.a_init[num].q;
        return (short)bb;
    }
    private static short test_2_15(short num)
    {
        double dbl = AA.a_init[num].q;
        return (short)dbl;
    }
    private static short test_2_16(short num)
    {
        return AA.call_target(AA.a_init[num].q);
    }
    private static short test_2_17(short num)
    {
        return AA.call_target_ref(ref AA.a_init[num].q);
    }
    private static short test_3_0(short num)
    {
        return AA.aa_init[0, num - 1, num / 100].q;
    }
    private static short test_3_1(short num)
    {
        AA.aa_zero[0, num - 1, num / 100].q = num;
        return AA.aa_zero[0, num - 1, num / 100].q;
    }
    private static short test_3_2(short num)
    {
        return (short)(AA.aa_init[0, num - 1, num / 100].q + AA.aa_zero[0, num - 1, num / 100].q);
    }
    private static short test_3_3(short num)
    {
        return (short)checked(AA.aa_init[0, num - 1, num / 100].q - AA.aa_zero[0, num - 1, num / 100].q);
    }
    private static short test_3_4(short num)
    {
        AA.aa_zero[0, num - 1, num / 100].q += num; return AA.aa_zero[0, num - 1, num / 100].q;
    }
    private static short test_3_5(short num)
    {
        AA.aa_zero[0, num - 1, num / 100].q += AA.aa_init[0, num - 1, num / 100].q; return AA.aa_zero[0, num - 1, num / 100].q;
    }
    private static short test_3_6(short num)
    {
        if (AA.aa_init[0, num - 1, num / 100].q == num)
            return 100;
        else
            return AA.aa_zero[0, num - 1, num / 100].q;
    }
    private static short test_3_7(short num)
    {
        return (short)(AA.aa_init[0, num - 1, num / 100].q < num + 1 ? 100 : -1);
    }
    private static short test_3_8(short num)
    {
        return (short)((AA.aa_init[0, num - 1, num / 100].q > AA.aa_zero[0, num - 1, num / 100].q ? 1 : 0) + 99);
    }
    private static short test_3_9(short num)
    {
        return (short)((AA.aa_init[0, num - 1, num / 100].q ^ AA.aa_zero[0, num - 1, num / 100].q) | num);
    }
    private static short test_3_10(short num)
    {
        AA.aa_zero[0, num - 1, num / 100].q |= AA.aa_init[0, num - 1, num / 100].q;
        return (short)(AA.aa_zero[0, num - 1, num / 100].q & num);
    }
    private static short test_3_11(short num)
    {
        return (short)(AA.aa_init[0, num - 1, num / 100].q >> AA.aa_zero[0, num - 1, num / 100].q);
    }
    private static short test_3_12(short num)
    {
        return AA.a_init[AA.aa_init[0, num - 1, num / 100].q].q;
    }
    private static short test_3_13(short num)
    {
        return AA.aa_init[num - 100, (AA.aa_init[0, num - 1, num / 100].q | 1) - 2, 1 + AA.aa_zero[0, num - 1, num / 100].q].q;
    }
    private static short test_3_14(short num)
    {
        object bb = AA.aa_init[0, num - 1, num / 100].q;
        return (short)bb;
    }
    private static short test_3_15(short num)
    {
        double dbl = AA.aa_init[0, num - 1, num / 100].q;
        return (short)dbl;
    }
    private static short test_3_16(short num)
    {
        return AA.call_target(AA.aa_init[0, num - 1, num / 100].q);
    }
    private static short test_3_17(short num)
    {
        return AA.call_target_ref(ref AA.aa_init[0, num - 1, num / 100].q);
    }
    private static short test_4_0(short num)
    {
        return BB.f_init.q;
    }
    private static short test_4_1(short num)
    {
        BB.f_zero.q = num;
        return BB.f_zero.q;
    }
    private static short test_4_2(short num)
    {
        return (short)(BB.f_init.q + BB.f_zero.q);
    }
    private static short test_4_3(short num)
    {
        return (short)checked(BB.f_init.q - BB.f_zero.q);
    }
    private static short test_4_4(short num)
    {
        BB.f_zero.q += num; return BB.f_zero.q;
    }
    private static short test_4_5(short num)
    {
        BB.f_zero.q += BB.f_init.q; return BB.f_zero.q;
    }
    private static short test_4_6(short num)
    {
        if (BB.f_init.q == num)
            return 100;
        else
            return BB.f_zero.q;
    }
    private static short test_4_7(short num)
    {
        return (short)(BB.f_init.q < num + 1 ? 100 : -1);
    }
    private static short test_4_8(short num)
    {
        return (short)((BB.f_init.q > BB.f_zero.q ? 1 : 0) + 99);
    }
    private static short test_4_9(short num)
    {
        return (short)((BB.f_init.q ^ BB.f_zero.q) | num);
    }
    private static short test_4_10(short num)
    {
        BB.f_zero.q |= BB.f_init.q;
        return (short)(BB.f_zero.q & num);
    }
    private static short test_4_11(short num)
    {
        return (short)(BB.f_init.q >> BB.f_zero.q);
    }
    private static short test_4_12(short num)
    {
        return AA.a_init[BB.f_init.q].q;
    }
    private static short test_4_13(short num)
    {
        return AA.aa_init[num - 100, (BB.f_init.q | 1) - 2, 1 + BB.f_zero.q].q;
    }
    private static short test_4_14(short num)
    {
        object bb = BB.f_init.q;
        return (short)bb;
    }
    private static short test_4_15(short num)
    {
        double dbl = BB.f_init.q;
        return (short)dbl;
    }
    private static short test_4_16(short num)
    {
        return AA.call_target(BB.f_init.q);
    }
    private static short test_4_17(short num)
    {
        return AA.call_target_ref(ref BB.f_init.q);
    }
    private static short test_5_0(short num)
    {
        return ((AA)AA.b_init).q;
    }
    private static short test_6_0(short num, TypedReference tr_init)
    {
        return __refvalue(tr_init, AA).q;
    }
    private static unsafe short test_7_0(short num, void* ptr_init, void* ptr_zero)
    {
        return (*((AA*)ptr_init)).q;
    }
    private static unsafe short test_7_1(short num, void* ptr_init, void* ptr_zero)
    {
        (*((AA*)ptr_zero)).q = num;
        return (*((AA*)ptr_zero)).q;
    }
    private static unsafe short test_7_2(short num, void* ptr_init, void* ptr_zero)
    {
        return (short)((*((AA*)ptr_init)).q + (*((AA*)ptr_zero)).q);
    }
    private static unsafe short test_7_3(short num, void* ptr_init, void* ptr_zero)
    {
        return (short)checked((*((AA*)ptr_init)).q - (*((AA*)ptr_zero)).q);
    }
    private static unsafe short test_7_4(short num, void* ptr_init, void* ptr_zero)
    {
        (*((AA*)ptr_zero)).q += num; return (*((AA*)ptr_zero)).q;
    }
    private static unsafe short test_7_5(short num, void* ptr_init, void* ptr_zero)
    {
        (*((AA*)ptr_zero)).q += (*((AA*)ptr_init)).q; return (*((AA*)ptr_zero)).q;
    }
    private static unsafe short test_7_6(short num, void* ptr_init, void* ptr_zero)
    {
        if ((*((AA*)ptr_init)).q == num)
            return 100;
        else
            return (*((AA*)ptr_zero)).q;
    }
    private static unsafe short test_7_7(short num, void* ptr_init, void* ptr_zero)
    {
        return (short)((*((AA*)ptr_init)).q < num + 1 ? 100 : -1);
    }
    private static unsafe short test_7_8(short num, void* ptr_init, void* ptr_zero)
    {
        return (short)(((*((AA*)ptr_init)).q > (*((AA*)ptr_zero)).q ? 1 : 0) + 99);
    }
    private static unsafe short test_7_9(short num, void* ptr_init, void* ptr_zero)
    {
        return (short)(((*((AA*)ptr_init)).q ^ (*((AA*)ptr_zero)).q) | num);
    }
    private static unsafe short test_7_10(short num, void* ptr_init, void* ptr_zero)
    {
        (*((AA*)ptr_zero)).q |= (*((AA*)ptr_init)).q;
        return (short)((*((AA*)ptr_zero)).q & num);
    }
    private static unsafe short test_7_11(short num, void* ptr_init, void* ptr_zero)
    {
        return (short)((*((AA*)ptr_init)).q >> (*((AA*)ptr_zero)).q);
    }
    private static unsafe short test_7_12(short num, void* ptr_init, void* ptr_zero)
    {
        return AA.a_init[(*((AA*)ptr_init)).q].q;
    }
    private static unsafe short test_7_13(short num, void* ptr_init, void* ptr_zero)
    {
        return AA.aa_init[num - 100, ((*((AA*)ptr_init)).q | 1) - 2, 1 + (*((AA*)ptr_zero)).q].q;
    }
    private static unsafe short test_7_14(short num, void* ptr_init, void* ptr_zero)
    {
        object bb = (*((AA*)ptr_init)).q;
        return (short)bb;
    }
    private static unsafe short test_7_15(short num, void* ptr_init, void* ptr_zero)
    {
        double dbl = (*((AA*)ptr_init)).q;
        return (short)dbl;
    }
    private static unsafe short test_7_16(short num, void* ptr_init, void* ptr_zero)
    {
        return AA.call_target((*((AA*)ptr_init)).q);
    }
    private static unsafe short test_7_17(short num, void* ptr_init, void* ptr_zero)
    {
        return AA.call_target_ref(ref (*((AA*)ptr_init)).q);
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
        if (test_0_15(100, new AA(100), new AA(0)) != 100)
        {
            Console.WriteLine("test_0_15() failed.");
            return 116;
        }
        AA.verify_all(); AA.reset();
        if (test_0_16(100, new AA(100), new AA(0)) != 100)
        {
            Console.WriteLine("test_0_16() failed.");
            return 117;
        }
        AA.verify_all(); AA.reset();
        if (test_0_17(100, new AA(100), new AA(0)) != 100)
        {
            Console.WriteLine("test_0_17() failed.");
            return 118;
        }
        AA.verify_all(); AA.reset();
        if (test_1_0(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_0() failed.");
            return 119;
        }
        AA.verify_all(); AA.reset();
        if (test_1_1(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_1() failed.");
            return 120;
        }
        AA.verify_all(); AA.reset();
        if (test_1_2(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_2() failed.");
            return 121;
        }
        AA.verify_all(); AA.reset();
        if (test_1_3(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_3() failed.");
            return 122;
        }
        AA.verify_all(); AA.reset();
        if (test_1_4(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_4() failed.");
            return 123;
        }
        AA.verify_all(); AA.reset();
        if (test_1_5(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_5() failed.");
            return 124;
        }
        AA.verify_all(); AA.reset();
        if (test_1_6(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_6() failed.");
            return 125;
        }
        AA.verify_all(); AA.reset();
        if (test_1_7(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_7() failed.");
            return 126;
        }
        AA.verify_all(); AA.reset();
        if (test_1_8(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_8() failed.");
            return 127;
        }
        AA.verify_all(); AA.reset();
        if (test_1_9(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_9() failed.");
            return 128;
        }
        AA.verify_all(); AA.reset();
        if (test_1_10(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_10() failed.");
            return 129;
        }
        AA.verify_all(); AA.reset();
        if (test_1_11(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_11() failed.");
            return 130;
        }
        AA.verify_all(); AA.reset();
        if (test_1_12(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_12() failed.");
            return 131;
        }
        AA.verify_all(); AA.reset();
        if (test_1_13(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_13() failed.");
            return 132;
        }
        AA.verify_all(); AA.reset();
        if (test_1_14(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_14() failed.");
            return 133;
        }
        AA.verify_all(); AA.reset();
        if (test_1_15(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_15() failed.");
            return 134;
        }
        AA.verify_all(); AA.reset();
        if (test_1_16(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_16() failed.");
            return 135;
        }
        AA.verify_all(); AA.reset();
        if (test_1_17(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_17() failed.");
            return 136;
        }
        AA.verify_all(); AA.reset();
        if (test_2_0(100) != 100)
        {
            Console.WriteLine("test_2_0() failed.");
            return 137;
        }
        AA.verify_all(); AA.reset();
        if (test_2_1(100) != 100)
        {
            Console.WriteLine("test_2_1() failed.");
            return 138;
        }
        AA.verify_all(); AA.reset();
        if (test_2_2(100) != 100)
        {
            Console.WriteLine("test_2_2() failed.");
            return 139;
        }
        AA.verify_all(); AA.reset();
        if (test_2_3(100) != 100)
        {
            Console.WriteLine("test_2_3() failed.");
            return 140;
        }
        AA.verify_all(); AA.reset();
        if (test_2_4(100) != 100)
        {
            Console.WriteLine("test_2_4() failed.");
            return 141;
        }
        AA.verify_all(); AA.reset();
        if (test_2_5(100) != 100)
        {
            Console.WriteLine("test_2_5() failed.");
            return 142;
        }
        AA.verify_all(); AA.reset();
        if (test_2_6(100) != 100)
        {
            Console.WriteLine("test_2_6() failed.");
            return 143;
        }
        AA.verify_all(); AA.reset();
        if (test_2_7(100) != 100)
        {
            Console.WriteLine("test_2_7() failed.");
            return 144;
        }
        AA.verify_all(); AA.reset();
        if (test_2_8(100) != 100)
        {
            Console.WriteLine("test_2_8() failed.");
            return 145;
        }
        AA.verify_all(); AA.reset();
        if (test_2_9(100) != 100)
        {
            Console.WriteLine("test_2_9() failed.");
            return 146;
        }
        AA.verify_all(); AA.reset();
        if (test_2_10(100) != 100)
        {
            Console.WriteLine("test_2_10() failed.");
            return 147;
        }
        AA.verify_all(); AA.reset();
        if (test_2_11(100) != 100)
        {
            Console.WriteLine("test_2_11() failed.");
            return 148;
        }
        AA.verify_all(); AA.reset();
        if (test_2_12(100) != 100)
        {
            Console.WriteLine("test_2_12() failed.");
            return 149;
        }
        AA.verify_all(); AA.reset();
        if (test_2_13(100) != 100)
        {
            Console.WriteLine("test_2_13() failed.");
            return 150;
        }
        AA.verify_all(); AA.reset();
        if (test_2_14(100) != 100)
        {
            Console.WriteLine("test_2_14() failed.");
            return 151;
        }
        AA.verify_all(); AA.reset();
        if (test_2_15(100) != 100)
        {
            Console.WriteLine("test_2_15() failed.");
            return 152;
        }
        AA.verify_all(); AA.reset();
        if (test_2_16(100) != 100)
        {
            Console.WriteLine("test_2_16() failed.");
            return 153;
        }
        AA.verify_all(); AA.reset();
        if (test_2_17(100) != 100)
        {
            Console.WriteLine("test_2_17() failed.");
            return 154;
        }
        AA.verify_all(); AA.reset();
        if (test_3_0(100) != 100)
        {
            Console.WriteLine("test_3_0() failed.");
            return 155;
        }
        AA.verify_all(); AA.reset();
        if (test_3_1(100) != 100)
        {
            Console.WriteLine("test_3_1() failed.");
            return 156;
        }
        AA.verify_all(); AA.reset();
        if (test_3_2(100) != 100)
        {
            Console.WriteLine("test_3_2() failed.");
            return 157;
        }
        AA.verify_all(); AA.reset();
        if (test_3_3(100) != 100)
        {
            Console.WriteLine("test_3_3() failed.");
            return 158;
        }
        AA.verify_all(); AA.reset();
        if (test_3_4(100) != 100)
        {
            Console.WriteLine("test_3_4() failed.");
            return 159;
        }
        AA.verify_all(); AA.reset();
        if (test_3_5(100) != 100)
        {
            Console.WriteLine("test_3_5() failed.");
            return 160;
        }
        AA.verify_all(); AA.reset();
        if (test_3_6(100) != 100)
        {
            Console.WriteLine("test_3_6() failed.");
            return 161;
        }
        AA.verify_all(); AA.reset();
        if (test_3_7(100) != 100)
        {
            Console.WriteLine("test_3_7() failed.");
            return 162;
        }
        AA.verify_all(); AA.reset();
        if (test_3_8(100) != 100)
        {
            Console.WriteLine("test_3_8() failed.");
            return 163;
        }
        AA.verify_all(); AA.reset();
        if (test_3_9(100) != 100)
        {
            Console.WriteLine("test_3_9() failed.");
            return 164;
        }
        AA.verify_all(); AA.reset();
        if (test_3_10(100) != 100)
        {
            Console.WriteLine("test_3_10() failed.");
            return 165;
        }
        AA.verify_all(); AA.reset();
        if (test_3_11(100) != 100)
        {
            Console.WriteLine("test_3_11() failed.");
            return 166;
        }
        AA.verify_all(); AA.reset();
        if (test_3_12(100) != 100)
        {
            Console.WriteLine("test_3_12() failed.");
            return 167;
        }
        AA.verify_all(); AA.reset();
        if (test_3_13(100) != 100)
        {
            Console.WriteLine("test_3_13() failed.");
            return 168;
        }
        AA.verify_all(); AA.reset();
        if (test_3_14(100) != 100)
        {
            Console.WriteLine("test_3_14() failed.");
            return 169;
        }
        AA.verify_all(); AA.reset();
        if (test_3_15(100) != 100)
        {
            Console.WriteLine("test_3_15() failed.");
            return 170;
        }
        AA.verify_all(); AA.reset();
        if (test_3_16(100) != 100)
        {
            Console.WriteLine("test_3_16() failed.");
            return 171;
        }
        AA.verify_all(); AA.reset();
        if (test_3_17(100) != 100)
        {
            Console.WriteLine("test_3_17() failed.");
            return 172;
        }
        AA.verify_all(); AA.reset();
        if (test_4_0(100) != 100)
        {
            Console.WriteLine("test_4_0() failed.");
            return 173;
        }
        AA.verify_all(); AA.reset();
        if (test_4_1(100) != 100)
        {
            Console.WriteLine("test_4_1() failed.");
            return 174;
        }
        AA.verify_all(); AA.reset();
        if (test_4_2(100) != 100)
        {
            Console.WriteLine("test_4_2() failed.");
            return 175;
        }
        AA.verify_all(); AA.reset();
        if (test_4_3(100) != 100)
        {
            Console.WriteLine("test_4_3() failed.");
            return 176;
        }
        AA.verify_all(); AA.reset();
        if (test_4_4(100) != 100)
        {
            Console.WriteLine("test_4_4() failed.");
            return 177;
        }
        AA.verify_all(); AA.reset();
        if (test_4_5(100) != 100)
        {
            Console.WriteLine("test_4_5() failed.");
            return 178;
        }
        AA.verify_all(); AA.reset();
        if (test_4_6(100) != 100)
        {
            Console.WriteLine("test_4_6() failed.");
            return 179;
        }
        AA.verify_all(); AA.reset();
        if (test_4_7(100) != 100)
        {
            Console.WriteLine("test_4_7() failed.");
            return 180;
        }
        AA.verify_all(); AA.reset();
        if (test_4_8(100) != 100)
        {
            Console.WriteLine("test_4_8() failed.");
            return 181;
        }
        AA.verify_all(); AA.reset();
        if (test_4_9(100) != 100)
        {
            Console.WriteLine("test_4_9() failed.");
            return 182;
        }
        AA.verify_all(); AA.reset();
        if (test_4_10(100) != 100)
        {
            Console.WriteLine("test_4_10() failed.");
            return 183;
        }
        AA.verify_all(); AA.reset();
        if (test_4_11(100) != 100)
        {
            Console.WriteLine("test_4_11() failed.");
            return 184;
        }
        AA.verify_all(); AA.reset();
        if (test_4_12(100) != 100)
        {
            Console.WriteLine("test_4_12() failed.");
            return 185;
        }
        AA.verify_all(); AA.reset();
        if (test_4_13(100) != 100)
        {
            Console.WriteLine("test_4_13() failed.");
            return 186;
        }
        AA.verify_all(); AA.reset();
        if (test_4_14(100) != 100)
        {
            Console.WriteLine("test_4_14() failed.");
            return 187;
        }
        AA.verify_all(); AA.reset();
        if (test_4_15(100) != 100)
        {
            Console.WriteLine("test_4_15() failed.");
            return 188;
        }
        AA.verify_all(); AA.reset();
        if (test_4_16(100) != 100)
        {
            Console.WriteLine("test_4_16() failed.");
            return 189;
        }
        AA.verify_all(); AA.reset();
        if (test_4_17(100) != 100)
        {
            Console.WriteLine("test_4_17() failed.");
            return 190;
        }
        AA.verify_all(); AA.reset();
        if (test_5_0(100) != 100)
        {
            Console.WriteLine("test_5_0() failed.");
            return 191;
        }
        AA.verify_all(); AA.reset();
        if (test_6_0(100, __makeref(AA._init)) != 100)
        {
            Console.WriteLine("test_6_0() failed.");
            return 192;
        }
        AA.verify_all(); AA.reset();
        fixed (void* p_init = &AA._init, p_zero = &AA._zero)
        {
            if (test_7_0(100, p_init, p_zero) != 100)
            {
                Console.WriteLine("test_7_0() failed.");
                return 193;
            }
        }
        AA.verify_all(); AA.reset();
        fixed (void* p_init = &AA._init, p_zero = &AA._zero)
        {
            if (test_7_1(100, p_init, p_zero) != 100)
            {
                Console.WriteLine("test_7_1() failed.");
                return 194;
            }
        }
        AA.verify_all(); AA.reset();
        fixed (void* p_init = &AA._init, p_zero = &AA._zero)
        {
            if (test_7_2(100, p_init, p_zero) != 100)
            {
                Console.WriteLine("test_7_2() failed.");
                return 195;
            }
        }
        AA.verify_all(); AA.reset();
        fixed (void* p_init = &AA._init, p_zero = &AA._zero)
        {
            if (test_7_3(100, p_init, p_zero) != 100)
            {
                Console.WriteLine("test_7_3() failed.");
                return 196;
            }
        }
        AA.verify_all(); AA.reset();
        fixed (void* p_init = &AA._init, p_zero = &AA._zero)
        {
            if (test_7_4(100, p_init, p_zero) != 100)
            {
                Console.WriteLine("test_7_4() failed.");
                return 197;
            }
        }
        AA.verify_all(); AA.reset();
        fixed (void* p_init = &AA._init, p_zero = &AA._zero)
        {
            if (test_7_5(100, p_init, p_zero) != 100)
            {
                Console.WriteLine("test_7_5() failed.");
                return 198;
            }
        }
        AA.verify_all(); AA.reset();
        fixed (void* p_init = &AA._init, p_zero = &AA._zero)
        {
            if (test_7_6(100, p_init, p_zero) != 100)
            {
                Console.WriteLine("test_7_6() failed.");
                return 199;
            }
        }
        AA.verify_all(); AA.reset();
        fixed (void* p_init = &AA._init, p_zero = &AA._zero)
        {
            if (test_7_7(100, p_init, p_zero) != 100)
            {
                Console.WriteLine("test_7_7() failed.");
                return 200;
            }
        }
        AA.verify_all(); AA.reset();
        fixed (void* p_init = &AA._init, p_zero = &AA._zero)
        {
            if (test_7_8(100, p_init, p_zero) != 100)
            {
                Console.WriteLine("test_7_8() failed.");
                return 201;
            }
        }
        AA.verify_all(); AA.reset();
        fixed (void* p_init = &AA._init, p_zero = &AA._zero)
        {
            if (test_7_9(100, p_init, p_zero) != 100)
            {
                Console.WriteLine("test_7_9() failed.");
                return 202;
            }
        }
        AA.verify_all(); AA.reset();
        fixed (void* p_init = &AA._init, p_zero = &AA._zero)
        {
            if (test_7_10(100, p_init, p_zero) != 100)
            {
                Console.WriteLine("test_7_10() failed.");
                return 203;
            }
        }
        AA.verify_all(); AA.reset();
        fixed (void* p_init = &AA._init, p_zero = &AA._zero)
        {
            if (test_7_11(100, p_init, p_zero) != 100)
            {
                Console.WriteLine("test_7_11() failed.");
                return 204;
            }
        }
        AA.verify_all(); AA.reset();
        fixed (void* p_init = &AA._init, p_zero = &AA._zero)
        {
            if (test_7_12(100, p_init, p_zero) != 100)
            {
                Console.WriteLine("test_7_12() failed.");
                return 205;
            }
        }
        AA.verify_all(); AA.reset();
        fixed (void* p_init = &AA._init, p_zero = &AA._zero)
        {
            if (test_7_13(100, p_init, p_zero) != 100)
            {
                Console.WriteLine("test_7_13() failed.");
                return 206;
            }
        }
        AA.verify_all(); AA.reset();
        fixed (void* p_init = &AA._init, p_zero = &AA._zero)
        {
            if (test_7_14(100, p_init, p_zero) != 100)
            {
                Console.WriteLine("test_7_14() failed.");
                return 207;
            }
        }
        AA.verify_all(); AA.reset();
        fixed (void* p_init = &AA._init, p_zero = &AA._zero)
        {
            if (test_7_15(100, p_init, p_zero) != 100)
            {
                Console.WriteLine("test_7_15() failed.");
                return 208;
            }
        }
        AA.verify_all(); AA.reset();
        fixed (void* p_init = &AA._init, p_zero = &AA._zero)
        {
            if (test_7_16(100, p_init, p_zero) != 100)
            {
                Console.WriteLine("test_7_16() failed.");
                return 209;
            }
        }
        AA.verify_all(); AA.reset();
        fixed (void* p_init = &AA._init, p_zero = &AA._zero)
        {
            if (test_7_17(100, p_init, p_zero) != 100)
            {
                Console.WriteLine("test_7_17() failed.");
                return 210;
            }
        }
        AA.verify_all(); Console.WriteLine("All tests passed.");
        return 100;
    }
}
