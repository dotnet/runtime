// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

internal class TestApp
{
    private static float test_0_0(float num, AA init, AA zero)
    {
        return init.q;
    }
    private static float test_0_1(float num, AA init, AA zero)
    {
        zero.q = num;
        return zero.q;
    }
    private static float test_0_2(float num, AA init, AA zero)
    {
        return init.q + zero.q;
    }
    private static float test_0_3(float num, AA init, AA zero)
    {
        return checked(init.q - zero.q);
    }
    private static float test_0_4(float num, AA init, AA zero)
    {
        zero.q += num; return zero.q;
    }
    private static float test_0_5(float num, AA init, AA zero)
    {
        zero.q += init.q; return zero.q;
    }
    private static float test_0_6(float num, AA init, AA zero)
    {
        if (init.q == num)
            return 100;
        else
            return zero.q;
    }
    private static float test_0_7(float num, AA init, AA zero)
    {
        return init.q < num + 1 ? 100 : -1;
    }
    private static float test_0_8(float num, AA init, AA zero)
    {
        return (init.q > zero.q ? 1 : 0) + 99;
    }
    private static float test_0_9(float num, AA init, AA zero)
    {
        object bb = init.q;
        return (float)bb;
    }
    private static float test_0_10(float num, AA init, AA zero)
    {
        double dbl = init.q;
        return (float)dbl;
    }
    private static float test_0_11(float num, AA init, AA zero)
    {
        return AA.call_target(init.q);
    }
    private static float test_0_12(float num, AA init, AA zero)
    {
        return AA.call_target_ref(ref init.q);
    }
    private static float test_1_0(float num, ref AA r_init, ref AA r_zero)
    {
        return r_init.q;
    }
    private static float test_1_1(float num, ref AA r_init, ref AA r_zero)
    {
        r_zero.q = num;
        return r_zero.q;
    }
    private static float test_1_2(float num, ref AA r_init, ref AA r_zero)
    {
        return r_init.q + r_zero.q;
    }
    private static float test_1_3(float num, ref AA r_init, ref AA r_zero)
    {
        return checked(r_init.q - r_zero.q);
    }
    private static float test_1_4(float num, ref AA r_init, ref AA r_zero)
    {
        r_zero.q += num; return r_zero.q;
    }
    private static float test_1_5(float num, ref AA r_init, ref AA r_zero)
    {
        r_zero.q += r_init.q; return r_zero.q;
    }
    private static float test_1_6(float num, ref AA r_init, ref AA r_zero)
    {
        if (r_init.q == num)
            return 100;
        else
            return r_zero.q;
    }
    private static float test_1_7(float num, ref AA r_init, ref AA r_zero)
    {
        return r_init.q < num + 1 ? 100 : -1;
    }
    private static float test_1_8(float num, ref AA r_init, ref AA r_zero)
    {
        return (r_init.q > r_zero.q ? 1 : 0) + 99;
    }
    private static float test_1_9(float num, ref AA r_init, ref AA r_zero)
    {
        object bb = r_init.q;
        return (float)bb;
    }
    private static float test_1_10(float num, ref AA r_init, ref AA r_zero)
    {
        double dbl = r_init.q;
        return (float)dbl;
    }
    private static float test_1_11(float num, ref AA r_init, ref AA r_zero)
    {
        return AA.call_target(r_init.q);
    }
    private static float test_1_12(float num, ref AA r_init, ref AA r_zero)
    {
        return AA.call_target_ref(ref r_init.q);
    }
    private static float test_2_0(float num)
    {
        return AA.a_init[(int)num].q;
    }
    private static float test_2_1(float num)
    {
        AA.a_zero[(int)num].q = num;
        return AA.a_zero[(int)num].q;
    }
    private static float test_2_2(float num)
    {
        return AA.a_init[(int)num].q + AA.a_zero[(int)num].q;
    }
    private static float test_2_3(float num)
    {
        return checked(AA.a_init[(int)num].q - AA.a_zero[(int)num].q);
    }
    private static float test_2_4(float num)
    {
        AA.a_zero[(int)num].q += num; return AA.a_zero[(int)num].q;
    }
    private static float test_2_5(float num)
    {
        AA.a_zero[(int)num].q += AA.a_init[(int)num].q; return AA.a_zero[(int)num].q;
    }
    private static float test_2_6(float num)
    {
        if (AA.a_init[(int)num].q == num)
            return 100;
        else
            return AA.a_zero[(int)num].q;
    }
    private static float test_2_7(float num)
    {
        return AA.a_init[(int)num].q < num + 1 ? 100 : -1;
    }
    private static float test_2_8(float num)
    {
        return (AA.a_init[(int)num].q > AA.a_zero[(int)num].q ? 1 : 0) + 99;
    }
    private static float test_2_9(float num)
    {
        object bb = AA.a_init[(int)num].q;
        return (float)bb;
    }
    private static float test_2_10(float num)
    {
        double dbl = AA.a_init[(int)num].q;
        return (float)dbl;
    }
    private static float test_2_11(float num)
    {
        return AA.call_target(AA.a_init[(int)num].q);
    }
    private static float test_2_12(float num)
    {
        return AA.call_target_ref(ref AA.a_init[(int)num].q);
    }
    private static float test_3_0(float num)
    {
        return AA.aa_init[0, (int)num - 1, (int)num / 100].q;
    }
    private static float test_3_1(float num)
    {
        AA.aa_zero[0, (int)num - 1, (int)num / 100].q = num;
        return AA.aa_zero[0, (int)num - 1, (int)num / 100].q;
    }
    private static float test_3_2(float num)
    {
        return AA.aa_init[0, (int)num - 1, (int)num / 100].q + AA.aa_zero[0, (int)num - 1, (int)num / 100].q;
    }
    private static float test_3_3(float num)
    {
        return checked(AA.aa_init[0, (int)num - 1, (int)num / 100].q - AA.aa_zero[0, (int)num - 1, (int)num / 100].q);
    }
    private static float test_3_4(float num)
    {
        AA.aa_zero[0, (int)num - 1, (int)num / 100].q += num; return AA.aa_zero[0, (int)num - 1, (int)num / 100].q;
    }
    private static float test_3_5(float num)
    {
        AA.aa_zero[0, (int)num - 1, (int)num / 100].q += AA.aa_init[0, (int)num - 1, (int)num / 100].q; return AA.aa_zero[0, (int)num - 1, (int)num / 100].q;
    }
    private static float test_3_6(float num)
    {
        if (AA.aa_init[0, (int)num - 1, (int)num / 100].q == num)
            return 100;
        else
            return AA.aa_zero[0, (int)num - 1, (int)num / 100].q;
    }
    private static float test_3_7(float num)
    {
        return AA.aa_init[0, (int)num - 1, (int)num / 100].q < num + 1 ? 100 : -1;
    }
    private static float test_3_8(float num)
    {
        return (AA.aa_init[0, (int)num - 1, (int)num / 100].q > AA.aa_zero[0, (int)num - 1, (int)num / 100].q ? 1 : 0) + 99;
    }
    private static float test_3_9(float num)
    {
        object bb = AA.aa_init[0, (int)num - 1, (int)num / 100].q;
        return (float)bb;
    }
    private static float test_3_10(float num)
    {
        double dbl = AA.aa_init[0, (int)num - 1, (int)num / 100].q;
        return (float)dbl;
    }
    private static float test_3_11(float num)
    {
        return AA.call_target(AA.aa_init[0, (int)num - 1, (int)num / 100].q);
    }
    private static float test_3_12(float num)
    {
        return AA.call_target_ref(ref AA.aa_init[0, (int)num - 1, (int)num / 100].q);
    }
    private static float test_4_0(float num)
    {
        return BB.f_init.q;
    }
    private static float test_4_1(float num)
    {
        BB.f_zero.q = num;
        return BB.f_zero.q;
    }
    private static float test_4_2(float num)
    {
        return BB.f_init.q + BB.f_zero.q;
    }
    private static float test_4_3(float num)
    {
        return checked(BB.f_init.q - BB.f_zero.q);
    }
    private static float test_4_4(float num)
    {
        BB.f_zero.q += num; return BB.f_zero.q;
    }
    private static float test_4_5(float num)
    {
        BB.f_zero.q += BB.f_init.q; return BB.f_zero.q;
    }
    private static float test_4_6(float num)
    {
        if (BB.f_init.q == num)
            return 100;
        else
            return BB.f_zero.q;
    }
    private static float test_4_7(float num)
    {
        return BB.f_init.q < num + 1 ? 100 : -1;
    }
    private static float test_4_8(float num)
    {
        return (BB.f_init.q > BB.f_zero.q ? 1 : 0) + 99;
    }
    private static float test_4_9(float num)
    {
        object bb = BB.f_init.q;
        return (float)bb;
    }
    private static float test_4_10(float num)
    {
        double dbl = BB.f_init.q;
        return (float)dbl;
    }
    private static float test_4_11(float num)
    {
        return AA.call_target(BB.f_init.q);
    }
    private static float test_4_12(float num)
    {
        return AA.call_target_ref(ref BB.f_init.q);
    }
    private static float test_5_0(float num)
    {
        return ((AA)AA.b_init).q;
    }
    private static float test_6_0(float num, TypedReference tr_init)
    {
        return __refvalue(tr_init, AA).q;
    }

    private static unsafe int Main()
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
        if (test_1_0(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_0() failed.");
            return 114;
        }
        AA.verify_all(); AA.reset();
        if (test_1_1(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_1() failed.");
            return 115;
        }
        AA.verify_all(); AA.reset();
        if (test_1_2(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_2() failed.");
            return 116;
        }
        AA.verify_all(); AA.reset();
        if (test_1_3(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_3() failed.");
            return 117;
        }
        AA.verify_all(); AA.reset();
        if (test_1_4(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_4() failed.");
            return 118;
        }
        AA.verify_all(); AA.reset();
        if (test_1_5(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_5() failed.");
            return 119;
        }
        AA.verify_all(); AA.reset();
        if (test_1_6(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_6() failed.");
            return 120;
        }
        AA.verify_all(); AA.reset();
        if (test_1_7(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_7() failed.");
            return 121;
        }
        AA.verify_all(); AA.reset();
        if (test_1_8(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_8() failed.");
            return 122;
        }
        AA.verify_all(); AA.reset();
        if (test_1_9(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_9() failed.");
            return 123;
        }
        AA.verify_all(); AA.reset();
        if (test_1_10(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_10() failed.");
            return 124;
        }
        AA.verify_all(); AA.reset();
        if (test_1_11(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_11() failed.");
            return 125;
        }
        AA.verify_all(); AA.reset();
        if (test_1_12(100, ref AA._init, ref AA._zero) != 100)
        {
            Console.WriteLine("test_1_12() failed.");
            return 126;
        }
        AA.verify_all(); AA.reset();
        if (test_2_0(100) != 100)
        {
            Console.WriteLine("test_2_0() failed.");
            return 127;
        }
        AA.verify_all(); AA.reset();
        if (test_2_1(100) != 100)
        {
            Console.WriteLine("test_2_1() failed.");
            return 128;
        }
        AA.verify_all(); AA.reset();
        if (test_2_2(100) != 100)
        {
            Console.WriteLine("test_2_2() failed.");
            return 129;
        }
        AA.verify_all(); AA.reset();
        if (test_2_3(100) != 100)
        {
            Console.WriteLine("test_2_3() failed.");
            return 130;
        }
        AA.verify_all(); AA.reset();
        if (test_2_4(100) != 100)
        {
            Console.WriteLine("test_2_4() failed.");
            return 131;
        }
        AA.verify_all(); AA.reset();
        if (test_2_5(100) != 100)
        {
            Console.WriteLine("test_2_5() failed.");
            return 132;
        }
        AA.verify_all(); AA.reset();
        if (test_2_6(100) != 100)
        {
            Console.WriteLine("test_2_6() failed.");
            return 133;
        }
        AA.verify_all(); AA.reset();
        if (test_2_7(100) != 100)
        {
            Console.WriteLine("test_2_7() failed.");
            return 134;
        }
        AA.verify_all(); AA.reset();
        if (test_2_8(100) != 100)
        {
            Console.WriteLine("test_2_8() failed.");
            return 135;
        }
        AA.verify_all(); AA.reset();
        if (test_2_9(100) != 100)
        {
            Console.WriteLine("test_2_9() failed.");
            return 136;
        }
        AA.verify_all(); AA.reset();
        if (test_2_10(100) != 100)
        {
            Console.WriteLine("test_2_10() failed.");
            return 137;
        }
        AA.verify_all(); AA.reset();
        if (test_2_11(100) != 100)
        {
            Console.WriteLine("test_2_11() failed.");
            return 138;
        }
        AA.verify_all(); AA.reset();
        if (test_2_12(100) != 100)
        {
            Console.WriteLine("test_2_12() failed.");
            return 139;
        }
        AA.verify_all(); AA.reset();
        if (test_3_0(100) != 100)
        {
            Console.WriteLine("test_3_0() failed.");
            return 140;
        }
        AA.verify_all(); AA.reset();
        if (test_3_1(100) != 100)
        {
            Console.WriteLine("test_3_1() failed.");
            return 141;
        }
        AA.verify_all(); AA.reset();
        if (test_3_2(100) != 100)
        {
            Console.WriteLine("test_3_2() failed.");
            return 142;
        }
        AA.verify_all(); AA.reset();
        if (test_3_3(100) != 100)
        {
            Console.WriteLine("test_3_3() failed.");
            return 143;
        }
        AA.verify_all(); AA.reset();
        if (test_3_4(100) != 100)
        {
            Console.WriteLine("test_3_4() failed.");
            return 144;
        }
        AA.verify_all(); AA.reset();
        if (test_3_5(100) != 100)
        {
            Console.WriteLine("test_3_5() failed.");
            return 145;
        }
        AA.verify_all(); AA.reset();
        if (test_3_6(100) != 100)
        {
            Console.WriteLine("test_3_6() failed.");
            return 146;
        }
        AA.verify_all(); AA.reset();
        if (test_3_7(100) != 100)
        {
            Console.WriteLine("test_3_7() failed.");
            return 147;
        }
        AA.verify_all(); AA.reset();
        if (test_3_8(100) != 100)
        {
            Console.WriteLine("test_3_8() failed.");
            return 148;
        }
        AA.verify_all(); AA.reset();
        if (test_3_9(100) != 100)
        {
            Console.WriteLine("test_3_9() failed.");
            return 149;
        }
        AA.verify_all(); AA.reset();
        if (test_3_10(100) != 100)
        {
            Console.WriteLine("test_3_10() failed.");
            return 150;
        }
        AA.verify_all(); AA.reset();
        if (test_3_11(100) != 100)
        {
            Console.WriteLine("test_3_11() failed.");
            return 151;
        }
        AA.verify_all(); AA.reset();
        if (test_3_12(100) != 100)
        {
            Console.WriteLine("test_3_12() failed.");
            return 152;
        }
        AA.verify_all(); AA.reset();
        if (test_4_0(100) != 100)
        {
            Console.WriteLine("test_4_0() failed.");
            return 153;
        }
        AA.verify_all(); AA.reset();
        if (test_4_1(100) != 100)
        {
            Console.WriteLine("test_4_1() failed.");
            return 154;
        }
        AA.verify_all(); AA.reset();
        if (test_4_2(100) != 100)
        {
            Console.WriteLine("test_4_2() failed.");
            return 155;
        }
        AA.verify_all(); AA.reset();
        if (test_4_3(100) != 100)
        {
            Console.WriteLine("test_4_3() failed.");
            return 156;
        }
        AA.verify_all(); AA.reset();
        if (test_4_4(100) != 100)
        {
            Console.WriteLine("test_4_4() failed.");
            return 157;
        }
        AA.verify_all(); AA.reset();
        if (test_4_5(100) != 100)
        {
            Console.WriteLine("test_4_5() failed.");
            return 158;
        }
        AA.verify_all(); AA.reset();
        if (test_4_6(100) != 100)
        {
            Console.WriteLine("test_4_6() failed.");
            return 159;
        }
        AA.verify_all(); AA.reset();
        if (test_4_7(100) != 100)
        {
            Console.WriteLine("test_4_7() failed.");
            return 160;
        }
        AA.verify_all(); AA.reset();
        if (test_4_8(100) != 100)
        {
            Console.WriteLine("test_4_8() failed.");
            return 161;
        }
        AA.verify_all(); AA.reset();
        if (test_4_9(100) != 100)
        {
            Console.WriteLine("test_4_9() failed.");
            return 162;
        }
        AA.verify_all(); AA.reset();
        if (test_4_10(100) != 100)
        {
            Console.WriteLine("test_4_10() failed.");
            return 163;
        }
        AA.verify_all(); AA.reset();
        if (test_4_11(100) != 100)
        {
            Console.WriteLine("test_4_11() failed.");
            return 164;
        }
        AA.verify_all(); AA.reset();
        if (test_4_12(100) != 100)
        {
            Console.WriteLine("test_4_12() failed.");
            return 165;
        }
        AA.verify_all(); AA.reset();
        if (test_5_0(100) != 100)
        {
            Console.WriteLine("test_5_0() failed.");
            return 166;
        }
        AA.verify_all(); AA.reset();
        if (test_6_0(100, __makeref(AA._init)) != 100)
        {
            Console.WriteLine("test_6_0() failed.");
            return 167;
        }
        AA.verify_all(); Console.WriteLine("All tests passed.");
        return 100;
    }
}
