// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#pragma warning disable

using System;
using Xunit;
public class testout1
{
    static int static_field_int;
    static bool sfb_false;
    static bool sfb_true;
    int mfi;
    bool mfb_false;
    bool mfb_true;
    static int simple_func_int()
    {
        return 17;
    }
    static bool func_sb_true()
    {
        return true;
    }
    static bool func_sb_false()
    {
        return false;
    }

    static int Sub_Funclet_0()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ true ? 3 : 3);
        Sum += (true ^ true ? 3 : -5);
        Sum += (true ^ true ? 3 : local_int);
        Sum += (true ^ true ? 3 : static_field_int);
        Sum += (true ^ true ? 3 : t1_i.mfi);
        Sum += (true ^ true ? 3 : simple_func_int());
        Sum += (true ^ true ? 3 : ab[index]);
        Sum += (true ^ true ? 3 : ab[index - 1]);
        Sum += (true ^ true ? -5 : 3);
        Sum += (true ^ true ? -5 : -5);
        Sum += (true ^ true ? -5 : local_int);
        Sum += (true ^ true ? -5 : static_field_int);
        Sum += (true ^ true ? -5 : t1_i.mfi);
        Sum += (true ^ true ? -5 : simple_func_int());
        Sum += (true ^ true ? -5 : ab[index]);
        Sum += (true ^ true ? -5 : ab[index - 1]);
        Sum += (true ^ true ? local_int : 3);
        Sum += (true ^ true ? local_int : -5);
        Sum += (true ^ true ? local_int : local_int);
        Sum += (true ^ true ? local_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_1()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ true ? local_int : t1_i.mfi);
        Sum += (true ^ true ? local_int : simple_func_int());
        Sum += (true ^ true ? local_int : ab[index]);
        Sum += (true ^ true ? local_int : ab[index - 1]);
        Sum += (true ^ true ? static_field_int : 3);
        Sum += (true ^ true ? static_field_int : -5);
        Sum += (true ^ true ? static_field_int : local_int);
        Sum += (true ^ true ? static_field_int : static_field_int);
        Sum += (true ^ true ? static_field_int : t1_i.mfi);
        Sum += (true ^ true ? static_field_int : simple_func_int());
        Sum += (true ^ true ? static_field_int : ab[index]);
        Sum += (true ^ true ? static_field_int : ab[index - 1]);
        Sum += (true ^ true ? t1_i.mfi : 3);
        Sum += (true ^ true ? t1_i.mfi : -5);
        Sum += (true ^ true ? t1_i.mfi : local_int);
        Sum += (true ^ true ? t1_i.mfi : static_field_int);
        Sum += (true ^ true ? t1_i.mfi : t1_i.mfi);
        Sum += (true ^ true ? t1_i.mfi : simple_func_int());
        Sum += (true ^ true ? t1_i.mfi : ab[index]);
        Sum += (true ^ true ? t1_i.mfi : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_2()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ true ? simple_func_int() : 3);
        Sum += (true ^ true ? simple_func_int() : -5);
        Sum += (true ^ true ? simple_func_int() : local_int);
        Sum += (true ^ true ? simple_func_int() : static_field_int);
        Sum += (true ^ true ? simple_func_int() : t1_i.mfi);
        Sum += (true ^ true ? simple_func_int() : simple_func_int());
        Sum += (true ^ true ? simple_func_int() : ab[index]);
        Sum += (true ^ true ? simple_func_int() : ab[index - 1]);
        Sum += (true ^ true ? ab[index] : 3);
        Sum += (true ^ true ? ab[index] : -5);
        Sum += (true ^ true ? ab[index] : local_int);
        Sum += (true ^ true ? ab[index] : static_field_int);
        Sum += (true ^ true ? ab[index] : t1_i.mfi);
        Sum += (true ^ true ? ab[index] : simple_func_int());
        Sum += (true ^ true ? ab[index] : ab[index]);
        Sum += (true ^ true ? ab[index] : ab[index - 1]);
        Sum += (true ^ true ? ab[index - 1] : 3);
        Sum += (true ^ true ? ab[index - 1] : -5);
        Sum += (true ^ true ? ab[index - 1] : local_int);
        Sum += (true ^ true ? ab[index - 1] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_3()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ true ? ab[index - 1] : t1_i.mfi);
        Sum += (true ^ true ? ab[index - 1] : simple_func_int());
        Sum += (true ^ true ? ab[index - 1] : ab[index]);
        Sum += (true ^ true ? ab[index - 1] : ab[index - 1]);
        Sum += (true ^ false ? 3 : 3);
        Sum += (true ^ false ? 3 : -5);
        Sum += (true ^ false ? 3 : local_int);
        Sum += (true ^ false ? 3 : static_field_int);
        Sum += (true ^ false ? 3 : t1_i.mfi);
        Sum += (true ^ false ? 3 : simple_func_int());
        Sum += (true ^ false ? 3 : ab[index]);
        Sum += (true ^ false ? 3 : ab[index - 1]);
        Sum += (true ^ false ? -5 : 3);
        Sum += (true ^ false ? -5 : -5);
        Sum += (true ^ false ? -5 : local_int);
        Sum += (true ^ false ? -5 : static_field_int);
        Sum += (true ^ false ? -5 : t1_i.mfi);
        Sum += (true ^ false ? -5 : simple_func_int());
        Sum += (true ^ false ? -5 : ab[index]);
        Sum += (true ^ false ? -5 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_4()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ false ? local_int : 3);
        Sum += (true ^ false ? local_int : -5);
        Sum += (true ^ false ? local_int : local_int);
        Sum += (true ^ false ? local_int : static_field_int);
        Sum += (true ^ false ? local_int : t1_i.mfi);
        Sum += (true ^ false ? local_int : simple_func_int());
        Sum += (true ^ false ? local_int : ab[index]);
        Sum += (true ^ false ? local_int : ab[index - 1]);
        Sum += (true ^ false ? static_field_int : 3);
        Sum += (true ^ false ? static_field_int : -5);
        Sum += (true ^ false ? static_field_int : local_int);
        Sum += (true ^ false ? static_field_int : static_field_int);
        Sum += (true ^ false ? static_field_int : t1_i.mfi);
        Sum += (true ^ false ? static_field_int : simple_func_int());
        Sum += (true ^ false ? static_field_int : ab[index]);
        Sum += (true ^ false ? static_field_int : ab[index - 1]);
        Sum += (true ^ false ? t1_i.mfi : 3);
        Sum += (true ^ false ? t1_i.mfi : -5);
        Sum += (true ^ false ? t1_i.mfi : local_int);
        Sum += (true ^ false ? t1_i.mfi : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_5()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ false ? t1_i.mfi : t1_i.mfi);
        Sum += (true ^ false ? t1_i.mfi : simple_func_int());
        Sum += (true ^ false ? t1_i.mfi : ab[index]);
        Sum += (true ^ false ? t1_i.mfi : ab[index - 1]);
        Sum += (true ^ false ? simple_func_int() : 3);
        Sum += (true ^ false ? simple_func_int() : -5);
        Sum += (true ^ false ? simple_func_int() : local_int);
        Sum += (true ^ false ? simple_func_int() : static_field_int);
        Sum += (true ^ false ? simple_func_int() : t1_i.mfi);
        Sum += (true ^ false ? simple_func_int() : simple_func_int());
        Sum += (true ^ false ? simple_func_int() : ab[index]);
        Sum += (true ^ false ? simple_func_int() : ab[index - 1]);
        Sum += (true ^ false ? ab[index] : 3);
        Sum += (true ^ false ? ab[index] : -5);
        Sum += (true ^ false ? ab[index] : local_int);
        Sum += (true ^ false ? ab[index] : static_field_int);
        Sum += (true ^ false ? ab[index] : t1_i.mfi);
        Sum += (true ^ false ? ab[index] : simple_func_int());
        Sum += (true ^ false ? ab[index] : ab[index]);
        Sum += (true ^ false ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_6()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ false ? ab[index - 1] : 3);
        Sum += (true ^ false ? ab[index - 1] : -5);
        Sum += (true ^ false ? ab[index - 1] : local_int);
        Sum += (true ^ false ? ab[index - 1] : static_field_int);
        Sum += (true ^ false ? ab[index - 1] : t1_i.mfi);
        Sum += (true ^ false ? ab[index - 1] : simple_func_int());
        Sum += (true ^ false ? ab[index - 1] : ab[index]);
        Sum += (true ^ false ? ab[index - 1] : ab[index - 1]);
        Sum += (true ^ lb_true ? 3 : 3);
        Sum += (true ^ lb_true ? 3 : -5);
        Sum += (true ^ lb_true ? 3 : local_int);
        Sum += (true ^ lb_true ? 3 : static_field_int);
        Sum += (true ^ lb_true ? 3 : t1_i.mfi);
        Sum += (true ^ lb_true ? 3 : simple_func_int());
        Sum += (true ^ lb_true ? 3 : ab[index]);
        Sum += (true ^ lb_true ? 3 : ab[index - 1]);
        Sum += (true ^ lb_true ? -5 : 3);
        Sum += (true ^ lb_true ? -5 : -5);
        Sum += (true ^ lb_true ? -5 : local_int);
        Sum += (true ^ lb_true ? -5 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_7()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ lb_true ? -5 : t1_i.mfi);
        Sum += (true ^ lb_true ? -5 : simple_func_int());
        Sum += (true ^ lb_true ? -5 : ab[index]);
        Sum += (true ^ lb_true ? -5 : ab[index - 1]);
        Sum += (true ^ lb_true ? local_int : 3);
        Sum += (true ^ lb_true ? local_int : -5);
        Sum += (true ^ lb_true ? local_int : local_int);
        Sum += (true ^ lb_true ? local_int : static_field_int);
        Sum += (true ^ lb_true ? local_int : t1_i.mfi);
        Sum += (true ^ lb_true ? local_int : simple_func_int());
        Sum += (true ^ lb_true ? local_int : ab[index]);
        Sum += (true ^ lb_true ? local_int : ab[index - 1]);
        Sum += (true ^ lb_true ? static_field_int : 3);
        Sum += (true ^ lb_true ? static_field_int : -5);
        Sum += (true ^ lb_true ? static_field_int : local_int);
        Sum += (true ^ lb_true ? static_field_int : static_field_int);
        Sum += (true ^ lb_true ? static_field_int : t1_i.mfi);
        Sum += (true ^ lb_true ? static_field_int : simple_func_int());
        Sum += (true ^ lb_true ? static_field_int : ab[index]);
        Sum += (true ^ lb_true ? static_field_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_8()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ lb_true ? t1_i.mfi : 3);
        Sum += (true ^ lb_true ? t1_i.mfi : -5);
        Sum += (true ^ lb_true ? t1_i.mfi : local_int);
        Sum += (true ^ lb_true ? t1_i.mfi : static_field_int);
        Sum += (true ^ lb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (true ^ lb_true ? t1_i.mfi : simple_func_int());
        Sum += (true ^ lb_true ? t1_i.mfi : ab[index]);
        Sum += (true ^ lb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (true ^ lb_true ? simple_func_int() : 3);
        Sum += (true ^ lb_true ? simple_func_int() : -5);
        Sum += (true ^ lb_true ? simple_func_int() : local_int);
        Sum += (true ^ lb_true ? simple_func_int() : static_field_int);
        Sum += (true ^ lb_true ? simple_func_int() : t1_i.mfi);
        Sum += (true ^ lb_true ? simple_func_int() : simple_func_int());
        Sum += (true ^ lb_true ? simple_func_int() : ab[index]);
        Sum += (true ^ lb_true ? simple_func_int() : ab[index - 1]);
        Sum += (true ^ lb_true ? ab[index] : 3);
        Sum += (true ^ lb_true ? ab[index] : -5);
        Sum += (true ^ lb_true ? ab[index] : local_int);
        Sum += (true ^ lb_true ? ab[index] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_9()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ lb_true ? ab[index] : t1_i.mfi);
        Sum += (true ^ lb_true ? ab[index] : simple_func_int());
        Sum += (true ^ lb_true ? ab[index] : ab[index]);
        Sum += (true ^ lb_true ? ab[index] : ab[index - 1]);
        Sum += (true ^ lb_true ? ab[index - 1] : 3);
        Sum += (true ^ lb_true ? ab[index - 1] : -5);
        Sum += (true ^ lb_true ? ab[index - 1] : local_int);
        Sum += (true ^ lb_true ? ab[index - 1] : static_field_int);
        Sum += (true ^ lb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (true ^ lb_true ? ab[index - 1] : simple_func_int());
        Sum += (true ^ lb_true ? ab[index - 1] : ab[index]);
        Sum += (true ^ lb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (true ^ lb_false ? 3 : 3);
        Sum += (true ^ lb_false ? 3 : -5);
        Sum += (true ^ lb_false ? 3 : local_int);
        Sum += (true ^ lb_false ? 3 : static_field_int);
        Sum += (true ^ lb_false ? 3 : t1_i.mfi);
        Sum += (true ^ lb_false ? 3 : simple_func_int());
        Sum += (true ^ lb_false ? 3 : ab[index]);
        Sum += (true ^ lb_false ? 3 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_10()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ lb_false ? -5 : 3);
        Sum += (true ^ lb_false ? -5 : -5);
        Sum += (true ^ lb_false ? -5 : local_int);
        Sum += (true ^ lb_false ? -5 : static_field_int);
        Sum += (true ^ lb_false ? -5 : t1_i.mfi);
        Sum += (true ^ lb_false ? -5 : simple_func_int());
        Sum += (true ^ lb_false ? -5 : ab[index]);
        Sum += (true ^ lb_false ? -5 : ab[index - 1]);
        Sum += (true ^ lb_false ? local_int : 3);
        Sum += (true ^ lb_false ? local_int : -5);
        Sum += (true ^ lb_false ? local_int : local_int);
        Sum += (true ^ lb_false ? local_int : static_field_int);
        Sum += (true ^ lb_false ? local_int : t1_i.mfi);
        Sum += (true ^ lb_false ? local_int : simple_func_int());
        Sum += (true ^ lb_false ? local_int : ab[index]);
        Sum += (true ^ lb_false ? local_int : ab[index - 1]);
        Sum += (true ^ lb_false ? static_field_int : 3);
        Sum += (true ^ lb_false ? static_field_int : -5);
        Sum += (true ^ lb_false ? static_field_int : local_int);
        Sum += (true ^ lb_false ? static_field_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_11()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ lb_false ? static_field_int : t1_i.mfi);
        Sum += (true ^ lb_false ? static_field_int : simple_func_int());
        Sum += (true ^ lb_false ? static_field_int : ab[index]);
        Sum += (true ^ lb_false ? static_field_int : ab[index - 1]);
        Sum += (true ^ lb_false ? t1_i.mfi : 3);
        Sum += (true ^ lb_false ? t1_i.mfi : -5);
        Sum += (true ^ lb_false ? t1_i.mfi : local_int);
        Sum += (true ^ lb_false ? t1_i.mfi : static_field_int);
        Sum += (true ^ lb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (true ^ lb_false ? t1_i.mfi : simple_func_int());
        Sum += (true ^ lb_false ? t1_i.mfi : ab[index]);
        Sum += (true ^ lb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (true ^ lb_false ? simple_func_int() : 3);
        Sum += (true ^ lb_false ? simple_func_int() : -5);
        Sum += (true ^ lb_false ? simple_func_int() : local_int);
        Sum += (true ^ lb_false ? simple_func_int() : static_field_int);
        Sum += (true ^ lb_false ? simple_func_int() : t1_i.mfi);
        Sum += (true ^ lb_false ? simple_func_int() : simple_func_int());
        Sum += (true ^ lb_false ? simple_func_int() : ab[index]);
        Sum += (true ^ lb_false ? simple_func_int() : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_12()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ lb_false ? ab[index] : 3);
        Sum += (true ^ lb_false ? ab[index] : -5);
        Sum += (true ^ lb_false ? ab[index] : local_int);
        Sum += (true ^ lb_false ? ab[index] : static_field_int);
        Sum += (true ^ lb_false ? ab[index] : t1_i.mfi);
        Sum += (true ^ lb_false ? ab[index] : simple_func_int());
        Sum += (true ^ lb_false ? ab[index] : ab[index]);
        Sum += (true ^ lb_false ? ab[index] : ab[index - 1]);
        Sum += (true ^ lb_false ? ab[index - 1] : 3);
        Sum += (true ^ lb_false ? ab[index - 1] : -5);
        Sum += (true ^ lb_false ? ab[index - 1] : local_int);
        Sum += (true ^ lb_false ? ab[index - 1] : static_field_int);
        Sum += (true ^ lb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (true ^ lb_false ? ab[index - 1] : simple_func_int());
        Sum += (true ^ lb_false ? ab[index - 1] : ab[index]);
        Sum += (true ^ lb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (true ^ sfb_true ? 3 : 3);
        Sum += (true ^ sfb_true ? 3 : -5);
        Sum += (true ^ sfb_true ? 3 : local_int);
        Sum += (true ^ sfb_true ? 3 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_13()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ sfb_true ? 3 : t1_i.mfi);
        Sum += (true ^ sfb_true ? 3 : simple_func_int());
        Sum += (true ^ sfb_true ? 3 : ab[index]);
        Sum += (true ^ sfb_true ? 3 : ab[index - 1]);
        Sum += (true ^ sfb_true ? -5 : 3);
        Sum += (true ^ sfb_true ? -5 : -5);
        Sum += (true ^ sfb_true ? -5 : local_int);
        Sum += (true ^ sfb_true ? -5 : static_field_int);
        Sum += (true ^ sfb_true ? -5 : t1_i.mfi);
        Sum += (true ^ sfb_true ? -5 : simple_func_int());
        Sum += (true ^ sfb_true ? -5 : ab[index]);
        Sum += (true ^ sfb_true ? -5 : ab[index - 1]);
        Sum += (true ^ sfb_true ? local_int : 3);
        Sum += (true ^ sfb_true ? local_int : -5);
        Sum += (true ^ sfb_true ? local_int : local_int);
        Sum += (true ^ sfb_true ? local_int : static_field_int);
        Sum += (true ^ sfb_true ? local_int : t1_i.mfi);
        Sum += (true ^ sfb_true ? local_int : simple_func_int());
        Sum += (true ^ sfb_true ? local_int : ab[index]);
        Sum += (true ^ sfb_true ? local_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_14()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ sfb_true ? static_field_int : 3);
        Sum += (true ^ sfb_true ? static_field_int : -5);
        Sum += (true ^ sfb_true ? static_field_int : local_int);
        Sum += (true ^ sfb_true ? static_field_int : static_field_int);
        Sum += (true ^ sfb_true ? static_field_int : t1_i.mfi);
        Sum += (true ^ sfb_true ? static_field_int : simple_func_int());
        Sum += (true ^ sfb_true ? static_field_int : ab[index]);
        Sum += (true ^ sfb_true ? static_field_int : ab[index - 1]);
        Sum += (true ^ sfb_true ? t1_i.mfi : 3);
        Sum += (true ^ sfb_true ? t1_i.mfi : -5);
        Sum += (true ^ sfb_true ? t1_i.mfi : local_int);
        Sum += (true ^ sfb_true ? t1_i.mfi : static_field_int);
        Sum += (true ^ sfb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (true ^ sfb_true ? t1_i.mfi : simple_func_int());
        Sum += (true ^ sfb_true ? t1_i.mfi : ab[index]);
        Sum += (true ^ sfb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (true ^ sfb_true ? simple_func_int() : 3);
        Sum += (true ^ sfb_true ? simple_func_int() : -5);
        Sum += (true ^ sfb_true ? simple_func_int() : local_int);
        Sum += (true ^ sfb_true ? simple_func_int() : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_15()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ sfb_true ? simple_func_int() : t1_i.mfi);
        Sum += (true ^ sfb_true ? simple_func_int() : simple_func_int());
        Sum += (true ^ sfb_true ? simple_func_int() : ab[index]);
        Sum += (true ^ sfb_true ? simple_func_int() : ab[index - 1]);
        Sum += (true ^ sfb_true ? ab[index] : 3);
        Sum += (true ^ sfb_true ? ab[index] : -5);
        Sum += (true ^ sfb_true ? ab[index] : local_int);
        Sum += (true ^ sfb_true ? ab[index] : static_field_int);
        Sum += (true ^ sfb_true ? ab[index] : t1_i.mfi);
        Sum += (true ^ sfb_true ? ab[index] : simple_func_int());
        Sum += (true ^ sfb_true ? ab[index] : ab[index]);
        Sum += (true ^ sfb_true ? ab[index] : ab[index - 1]);
        Sum += (true ^ sfb_true ? ab[index - 1] : 3);
        Sum += (true ^ sfb_true ? ab[index - 1] : -5);
        Sum += (true ^ sfb_true ? ab[index - 1] : local_int);
        Sum += (true ^ sfb_true ? ab[index - 1] : static_field_int);
        Sum += (true ^ sfb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (true ^ sfb_true ? ab[index - 1] : simple_func_int());
        Sum += (true ^ sfb_true ? ab[index - 1] : ab[index]);
        Sum += (true ^ sfb_true ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_16()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ sfb_false ? 3 : 3);
        Sum += (true ^ sfb_false ? 3 : -5);
        Sum += (true ^ sfb_false ? 3 : local_int);
        Sum += (true ^ sfb_false ? 3 : static_field_int);
        Sum += (true ^ sfb_false ? 3 : t1_i.mfi);
        Sum += (true ^ sfb_false ? 3 : simple_func_int());
        Sum += (true ^ sfb_false ? 3 : ab[index]);
        Sum += (true ^ sfb_false ? 3 : ab[index - 1]);
        Sum += (true ^ sfb_false ? -5 : 3);
        Sum += (true ^ sfb_false ? -5 : -5);
        Sum += (true ^ sfb_false ? -5 : local_int);
        Sum += (true ^ sfb_false ? -5 : static_field_int);
        Sum += (true ^ sfb_false ? -5 : t1_i.mfi);
        Sum += (true ^ sfb_false ? -5 : simple_func_int());
        Sum += (true ^ sfb_false ? -5 : ab[index]);
        Sum += (true ^ sfb_false ? -5 : ab[index - 1]);
        Sum += (true ^ sfb_false ? local_int : 3);
        Sum += (true ^ sfb_false ? local_int : -5);
        Sum += (true ^ sfb_false ? local_int : local_int);
        Sum += (true ^ sfb_false ? local_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_17()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ sfb_false ? local_int : t1_i.mfi);
        Sum += (true ^ sfb_false ? local_int : simple_func_int());
        Sum += (true ^ sfb_false ? local_int : ab[index]);
        Sum += (true ^ sfb_false ? local_int : ab[index - 1]);
        Sum += (true ^ sfb_false ? static_field_int : 3);
        Sum += (true ^ sfb_false ? static_field_int : -5);
        Sum += (true ^ sfb_false ? static_field_int : local_int);
        Sum += (true ^ sfb_false ? static_field_int : static_field_int);
        Sum += (true ^ sfb_false ? static_field_int : t1_i.mfi);
        Sum += (true ^ sfb_false ? static_field_int : simple_func_int());
        Sum += (true ^ sfb_false ? static_field_int : ab[index]);
        Sum += (true ^ sfb_false ? static_field_int : ab[index - 1]);
        Sum += (true ^ sfb_false ? t1_i.mfi : 3);
        Sum += (true ^ sfb_false ? t1_i.mfi : -5);
        Sum += (true ^ sfb_false ? t1_i.mfi : local_int);
        Sum += (true ^ sfb_false ? t1_i.mfi : static_field_int);
        Sum += (true ^ sfb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (true ^ sfb_false ? t1_i.mfi : simple_func_int());
        Sum += (true ^ sfb_false ? t1_i.mfi : ab[index]);
        Sum += (true ^ sfb_false ? t1_i.mfi : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_18()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ sfb_false ? simple_func_int() : 3);
        Sum += (true ^ sfb_false ? simple_func_int() : -5);
        Sum += (true ^ sfb_false ? simple_func_int() : local_int);
        Sum += (true ^ sfb_false ? simple_func_int() : static_field_int);
        Sum += (true ^ sfb_false ? simple_func_int() : t1_i.mfi);
        Sum += (true ^ sfb_false ? simple_func_int() : simple_func_int());
        Sum += (true ^ sfb_false ? simple_func_int() : ab[index]);
        Sum += (true ^ sfb_false ? simple_func_int() : ab[index - 1]);
        Sum += (true ^ sfb_false ? ab[index] : 3);
        Sum += (true ^ sfb_false ? ab[index] : -5);
        Sum += (true ^ sfb_false ? ab[index] : local_int);
        Sum += (true ^ sfb_false ? ab[index] : static_field_int);
        Sum += (true ^ sfb_false ? ab[index] : t1_i.mfi);
        Sum += (true ^ sfb_false ? ab[index] : simple_func_int());
        Sum += (true ^ sfb_false ? ab[index] : ab[index]);
        Sum += (true ^ sfb_false ? ab[index] : ab[index - 1]);
        Sum += (true ^ sfb_false ? ab[index - 1] : 3);
        Sum += (true ^ sfb_false ? ab[index - 1] : -5);
        Sum += (true ^ sfb_false ? ab[index - 1] : local_int);
        Sum += (true ^ sfb_false ? ab[index - 1] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_19()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ sfb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (true ^ sfb_false ? ab[index - 1] : simple_func_int());
        Sum += (true ^ sfb_false ? ab[index - 1] : ab[index]);
        Sum += (true ^ sfb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (true ^ t1_i.mfb_true ? 3 : 3);
        Sum += (true ^ t1_i.mfb_true ? 3 : -5);
        Sum += (true ^ t1_i.mfb_true ? 3 : local_int);
        Sum += (true ^ t1_i.mfb_true ? 3 : static_field_int);
        Sum += (true ^ t1_i.mfb_true ? 3 : t1_i.mfi);
        Sum += (true ^ t1_i.mfb_true ? 3 : simple_func_int());
        Sum += (true ^ t1_i.mfb_true ? 3 : ab[index]);
        Sum += (true ^ t1_i.mfb_true ? 3 : ab[index - 1]);
        Sum += (true ^ t1_i.mfb_true ? -5 : 3);
        Sum += (true ^ t1_i.mfb_true ? -5 : -5);
        Sum += (true ^ t1_i.mfb_true ? -5 : local_int);
        Sum += (true ^ t1_i.mfb_true ? -5 : static_field_int);
        Sum += (true ^ t1_i.mfb_true ? -5 : t1_i.mfi);
        Sum += (true ^ t1_i.mfb_true ? -5 : simple_func_int());
        Sum += (true ^ t1_i.mfb_true ? -5 : ab[index]);
        Sum += (true ^ t1_i.mfb_true ? -5 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_20()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ t1_i.mfb_true ? local_int : 3);
        Sum += (true ^ t1_i.mfb_true ? local_int : -5);
        Sum += (true ^ t1_i.mfb_true ? local_int : local_int);
        Sum += (true ^ t1_i.mfb_true ? local_int : static_field_int);
        Sum += (true ^ t1_i.mfb_true ? local_int : t1_i.mfi);
        Sum += (true ^ t1_i.mfb_true ? local_int : simple_func_int());
        Sum += (true ^ t1_i.mfb_true ? local_int : ab[index]);
        Sum += (true ^ t1_i.mfb_true ? local_int : ab[index - 1]);
        Sum += (true ^ t1_i.mfb_true ? static_field_int : 3);
        Sum += (true ^ t1_i.mfb_true ? static_field_int : -5);
        Sum += (true ^ t1_i.mfb_true ? static_field_int : local_int);
        Sum += (true ^ t1_i.mfb_true ? static_field_int : static_field_int);
        Sum += (true ^ t1_i.mfb_true ? static_field_int : t1_i.mfi);
        Sum += (true ^ t1_i.mfb_true ? static_field_int : simple_func_int());
        Sum += (true ^ t1_i.mfb_true ? static_field_int : ab[index]);
        Sum += (true ^ t1_i.mfb_true ? static_field_int : ab[index - 1]);
        Sum += (true ^ t1_i.mfb_true ? t1_i.mfi : 3);
        Sum += (true ^ t1_i.mfb_true ? t1_i.mfi : -5);
        Sum += (true ^ t1_i.mfb_true ? t1_i.mfi : local_int);
        Sum += (true ^ t1_i.mfb_true ? t1_i.mfi : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_21()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ t1_i.mfb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (true ^ t1_i.mfb_true ? t1_i.mfi : simple_func_int());
        Sum += (true ^ t1_i.mfb_true ? t1_i.mfi : ab[index]);
        Sum += (true ^ t1_i.mfb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (true ^ t1_i.mfb_true ? simple_func_int() : 3);
        Sum += (true ^ t1_i.mfb_true ? simple_func_int() : -5);
        Sum += (true ^ t1_i.mfb_true ? simple_func_int() : local_int);
        Sum += (true ^ t1_i.mfb_true ? simple_func_int() : static_field_int);
        Sum += (true ^ t1_i.mfb_true ? simple_func_int() : t1_i.mfi);
        Sum += (true ^ t1_i.mfb_true ? simple_func_int() : simple_func_int());
        Sum += (true ^ t1_i.mfb_true ? simple_func_int() : ab[index]);
        Sum += (true ^ t1_i.mfb_true ? simple_func_int() : ab[index - 1]);
        Sum += (true ^ t1_i.mfb_true ? ab[index] : 3);
        Sum += (true ^ t1_i.mfb_true ? ab[index] : -5);
        Sum += (true ^ t1_i.mfb_true ? ab[index] : local_int);
        Sum += (true ^ t1_i.mfb_true ? ab[index] : static_field_int);
        Sum += (true ^ t1_i.mfb_true ? ab[index] : t1_i.mfi);
        Sum += (true ^ t1_i.mfb_true ? ab[index] : simple_func_int());
        Sum += (true ^ t1_i.mfb_true ? ab[index] : ab[index]);
        Sum += (true ^ t1_i.mfb_true ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_22()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ t1_i.mfb_true ? ab[index - 1] : 3);
        Sum += (true ^ t1_i.mfb_true ? ab[index - 1] : -5);
        Sum += (true ^ t1_i.mfb_true ? ab[index - 1] : local_int);
        Sum += (true ^ t1_i.mfb_true ? ab[index - 1] : static_field_int);
        Sum += (true ^ t1_i.mfb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (true ^ t1_i.mfb_true ? ab[index - 1] : simple_func_int());
        Sum += (true ^ t1_i.mfb_true ? ab[index - 1] : ab[index]);
        Sum += (true ^ t1_i.mfb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (true ^ t1_i.mfb_false ? 3 : 3);
        Sum += (true ^ t1_i.mfb_false ? 3 : -5);
        Sum += (true ^ t1_i.mfb_false ? 3 : local_int);
        Sum += (true ^ t1_i.mfb_false ? 3 : static_field_int);
        Sum += (true ^ t1_i.mfb_false ? 3 : t1_i.mfi);
        Sum += (true ^ t1_i.mfb_false ? 3 : simple_func_int());
        Sum += (true ^ t1_i.mfb_false ? 3 : ab[index]);
        Sum += (true ^ t1_i.mfb_false ? 3 : ab[index - 1]);
        Sum += (true ^ t1_i.mfb_false ? -5 : 3);
        Sum += (true ^ t1_i.mfb_false ? -5 : -5);
        Sum += (true ^ t1_i.mfb_false ? -5 : local_int);
        Sum += (true ^ t1_i.mfb_false ? -5 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_23()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ t1_i.mfb_false ? -5 : t1_i.mfi);
        Sum += (true ^ t1_i.mfb_false ? -5 : simple_func_int());
        Sum += (true ^ t1_i.mfb_false ? -5 : ab[index]);
        Sum += (true ^ t1_i.mfb_false ? -5 : ab[index - 1]);
        Sum += (true ^ t1_i.mfb_false ? local_int : 3);
        Sum += (true ^ t1_i.mfb_false ? local_int : -5);
        Sum += (true ^ t1_i.mfb_false ? local_int : local_int);
        Sum += (true ^ t1_i.mfb_false ? local_int : static_field_int);
        Sum += (true ^ t1_i.mfb_false ? local_int : t1_i.mfi);
        Sum += (true ^ t1_i.mfb_false ? local_int : simple_func_int());
        Sum += (true ^ t1_i.mfb_false ? local_int : ab[index]);
        Sum += (true ^ t1_i.mfb_false ? local_int : ab[index - 1]);
        Sum += (true ^ t1_i.mfb_false ? static_field_int : 3);
        Sum += (true ^ t1_i.mfb_false ? static_field_int : -5);
        Sum += (true ^ t1_i.mfb_false ? static_field_int : local_int);
        Sum += (true ^ t1_i.mfb_false ? static_field_int : static_field_int);
        Sum += (true ^ t1_i.mfb_false ? static_field_int : t1_i.mfi);
        Sum += (true ^ t1_i.mfb_false ? static_field_int : simple_func_int());
        Sum += (true ^ t1_i.mfb_false ? static_field_int : ab[index]);
        Sum += (true ^ t1_i.mfb_false ? static_field_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_24()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ t1_i.mfb_false ? t1_i.mfi : 3);
        Sum += (true ^ t1_i.mfb_false ? t1_i.mfi : -5);
        Sum += (true ^ t1_i.mfb_false ? t1_i.mfi : local_int);
        Sum += (true ^ t1_i.mfb_false ? t1_i.mfi : static_field_int);
        Sum += (true ^ t1_i.mfb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (true ^ t1_i.mfb_false ? t1_i.mfi : simple_func_int());
        Sum += (true ^ t1_i.mfb_false ? t1_i.mfi : ab[index]);
        Sum += (true ^ t1_i.mfb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (true ^ t1_i.mfb_false ? simple_func_int() : 3);
        Sum += (true ^ t1_i.mfb_false ? simple_func_int() : -5);
        Sum += (true ^ t1_i.mfb_false ? simple_func_int() : local_int);
        Sum += (true ^ t1_i.mfb_false ? simple_func_int() : static_field_int);
        Sum += (true ^ t1_i.mfb_false ? simple_func_int() : t1_i.mfi);
        Sum += (true ^ t1_i.mfb_false ? simple_func_int() : simple_func_int());
        Sum += (true ^ t1_i.mfb_false ? simple_func_int() : ab[index]);
        Sum += (true ^ t1_i.mfb_false ? simple_func_int() : ab[index - 1]);
        Sum += (true ^ t1_i.mfb_false ? ab[index] : 3);
        Sum += (true ^ t1_i.mfb_false ? ab[index] : -5);
        Sum += (true ^ t1_i.mfb_false ? ab[index] : local_int);
        Sum += (true ^ t1_i.mfb_false ? ab[index] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_25()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ t1_i.mfb_false ? ab[index] : t1_i.mfi);
        Sum += (true ^ t1_i.mfb_false ? ab[index] : simple_func_int());
        Sum += (true ^ t1_i.mfb_false ? ab[index] : ab[index]);
        Sum += (true ^ t1_i.mfb_false ? ab[index] : ab[index - 1]);
        Sum += (true ^ t1_i.mfb_false ? ab[index - 1] : 3);
        Sum += (true ^ t1_i.mfb_false ? ab[index - 1] : -5);
        Sum += (true ^ t1_i.mfb_false ? ab[index - 1] : local_int);
        Sum += (true ^ t1_i.mfb_false ? ab[index - 1] : static_field_int);
        Sum += (true ^ t1_i.mfb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (true ^ t1_i.mfb_false ? ab[index - 1] : simple_func_int());
        Sum += (true ^ t1_i.mfb_false ? ab[index - 1] : ab[index]);
        Sum += (true ^ t1_i.mfb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (true ^ func_sb_true() ? 3 : 3);
        Sum += (true ^ func_sb_true() ? 3 : -5);
        Sum += (true ^ func_sb_true() ? 3 : local_int);
        Sum += (true ^ func_sb_true() ? 3 : static_field_int);
        Sum += (true ^ func_sb_true() ? 3 : t1_i.mfi);
        Sum += (true ^ func_sb_true() ? 3 : simple_func_int());
        Sum += (true ^ func_sb_true() ? 3 : ab[index]);
        Sum += (true ^ func_sb_true() ? 3 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_26()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ func_sb_true() ? -5 : 3);
        Sum += (true ^ func_sb_true() ? -5 : -5);
        Sum += (true ^ func_sb_true() ? -5 : local_int);
        Sum += (true ^ func_sb_true() ? -5 : static_field_int);
        Sum += (true ^ func_sb_true() ? -5 : t1_i.mfi);
        Sum += (true ^ func_sb_true() ? -5 : simple_func_int());
        Sum += (true ^ func_sb_true() ? -5 : ab[index]);
        Sum += (true ^ func_sb_true() ? -5 : ab[index - 1]);
        Sum += (true ^ func_sb_true() ? local_int : 3);
        Sum += (true ^ func_sb_true() ? local_int : -5);
        Sum += (true ^ func_sb_true() ? local_int : local_int);
        Sum += (true ^ func_sb_true() ? local_int : static_field_int);
        Sum += (true ^ func_sb_true() ? local_int : t1_i.mfi);
        Sum += (true ^ func_sb_true() ? local_int : simple_func_int());
        Sum += (true ^ func_sb_true() ? local_int : ab[index]);
        Sum += (true ^ func_sb_true() ? local_int : ab[index - 1]);
        Sum += (true ^ func_sb_true() ? static_field_int : 3);
        Sum += (true ^ func_sb_true() ? static_field_int : -5);
        Sum += (true ^ func_sb_true() ? static_field_int : local_int);
        Sum += (true ^ func_sb_true() ? static_field_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_27()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ func_sb_true() ? static_field_int : t1_i.mfi);
        Sum += (true ^ func_sb_true() ? static_field_int : simple_func_int());
        Sum += (true ^ func_sb_true() ? static_field_int : ab[index]);
        Sum += (true ^ func_sb_true() ? static_field_int : ab[index - 1]);
        Sum += (true ^ func_sb_true() ? t1_i.mfi : 3);
        Sum += (true ^ func_sb_true() ? t1_i.mfi : -5);
        Sum += (true ^ func_sb_true() ? t1_i.mfi : local_int);
        Sum += (true ^ func_sb_true() ? t1_i.mfi : static_field_int);
        Sum += (true ^ func_sb_true() ? t1_i.mfi : t1_i.mfi);
        Sum += (true ^ func_sb_true() ? t1_i.mfi : simple_func_int());
        Sum += (true ^ func_sb_true() ? t1_i.mfi : ab[index]);
        Sum += (true ^ func_sb_true() ? t1_i.mfi : ab[index - 1]);
        Sum += (true ^ func_sb_true() ? simple_func_int() : 3);
        Sum += (true ^ func_sb_true() ? simple_func_int() : -5);
        Sum += (true ^ func_sb_true() ? simple_func_int() : local_int);
        Sum += (true ^ func_sb_true() ? simple_func_int() : static_field_int);
        Sum += (true ^ func_sb_true() ? simple_func_int() : t1_i.mfi);
        Sum += (true ^ func_sb_true() ? simple_func_int() : simple_func_int());
        Sum += (true ^ func_sb_true() ? simple_func_int() : ab[index]);
        Sum += (true ^ func_sb_true() ? simple_func_int() : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_28()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ func_sb_true() ? ab[index] : 3);
        Sum += (true ^ func_sb_true() ? ab[index] : -5);
        Sum += (true ^ func_sb_true() ? ab[index] : local_int);
        Sum += (true ^ func_sb_true() ? ab[index] : static_field_int);
        Sum += (true ^ func_sb_true() ? ab[index] : t1_i.mfi);
        Sum += (true ^ func_sb_true() ? ab[index] : simple_func_int());
        Sum += (true ^ func_sb_true() ? ab[index] : ab[index]);
        Sum += (true ^ func_sb_true() ? ab[index] : ab[index - 1]);
        Sum += (true ^ func_sb_true() ? ab[index - 1] : 3);
        Sum += (true ^ func_sb_true() ? ab[index - 1] : -5);
        Sum += (true ^ func_sb_true() ? ab[index - 1] : local_int);
        Sum += (true ^ func_sb_true() ? ab[index - 1] : static_field_int);
        Sum += (true ^ func_sb_true() ? ab[index - 1] : t1_i.mfi);
        Sum += (true ^ func_sb_true() ? ab[index - 1] : simple_func_int());
        Sum += (true ^ func_sb_true() ? ab[index - 1] : ab[index]);
        Sum += (true ^ func_sb_true() ? ab[index - 1] : ab[index - 1]);
        Sum += (true ^ func_sb_false() ? 3 : 3);
        Sum += (true ^ func_sb_false() ? 3 : -5);
        Sum += (true ^ func_sb_false() ? 3 : local_int);
        Sum += (true ^ func_sb_false() ? 3 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_29()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ func_sb_false() ? 3 : t1_i.mfi);
        Sum += (true ^ func_sb_false() ? 3 : simple_func_int());
        Sum += (true ^ func_sb_false() ? 3 : ab[index]);
        Sum += (true ^ func_sb_false() ? 3 : ab[index - 1]);
        Sum += (true ^ func_sb_false() ? -5 : 3);
        Sum += (true ^ func_sb_false() ? -5 : -5);
        Sum += (true ^ func_sb_false() ? -5 : local_int);
        Sum += (true ^ func_sb_false() ? -5 : static_field_int);
        Sum += (true ^ func_sb_false() ? -5 : t1_i.mfi);
        Sum += (true ^ func_sb_false() ? -5 : simple_func_int());
        Sum += (true ^ func_sb_false() ? -5 : ab[index]);
        Sum += (true ^ func_sb_false() ? -5 : ab[index - 1]);
        Sum += (true ^ func_sb_false() ? local_int : 3);
        Sum += (true ^ func_sb_false() ? local_int : -5);
        Sum += (true ^ func_sb_false() ? local_int : local_int);
        Sum += (true ^ func_sb_false() ? local_int : static_field_int);
        Sum += (true ^ func_sb_false() ? local_int : t1_i.mfi);
        Sum += (true ^ func_sb_false() ? local_int : simple_func_int());
        Sum += (true ^ func_sb_false() ? local_int : ab[index]);
        Sum += (true ^ func_sb_false() ? local_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_30()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ func_sb_false() ? static_field_int : 3);
        Sum += (true ^ func_sb_false() ? static_field_int : -5);
        Sum += (true ^ func_sb_false() ? static_field_int : local_int);
        Sum += (true ^ func_sb_false() ? static_field_int : static_field_int);
        Sum += (true ^ func_sb_false() ? static_field_int : t1_i.mfi);
        Sum += (true ^ func_sb_false() ? static_field_int : simple_func_int());
        Sum += (true ^ func_sb_false() ? static_field_int : ab[index]);
        Sum += (true ^ func_sb_false() ? static_field_int : ab[index - 1]);
        Sum += (true ^ func_sb_false() ? t1_i.mfi : 3);
        Sum += (true ^ func_sb_false() ? t1_i.mfi : -5);
        Sum += (true ^ func_sb_false() ? t1_i.mfi : local_int);
        Sum += (true ^ func_sb_false() ? t1_i.mfi : static_field_int);
        Sum += (true ^ func_sb_false() ? t1_i.mfi : t1_i.mfi);
        Sum += (true ^ func_sb_false() ? t1_i.mfi : simple_func_int());
        Sum += (true ^ func_sb_false() ? t1_i.mfi : ab[index]);
        Sum += (true ^ func_sb_false() ? t1_i.mfi : ab[index - 1]);
        Sum += (true ^ func_sb_false() ? simple_func_int() : 3);
        Sum += (true ^ func_sb_false() ? simple_func_int() : -5);
        Sum += (true ^ func_sb_false() ? simple_func_int() : local_int);
        Sum += (true ^ func_sb_false() ? simple_func_int() : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_31()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ func_sb_false() ? simple_func_int() : t1_i.mfi);
        Sum += (true ^ func_sb_false() ? simple_func_int() : simple_func_int());
        Sum += (true ^ func_sb_false() ? simple_func_int() : ab[index]);
        Sum += (true ^ func_sb_false() ? simple_func_int() : ab[index - 1]);
        Sum += (true ^ func_sb_false() ? ab[index] : 3);
        Sum += (true ^ func_sb_false() ? ab[index] : -5);
        Sum += (true ^ func_sb_false() ? ab[index] : local_int);
        Sum += (true ^ func_sb_false() ? ab[index] : static_field_int);
        Sum += (true ^ func_sb_false() ? ab[index] : t1_i.mfi);
        Sum += (true ^ func_sb_false() ? ab[index] : simple_func_int());
        Sum += (true ^ func_sb_false() ? ab[index] : ab[index]);
        Sum += (true ^ func_sb_false() ? ab[index] : ab[index - 1]);
        Sum += (true ^ func_sb_false() ? ab[index - 1] : 3);
        Sum += (true ^ func_sb_false() ? ab[index - 1] : -5);
        Sum += (true ^ func_sb_false() ? ab[index - 1] : local_int);
        Sum += (true ^ func_sb_false() ? ab[index - 1] : static_field_int);
        Sum += (true ^ func_sb_false() ? ab[index - 1] : t1_i.mfi);
        Sum += (true ^ func_sb_false() ? ab[index - 1] : simple_func_int());
        Sum += (true ^ func_sb_false() ? ab[index - 1] : ab[index]);
        Sum += (true ^ func_sb_false() ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_32()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ ab_true[index] ? 3 : 3);
        Sum += (true ^ ab_true[index] ? 3 : -5);
        Sum += (true ^ ab_true[index] ? 3 : local_int);
        Sum += (true ^ ab_true[index] ? 3 : static_field_int);
        Sum += (true ^ ab_true[index] ? 3 : t1_i.mfi);
        Sum += (true ^ ab_true[index] ? 3 : simple_func_int());
        Sum += (true ^ ab_true[index] ? 3 : ab[index]);
        Sum += (true ^ ab_true[index] ? 3 : ab[index - 1]);
        Sum += (true ^ ab_true[index] ? -5 : 3);
        Sum += (true ^ ab_true[index] ? -5 : -5);
        Sum += (true ^ ab_true[index] ? -5 : local_int);
        Sum += (true ^ ab_true[index] ? -5 : static_field_int);
        Sum += (true ^ ab_true[index] ? -5 : t1_i.mfi);
        Sum += (true ^ ab_true[index] ? -5 : simple_func_int());
        Sum += (true ^ ab_true[index] ? -5 : ab[index]);
        Sum += (true ^ ab_true[index] ? -5 : ab[index - 1]);
        Sum += (true ^ ab_true[index] ? local_int : 3);
        Sum += (true ^ ab_true[index] ? local_int : -5);
        Sum += (true ^ ab_true[index] ? local_int : local_int);
        Sum += (true ^ ab_true[index] ? local_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_33()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ ab_true[index] ? local_int : t1_i.mfi);
        Sum += (true ^ ab_true[index] ? local_int : simple_func_int());
        Sum += (true ^ ab_true[index] ? local_int : ab[index]);
        Sum += (true ^ ab_true[index] ? local_int : ab[index - 1]);
        Sum += (true ^ ab_true[index] ? static_field_int : 3);
        Sum += (true ^ ab_true[index] ? static_field_int : -5);
        Sum += (true ^ ab_true[index] ? static_field_int : local_int);
        Sum += (true ^ ab_true[index] ? static_field_int : static_field_int);
        Sum += (true ^ ab_true[index] ? static_field_int : t1_i.mfi);
        Sum += (true ^ ab_true[index] ? static_field_int : simple_func_int());
        Sum += (true ^ ab_true[index] ? static_field_int : ab[index]);
        Sum += (true ^ ab_true[index] ? static_field_int : ab[index - 1]);
        Sum += (true ^ ab_true[index] ? t1_i.mfi : 3);
        Sum += (true ^ ab_true[index] ? t1_i.mfi : -5);
        Sum += (true ^ ab_true[index] ? t1_i.mfi : local_int);
        Sum += (true ^ ab_true[index] ? t1_i.mfi : static_field_int);
        Sum += (true ^ ab_true[index] ? t1_i.mfi : t1_i.mfi);
        Sum += (true ^ ab_true[index] ? t1_i.mfi : simple_func_int());
        Sum += (true ^ ab_true[index] ? t1_i.mfi : ab[index]);
        Sum += (true ^ ab_true[index] ? t1_i.mfi : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_34()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ ab_true[index] ? simple_func_int() : 3);
        Sum += (true ^ ab_true[index] ? simple_func_int() : -5);
        Sum += (true ^ ab_true[index] ? simple_func_int() : local_int);
        Sum += (true ^ ab_true[index] ? simple_func_int() : static_field_int);
        Sum += (true ^ ab_true[index] ? simple_func_int() : t1_i.mfi);
        Sum += (true ^ ab_true[index] ? simple_func_int() : simple_func_int());
        Sum += (true ^ ab_true[index] ? simple_func_int() : ab[index]);
        Sum += (true ^ ab_true[index] ? simple_func_int() : ab[index - 1]);
        Sum += (true ^ ab_true[index] ? ab[index] : 3);
        Sum += (true ^ ab_true[index] ? ab[index] : -5);
        Sum += (true ^ ab_true[index] ? ab[index] : local_int);
        Sum += (true ^ ab_true[index] ? ab[index] : static_field_int);
        Sum += (true ^ ab_true[index] ? ab[index] : t1_i.mfi);
        Sum += (true ^ ab_true[index] ? ab[index] : simple_func_int());
        Sum += (true ^ ab_true[index] ? ab[index] : ab[index]);
        Sum += (true ^ ab_true[index] ? ab[index] : ab[index - 1]);
        Sum += (true ^ ab_true[index] ? ab[index - 1] : 3);
        Sum += (true ^ ab_true[index] ? ab[index - 1] : -5);
        Sum += (true ^ ab_true[index] ? ab[index - 1] : local_int);
        Sum += (true ^ ab_true[index] ? ab[index - 1] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_35()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ ab_true[index] ? ab[index - 1] : t1_i.mfi);
        Sum += (true ^ ab_true[index] ? ab[index - 1] : simple_func_int());
        Sum += (true ^ ab_true[index] ? ab[index - 1] : ab[index]);
        Sum += (true ^ ab_true[index] ? ab[index - 1] : ab[index - 1]);
        Sum += (true ^ ab_false[index] ? 3 : 3);
        Sum += (true ^ ab_false[index] ? 3 : -5);
        Sum += (true ^ ab_false[index] ? 3 : local_int);
        Sum += (true ^ ab_false[index] ? 3 : static_field_int);
        Sum += (true ^ ab_false[index] ? 3 : t1_i.mfi);
        Sum += (true ^ ab_false[index] ? 3 : simple_func_int());
        Sum += (true ^ ab_false[index] ? 3 : ab[index]);
        Sum += (true ^ ab_false[index] ? 3 : ab[index - 1]);
        Sum += (true ^ ab_false[index] ? -5 : 3);
        Sum += (true ^ ab_false[index] ? -5 : -5);
        Sum += (true ^ ab_false[index] ? -5 : local_int);
        Sum += (true ^ ab_false[index] ? -5 : static_field_int);
        Sum += (true ^ ab_false[index] ? -5 : t1_i.mfi);
        Sum += (true ^ ab_false[index] ? -5 : simple_func_int());
        Sum += (true ^ ab_false[index] ? -5 : ab[index]);
        Sum += (true ^ ab_false[index] ? -5 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_36()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ ab_false[index] ? local_int : 3);
        Sum += (true ^ ab_false[index] ? local_int : -5);
        Sum += (true ^ ab_false[index] ? local_int : local_int);
        Sum += (true ^ ab_false[index] ? local_int : static_field_int);
        Sum += (true ^ ab_false[index] ? local_int : t1_i.mfi);
        Sum += (true ^ ab_false[index] ? local_int : simple_func_int());
        Sum += (true ^ ab_false[index] ? local_int : ab[index]);
        Sum += (true ^ ab_false[index] ? local_int : ab[index - 1]);
        Sum += (true ^ ab_false[index] ? static_field_int : 3);
        Sum += (true ^ ab_false[index] ? static_field_int : -5);
        Sum += (true ^ ab_false[index] ? static_field_int : local_int);
        Sum += (true ^ ab_false[index] ? static_field_int : static_field_int);
        Sum += (true ^ ab_false[index] ? static_field_int : t1_i.mfi);
        Sum += (true ^ ab_false[index] ? static_field_int : simple_func_int());
        Sum += (true ^ ab_false[index] ? static_field_int : ab[index]);
        Sum += (true ^ ab_false[index] ? static_field_int : ab[index - 1]);
        Sum += (true ^ ab_false[index] ? t1_i.mfi : 3);
        Sum += (true ^ ab_false[index] ? t1_i.mfi : -5);
        Sum += (true ^ ab_false[index] ? t1_i.mfi : local_int);
        Sum += (true ^ ab_false[index] ? t1_i.mfi : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_37()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ ab_false[index] ? t1_i.mfi : t1_i.mfi);
        Sum += (true ^ ab_false[index] ? t1_i.mfi : simple_func_int());
        Sum += (true ^ ab_false[index] ? t1_i.mfi : ab[index]);
        Sum += (true ^ ab_false[index] ? t1_i.mfi : ab[index - 1]);
        Sum += (true ^ ab_false[index] ? simple_func_int() : 3);
        Sum += (true ^ ab_false[index] ? simple_func_int() : -5);
        Sum += (true ^ ab_false[index] ? simple_func_int() : local_int);
        Sum += (true ^ ab_false[index] ? simple_func_int() : static_field_int);
        Sum += (true ^ ab_false[index] ? simple_func_int() : t1_i.mfi);
        Sum += (true ^ ab_false[index] ? simple_func_int() : simple_func_int());
        Sum += (true ^ ab_false[index] ? simple_func_int() : ab[index]);
        Sum += (true ^ ab_false[index] ? simple_func_int() : ab[index - 1]);
        Sum += (true ^ ab_false[index] ? ab[index] : 3);
        Sum += (true ^ ab_false[index] ? ab[index] : -5);
        Sum += (true ^ ab_false[index] ? ab[index] : local_int);
        Sum += (true ^ ab_false[index] ? ab[index] : static_field_int);
        Sum += (true ^ ab_false[index] ? ab[index] : t1_i.mfi);
        Sum += (true ^ ab_false[index] ? ab[index] : simple_func_int());
        Sum += (true ^ ab_false[index] ? ab[index] : ab[index]);
        Sum += (true ^ ab_false[index] ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_38()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ^ ab_false[index] ? ab[index - 1] : 3);
        Sum += (true ^ ab_false[index] ? ab[index - 1] : -5);
        Sum += (true ^ ab_false[index] ? ab[index - 1] : local_int);
        Sum += (true ^ ab_false[index] ? ab[index - 1] : static_field_int);
        Sum += (true ^ ab_false[index] ? ab[index - 1] : t1_i.mfi);
        Sum += (true ^ ab_false[index] ? ab[index - 1] : simple_func_int());
        Sum += (true ^ ab_false[index] ? ab[index - 1] : ab[index]);
        Sum += (true ^ ab_false[index] ? ab[index - 1] : ab[index - 1]);
        Sum += (false ^ true ? 3 : 3);
        Sum += (false ^ true ? 3 : -5);
        Sum += (false ^ true ? 3 : local_int);
        Sum += (false ^ true ? 3 : static_field_int);
        Sum += (false ^ true ? 3 : t1_i.mfi);
        Sum += (false ^ true ? 3 : simple_func_int());
        Sum += (false ^ true ? 3 : ab[index]);
        Sum += (false ^ true ? 3 : ab[index - 1]);
        Sum += (false ^ true ? -5 : 3);
        Sum += (false ^ true ? -5 : -5);
        Sum += (false ^ true ? -5 : local_int);
        Sum += (false ^ true ? -5 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_39()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ true ? -5 : t1_i.mfi);
        Sum += (false ^ true ? -5 : simple_func_int());
        Sum += (false ^ true ? -5 : ab[index]);
        Sum += (false ^ true ? -5 : ab[index - 1]);
        Sum += (false ^ true ? local_int : 3);
        Sum += (false ^ true ? local_int : -5);
        Sum += (false ^ true ? local_int : local_int);
        Sum += (false ^ true ? local_int : static_field_int);
        Sum += (false ^ true ? local_int : t1_i.mfi);
        Sum += (false ^ true ? local_int : simple_func_int());
        Sum += (false ^ true ? local_int : ab[index]);
        Sum += (false ^ true ? local_int : ab[index - 1]);
        Sum += (false ^ true ? static_field_int : 3);
        Sum += (false ^ true ? static_field_int : -5);
        Sum += (false ^ true ? static_field_int : local_int);
        Sum += (false ^ true ? static_field_int : static_field_int);
        Sum += (false ^ true ? static_field_int : t1_i.mfi);
        Sum += (false ^ true ? static_field_int : simple_func_int());
        Sum += (false ^ true ? static_field_int : ab[index]);
        Sum += (false ^ true ? static_field_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_40()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ true ? t1_i.mfi : 3);
        Sum += (false ^ true ? t1_i.mfi : -5);
        Sum += (false ^ true ? t1_i.mfi : local_int);
        Sum += (false ^ true ? t1_i.mfi : static_field_int);
        Sum += (false ^ true ? t1_i.mfi : t1_i.mfi);
        Sum += (false ^ true ? t1_i.mfi : simple_func_int());
        Sum += (false ^ true ? t1_i.mfi : ab[index]);
        Sum += (false ^ true ? t1_i.mfi : ab[index - 1]);
        Sum += (false ^ true ? simple_func_int() : 3);
        Sum += (false ^ true ? simple_func_int() : -5);
        Sum += (false ^ true ? simple_func_int() : local_int);
        Sum += (false ^ true ? simple_func_int() : static_field_int);
        Sum += (false ^ true ? simple_func_int() : t1_i.mfi);
        Sum += (false ^ true ? simple_func_int() : simple_func_int());
        Sum += (false ^ true ? simple_func_int() : ab[index]);
        Sum += (false ^ true ? simple_func_int() : ab[index - 1]);
        Sum += (false ^ true ? ab[index] : 3);
        Sum += (false ^ true ? ab[index] : -5);
        Sum += (false ^ true ? ab[index] : local_int);
        Sum += (false ^ true ? ab[index] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_41()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ true ? ab[index] : t1_i.mfi);
        Sum += (false ^ true ? ab[index] : simple_func_int());
        Sum += (false ^ true ? ab[index] : ab[index]);
        Sum += (false ^ true ? ab[index] : ab[index - 1]);
        Sum += (false ^ true ? ab[index - 1] : 3);
        Sum += (false ^ true ? ab[index - 1] : -5);
        Sum += (false ^ true ? ab[index - 1] : local_int);
        Sum += (false ^ true ? ab[index - 1] : static_field_int);
        Sum += (false ^ true ? ab[index - 1] : t1_i.mfi);
        Sum += (false ^ true ? ab[index - 1] : simple_func_int());
        Sum += (false ^ true ? ab[index - 1] : ab[index]);
        Sum += (false ^ true ? ab[index - 1] : ab[index - 1]);
        Sum += (false ^ false ? 3 : 3);
        Sum += (false ^ false ? 3 : -5);
        Sum += (false ^ false ? 3 : local_int);
        Sum += (false ^ false ? 3 : static_field_int);
        Sum += (false ^ false ? 3 : t1_i.mfi);
        Sum += (false ^ false ? 3 : simple_func_int());
        Sum += (false ^ false ? 3 : ab[index]);
        Sum += (false ^ false ? 3 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_42()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ false ? -5 : 3);
        Sum += (false ^ false ? -5 : -5);
        Sum += (false ^ false ? -5 : local_int);
        Sum += (false ^ false ? -5 : static_field_int);
        Sum += (false ^ false ? -5 : t1_i.mfi);
        Sum += (false ^ false ? -5 : simple_func_int());
        Sum += (false ^ false ? -5 : ab[index]);
        Sum += (false ^ false ? -5 : ab[index - 1]);
        Sum += (false ^ false ? local_int : 3);
        Sum += (false ^ false ? local_int : -5);
        Sum += (false ^ false ? local_int : local_int);
        Sum += (false ^ false ? local_int : static_field_int);
        Sum += (false ^ false ? local_int : t1_i.mfi);
        Sum += (false ^ false ? local_int : simple_func_int());
        Sum += (false ^ false ? local_int : ab[index]);
        Sum += (false ^ false ? local_int : ab[index - 1]);
        Sum += (false ^ false ? static_field_int : 3);
        Sum += (false ^ false ? static_field_int : -5);
        Sum += (false ^ false ? static_field_int : local_int);
        Sum += (false ^ false ? static_field_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_43()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ false ? static_field_int : t1_i.mfi);
        Sum += (false ^ false ? static_field_int : simple_func_int());
        Sum += (false ^ false ? static_field_int : ab[index]);
        Sum += (false ^ false ? static_field_int : ab[index - 1]);
        Sum += (false ^ false ? t1_i.mfi : 3);
        Sum += (false ^ false ? t1_i.mfi : -5);
        Sum += (false ^ false ? t1_i.mfi : local_int);
        Sum += (false ^ false ? t1_i.mfi : static_field_int);
        Sum += (false ^ false ? t1_i.mfi : t1_i.mfi);
        Sum += (false ^ false ? t1_i.mfi : simple_func_int());
        Sum += (false ^ false ? t1_i.mfi : ab[index]);
        Sum += (false ^ false ? t1_i.mfi : ab[index - 1]);
        Sum += (false ^ false ? simple_func_int() : 3);
        Sum += (false ^ false ? simple_func_int() : -5);
        Sum += (false ^ false ? simple_func_int() : local_int);
        Sum += (false ^ false ? simple_func_int() : static_field_int);
        Sum += (false ^ false ? simple_func_int() : t1_i.mfi);
        Sum += (false ^ false ? simple_func_int() : simple_func_int());
        Sum += (false ^ false ? simple_func_int() : ab[index]);
        Sum += (false ^ false ? simple_func_int() : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_44()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ false ? ab[index] : 3);
        Sum += (false ^ false ? ab[index] : -5);
        Sum += (false ^ false ? ab[index] : local_int);
        Sum += (false ^ false ? ab[index] : static_field_int);
        Sum += (false ^ false ? ab[index] : t1_i.mfi);
        Sum += (false ^ false ? ab[index] : simple_func_int());
        Sum += (false ^ false ? ab[index] : ab[index]);
        Sum += (false ^ false ? ab[index] : ab[index - 1]);
        Sum += (false ^ false ? ab[index - 1] : 3);
        Sum += (false ^ false ? ab[index - 1] : -5);
        Sum += (false ^ false ? ab[index - 1] : local_int);
        Sum += (false ^ false ? ab[index - 1] : static_field_int);
        Sum += (false ^ false ? ab[index - 1] : t1_i.mfi);
        Sum += (false ^ false ? ab[index - 1] : simple_func_int());
        Sum += (false ^ false ? ab[index - 1] : ab[index]);
        Sum += (false ^ false ? ab[index - 1] : ab[index - 1]);
        Sum += (false ^ lb_true ? 3 : 3);
        Sum += (false ^ lb_true ? 3 : -5);
        Sum += (false ^ lb_true ? 3 : local_int);
        Sum += (false ^ lb_true ? 3 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_45()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ lb_true ? 3 : t1_i.mfi);
        Sum += (false ^ lb_true ? 3 : simple_func_int());
        Sum += (false ^ lb_true ? 3 : ab[index]);
        Sum += (false ^ lb_true ? 3 : ab[index - 1]);
        Sum += (false ^ lb_true ? -5 : 3);
        Sum += (false ^ lb_true ? -5 : -5);
        Sum += (false ^ lb_true ? -5 : local_int);
        Sum += (false ^ lb_true ? -5 : static_field_int);
        Sum += (false ^ lb_true ? -5 : t1_i.mfi);
        Sum += (false ^ lb_true ? -5 : simple_func_int());
        Sum += (false ^ lb_true ? -5 : ab[index]);
        Sum += (false ^ lb_true ? -5 : ab[index - 1]);
        Sum += (false ^ lb_true ? local_int : 3);
        Sum += (false ^ lb_true ? local_int : -5);
        Sum += (false ^ lb_true ? local_int : local_int);
        Sum += (false ^ lb_true ? local_int : static_field_int);
        Sum += (false ^ lb_true ? local_int : t1_i.mfi);
        Sum += (false ^ lb_true ? local_int : simple_func_int());
        Sum += (false ^ lb_true ? local_int : ab[index]);
        Sum += (false ^ lb_true ? local_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_46()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ lb_true ? static_field_int : 3);
        Sum += (false ^ lb_true ? static_field_int : -5);
        Sum += (false ^ lb_true ? static_field_int : local_int);
        Sum += (false ^ lb_true ? static_field_int : static_field_int);
        Sum += (false ^ lb_true ? static_field_int : t1_i.mfi);
        Sum += (false ^ lb_true ? static_field_int : simple_func_int());
        Sum += (false ^ lb_true ? static_field_int : ab[index]);
        Sum += (false ^ lb_true ? static_field_int : ab[index - 1]);
        Sum += (false ^ lb_true ? t1_i.mfi : 3);
        Sum += (false ^ lb_true ? t1_i.mfi : -5);
        Sum += (false ^ lb_true ? t1_i.mfi : local_int);
        Sum += (false ^ lb_true ? t1_i.mfi : static_field_int);
        Sum += (false ^ lb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (false ^ lb_true ? t1_i.mfi : simple_func_int());
        Sum += (false ^ lb_true ? t1_i.mfi : ab[index]);
        Sum += (false ^ lb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (false ^ lb_true ? simple_func_int() : 3);
        Sum += (false ^ lb_true ? simple_func_int() : -5);
        Sum += (false ^ lb_true ? simple_func_int() : local_int);
        Sum += (false ^ lb_true ? simple_func_int() : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_47()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ lb_true ? simple_func_int() : t1_i.mfi);
        Sum += (false ^ lb_true ? simple_func_int() : simple_func_int());
        Sum += (false ^ lb_true ? simple_func_int() : ab[index]);
        Sum += (false ^ lb_true ? simple_func_int() : ab[index - 1]);
        Sum += (false ^ lb_true ? ab[index] : 3);
        Sum += (false ^ lb_true ? ab[index] : -5);
        Sum += (false ^ lb_true ? ab[index] : local_int);
        Sum += (false ^ lb_true ? ab[index] : static_field_int);
        Sum += (false ^ lb_true ? ab[index] : t1_i.mfi);
        Sum += (false ^ lb_true ? ab[index] : simple_func_int());
        Sum += (false ^ lb_true ? ab[index] : ab[index]);
        Sum += (false ^ lb_true ? ab[index] : ab[index - 1]);
        Sum += (false ^ lb_true ? ab[index - 1] : 3);
        Sum += (false ^ lb_true ? ab[index - 1] : -5);
        Sum += (false ^ lb_true ? ab[index - 1] : local_int);
        Sum += (false ^ lb_true ? ab[index - 1] : static_field_int);
        Sum += (false ^ lb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (false ^ lb_true ? ab[index - 1] : simple_func_int());
        Sum += (false ^ lb_true ? ab[index - 1] : ab[index]);
        Sum += (false ^ lb_true ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_48()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ lb_false ? 3 : 3);
        Sum += (false ^ lb_false ? 3 : -5);
        Sum += (false ^ lb_false ? 3 : local_int);
        Sum += (false ^ lb_false ? 3 : static_field_int);
        Sum += (false ^ lb_false ? 3 : t1_i.mfi);
        Sum += (false ^ lb_false ? 3 : simple_func_int());
        Sum += (false ^ lb_false ? 3 : ab[index]);
        Sum += (false ^ lb_false ? 3 : ab[index - 1]);
        Sum += (false ^ lb_false ? -5 : 3);
        Sum += (false ^ lb_false ? -5 : -5);
        Sum += (false ^ lb_false ? -5 : local_int);
        Sum += (false ^ lb_false ? -5 : static_field_int);
        Sum += (false ^ lb_false ? -5 : t1_i.mfi);
        Sum += (false ^ lb_false ? -5 : simple_func_int());
        Sum += (false ^ lb_false ? -5 : ab[index]);
        Sum += (false ^ lb_false ? -5 : ab[index - 1]);
        Sum += (false ^ lb_false ? local_int : 3);
        Sum += (false ^ lb_false ? local_int : -5);
        Sum += (false ^ lb_false ? local_int : local_int);
        Sum += (false ^ lb_false ? local_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_49()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ lb_false ? local_int : t1_i.mfi);
        Sum += (false ^ lb_false ? local_int : simple_func_int());
        Sum += (false ^ lb_false ? local_int : ab[index]);
        Sum += (false ^ lb_false ? local_int : ab[index - 1]);
        Sum += (false ^ lb_false ? static_field_int : 3);
        Sum += (false ^ lb_false ? static_field_int : -5);
        Sum += (false ^ lb_false ? static_field_int : local_int);
        Sum += (false ^ lb_false ? static_field_int : static_field_int);
        Sum += (false ^ lb_false ? static_field_int : t1_i.mfi);
        Sum += (false ^ lb_false ? static_field_int : simple_func_int());
        Sum += (false ^ lb_false ? static_field_int : ab[index]);
        Sum += (false ^ lb_false ? static_field_int : ab[index - 1]);
        Sum += (false ^ lb_false ? t1_i.mfi : 3);
        Sum += (false ^ lb_false ? t1_i.mfi : -5);
        Sum += (false ^ lb_false ? t1_i.mfi : local_int);
        Sum += (false ^ lb_false ? t1_i.mfi : static_field_int);
        Sum += (false ^ lb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (false ^ lb_false ? t1_i.mfi : simple_func_int());
        Sum += (false ^ lb_false ? t1_i.mfi : ab[index]);
        Sum += (false ^ lb_false ? t1_i.mfi : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_50()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ lb_false ? simple_func_int() : 3);
        Sum += (false ^ lb_false ? simple_func_int() : -5);
        Sum += (false ^ lb_false ? simple_func_int() : local_int);
        Sum += (false ^ lb_false ? simple_func_int() : static_field_int);
        Sum += (false ^ lb_false ? simple_func_int() : t1_i.mfi);
        Sum += (false ^ lb_false ? simple_func_int() : simple_func_int());
        Sum += (false ^ lb_false ? simple_func_int() : ab[index]);
        Sum += (false ^ lb_false ? simple_func_int() : ab[index - 1]);
        Sum += (false ^ lb_false ? ab[index] : 3);
        Sum += (false ^ lb_false ? ab[index] : -5);
        Sum += (false ^ lb_false ? ab[index] : local_int);
        Sum += (false ^ lb_false ? ab[index] : static_field_int);
        Sum += (false ^ lb_false ? ab[index] : t1_i.mfi);
        Sum += (false ^ lb_false ? ab[index] : simple_func_int());
        Sum += (false ^ lb_false ? ab[index] : ab[index]);
        Sum += (false ^ lb_false ? ab[index] : ab[index - 1]);
        Sum += (false ^ lb_false ? ab[index - 1] : 3);
        Sum += (false ^ lb_false ? ab[index - 1] : -5);
        Sum += (false ^ lb_false ? ab[index - 1] : local_int);
        Sum += (false ^ lb_false ? ab[index - 1] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_51()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ lb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (false ^ lb_false ? ab[index - 1] : simple_func_int());
        Sum += (false ^ lb_false ? ab[index - 1] : ab[index]);
        Sum += (false ^ lb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (false ^ sfb_true ? 3 : 3);
        Sum += (false ^ sfb_true ? 3 : -5);
        Sum += (false ^ sfb_true ? 3 : local_int);
        Sum += (false ^ sfb_true ? 3 : static_field_int);
        Sum += (false ^ sfb_true ? 3 : t1_i.mfi);
        Sum += (false ^ sfb_true ? 3 : simple_func_int());
        Sum += (false ^ sfb_true ? 3 : ab[index]);
        Sum += (false ^ sfb_true ? 3 : ab[index - 1]);
        Sum += (false ^ sfb_true ? -5 : 3);
        Sum += (false ^ sfb_true ? -5 : -5);
        Sum += (false ^ sfb_true ? -5 : local_int);
        Sum += (false ^ sfb_true ? -5 : static_field_int);
        Sum += (false ^ sfb_true ? -5 : t1_i.mfi);
        Sum += (false ^ sfb_true ? -5 : simple_func_int());
        Sum += (false ^ sfb_true ? -5 : ab[index]);
        Sum += (false ^ sfb_true ? -5 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_52()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ sfb_true ? local_int : 3);
        Sum += (false ^ sfb_true ? local_int : -5);
        Sum += (false ^ sfb_true ? local_int : local_int);
        Sum += (false ^ sfb_true ? local_int : static_field_int);
        Sum += (false ^ sfb_true ? local_int : t1_i.mfi);
        Sum += (false ^ sfb_true ? local_int : simple_func_int());
        Sum += (false ^ sfb_true ? local_int : ab[index]);
        Sum += (false ^ sfb_true ? local_int : ab[index - 1]);
        Sum += (false ^ sfb_true ? static_field_int : 3);
        Sum += (false ^ sfb_true ? static_field_int : -5);
        Sum += (false ^ sfb_true ? static_field_int : local_int);
        Sum += (false ^ sfb_true ? static_field_int : static_field_int);
        Sum += (false ^ sfb_true ? static_field_int : t1_i.mfi);
        Sum += (false ^ sfb_true ? static_field_int : simple_func_int());
        Sum += (false ^ sfb_true ? static_field_int : ab[index]);
        Sum += (false ^ sfb_true ? static_field_int : ab[index - 1]);
        Sum += (false ^ sfb_true ? t1_i.mfi : 3);
        Sum += (false ^ sfb_true ? t1_i.mfi : -5);
        Sum += (false ^ sfb_true ? t1_i.mfi : local_int);
        Sum += (false ^ sfb_true ? t1_i.mfi : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_53()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ sfb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (false ^ sfb_true ? t1_i.mfi : simple_func_int());
        Sum += (false ^ sfb_true ? t1_i.mfi : ab[index]);
        Sum += (false ^ sfb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (false ^ sfb_true ? simple_func_int() : 3);
        Sum += (false ^ sfb_true ? simple_func_int() : -5);
        Sum += (false ^ sfb_true ? simple_func_int() : local_int);
        Sum += (false ^ sfb_true ? simple_func_int() : static_field_int);
        Sum += (false ^ sfb_true ? simple_func_int() : t1_i.mfi);
        Sum += (false ^ sfb_true ? simple_func_int() : simple_func_int());
        Sum += (false ^ sfb_true ? simple_func_int() : ab[index]);
        Sum += (false ^ sfb_true ? simple_func_int() : ab[index - 1]);
        Sum += (false ^ sfb_true ? ab[index] : 3);
        Sum += (false ^ sfb_true ? ab[index] : -5);
        Sum += (false ^ sfb_true ? ab[index] : local_int);
        Sum += (false ^ sfb_true ? ab[index] : static_field_int);
        Sum += (false ^ sfb_true ? ab[index] : t1_i.mfi);
        Sum += (false ^ sfb_true ? ab[index] : simple_func_int());
        Sum += (false ^ sfb_true ? ab[index] : ab[index]);
        Sum += (false ^ sfb_true ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_54()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ sfb_true ? ab[index - 1] : 3);
        Sum += (false ^ sfb_true ? ab[index - 1] : -5);
        Sum += (false ^ sfb_true ? ab[index - 1] : local_int);
        Sum += (false ^ sfb_true ? ab[index - 1] : static_field_int);
        Sum += (false ^ sfb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (false ^ sfb_true ? ab[index - 1] : simple_func_int());
        Sum += (false ^ sfb_true ? ab[index - 1] : ab[index]);
        Sum += (false ^ sfb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (false ^ sfb_false ? 3 : 3);
        Sum += (false ^ sfb_false ? 3 : -5);
        Sum += (false ^ sfb_false ? 3 : local_int);
        Sum += (false ^ sfb_false ? 3 : static_field_int);
        Sum += (false ^ sfb_false ? 3 : t1_i.mfi);
        Sum += (false ^ sfb_false ? 3 : simple_func_int());
        Sum += (false ^ sfb_false ? 3 : ab[index]);
        Sum += (false ^ sfb_false ? 3 : ab[index - 1]);
        Sum += (false ^ sfb_false ? -5 : 3);
        Sum += (false ^ sfb_false ? -5 : -5);
        Sum += (false ^ sfb_false ? -5 : local_int);
        Sum += (false ^ sfb_false ? -5 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_55()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ sfb_false ? -5 : t1_i.mfi);
        Sum += (false ^ sfb_false ? -5 : simple_func_int());
        Sum += (false ^ sfb_false ? -5 : ab[index]);
        Sum += (false ^ sfb_false ? -5 : ab[index - 1]);
        Sum += (false ^ sfb_false ? local_int : 3);
        Sum += (false ^ sfb_false ? local_int : -5);
        Sum += (false ^ sfb_false ? local_int : local_int);
        Sum += (false ^ sfb_false ? local_int : static_field_int);
        Sum += (false ^ sfb_false ? local_int : t1_i.mfi);
        Sum += (false ^ sfb_false ? local_int : simple_func_int());
        Sum += (false ^ sfb_false ? local_int : ab[index]);
        Sum += (false ^ sfb_false ? local_int : ab[index - 1]);
        Sum += (false ^ sfb_false ? static_field_int : 3);
        Sum += (false ^ sfb_false ? static_field_int : -5);
        Sum += (false ^ sfb_false ? static_field_int : local_int);
        Sum += (false ^ sfb_false ? static_field_int : static_field_int);
        Sum += (false ^ sfb_false ? static_field_int : t1_i.mfi);
        Sum += (false ^ sfb_false ? static_field_int : simple_func_int());
        Sum += (false ^ sfb_false ? static_field_int : ab[index]);
        Sum += (false ^ sfb_false ? static_field_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_56()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ sfb_false ? t1_i.mfi : 3);
        Sum += (false ^ sfb_false ? t1_i.mfi : -5);
        Sum += (false ^ sfb_false ? t1_i.mfi : local_int);
        Sum += (false ^ sfb_false ? t1_i.mfi : static_field_int);
        Sum += (false ^ sfb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (false ^ sfb_false ? t1_i.mfi : simple_func_int());
        Sum += (false ^ sfb_false ? t1_i.mfi : ab[index]);
        Sum += (false ^ sfb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (false ^ sfb_false ? simple_func_int() : 3);
        Sum += (false ^ sfb_false ? simple_func_int() : -5);
        Sum += (false ^ sfb_false ? simple_func_int() : local_int);
        Sum += (false ^ sfb_false ? simple_func_int() : static_field_int);
        Sum += (false ^ sfb_false ? simple_func_int() : t1_i.mfi);
        Sum += (false ^ sfb_false ? simple_func_int() : simple_func_int());
        Sum += (false ^ sfb_false ? simple_func_int() : ab[index]);
        Sum += (false ^ sfb_false ? simple_func_int() : ab[index - 1]);
        Sum += (false ^ sfb_false ? ab[index] : 3);
        Sum += (false ^ sfb_false ? ab[index] : -5);
        Sum += (false ^ sfb_false ? ab[index] : local_int);
        Sum += (false ^ sfb_false ? ab[index] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_57()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ sfb_false ? ab[index] : t1_i.mfi);
        Sum += (false ^ sfb_false ? ab[index] : simple_func_int());
        Sum += (false ^ sfb_false ? ab[index] : ab[index]);
        Sum += (false ^ sfb_false ? ab[index] : ab[index - 1]);
        Sum += (false ^ sfb_false ? ab[index - 1] : 3);
        Sum += (false ^ sfb_false ? ab[index - 1] : -5);
        Sum += (false ^ sfb_false ? ab[index - 1] : local_int);
        Sum += (false ^ sfb_false ? ab[index - 1] : static_field_int);
        Sum += (false ^ sfb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (false ^ sfb_false ? ab[index - 1] : simple_func_int());
        Sum += (false ^ sfb_false ? ab[index - 1] : ab[index]);
        Sum += (false ^ sfb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (false ^ t1_i.mfb_true ? 3 : 3);
        Sum += (false ^ t1_i.mfb_true ? 3 : -5);
        Sum += (false ^ t1_i.mfb_true ? 3 : local_int);
        Sum += (false ^ t1_i.mfb_true ? 3 : static_field_int);
        Sum += (false ^ t1_i.mfb_true ? 3 : t1_i.mfi);
        Sum += (false ^ t1_i.mfb_true ? 3 : simple_func_int());
        Sum += (false ^ t1_i.mfb_true ? 3 : ab[index]);
        Sum += (false ^ t1_i.mfb_true ? 3 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_58()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ t1_i.mfb_true ? -5 : 3);
        Sum += (false ^ t1_i.mfb_true ? -5 : -5);
        Sum += (false ^ t1_i.mfb_true ? -5 : local_int);
        Sum += (false ^ t1_i.mfb_true ? -5 : static_field_int);
        Sum += (false ^ t1_i.mfb_true ? -5 : t1_i.mfi);
        Sum += (false ^ t1_i.mfb_true ? -5 : simple_func_int());
        Sum += (false ^ t1_i.mfb_true ? -5 : ab[index]);
        Sum += (false ^ t1_i.mfb_true ? -5 : ab[index - 1]);
        Sum += (false ^ t1_i.mfb_true ? local_int : 3);
        Sum += (false ^ t1_i.mfb_true ? local_int : -5);
        Sum += (false ^ t1_i.mfb_true ? local_int : local_int);
        Sum += (false ^ t1_i.mfb_true ? local_int : static_field_int);
        Sum += (false ^ t1_i.mfb_true ? local_int : t1_i.mfi);
        Sum += (false ^ t1_i.mfb_true ? local_int : simple_func_int());
        Sum += (false ^ t1_i.mfb_true ? local_int : ab[index]);
        Sum += (false ^ t1_i.mfb_true ? local_int : ab[index - 1]);
        Sum += (false ^ t1_i.mfb_true ? static_field_int : 3);
        Sum += (false ^ t1_i.mfb_true ? static_field_int : -5);
        Sum += (false ^ t1_i.mfb_true ? static_field_int : local_int);
        Sum += (false ^ t1_i.mfb_true ? static_field_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_59()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ t1_i.mfb_true ? static_field_int : t1_i.mfi);
        Sum += (false ^ t1_i.mfb_true ? static_field_int : simple_func_int());
        Sum += (false ^ t1_i.mfb_true ? static_field_int : ab[index]);
        Sum += (false ^ t1_i.mfb_true ? static_field_int : ab[index - 1]);
        Sum += (false ^ t1_i.mfb_true ? t1_i.mfi : 3);
        Sum += (false ^ t1_i.mfb_true ? t1_i.mfi : -5);
        Sum += (false ^ t1_i.mfb_true ? t1_i.mfi : local_int);
        Sum += (false ^ t1_i.mfb_true ? t1_i.mfi : static_field_int);
        Sum += (false ^ t1_i.mfb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (false ^ t1_i.mfb_true ? t1_i.mfi : simple_func_int());
        Sum += (false ^ t1_i.mfb_true ? t1_i.mfi : ab[index]);
        Sum += (false ^ t1_i.mfb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (false ^ t1_i.mfb_true ? simple_func_int() : 3);
        Sum += (false ^ t1_i.mfb_true ? simple_func_int() : -5);
        Sum += (false ^ t1_i.mfb_true ? simple_func_int() : local_int);
        Sum += (false ^ t1_i.mfb_true ? simple_func_int() : static_field_int);
        Sum += (false ^ t1_i.mfb_true ? simple_func_int() : t1_i.mfi);
        Sum += (false ^ t1_i.mfb_true ? simple_func_int() : simple_func_int());
        Sum += (false ^ t1_i.mfb_true ? simple_func_int() : ab[index]);
        Sum += (false ^ t1_i.mfb_true ? simple_func_int() : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_60()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ t1_i.mfb_true ? ab[index] : 3);
        Sum += (false ^ t1_i.mfb_true ? ab[index] : -5);
        Sum += (false ^ t1_i.mfb_true ? ab[index] : local_int);
        Sum += (false ^ t1_i.mfb_true ? ab[index] : static_field_int);
        Sum += (false ^ t1_i.mfb_true ? ab[index] : t1_i.mfi);
        Sum += (false ^ t1_i.mfb_true ? ab[index] : simple_func_int());
        Sum += (false ^ t1_i.mfb_true ? ab[index] : ab[index]);
        Sum += (false ^ t1_i.mfb_true ? ab[index] : ab[index - 1]);
        Sum += (false ^ t1_i.mfb_true ? ab[index - 1] : 3);
        Sum += (false ^ t1_i.mfb_true ? ab[index - 1] : -5);
        Sum += (false ^ t1_i.mfb_true ? ab[index - 1] : local_int);
        Sum += (false ^ t1_i.mfb_true ? ab[index - 1] : static_field_int);
        Sum += (false ^ t1_i.mfb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (false ^ t1_i.mfb_true ? ab[index - 1] : simple_func_int());
        Sum += (false ^ t1_i.mfb_true ? ab[index - 1] : ab[index]);
        Sum += (false ^ t1_i.mfb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (false ^ t1_i.mfb_false ? 3 : 3);
        Sum += (false ^ t1_i.mfb_false ? 3 : -5);
        Sum += (false ^ t1_i.mfb_false ? 3 : local_int);
        Sum += (false ^ t1_i.mfb_false ? 3 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_61()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ t1_i.mfb_false ? 3 : t1_i.mfi);
        Sum += (false ^ t1_i.mfb_false ? 3 : simple_func_int());
        Sum += (false ^ t1_i.mfb_false ? 3 : ab[index]);
        Sum += (false ^ t1_i.mfb_false ? 3 : ab[index - 1]);
        Sum += (false ^ t1_i.mfb_false ? -5 : 3);
        Sum += (false ^ t1_i.mfb_false ? -5 : -5);
        Sum += (false ^ t1_i.mfb_false ? -5 : local_int);
        Sum += (false ^ t1_i.mfb_false ? -5 : static_field_int);
        Sum += (false ^ t1_i.mfb_false ? -5 : t1_i.mfi);
        Sum += (false ^ t1_i.mfb_false ? -5 : simple_func_int());
        Sum += (false ^ t1_i.mfb_false ? -5 : ab[index]);
        Sum += (false ^ t1_i.mfb_false ? -5 : ab[index - 1]);
        Sum += (false ^ t1_i.mfb_false ? local_int : 3);
        Sum += (false ^ t1_i.mfb_false ? local_int : -5);
        Sum += (false ^ t1_i.mfb_false ? local_int : local_int);
        Sum += (false ^ t1_i.mfb_false ? local_int : static_field_int);
        Sum += (false ^ t1_i.mfb_false ? local_int : t1_i.mfi);
        Sum += (false ^ t1_i.mfb_false ? local_int : simple_func_int());
        Sum += (false ^ t1_i.mfb_false ? local_int : ab[index]);
        Sum += (false ^ t1_i.mfb_false ? local_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_62()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ t1_i.mfb_false ? static_field_int : 3);
        Sum += (false ^ t1_i.mfb_false ? static_field_int : -5);
        Sum += (false ^ t1_i.mfb_false ? static_field_int : local_int);
        Sum += (false ^ t1_i.mfb_false ? static_field_int : static_field_int);
        Sum += (false ^ t1_i.mfb_false ? static_field_int : t1_i.mfi);
        Sum += (false ^ t1_i.mfb_false ? static_field_int : simple_func_int());
        Sum += (false ^ t1_i.mfb_false ? static_field_int : ab[index]);
        Sum += (false ^ t1_i.mfb_false ? static_field_int : ab[index - 1]);
        Sum += (false ^ t1_i.mfb_false ? t1_i.mfi : 3);
        Sum += (false ^ t1_i.mfb_false ? t1_i.mfi : -5);
        Sum += (false ^ t1_i.mfb_false ? t1_i.mfi : local_int);
        Sum += (false ^ t1_i.mfb_false ? t1_i.mfi : static_field_int);
        Sum += (false ^ t1_i.mfb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (false ^ t1_i.mfb_false ? t1_i.mfi : simple_func_int());
        Sum += (false ^ t1_i.mfb_false ? t1_i.mfi : ab[index]);
        Sum += (false ^ t1_i.mfb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (false ^ t1_i.mfb_false ? simple_func_int() : 3);
        Sum += (false ^ t1_i.mfb_false ? simple_func_int() : -5);
        Sum += (false ^ t1_i.mfb_false ? simple_func_int() : local_int);
        Sum += (false ^ t1_i.mfb_false ? simple_func_int() : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_63()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ t1_i.mfb_false ? simple_func_int() : t1_i.mfi);
        Sum += (false ^ t1_i.mfb_false ? simple_func_int() : simple_func_int());
        Sum += (false ^ t1_i.mfb_false ? simple_func_int() : ab[index]);
        Sum += (false ^ t1_i.mfb_false ? simple_func_int() : ab[index - 1]);
        Sum += (false ^ t1_i.mfb_false ? ab[index] : 3);
        Sum += (false ^ t1_i.mfb_false ? ab[index] : -5);
        Sum += (false ^ t1_i.mfb_false ? ab[index] : local_int);
        Sum += (false ^ t1_i.mfb_false ? ab[index] : static_field_int);
        Sum += (false ^ t1_i.mfb_false ? ab[index] : t1_i.mfi);
        Sum += (false ^ t1_i.mfb_false ? ab[index] : simple_func_int());
        Sum += (false ^ t1_i.mfb_false ? ab[index] : ab[index]);
        Sum += (false ^ t1_i.mfb_false ? ab[index] : ab[index - 1]);
        Sum += (false ^ t1_i.mfb_false ? ab[index - 1] : 3);
        Sum += (false ^ t1_i.mfb_false ? ab[index - 1] : -5);
        Sum += (false ^ t1_i.mfb_false ? ab[index - 1] : local_int);
        Sum += (false ^ t1_i.mfb_false ? ab[index - 1] : static_field_int);
        Sum += (false ^ t1_i.mfb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (false ^ t1_i.mfb_false ? ab[index - 1] : simple_func_int());
        Sum += (false ^ t1_i.mfb_false ? ab[index - 1] : ab[index]);
        Sum += (false ^ t1_i.mfb_false ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_64()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ func_sb_true() ? 3 : 3);
        Sum += (false ^ func_sb_true() ? 3 : -5);
        Sum += (false ^ func_sb_true() ? 3 : local_int);
        Sum += (false ^ func_sb_true() ? 3 : static_field_int);
        Sum += (false ^ func_sb_true() ? 3 : t1_i.mfi);
        Sum += (false ^ func_sb_true() ? 3 : simple_func_int());
        Sum += (false ^ func_sb_true() ? 3 : ab[index]);
        Sum += (false ^ func_sb_true() ? 3 : ab[index - 1]);
        Sum += (false ^ func_sb_true() ? -5 : 3);
        Sum += (false ^ func_sb_true() ? -5 : -5);
        Sum += (false ^ func_sb_true() ? -5 : local_int);
        Sum += (false ^ func_sb_true() ? -5 : static_field_int);
        Sum += (false ^ func_sb_true() ? -5 : t1_i.mfi);
        Sum += (false ^ func_sb_true() ? -5 : simple_func_int());
        Sum += (false ^ func_sb_true() ? -5 : ab[index]);
        Sum += (false ^ func_sb_true() ? -5 : ab[index - 1]);
        Sum += (false ^ func_sb_true() ? local_int : 3);
        Sum += (false ^ func_sb_true() ? local_int : -5);
        Sum += (false ^ func_sb_true() ? local_int : local_int);
        Sum += (false ^ func_sb_true() ? local_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_65()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ func_sb_true() ? local_int : t1_i.mfi);
        Sum += (false ^ func_sb_true() ? local_int : simple_func_int());
        Sum += (false ^ func_sb_true() ? local_int : ab[index]);
        Sum += (false ^ func_sb_true() ? local_int : ab[index - 1]);
        Sum += (false ^ func_sb_true() ? static_field_int : 3);
        Sum += (false ^ func_sb_true() ? static_field_int : -5);
        Sum += (false ^ func_sb_true() ? static_field_int : local_int);
        Sum += (false ^ func_sb_true() ? static_field_int : static_field_int);
        Sum += (false ^ func_sb_true() ? static_field_int : t1_i.mfi);
        Sum += (false ^ func_sb_true() ? static_field_int : simple_func_int());
        Sum += (false ^ func_sb_true() ? static_field_int : ab[index]);
        Sum += (false ^ func_sb_true() ? static_field_int : ab[index - 1]);
        Sum += (false ^ func_sb_true() ? t1_i.mfi : 3);
        Sum += (false ^ func_sb_true() ? t1_i.mfi : -5);
        Sum += (false ^ func_sb_true() ? t1_i.mfi : local_int);
        Sum += (false ^ func_sb_true() ? t1_i.mfi : static_field_int);
        Sum += (false ^ func_sb_true() ? t1_i.mfi : t1_i.mfi);
        Sum += (false ^ func_sb_true() ? t1_i.mfi : simple_func_int());
        Sum += (false ^ func_sb_true() ? t1_i.mfi : ab[index]);
        Sum += (false ^ func_sb_true() ? t1_i.mfi : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_66()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ func_sb_true() ? simple_func_int() : 3);
        Sum += (false ^ func_sb_true() ? simple_func_int() : -5);
        Sum += (false ^ func_sb_true() ? simple_func_int() : local_int);
        Sum += (false ^ func_sb_true() ? simple_func_int() : static_field_int);
        Sum += (false ^ func_sb_true() ? simple_func_int() : t1_i.mfi);
        Sum += (false ^ func_sb_true() ? simple_func_int() : simple_func_int());
        Sum += (false ^ func_sb_true() ? simple_func_int() : ab[index]);
        Sum += (false ^ func_sb_true() ? simple_func_int() : ab[index - 1]);
        Sum += (false ^ func_sb_true() ? ab[index] : 3);
        Sum += (false ^ func_sb_true() ? ab[index] : -5);
        Sum += (false ^ func_sb_true() ? ab[index] : local_int);
        Sum += (false ^ func_sb_true() ? ab[index] : static_field_int);
        Sum += (false ^ func_sb_true() ? ab[index] : t1_i.mfi);
        Sum += (false ^ func_sb_true() ? ab[index] : simple_func_int());
        Sum += (false ^ func_sb_true() ? ab[index] : ab[index]);
        Sum += (false ^ func_sb_true() ? ab[index] : ab[index - 1]);
        Sum += (false ^ func_sb_true() ? ab[index - 1] : 3);
        Sum += (false ^ func_sb_true() ? ab[index - 1] : -5);
        Sum += (false ^ func_sb_true() ? ab[index - 1] : local_int);
        Sum += (false ^ func_sb_true() ? ab[index - 1] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_67()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ func_sb_true() ? ab[index - 1] : t1_i.mfi);
        Sum += (false ^ func_sb_true() ? ab[index - 1] : simple_func_int());
        Sum += (false ^ func_sb_true() ? ab[index - 1] : ab[index]);
        Sum += (false ^ func_sb_true() ? ab[index - 1] : ab[index - 1]);
        Sum += (false ^ func_sb_false() ? 3 : 3);
        Sum += (false ^ func_sb_false() ? 3 : -5);
        Sum += (false ^ func_sb_false() ? 3 : local_int);
        Sum += (false ^ func_sb_false() ? 3 : static_field_int);
        Sum += (false ^ func_sb_false() ? 3 : t1_i.mfi);
        Sum += (false ^ func_sb_false() ? 3 : simple_func_int());
        Sum += (false ^ func_sb_false() ? 3 : ab[index]);
        Sum += (false ^ func_sb_false() ? 3 : ab[index - 1]);
        Sum += (false ^ func_sb_false() ? -5 : 3);
        Sum += (false ^ func_sb_false() ? -5 : -5);
        Sum += (false ^ func_sb_false() ? -5 : local_int);
        Sum += (false ^ func_sb_false() ? -5 : static_field_int);
        Sum += (false ^ func_sb_false() ? -5 : t1_i.mfi);
        Sum += (false ^ func_sb_false() ? -5 : simple_func_int());
        Sum += (false ^ func_sb_false() ? -5 : ab[index]);
        Sum += (false ^ func_sb_false() ? -5 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_68()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ func_sb_false() ? local_int : 3);
        Sum += (false ^ func_sb_false() ? local_int : -5);
        Sum += (false ^ func_sb_false() ? local_int : local_int);
        Sum += (false ^ func_sb_false() ? local_int : static_field_int);
        Sum += (false ^ func_sb_false() ? local_int : t1_i.mfi);
        Sum += (false ^ func_sb_false() ? local_int : simple_func_int());
        Sum += (false ^ func_sb_false() ? local_int : ab[index]);
        Sum += (false ^ func_sb_false() ? local_int : ab[index - 1]);
        Sum += (false ^ func_sb_false() ? static_field_int : 3);
        Sum += (false ^ func_sb_false() ? static_field_int : -5);
        Sum += (false ^ func_sb_false() ? static_field_int : local_int);
        Sum += (false ^ func_sb_false() ? static_field_int : static_field_int);
        Sum += (false ^ func_sb_false() ? static_field_int : t1_i.mfi);
        Sum += (false ^ func_sb_false() ? static_field_int : simple_func_int());
        Sum += (false ^ func_sb_false() ? static_field_int : ab[index]);
        Sum += (false ^ func_sb_false() ? static_field_int : ab[index - 1]);
        Sum += (false ^ func_sb_false() ? t1_i.mfi : 3);
        Sum += (false ^ func_sb_false() ? t1_i.mfi : -5);
        Sum += (false ^ func_sb_false() ? t1_i.mfi : local_int);
        Sum += (false ^ func_sb_false() ? t1_i.mfi : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_69()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ func_sb_false() ? t1_i.mfi : t1_i.mfi);
        Sum += (false ^ func_sb_false() ? t1_i.mfi : simple_func_int());
        Sum += (false ^ func_sb_false() ? t1_i.mfi : ab[index]);
        Sum += (false ^ func_sb_false() ? t1_i.mfi : ab[index - 1]);
        Sum += (false ^ func_sb_false() ? simple_func_int() : 3);
        Sum += (false ^ func_sb_false() ? simple_func_int() : -5);
        Sum += (false ^ func_sb_false() ? simple_func_int() : local_int);
        Sum += (false ^ func_sb_false() ? simple_func_int() : static_field_int);
        Sum += (false ^ func_sb_false() ? simple_func_int() : t1_i.mfi);
        Sum += (false ^ func_sb_false() ? simple_func_int() : simple_func_int());
        Sum += (false ^ func_sb_false() ? simple_func_int() : ab[index]);
        Sum += (false ^ func_sb_false() ? simple_func_int() : ab[index - 1]);
        Sum += (false ^ func_sb_false() ? ab[index] : 3);
        Sum += (false ^ func_sb_false() ? ab[index] : -5);
        Sum += (false ^ func_sb_false() ? ab[index] : local_int);
        Sum += (false ^ func_sb_false() ? ab[index] : static_field_int);
        Sum += (false ^ func_sb_false() ? ab[index] : t1_i.mfi);
        Sum += (false ^ func_sb_false() ? ab[index] : simple_func_int());
        Sum += (false ^ func_sb_false() ? ab[index] : ab[index]);
        Sum += (false ^ func_sb_false() ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_70()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ func_sb_false() ? ab[index - 1] : 3);
        Sum += (false ^ func_sb_false() ? ab[index - 1] : -5);
        Sum += (false ^ func_sb_false() ? ab[index - 1] : local_int);
        Sum += (false ^ func_sb_false() ? ab[index - 1] : static_field_int);
        Sum += (false ^ func_sb_false() ? ab[index - 1] : t1_i.mfi);
        Sum += (false ^ func_sb_false() ? ab[index - 1] : simple_func_int());
        Sum += (false ^ func_sb_false() ? ab[index - 1] : ab[index]);
        Sum += (false ^ func_sb_false() ? ab[index - 1] : ab[index - 1]);
        Sum += (false ^ ab_true[index] ? 3 : 3);
        Sum += (false ^ ab_true[index] ? 3 : -5);
        Sum += (false ^ ab_true[index] ? 3 : local_int);
        Sum += (false ^ ab_true[index] ? 3 : static_field_int);
        Sum += (false ^ ab_true[index] ? 3 : t1_i.mfi);
        Sum += (false ^ ab_true[index] ? 3 : simple_func_int());
        Sum += (false ^ ab_true[index] ? 3 : ab[index]);
        Sum += (false ^ ab_true[index] ? 3 : ab[index - 1]);
        Sum += (false ^ ab_true[index] ? -5 : 3);
        Sum += (false ^ ab_true[index] ? -5 : -5);
        Sum += (false ^ ab_true[index] ? -5 : local_int);
        Sum += (false ^ ab_true[index] ? -5 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_71()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ ab_true[index] ? -5 : t1_i.mfi);
        Sum += (false ^ ab_true[index] ? -5 : simple_func_int());
        Sum += (false ^ ab_true[index] ? -5 : ab[index]);
        Sum += (false ^ ab_true[index] ? -5 : ab[index - 1]);
        Sum += (false ^ ab_true[index] ? local_int : 3);
        Sum += (false ^ ab_true[index] ? local_int : -5);
        Sum += (false ^ ab_true[index] ? local_int : local_int);
        Sum += (false ^ ab_true[index] ? local_int : static_field_int);
        Sum += (false ^ ab_true[index] ? local_int : t1_i.mfi);
        Sum += (false ^ ab_true[index] ? local_int : simple_func_int());
        Sum += (false ^ ab_true[index] ? local_int : ab[index]);
        Sum += (false ^ ab_true[index] ? local_int : ab[index - 1]);
        Sum += (false ^ ab_true[index] ? static_field_int : 3);
        Sum += (false ^ ab_true[index] ? static_field_int : -5);
        Sum += (false ^ ab_true[index] ? static_field_int : local_int);
        Sum += (false ^ ab_true[index] ? static_field_int : static_field_int);
        Sum += (false ^ ab_true[index] ? static_field_int : t1_i.mfi);
        Sum += (false ^ ab_true[index] ? static_field_int : simple_func_int());
        Sum += (false ^ ab_true[index] ? static_field_int : ab[index]);
        Sum += (false ^ ab_true[index] ? static_field_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_72()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ ab_true[index] ? t1_i.mfi : 3);
        Sum += (false ^ ab_true[index] ? t1_i.mfi : -5);
        Sum += (false ^ ab_true[index] ? t1_i.mfi : local_int);
        Sum += (false ^ ab_true[index] ? t1_i.mfi : static_field_int);
        Sum += (false ^ ab_true[index] ? t1_i.mfi : t1_i.mfi);
        Sum += (false ^ ab_true[index] ? t1_i.mfi : simple_func_int());
        Sum += (false ^ ab_true[index] ? t1_i.mfi : ab[index]);
        Sum += (false ^ ab_true[index] ? t1_i.mfi : ab[index - 1]);
        Sum += (false ^ ab_true[index] ? simple_func_int() : 3);
        Sum += (false ^ ab_true[index] ? simple_func_int() : -5);
        Sum += (false ^ ab_true[index] ? simple_func_int() : local_int);
        Sum += (false ^ ab_true[index] ? simple_func_int() : static_field_int);
        Sum += (false ^ ab_true[index] ? simple_func_int() : t1_i.mfi);
        Sum += (false ^ ab_true[index] ? simple_func_int() : simple_func_int());
        Sum += (false ^ ab_true[index] ? simple_func_int() : ab[index]);
        Sum += (false ^ ab_true[index] ? simple_func_int() : ab[index - 1]);
        Sum += (false ^ ab_true[index] ? ab[index] : 3);
        Sum += (false ^ ab_true[index] ? ab[index] : -5);
        Sum += (false ^ ab_true[index] ? ab[index] : local_int);
        Sum += (false ^ ab_true[index] ? ab[index] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_73()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ ab_true[index] ? ab[index] : t1_i.mfi);
        Sum += (false ^ ab_true[index] ? ab[index] : simple_func_int());
        Sum += (false ^ ab_true[index] ? ab[index] : ab[index]);
        Sum += (false ^ ab_true[index] ? ab[index] : ab[index - 1]);
        Sum += (false ^ ab_true[index] ? ab[index - 1] : 3);
        Sum += (false ^ ab_true[index] ? ab[index - 1] : -5);
        Sum += (false ^ ab_true[index] ? ab[index - 1] : local_int);
        Sum += (false ^ ab_true[index] ? ab[index - 1] : static_field_int);
        Sum += (false ^ ab_true[index] ? ab[index - 1] : t1_i.mfi);
        Sum += (false ^ ab_true[index] ? ab[index - 1] : simple_func_int());
        Sum += (false ^ ab_true[index] ? ab[index - 1] : ab[index]);
        Sum += (false ^ ab_true[index] ? ab[index - 1] : ab[index - 1]);
        Sum += (false ^ ab_false[index] ? 3 : 3);
        Sum += (false ^ ab_false[index] ? 3 : -5);
        Sum += (false ^ ab_false[index] ? 3 : local_int);
        Sum += (false ^ ab_false[index] ? 3 : static_field_int);
        Sum += (false ^ ab_false[index] ? 3 : t1_i.mfi);
        Sum += (false ^ ab_false[index] ? 3 : simple_func_int());
        Sum += (false ^ ab_false[index] ? 3 : ab[index]);
        Sum += (false ^ ab_false[index] ? 3 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_74()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ ab_false[index] ? -5 : 3);
        Sum += (false ^ ab_false[index] ? -5 : -5);
        Sum += (false ^ ab_false[index] ? -5 : local_int);
        Sum += (false ^ ab_false[index] ? -5 : static_field_int);
        Sum += (false ^ ab_false[index] ? -5 : t1_i.mfi);
        Sum += (false ^ ab_false[index] ? -5 : simple_func_int());
        Sum += (false ^ ab_false[index] ? -5 : ab[index]);
        Sum += (false ^ ab_false[index] ? -5 : ab[index - 1]);
        Sum += (false ^ ab_false[index] ? local_int : 3);
        Sum += (false ^ ab_false[index] ? local_int : -5);
        Sum += (false ^ ab_false[index] ? local_int : local_int);
        Sum += (false ^ ab_false[index] ? local_int : static_field_int);
        Sum += (false ^ ab_false[index] ? local_int : t1_i.mfi);
        Sum += (false ^ ab_false[index] ? local_int : simple_func_int());
        Sum += (false ^ ab_false[index] ? local_int : ab[index]);
        Sum += (false ^ ab_false[index] ? local_int : ab[index - 1]);
        Sum += (false ^ ab_false[index] ? static_field_int : 3);
        Sum += (false ^ ab_false[index] ? static_field_int : -5);
        Sum += (false ^ ab_false[index] ? static_field_int : local_int);
        Sum += (false ^ ab_false[index] ? static_field_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_75()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ ab_false[index] ? static_field_int : t1_i.mfi);
        Sum += (false ^ ab_false[index] ? static_field_int : simple_func_int());
        Sum += (false ^ ab_false[index] ? static_field_int : ab[index]);
        Sum += (false ^ ab_false[index] ? static_field_int : ab[index - 1]);
        Sum += (false ^ ab_false[index] ? t1_i.mfi : 3);
        Sum += (false ^ ab_false[index] ? t1_i.mfi : -5);
        Sum += (false ^ ab_false[index] ? t1_i.mfi : local_int);
        Sum += (false ^ ab_false[index] ? t1_i.mfi : static_field_int);
        Sum += (false ^ ab_false[index] ? t1_i.mfi : t1_i.mfi);
        Sum += (false ^ ab_false[index] ? t1_i.mfi : simple_func_int());
        Sum += (false ^ ab_false[index] ? t1_i.mfi : ab[index]);
        Sum += (false ^ ab_false[index] ? t1_i.mfi : ab[index - 1]);
        Sum += (false ^ ab_false[index] ? simple_func_int() : 3);
        Sum += (false ^ ab_false[index] ? simple_func_int() : -5);
        Sum += (false ^ ab_false[index] ? simple_func_int() : local_int);
        Sum += (false ^ ab_false[index] ? simple_func_int() : static_field_int);
        Sum += (false ^ ab_false[index] ? simple_func_int() : t1_i.mfi);
        Sum += (false ^ ab_false[index] ? simple_func_int() : simple_func_int());
        Sum += (false ^ ab_false[index] ? simple_func_int() : ab[index]);
        Sum += (false ^ ab_false[index] ? simple_func_int() : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_76()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ^ ab_false[index] ? ab[index] : 3);
        Sum += (false ^ ab_false[index] ? ab[index] : -5);
        Sum += (false ^ ab_false[index] ? ab[index] : local_int);
        Sum += (false ^ ab_false[index] ? ab[index] : static_field_int);
        Sum += (false ^ ab_false[index] ? ab[index] : t1_i.mfi);
        Sum += (false ^ ab_false[index] ? ab[index] : simple_func_int());
        Sum += (false ^ ab_false[index] ? ab[index] : ab[index]);
        Sum += (false ^ ab_false[index] ? ab[index] : ab[index - 1]);
        Sum += (false ^ ab_false[index] ? ab[index - 1] : 3);
        Sum += (false ^ ab_false[index] ? ab[index - 1] : -5);
        Sum += (false ^ ab_false[index] ? ab[index - 1] : local_int);
        Sum += (false ^ ab_false[index] ? ab[index - 1] : static_field_int);
        Sum += (false ^ ab_false[index] ? ab[index - 1] : t1_i.mfi);
        Sum += (false ^ ab_false[index] ? ab[index - 1] : simple_func_int());
        Sum += (false ^ ab_false[index] ? ab[index - 1] : ab[index]);
        Sum += (false ^ ab_false[index] ? ab[index - 1] : ab[index - 1]);
        Sum += (lb_true ^ true ? 3 : 3);
        Sum += (lb_true ^ true ? 3 : -5);
        Sum += (lb_true ^ true ? 3 : local_int);
        Sum += (lb_true ^ true ? 3 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_77()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ true ? 3 : t1_i.mfi);
        Sum += (lb_true ^ true ? 3 : simple_func_int());
        Sum += (lb_true ^ true ? 3 : ab[index]);
        Sum += (lb_true ^ true ? 3 : ab[index - 1]);
        Sum += (lb_true ^ true ? -5 : 3);
        Sum += (lb_true ^ true ? -5 : -5);
        Sum += (lb_true ^ true ? -5 : local_int);
        Sum += (lb_true ^ true ? -5 : static_field_int);
        Sum += (lb_true ^ true ? -5 : t1_i.mfi);
        Sum += (lb_true ^ true ? -5 : simple_func_int());
        Sum += (lb_true ^ true ? -5 : ab[index]);
        Sum += (lb_true ^ true ? -5 : ab[index - 1]);
        Sum += (lb_true ^ true ? local_int : 3);
        Sum += (lb_true ^ true ? local_int : -5);
        Sum += (lb_true ^ true ? local_int : local_int);
        Sum += (lb_true ^ true ? local_int : static_field_int);
        Sum += (lb_true ^ true ? local_int : t1_i.mfi);
        Sum += (lb_true ^ true ? local_int : simple_func_int());
        Sum += (lb_true ^ true ? local_int : ab[index]);
        Sum += (lb_true ^ true ? local_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_78()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ true ? static_field_int : 3);
        Sum += (lb_true ^ true ? static_field_int : -5);
        Sum += (lb_true ^ true ? static_field_int : local_int);
        Sum += (lb_true ^ true ? static_field_int : static_field_int);
        Sum += (lb_true ^ true ? static_field_int : t1_i.mfi);
        Sum += (lb_true ^ true ? static_field_int : simple_func_int());
        Sum += (lb_true ^ true ? static_field_int : ab[index]);
        Sum += (lb_true ^ true ? static_field_int : ab[index - 1]);
        Sum += (lb_true ^ true ? t1_i.mfi : 3);
        Sum += (lb_true ^ true ? t1_i.mfi : -5);
        Sum += (lb_true ^ true ? t1_i.mfi : local_int);
        Sum += (lb_true ^ true ? t1_i.mfi : static_field_int);
        Sum += (lb_true ^ true ? t1_i.mfi : t1_i.mfi);
        Sum += (lb_true ^ true ? t1_i.mfi : simple_func_int());
        Sum += (lb_true ^ true ? t1_i.mfi : ab[index]);
        Sum += (lb_true ^ true ? t1_i.mfi : ab[index - 1]);
        Sum += (lb_true ^ true ? simple_func_int() : 3);
        Sum += (lb_true ^ true ? simple_func_int() : -5);
        Sum += (lb_true ^ true ? simple_func_int() : local_int);
        Sum += (lb_true ^ true ? simple_func_int() : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_79()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ true ? simple_func_int() : t1_i.mfi);
        Sum += (lb_true ^ true ? simple_func_int() : simple_func_int());
        Sum += (lb_true ^ true ? simple_func_int() : ab[index]);
        Sum += (lb_true ^ true ? simple_func_int() : ab[index - 1]);
        Sum += (lb_true ^ true ? ab[index] : 3);
        Sum += (lb_true ^ true ? ab[index] : -5);
        Sum += (lb_true ^ true ? ab[index] : local_int);
        Sum += (lb_true ^ true ? ab[index] : static_field_int);
        Sum += (lb_true ^ true ? ab[index] : t1_i.mfi);
        Sum += (lb_true ^ true ? ab[index] : simple_func_int());
        Sum += (lb_true ^ true ? ab[index] : ab[index]);
        Sum += (lb_true ^ true ? ab[index] : ab[index - 1]);
        Sum += (lb_true ^ true ? ab[index - 1] : 3);
        Sum += (lb_true ^ true ? ab[index - 1] : -5);
        Sum += (lb_true ^ true ? ab[index - 1] : local_int);
        Sum += (lb_true ^ true ? ab[index - 1] : static_field_int);
        Sum += (lb_true ^ true ? ab[index - 1] : t1_i.mfi);
        Sum += (lb_true ^ true ? ab[index - 1] : simple_func_int());
        Sum += (lb_true ^ true ? ab[index - 1] : ab[index]);
        Sum += (lb_true ^ true ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_80()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ false ? 3 : 3);
        Sum += (lb_true ^ false ? 3 : -5);
        Sum += (lb_true ^ false ? 3 : local_int);
        Sum += (lb_true ^ false ? 3 : static_field_int);
        Sum += (lb_true ^ false ? 3 : t1_i.mfi);
        Sum += (lb_true ^ false ? 3 : simple_func_int());
        Sum += (lb_true ^ false ? 3 : ab[index]);
        Sum += (lb_true ^ false ? 3 : ab[index - 1]);
        Sum += (lb_true ^ false ? -5 : 3);
        Sum += (lb_true ^ false ? -5 : -5);
        Sum += (lb_true ^ false ? -5 : local_int);
        Sum += (lb_true ^ false ? -5 : static_field_int);
        Sum += (lb_true ^ false ? -5 : t1_i.mfi);
        Sum += (lb_true ^ false ? -5 : simple_func_int());
        Sum += (lb_true ^ false ? -5 : ab[index]);
        Sum += (lb_true ^ false ? -5 : ab[index - 1]);
        Sum += (lb_true ^ false ? local_int : 3);
        Sum += (lb_true ^ false ? local_int : -5);
        Sum += (lb_true ^ false ? local_int : local_int);
        Sum += (lb_true ^ false ? local_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_81()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ false ? local_int : t1_i.mfi);
        Sum += (lb_true ^ false ? local_int : simple_func_int());
        Sum += (lb_true ^ false ? local_int : ab[index]);
        Sum += (lb_true ^ false ? local_int : ab[index - 1]);
        Sum += (lb_true ^ false ? static_field_int : 3);
        Sum += (lb_true ^ false ? static_field_int : -5);
        Sum += (lb_true ^ false ? static_field_int : local_int);
        Sum += (lb_true ^ false ? static_field_int : static_field_int);
        Sum += (lb_true ^ false ? static_field_int : t1_i.mfi);
        Sum += (lb_true ^ false ? static_field_int : simple_func_int());
        Sum += (lb_true ^ false ? static_field_int : ab[index]);
        Sum += (lb_true ^ false ? static_field_int : ab[index - 1]);
        Sum += (lb_true ^ false ? t1_i.mfi : 3);
        Sum += (lb_true ^ false ? t1_i.mfi : -5);
        Sum += (lb_true ^ false ? t1_i.mfi : local_int);
        Sum += (lb_true ^ false ? t1_i.mfi : static_field_int);
        Sum += (lb_true ^ false ? t1_i.mfi : t1_i.mfi);
        Sum += (lb_true ^ false ? t1_i.mfi : simple_func_int());
        Sum += (lb_true ^ false ? t1_i.mfi : ab[index]);
        Sum += (lb_true ^ false ? t1_i.mfi : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_82()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ false ? simple_func_int() : 3);
        Sum += (lb_true ^ false ? simple_func_int() : -5);
        Sum += (lb_true ^ false ? simple_func_int() : local_int);
        Sum += (lb_true ^ false ? simple_func_int() : static_field_int);
        Sum += (lb_true ^ false ? simple_func_int() : t1_i.mfi);
        Sum += (lb_true ^ false ? simple_func_int() : simple_func_int());
        Sum += (lb_true ^ false ? simple_func_int() : ab[index]);
        Sum += (lb_true ^ false ? simple_func_int() : ab[index - 1]);
        Sum += (lb_true ^ false ? ab[index] : 3);
        Sum += (lb_true ^ false ? ab[index] : -5);
        Sum += (lb_true ^ false ? ab[index] : local_int);
        Sum += (lb_true ^ false ? ab[index] : static_field_int);
        Sum += (lb_true ^ false ? ab[index] : t1_i.mfi);
        Sum += (lb_true ^ false ? ab[index] : simple_func_int());
        Sum += (lb_true ^ false ? ab[index] : ab[index]);
        Sum += (lb_true ^ false ? ab[index] : ab[index - 1]);
        Sum += (lb_true ^ false ? ab[index - 1] : 3);
        Sum += (lb_true ^ false ? ab[index - 1] : -5);
        Sum += (lb_true ^ false ? ab[index - 1] : local_int);
        Sum += (lb_true ^ false ? ab[index - 1] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_83()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ false ? ab[index - 1] : t1_i.mfi);
        Sum += (lb_true ^ false ? ab[index - 1] : simple_func_int());
        Sum += (lb_true ^ false ? ab[index - 1] : ab[index]);
        Sum += (lb_true ^ false ? ab[index - 1] : ab[index - 1]);
        Sum += (lb_true ^ lb_true ? 3 : 3);
        Sum += (lb_true ^ lb_true ? 3 : -5);
        Sum += (lb_true ^ lb_true ? 3 : local_int);
        Sum += (lb_true ^ lb_true ? 3 : static_field_int);
        Sum += (lb_true ^ lb_true ? 3 : t1_i.mfi);
        Sum += (lb_true ^ lb_true ? 3 : simple_func_int());
        Sum += (lb_true ^ lb_true ? 3 : ab[index]);
        Sum += (lb_true ^ lb_true ? 3 : ab[index - 1]);
        Sum += (lb_true ^ lb_true ? -5 : 3);
        Sum += (lb_true ^ lb_true ? -5 : -5);
        Sum += (lb_true ^ lb_true ? -5 : local_int);
        Sum += (lb_true ^ lb_true ? -5 : static_field_int);
        Sum += (lb_true ^ lb_true ? -5 : t1_i.mfi);
        Sum += (lb_true ^ lb_true ? -5 : simple_func_int());
        Sum += (lb_true ^ lb_true ? -5 : ab[index]);
        Sum += (lb_true ^ lb_true ? -5 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_84()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ lb_true ? local_int : 3);
        Sum += (lb_true ^ lb_true ? local_int : -5);
        Sum += (lb_true ^ lb_true ? local_int : local_int);
        Sum += (lb_true ^ lb_true ? local_int : static_field_int);
        Sum += (lb_true ^ lb_true ? local_int : t1_i.mfi);
        Sum += (lb_true ^ lb_true ? local_int : simple_func_int());
        Sum += (lb_true ^ lb_true ? local_int : ab[index]);
        Sum += (lb_true ^ lb_true ? local_int : ab[index - 1]);
        Sum += (lb_true ^ lb_true ? static_field_int : 3);
        Sum += (lb_true ^ lb_true ? static_field_int : -5);
        Sum += (lb_true ^ lb_true ? static_field_int : local_int);
        Sum += (lb_true ^ lb_true ? static_field_int : static_field_int);
        Sum += (lb_true ^ lb_true ? static_field_int : t1_i.mfi);
        Sum += (lb_true ^ lb_true ? static_field_int : simple_func_int());
        Sum += (lb_true ^ lb_true ? static_field_int : ab[index]);
        Sum += (lb_true ^ lb_true ? static_field_int : ab[index - 1]);
        Sum += (lb_true ^ lb_true ? t1_i.mfi : 3);
        Sum += (lb_true ^ lb_true ? t1_i.mfi : -5);
        Sum += (lb_true ^ lb_true ? t1_i.mfi : local_int);
        Sum += (lb_true ^ lb_true ? t1_i.mfi : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_85()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ lb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (lb_true ^ lb_true ? t1_i.mfi : simple_func_int());
        Sum += (lb_true ^ lb_true ? t1_i.mfi : ab[index]);
        Sum += (lb_true ^ lb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (lb_true ^ lb_true ? simple_func_int() : 3);
        Sum += (lb_true ^ lb_true ? simple_func_int() : -5);
        Sum += (lb_true ^ lb_true ? simple_func_int() : local_int);
        Sum += (lb_true ^ lb_true ? simple_func_int() : static_field_int);
        Sum += (lb_true ^ lb_true ? simple_func_int() : t1_i.mfi);
        Sum += (lb_true ^ lb_true ? simple_func_int() : simple_func_int());
        Sum += (lb_true ^ lb_true ? simple_func_int() : ab[index]);
        Sum += (lb_true ^ lb_true ? simple_func_int() : ab[index - 1]);
        Sum += (lb_true ^ lb_true ? ab[index] : 3);
        Sum += (lb_true ^ lb_true ? ab[index] : -5);
        Sum += (lb_true ^ lb_true ? ab[index] : local_int);
        Sum += (lb_true ^ lb_true ? ab[index] : static_field_int);
        Sum += (lb_true ^ lb_true ? ab[index] : t1_i.mfi);
        Sum += (lb_true ^ lb_true ? ab[index] : simple_func_int());
        Sum += (lb_true ^ lb_true ? ab[index] : ab[index]);
        Sum += (lb_true ^ lb_true ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_86()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ lb_true ? ab[index - 1] : 3);
        Sum += (lb_true ^ lb_true ? ab[index - 1] : -5);
        Sum += (lb_true ^ lb_true ? ab[index - 1] : local_int);
        Sum += (lb_true ^ lb_true ? ab[index - 1] : static_field_int);
        Sum += (lb_true ^ lb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (lb_true ^ lb_true ? ab[index - 1] : simple_func_int());
        Sum += (lb_true ^ lb_true ? ab[index - 1] : ab[index]);
        Sum += (lb_true ^ lb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (lb_true ^ lb_false ? 3 : 3);
        Sum += (lb_true ^ lb_false ? 3 : -5);
        Sum += (lb_true ^ lb_false ? 3 : local_int);
        Sum += (lb_true ^ lb_false ? 3 : static_field_int);
        Sum += (lb_true ^ lb_false ? 3 : t1_i.mfi);
        Sum += (lb_true ^ lb_false ? 3 : simple_func_int());
        Sum += (lb_true ^ lb_false ? 3 : ab[index]);
        Sum += (lb_true ^ lb_false ? 3 : ab[index - 1]);
        Sum += (lb_true ^ lb_false ? -5 : 3);
        Sum += (lb_true ^ lb_false ? -5 : -5);
        Sum += (lb_true ^ lb_false ? -5 : local_int);
        Sum += (lb_true ^ lb_false ? -5 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_87()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ lb_false ? -5 : t1_i.mfi);
        Sum += (lb_true ^ lb_false ? -5 : simple_func_int());
        Sum += (lb_true ^ lb_false ? -5 : ab[index]);
        Sum += (lb_true ^ lb_false ? -5 : ab[index - 1]);
        Sum += (lb_true ^ lb_false ? local_int : 3);
        Sum += (lb_true ^ lb_false ? local_int : -5);
        Sum += (lb_true ^ lb_false ? local_int : local_int);
        Sum += (lb_true ^ lb_false ? local_int : static_field_int);
        Sum += (lb_true ^ lb_false ? local_int : t1_i.mfi);
        Sum += (lb_true ^ lb_false ? local_int : simple_func_int());
        Sum += (lb_true ^ lb_false ? local_int : ab[index]);
        Sum += (lb_true ^ lb_false ? local_int : ab[index - 1]);
        Sum += (lb_true ^ lb_false ? static_field_int : 3);
        Sum += (lb_true ^ lb_false ? static_field_int : -5);
        Sum += (lb_true ^ lb_false ? static_field_int : local_int);
        Sum += (lb_true ^ lb_false ? static_field_int : static_field_int);
        Sum += (lb_true ^ lb_false ? static_field_int : t1_i.mfi);
        Sum += (lb_true ^ lb_false ? static_field_int : simple_func_int());
        Sum += (lb_true ^ lb_false ? static_field_int : ab[index]);
        Sum += (lb_true ^ lb_false ? static_field_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_88()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ lb_false ? t1_i.mfi : 3);
        Sum += (lb_true ^ lb_false ? t1_i.mfi : -5);
        Sum += (lb_true ^ lb_false ? t1_i.mfi : local_int);
        Sum += (lb_true ^ lb_false ? t1_i.mfi : static_field_int);
        Sum += (lb_true ^ lb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (lb_true ^ lb_false ? t1_i.mfi : simple_func_int());
        Sum += (lb_true ^ lb_false ? t1_i.mfi : ab[index]);
        Sum += (lb_true ^ lb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (lb_true ^ lb_false ? simple_func_int() : 3);
        Sum += (lb_true ^ lb_false ? simple_func_int() : -5);
        Sum += (lb_true ^ lb_false ? simple_func_int() : local_int);
        Sum += (lb_true ^ lb_false ? simple_func_int() : static_field_int);
        Sum += (lb_true ^ lb_false ? simple_func_int() : t1_i.mfi);
        Sum += (lb_true ^ lb_false ? simple_func_int() : simple_func_int());
        Sum += (lb_true ^ lb_false ? simple_func_int() : ab[index]);
        Sum += (lb_true ^ lb_false ? simple_func_int() : ab[index - 1]);
        Sum += (lb_true ^ lb_false ? ab[index] : 3);
        Sum += (lb_true ^ lb_false ? ab[index] : -5);
        Sum += (lb_true ^ lb_false ? ab[index] : local_int);
        Sum += (lb_true ^ lb_false ? ab[index] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_89()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ lb_false ? ab[index] : t1_i.mfi);
        Sum += (lb_true ^ lb_false ? ab[index] : simple_func_int());
        Sum += (lb_true ^ lb_false ? ab[index] : ab[index]);
        Sum += (lb_true ^ lb_false ? ab[index] : ab[index - 1]);
        Sum += (lb_true ^ lb_false ? ab[index - 1] : 3);
        Sum += (lb_true ^ lb_false ? ab[index - 1] : -5);
        Sum += (lb_true ^ lb_false ? ab[index - 1] : local_int);
        Sum += (lb_true ^ lb_false ? ab[index - 1] : static_field_int);
        Sum += (lb_true ^ lb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (lb_true ^ lb_false ? ab[index - 1] : simple_func_int());
        Sum += (lb_true ^ lb_false ? ab[index - 1] : ab[index]);
        Sum += (lb_true ^ lb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (lb_true ^ sfb_true ? 3 : 3);
        Sum += (lb_true ^ sfb_true ? 3 : -5);
        Sum += (lb_true ^ sfb_true ? 3 : local_int);
        Sum += (lb_true ^ sfb_true ? 3 : static_field_int);
        Sum += (lb_true ^ sfb_true ? 3 : t1_i.mfi);
        Sum += (lb_true ^ sfb_true ? 3 : simple_func_int());
        Sum += (lb_true ^ sfb_true ? 3 : ab[index]);
        Sum += (lb_true ^ sfb_true ? 3 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_90()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ sfb_true ? -5 : 3);
        Sum += (lb_true ^ sfb_true ? -5 : -5);
        Sum += (lb_true ^ sfb_true ? -5 : local_int);
        Sum += (lb_true ^ sfb_true ? -5 : static_field_int);
        Sum += (lb_true ^ sfb_true ? -5 : t1_i.mfi);
        Sum += (lb_true ^ sfb_true ? -5 : simple_func_int());
        Sum += (lb_true ^ sfb_true ? -5 : ab[index]);
        Sum += (lb_true ^ sfb_true ? -5 : ab[index - 1]);
        Sum += (lb_true ^ sfb_true ? local_int : 3);
        Sum += (lb_true ^ sfb_true ? local_int : -5);
        Sum += (lb_true ^ sfb_true ? local_int : local_int);
        Sum += (lb_true ^ sfb_true ? local_int : static_field_int);
        Sum += (lb_true ^ sfb_true ? local_int : t1_i.mfi);
        Sum += (lb_true ^ sfb_true ? local_int : simple_func_int());
        Sum += (lb_true ^ sfb_true ? local_int : ab[index]);
        Sum += (lb_true ^ sfb_true ? local_int : ab[index - 1]);
        Sum += (lb_true ^ sfb_true ? static_field_int : 3);
        Sum += (lb_true ^ sfb_true ? static_field_int : -5);
        Sum += (lb_true ^ sfb_true ? static_field_int : local_int);
        Sum += (lb_true ^ sfb_true ? static_field_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_91()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ sfb_true ? static_field_int : t1_i.mfi);
        Sum += (lb_true ^ sfb_true ? static_field_int : simple_func_int());
        Sum += (lb_true ^ sfb_true ? static_field_int : ab[index]);
        Sum += (lb_true ^ sfb_true ? static_field_int : ab[index - 1]);
        Sum += (lb_true ^ sfb_true ? t1_i.mfi : 3);
        Sum += (lb_true ^ sfb_true ? t1_i.mfi : -5);
        Sum += (lb_true ^ sfb_true ? t1_i.mfi : local_int);
        Sum += (lb_true ^ sfb_true ? t1_i.mfi : static_field_int);
        Sum += (lb_true ^ sfb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (lb_true ^ sfb_true ? t1_i.mfi : simple_func_int());
        Sum += (lb_true ^ sfb_true ? t1_i.mfi : ab[index]);
        Sum += (lb_true ^ sfb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (lb_true ^ sfb_true ? simple_func_int() : 3);
        Sum += (lb_true ^ sfb_true ? simple_func_int() : -5);
        Sum += (lb_true ^ sfb_true ? simple_func_int() : local_int);
        Sum += (lb_true ^ sfb_true ? simple_func_int() : static_field_int);
        Sum += (lb_true ^ sfb_true ? simple_func_int() : t1_i.mfi);
        Sum += (lb_true ^ sfb_true ? simple_func_int() : simple_func_int());
        Sum += (lb_true ^ sfb_true ? simple_func_int() : ab[index]);
        Sum += (lb_true ^ sfb_true ? simple_func_int() : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_92()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ sfb_true ? ab[index] : 3);
        Sum += (lb_true ^ sfb_true ? ab[index] : -5);
        Sum += (lb_true ^ sfb_true ? ab[index] : local_int);
        Sum += (lb_true ^ sfb_true ? ab[index] : static_field_int);
        Sum += (lb_true ^ sfb_true ? ab[index] : t1_i.mfi);
        Sum += (lb_true ^ sfb_true ? ab[index] : simple_func_int());
        Sum += (lb_true ^ sfb_true ? ab[index] : ab[index]);
        Sum += (lb_true ^ sfb_true ? ab[index] : ab[index - 1]);
        Sum += (lb_true ^ sfb_true ? ab[index - 1] : 3);
        Sum += (lb_true ^ sfb_true ? ab[index - 1] : -5);
        Sum += (lb_true ^ sfb_true ? ab[index - 1] : local_int);
        Sum += (lb_true ^ sfb_true ? ab[index - 1] : static_field_int);
        Sum += (lb_true ^ sfb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (lb_true ^ sfb_true ? ab[index - 1] : simple_func_int());
        Sum += (lb_true ^ sfb_true ? ab[index - 1] : ab[index]);
        Sum += (lb_true ^ sfb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (lb_true ^ sfb_false ? 3 : 3);
        Sum += (lb_true ^ sfb_false ? 3 : -5);
        Sum += (lb_true ^ sfb_false ? 3 : local_int);
        Sum += (lb_true ^ sfb_false ? 3 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_93()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ sfb_false ? 3 : t1_i.mfi);
        Sum += (lb_true ^ sfb_false ? 3 : simple_func_int());
        Sum += (lb_true ^ sfb_false ? 3 : ab[index]);
        Sum += (lb_true ^ sfb_false ? 3 : ab[index - 1]);
        Sum += (lb_true ^ sfb_false ? -5 : 3);
        Sum += (lb_true ^ sfb_false ? -5 : -5);
        Sum += (lb_true ^ sfb_false ? -5 : local_int);
        Sum += (lb_true ^ sfb_false ? -5 : static_field_int);
        Sum += (lb_true ^ sfb_false ? -5 : t1_i.mfi);
        Sum += (lb_true ^ sfb_false ? -5 : simple_func_int());
        Sum += (lb_true ^ sfb_false ? -5 : ab[index]);
        Sum += (lb_true ^ sfb_false ? -5 : ab[index - 1]);
        Sum += (lb_true ^ sfb_false ? local_int : 3);
        Sum += (lb_true ^ sfb_false ? local_int : -5);
        Sum += (lb_true ^ sfb_false ? local_int : local_int);
        Sum += (lb_true ^ sfb_false ? local_int : static_field_int);
        Sum += (lb_true ^ sfb_false ? local_int : t1_i.mfi);
        Sum += (lb_true ^ sfb_false ? local_int : simple_func_int());
        Sum += (lb_true ^ sfb_false ? local_int : ab[index]);
        Sum += (lb_true ^ sfb_false ? local_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_94()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ sfb_false ? static_field_int : 3);
        Sum += (lb_true ^ sfb_false ? static_field_int : -5);
        Sum += (lb_true ^ sfb_false ? static_field_int : local_int);
        Sum += (lb_true ^ sfb_false ? static_field_int : static_field_int);
        Sum += (lb_true ^ sfb_false ? static_field_int : t1_i.mfi);
        Sum += (lb_true ^ sfb_false ? static_field_int : simple_func_int());
        Sum += (lb_true ^ sfb_false ? static_field_int : ab[index]);
        Sum += (lb_true ^ sfb_false ? static_field_int : ab[index - 1]);
        Sum += (lb_true ^ sfb_false ? t1_i.mfi : 3);
        Sum += (lb_true ^ sfb_false ? t1_i.mfi : -5);
        Sum += (lb_true ^ sfb_false ? t1_i.mfi : local_int);
        Sum += (lb_true ^ sfb_false ? t1_i.mfi : static_field_int);
        Sum += (lb_true ^ sfb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (lb_true ^ sfb_false ? t1_i.mfi : simple_func_int());
        Sum += (lb_true ^ sfb_false ? t1_i.mfi : ab[index]);
        Sum += (lb_true ^ sfb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (lb_true ^ sfb_false ? simple_func_int() : 3);
        Sum += (lb_true ^ sfb_false ? simple_func_int() : -5);
        Sum += (lb_true ^ sfb_false ? simple_func_int() : local_int);
        Sum += (lb_true ^ sfb_false ? simple_func_int() : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_95()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ sfb_false ? simple_func_int() : t1_i.mfi);
        Sum += (lb_true ^ sfb_false ? simple_func_int() : simple_func_int());
        Sum += (lb_true ^ sfb_false ? simple_func_int() : ab[index]);
        Sum += (lb_true ^ sfb_false ? simple_func_int() : ab[index - 1]);
        Sum += (lb_true ^ sfb_false ? ab[index] : 3);
        Sum += (lb_true ^ sfb_false ? ab[index] : -5);
        Sum += (lb_true ^ sfb_false ? ab[index] : local_int);
        Sum += (lb_true ^ sfb_false ? ab[index] : static_field_int);
        Sum += (lb_true ^ sfb_false ? ab[index] : t1_i.mfi);
        Sum += (lb_true ^ sfb_false ? ab[index] : simple_func_int());
        Sum += (lb_true ^ sfb_false ? ab[index] : ab[index]);
        Sum += (lb_true ^ sfb_false ? ab[index] : ab[index - 1]);
        Sum += (lb_true ^ sfb_false ? ab[index - 1] : 3);
        Sum += (lb_true ^ sfb_false ? ab[index - 1] : -5);
        Sum += (lb_true ^ sfb_false ? ab[index - 1] : local_int);
        Sum += (lb_true ^ sfb_false ? ab[index - 1] : static_field_int);
        Sum += (lb_true ^ sfb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (lb_true ^ sfb_false ? ab[index - 1] : simple_func_int());
        Sum += (lb_true ^ sfb_false ? ab[index - 1] : ab[index]);
        Sum += (lb_true ^ sfb_false ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_96()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ t1_i.mfb_true ? 3 : 3);
        Sum += (lb_true ^ t1_i.mfb_true ? 3 : -5);
        Sum += (lb_true ^ t1_i.mfb_true ? 3 : local_int);
        Sum += (lb_true ^ t1_i.mfb_true ? 3 : static_field_int);
        Sum += (lb_true ^ t1_i.mfb_true ? 3 : t1_i.mfi);
        Sum += (lb_true ^ t1_i.mfb_true ? 3 : simple_func_int());
        Sum += (lb_true ^ t1_i.mfb_true ? 3 : ab[index]);
        Sum += (lb_true ^ t1_i.mfb_true ? 3 : ab[index - 1]);
        Sum += (lb_true ^ t1_i.mfb_true ? -5 : 3);
        Sum += (lb_true ^ t1_i.mfb_true ? -5 : -5);
        Sum += (lb_true ^ t1_i.mfb_true ? -5 : local_int);
        Sum += (lb_true ^ t1_i.mfb_true ? -5 : static_field_int);
        Sum += (lb_true ^ t1_i.mfb_true ? -5 : t1_i.mfi);
        Sum += (lb_true ^ t1_i.mfb_true ? -5 : simple_func_int());
        Sum += (lb_true ^ t1_i.mfb_true ? -5 : ab[index]);
        Sum += (lb_true ^ t1_i.mfb_true ? -5 : ab[index - 1]);
        Sum += (lb_true ^ t1_i.mfb_true ? local_int : 3);
        Sum += (lb_true ^ t1_i.mfb_true ? local_int : -5);
        Sum += (lb_true ^ t1_i.mfb_true ? local_int : local_int);
        Sum += (lb_true ^ t1_i.mfb_true ? local_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_97()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ t1_i.mfb_true ? local_int : t1_i.mfi);
        Sum += (lb_true ^ t1_i.mfb_true ? local_int : simple_func_int());
        Sum += (lb_true ^ t1_i.mfb_true ? local_int : ab[index]);
        Sum += (lb_true ^ t1_i.mfb_true ? local_int : ab[index - 1]);
        Sum += (lb_true ^ t1_i.mfb_true ? static_field_int : 3);
        Sum += (lb_true ^ t1_i.mfb_true ? static_field_int : -5);
        Sum += (lb_true ^ t1_i.mfb_true ? static_field_int : local_int);
        Sum += (lb_true ^ t1_i.mfb_true ? static_field_int : static_field_int);
        Sum += (lb_true ^ t1_i.mfb_true ? static_field_int : t1_i.mfi);
        Sum += (lb_true ^ t1_i.mfb_true ? static_field_int : simple_func_int());
        Sum += (lb_true ^ t1_i.mfb_true ? static_field_int : ab[index]);
        Sum += (lb_true ^ t1_i.mfb_true ? static_field_int : ab[index - 1]);
        Sum += (lb_true ^ t1_i.mfb_true ? t1_i.mfi : 3);
        Sum += (lb_true ^ t1_i.mfb_true ? t1_i.mfi : -5);
        Sum += (lb_true ^ t1_i.mfb_true ? t1_i.mfi : local_int);
        Sum += (lb_true ^ t1_i.mfb_true ? t1_i.mfi : static_field_int);
        Sum += (lb_true ^ t1_i.mfb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (lb_true ^ t1_i.mfb_true ? t1_i.mfi : simple_func_int());
        Sum += (lb_true ^ t1_i.mfb_true ? t1_i.mfi : ab[index]);
        Sum += (lb_true ^ t1_i.mfb_true ? t1_i.mfi : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_98()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ t1_i.mfb_true ? simple_func_int() : 3);
        Sum += (lb_true ^ t1_i.mfb_true ? simple_func_int() : -5);
        Sum += (lb_true ^ t1_i.mfb_true ? simple_func_int() : local_int);
        Sum += (lb_true ^ t1_i.mfb_true ? simple_func_int() : static_field_int);
        Sum += (lb_true ^ t1_i.mfb_true ? simple_func_int() : t1_i.mfi);
        Sum += (lb_true ^ t1_i.mfb_true ? simple_func_int() : simple_func_int());
        Sum += (lb_true ^ t1_i.mfb_true ? simple_func_int() : ab[index]);
        Sum += (lb_true ^ t1_i.mfb_true ? simple_func_int() : ab[index - 1]);
        Sum += (lb_true ^ t1_i.mfb_true ? ab[index] : 3);
        Sum += (lb_true ^ t1_i.mfb_true ? ab[index] : -5);
        Sum += (lb_true ^ t1_i.mfb_true ? ab[index] : local_int);
        Sum += (lb_true ^ t1_i.mfb_true ? ab[index] : static_field_int);
        Sum += (lb_true ^ t1_i.mfb_true ? ab[index] : t1_i.mfi);
        Sum += (lb_true ^ t1_i.mfb_true ? ab[index] : simple_func_int());
        Sum += (lb_true ^ t1_i.mfb_true ? ab[index] : ab[index]);
        Sum += (lb_true ^ t1_i.mfb_true ? ab[index] : ab[index - 1]);
        Sum += (lb_true ^ t1_i.mfb_true ? ab[index - 1] : 3);
        Sum += (lb_true ^ t1_i.mfb_true ? ab[index - 1] : -5);
        Sum += (lb_true ^ t1_i.mfb_true ? ab[index - 1] : local_int);
        Sum += (lb_true ^ t1_i.mfb_true ? ab[index - 1] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_99()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ t1_i.mfb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (lb_true ^ t1_i.mfb_true ? ab[index - 1] : simple_func_int());
        Sum += (lb_true ^ t1_i.mfb_true ? ab[index - 1] : ab[index]);
        Sum += (lb_true ^ t1_i.mfb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (lb_true ^ t1_i.mfb_false ? 3 : 3);
        Sum += (lb_true ^ t1_i.mfb_false ? 3 : -5);
        Sum += (lb_true ^ t1_i.mfb_false ? 3 : local_int);
        Sum += (lb_true ^ t1_i.mfb_false ? 3 : static_field_int);
        Sum += (lb_true ^ t1_i.mfb_false ? 3 : t1_i.mfi);
        Sum += (lb_true ^ t1_i.mfb_false ? 3 : simple_func_int());
        Sum += (lb_true ^ t1_i.mfb_false ? 3 : ab[index]);
        Sum += (lb_true ^ t1_i.mfb_false ? 3 : ab[index - 1]);
        Sum += (lb_true ^ t1_i.mfb_false ? -5 : 3);
        Sum += (lb_true ^ t1_i.mfb_false ? -5 : -5);
        Sum += (lb_true ^ t1_i.mfb_false ? -5 : local_int);
        Sum += (lb_true ^ t1_i.mfb_false ? -5 : static_field_int);
        Sum += (lb_true ^ t1_i.mfb_false ? -5 : t1_i.mfi);
        Sum += (lb_true ^ t1_i.mfb_false ? -5 : simple_func_int());
        Sum += (lb_true ^ t1_i.mfb_false ? -5 : ab[index]);
        Sum += (lb_true ^ t1_i.mfb_false ? -5 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_100()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ t1_i.mfb_false ? local_int : 3);
        Sum += (lb_true ^ t1_i.mfb_false ? local_int : -5);
        Sum += (lb_true ^ t1_i.mfb_false ? local_int : local_int);
        Sum += (lb_true ^ t1_i.mfb_false ? local_int : static_field_int);
        Sum += (lb_true ^ t1_i.mfb_false ? local_int : t1_i.mfi);
        Sum += (lb_true ^ t1_i.mfb_false ? local_int : simple_func_int());
        Sum += (lb_true ^ t1_i.mfb_false ? local_int : ab[index]);
        Sum += (lb_true ^ t1_i.mfb_false ? local_int : ab[index - 1]);
        Sum += (lb_true ^ t1_i.mfb_false ? static_field_int : 3);
        Sum += (lb_true ^ t1_i.mfb_false ? static_field_int : -5);
        Sum += (lb_true ^ t1_i.mfb_false ? static_field_int : local_int);
        Sum += (lb_true ^ t1_i.mfb_false ? static_field_int : static_field_int);
        Sum += (lb_true ^ t1_i.mfb_false ? static_field_int : t1_i.mfi);
        Sum += (lb_true ^ t1_i.mfb_false ? static_field_int : simple_func_int());
        Sum += (lb_true ^ t1_i.mfb_false ? static_field_int : ab[index]);
        Sum += (lb_true ^ t1_i.mfb_false ? static_field_int : ab[index - 1]);
        Sum += (lb_true ^ t1_i.mfb_false ? t1_i.mfi : 3);
        Sum += (lb_true ^ t1_i.mfb_false ? t1_i.mfi : -5);
        Sum += (lb_true ^ t1_i.mfb_false ? t1_i.mfi : local_int);
        Sum += (lb_true ^ t1_i.mfb_false ? t1_i.mfi : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_101()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ t1_i.mfb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (lb_true ^ t1_i.mfb_false ? t1_i.mfi : simple_func_int());
        Sum += (lb_true ^ t1_i.mfb_false ? t1_i.mfi : ab[index]);
        Sum += (lb_true ^ t1_i.mfb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (lb_true ^ t1_i.mfb_false ? simple_func_int() : 3);
        Sum += (lb_true ^ t1_i.mfb_false ? simple_func_int() : -5);
        Sum += (lb_true ^ t1_i.mfb_false ? simple_func_int() : local_int);
        Sum += (lb_true ^ t1_i.mfb_false ? simple_func_int() : static_field_int);
        Sum += (lb_true ^ t1_i.mfb_false ? simple_func_int() : t1_i.mfi);
        Sum += (lb_true ^ t1_i.mfb_false ? simple_func_int() : simple_func_int());
        Sum += (lb_true ^ t1_i.mfb_false ? simple_func_int() : ab[index]);
        Sum += (lb_true ^ t1_i.mfb_false ? simple_func_int() : ab[index - 1]);
        Sum += (lb_true ^ t1_i.mfb_false ? ab[index] : 3);
        Sum += (lb_true ^ t1_i.mfb_false ? ab[index] : -5);
        Sum += (lb_true ^ t1_i.mfb_false ? ab[index] : local_int);
        Sum += (lb_true ^ t1_i.mfb_false ? ab[index] : static_field_int);
        Sum += (lb_true ^ t1_i.mfb_false ? ab[index] : t1_i.mfi);
        Sum += (lb_true ^ t1_i.mfb_false ? ab[index] : simple_func_int());
        Sum += (lb_true ^ t1_i.mfb_false ? ab[index] : ab[index]);
        Sum += (lb_true ^ t1_i.mfb_false ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_102()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ t1_i.mfb_false ? ab[index - 1] : 3);
        Sum += (lb_true ^ t1_i.mfb_false ? ab[index - 1] : -5);
        Sum += (lb_true ^ t1_i.mfb_false ? ab[index - 1] : local_int);
        Sum += (lb_true ^ t1_i.mfb_false ? ab[index - 1] : static_field_int);
        Sum += (lb_true ^ t1_i.mfb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (lb_true ^ t1_i.mfb_false ? ab[index - 1] : simple_func_int());
        Sum += (lb_true ^ t1_i.mfb_false ? ab[index - 1] : ab[index]);
        Sum += (lb_true ^ t1_i.mfb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (lb_true ^ func_sb_true() ? 3 : 3);
        Sum += (lb_true ^ func_sb_true() ? 3 : -5);
        Sum += (lb_true ^ func_sb_true() ? 3 : local_int);
        Sum += (lb_true ^ func_sb_true() ? 3 : static_field_int);
        Sum += (lb_true ^ func_sb_true() ? 3 : t1_i.mfi);
        Sum += (lb_true ^ func_sb_true() ? 3 : simple_func_int());
        Sum += (lb_true ^ func_sb_true() ? 3 : ab[index]);
        Sum += (lb_true ^ func_sb_true() ? 3 : ab[index - 1]);
        Sum += (lb_true ^ func_sb_true() ? -5 : 3);
        Sum += (lb_true ^ func_sb_true() ? -5 : -5);
        Sum += (lb_true ^ func_sb_true() ? -5 : local_int);
        Sum += (lb_true ^ func_sb_true() ? -5 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_103()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ func_sb_true() ? -5 : t1_i.mfi);
        Sum += (lb_true ^ func_sb_true() ? -5 : simple_func_int());
        Sum += (lb_true ^ func_sb_true() ? -5 : ab[index]);
        Sum += (lb_true ^ func_sb_true() ? -5 : ab[index - 1]);
        Sum += (lb_true ^ func_sb_true() ? local_int : 3);
        Sum += (lb_true ^ func_sb_true() ? local_int : -5);
        Sum += (lb_true ^ func_sb_true() ? local_int : local_int);
        Sum += (lb_true ^ func_sb_true() ? local_int : static_field_int);
        Sum += (lb_true ^ func_sb_true() ? local_int : t1_i.mfi);
        Sum += (lb_true ^ func_sb_true() ? local_int : simple_func_int());
        Sum += (lb_true ^ func_sb_true() ? local_int : ab[index]);
        Sum += (lb_true ^ func_sb_true() ? local_int : ab[index - 1]);
        Sum += (lb_true ^ func_sb_true() ? static_field_int : 3);
        Sum += (lb_true ^ func_sb_true() ? static_field_int : -5);
        Sum += (lb_true ^ func_sb_true() ? static_field_int : local_int);
        Sum += (lb_true ^ func_sb_true() ? static_field_int : static_field_int);
        Sum += (lb_true ^ func_sb_true() ? static_field_int : t1_i.mfi);
        Sum += (lb_true ^ func_sb_true() ? static_field_int : simple_func_int());
        Sum += (lb_true ^ func_sb_true() ? static_field_int : ab[index]);
        Sum += (lb_true ^ func_sb_true() ? static_field_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_104()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ func_sb_true() ? t1_i.mfi : 3);
        Sum += (lb_true ^ func_sb_true() ? t1_i.mfi : -5);
        Sum += (lb_true ^ func_sb_true() ? t1_i.mfi : local_int);
        Sum += (lb_true ^ func_sb_true() ? t1_i.mfi : static_field_int);
        Sum += (lb_true ^ func_sb_true() ? t1_i.mfi : t1_i.mfi);
        Sum += (lb_true ^ func_sb_true() ? t1_i.mfi : simple_func_int());
        Sum += (lb_true ^ func_sb_true() ? t1_i.mfi : ab[index]);
        Sum += (lb_true ^ func_sb_true() ? t1_i.mfi : ab[index - 1]);
        Sum += (lb_true ^ func_sb_true() ? simple_func_int() : 3);
        Sum += (lb_true ^ func_sb_true() ? simple_func_int() : -5);
        Sum += (lb_true ^ func_sb_true() ? simple_func_int() : local_int);
        Sum += (lb_true ^ func_sb_true() ? simple_func_int() : static_field_int);
        Sum += (lb_true ^ func_sb_true() ? simple_func_int() : t1_i.mfi);
        Sum += (lb_true ^ func_sb_true() ? simple_func_int() : simple_func_int());
        Sum += (lb_true ^ func_sb_true() ? simple_func_int() : ab[index]);
        Sum += (lb_true ^ func_sb_true() ? simple_func_int() : ab[index - 1]);
        Sum += (lb_true ^ func_sb_true() ? ab[index] : 3);
        Sum += (lb_true ^ func_sb_true() ? ab[index] : -5);
        Sum += (lb_true ^ func_sb_true() ? ab[index] : local_int);
        Sum += (lb_true ^ func_sb_true() ? ab[index] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_105()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ func_sb_true() ? ab[index] : t1_i.mfi);
        Sum += (lb_true ^ func_sb_true() ? ab[index] : simple_func_int());
        Sum += (lb_true ^ func_sb_true() ? ab[index] : ab[index]);
        Sum += (lb_true ^ func_sb_true() ? ab[index] : ab[index - 1]);
        Sum += (lb_true ^ func_sb_true() ? ab[index - 1] : 3);
        Sum += (lb_true ^ func_sb_true() ? ab[index - 1] : -5);
        Sum += (lb_true ^ func_sb_true() ? ab[index - 1] : local_int);
        Sum += (lb_true ^ func_sb_true() ? ab[index - 1] : static_field_int);
        Sum += (lb_true ^ func_sb_true() ? ab[index - 1] : t1_i.mfi);
        Sum += (lb_true ^ func_sb_true() ? ab[index - 1] : simple_func_int());
        Sum += (lb_true ^ func_sb_true() ? ab[index - 1] : ab[index]);
        Sum += (lb_true ^ func_sb_true() ? ab[index - 1] : ab[index - 1]);
        Sum += (lb_true ^ func_sb_false() ? 3 : 3);
        Sum += (lb_true ^ func_sb_false() ? 3 : -5);
        Sum += (lb_true ^ func_sb_false() ? 3 : local_int);
        Sum += (lb_true ^ func_sb_false() ? 3 : static_field_int);
        Sum += (lb_true ^ func_sb_false() ? 3 : t1_i.mfi);
        Sum += (lb_true ^ func_sb_false() ? 3 : simple_func_int());
        Sum += (lb_true ^ func_sb_false() ? 3 : ab[index]);
        Sum += (lb_true ^ func_sb_false() ? 3 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_106()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ func_sb_false() ? -5 : 3);
        Sum += (lb_true ^ func_sb_false() ? -5 : -5);
        Sum += (lb_true ^ func_sb_false() ? -5 : local_int);
        Sum += (lb_true ^ func_sb_false() ? -5 : static_field_int);
        Sum += (lb_true ^ func_sb_false() ? -5 : t1_i.mfi);
        Sum += (lb_true ^ func_sb_false() ? -5 : simple_func_int());
        Sum += (lb_true ^ func_sb_false() ? -5 : ab[index]);
        Sum += (lb_true ^ func_sb_false() ? -5 : ab[index - 1]);
        Sum += (lb_true ^ func_sb_false() ? local_int : 3);
        Sum += (lb_true ^ func_sb_false() ? local_int : -5);
        Sum += (lb_true ^ func_sb_false() ? local_int : local_int);
        Sum += (lb_true ^ func_sb_false() ? local_int : static_field_int);
        Sum += (lb_true ^ func_sb_false() ? local_int : t1_i.mfi);
        Sum += (lb_true ^ func_sb_false() ? local_int : simple_func_int());
        Sum += (lb_true ^ func_sb_false() ? local_int : ab[index]);
        Sum += (lb_true ^ func_sb_false() ? local_int : ab[index - 1]);
        Sum += (lb_true ^ func_sb_false() ? static_field_int : 3);
        Sum += (lb_true ^ func_sb_false() ? static_field_int : -5);
        Sum += (lb_true ^ func_sb_false() ? static_field_int : local_int);
        Sum += (lb_true ^ func_sb_false() ? static_field_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_107()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ func_sb_false() ? static_field_int : t1_i.mfi);
        Sum += (lb_true ^ func_sb_false() ? static_field_int : simple_func_int());
        Sum += (lb_true ^ func_sb_false() ? static_field_int : ab[index]);
        Sum += (lb_true ^ func_sb_false() ? static_field_int : ab[index - 1]);
        Sum += (lb_true ^ func_sb_false() ? t1_i.mfi : 3);
        Sum += (lb_true ^ func_sb_false() ? t1_i.mfi : -5);
        Sum += (lb_true ^ func_sb_false() ? t1_i.mfi : local_int);
        Sum += (lb_true ^ func_sb_false() ? t1_i.mfi : static_field_int);
        Sum += (lb_true ^ func_sb_false() ? t1_i.mfi : t1_i.mfi);
        Sum += (lb_true ^ func_sb_false() ? t1_i.mfi : simple_func_int());
        Sum += (lb_true ^ func_sb_false() ? t1_i.mfi : ab[index]);
        Sum += (lb_true ^ func_sb_false() ? t1_i.mfi : ab[index - 1]);
        Sum += (lb_true ^ func_sb_false() ? simple_func_int() : 3);
        Sum += (lb_true ^ func_sb_false() ? simple_func_int() : -5);
        Sum += (lb_true ^ func_sb_false() ? simple_func_int() : local_int);
        Sum += (lb_true ^ func_sb_false() ? simple_func_int() : static_field_int);
        Sum += (lb_true ^ func_sb_false() ? simple_func_int() : t1_i.mfi);
        Sum += (lb_true ^ func_sb_false() ? simple_func_int() : simple_func_int());
        Sum += (lb_true ^ func_sb_false() ? simple_func_int() : ab[index]);
        Sum += (lb_true ^ func_sb_false() ? simple_func_int() : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_108()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ func_sb_false() ? ab[index] : 3);
        Sum += (lb_true ^ func_sb_false() ? ab[index] : -5);
        Sum += (lb_true ^ func_sb_false() ? ab[index] : local_int);
        Sum += (lb_true ^ func_sb_false() ? ab[index] : static_field_int);
        Sum += (lb_true ^ func_sb_false() ? ab[index] : t1_i.mfi);
        Sum += (lb_true ^ func_sb_false() ? ab[index] : simple_func_int());
        Sum += (lb_true ^ func_sb_false() ? ab[index] : ab[index]);
        Sum += (lb_true ^ func_sb_false() ? ab[index] : ab[index - 1]);
        Sum += (lb_true ^ func_sb_false() ? ab[index - 1] : 3);
        Sum += (lb_true ^ func_sb_false() ? ab[index - 1] : -5);
        Sum += (lb_true ^ func_sb_false() ? ab[index - 1] : local_int);
        Sum += (lb_true ^ func_sb_false() ? ab[index - 1] : static_field_int);
        Sum += (lb_true ^ func_sb_false() ? ab[index - 1] : t1_i.mfi);
        Sum += (lb_true ^ func_sb_false() ? ab[index - 1] : simple_func_int());
        Sum += (lb_true ^ func_sb_false() ? ab[index - 1] : ab[index]);
        Sum += (lb_true ^ func_sb_false() ? ab[index - 1] : ab[index - 1]);
        Sum += (lb_true ^ ab_true[index] ? 3 : 3);
        Sum += (lb_true ^ ab_true[index] ? 3 : -5);
        Sum += (lb_true ^ ab_true[index] ? 3 : local_int);
        Sum += (lb_true ^ ab_true[index] ? 3 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_109()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ ab_true[index] ? 3 : t1_i.mfi);
        Sum += (lb_true ^ ab_true[index] ? 3 : simple_func_int());
        Sum += (lb_true ^ ab_true[index] ? 3 : ab[index]);
        Sum += (lb_true ^ ab_true[index] ? 3 : ab[index - 1]);
        Sum += (lb_true ^ ab_true[index] ? -5 : 3);
        Sum += (lb_true ^ ab_true[index] ? -5 : -5);
        Sum += (lb_true ^ ab_true[index] ? -5 : local_int);
        Sum += (lb_true ^ ab_true[index] ? -5 : static_field_int);
        Sum += (lb_true ^ ab_true[index] ? -5 : t1_i.mfi);
        Sum += (lb_true ^ ab_true[index] ? -5 : simple_func_int());
        Sum += (lb_true ^ ab_true[index] ? -5 : ab[index]);
        Sum += (lb_true ^ ab_true[index] ? -5 : ab[index - 1]);
        Sum += (lb_true ^ ab_true[index] ? local_int : 3);
        Sum += (lb_true ^ ab_true[index] ? local_int : -5);
        Sum += (lb_true ^ ab_true[index] ? local_int : local_int);
        Sum += (lb_true ^ ab_true[index] ? local_int : static_field_int);
        Sum += (lb_true ^ ab_true[index] ? local_int : t1_i.mfi);
        Sum += (lb_true ^ ab_true[index] ? local_int : simple_func_int());
        Sum += (lb_true ^ ab_true[index] ? local_int : ab[index]);
        Sum += (lb_true ^ ab_true[index] ? local_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_110()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ ab_true[index] ? static_field_int : 3);
        Sum += (lb_true ^ ab_true[index] ? static_field_int : -5);
        Sum += (lb_true ^ ab_true[index] ? static_field_int : local_int);
        Sum += (lb_true ^ ab_true[index] ? static_field_int : static_field_int);
        Sum += (lb_true ^ ab_true[index] ? static_field_int : t1_i.mfi);
        Sum += (lb_true ^ ab_true[index] ? static_field_int : simple_func_int());
        Sum += (lb_true ^ ab_true[index] ? static_field_int : ab[index]);
        Sum += (lb_true ^ ab_true[index] ? static_field_int : ab[index - 1]);
        Sum += (lb_true ^ ab_true[index] ? t1_i.mfi : 3);
        Sum += (lb_true ^ ab_true[index] ? t1_i.mfi : -5);
        Sum += (lb_true ^ ab_true[index] ? t1_i.mfi : local_int);
        Sum += (lb_true ^ ab_true[index] ? t1_i.mfi : static_field_int);
        Sum += (lb_true ^ ab_true[index] ? t1_i.mfi : t1_i.mfi);
        Sum += (lb_true ^ ab_true[index] ? t1_i.mfi : simple_func_int());
        Sum += (lb_true ^ ab_true[index] ? t1_i.mfi : ab[index]);
        Sum += (lb_true ^ ab_true[index] ? t1_i.mfi : ab[index - 1]);
        Sum += (lb_true ^ ab_true[index] ? simple_func_int() : 3);
        Sum += (lb_true ^ ab_true[index] ? simple_func_int() : -5);
        Sum += (lb_true ^ ab_true[index] ? simple_func_int() : local_int);
        Sum += (lb_true ^ ab_true[index] ? simple_func_int() : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_111()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ ab_true[index] ? simple_func_int() : t1_i.mfi);
        Sum += (lb_true ^ ab_true[index] ? simple_func_int() : simple_func_int());
        Sum += (lb_true ^ ab_true[index] ? simple_func_int() : ab[index]);
        Sum += (lb_true ^ ab_true[index] ? simple_func_int() : ab[index - 1]);
        Sum += (lb_true ^ ab_true[index] ? ab[index] : 3);
        Sum += (lb_true ^ ab_true[index] ? ab[index] : -5);
        Sum += (lb_true ^ ab_true[index] ? ab[index] : local_int);
        Sum += (lb_true ^ ab_true[index] ? ab[index] : static_field_int);
        Sum += (lb_true ^ ab_true[index] ? ab[index] : t1_i.mfi);
        Sum += (lb_true ^ ab_true[index] ? ab[index] : simple_func_int());
        Sum += (lb_true ^ ab_true[index] ? ab[index] : ab[index]);
        Sum += (lb_true ^ ab_true[index] ? ab[index] : ab[index - 1]);
        Sum += (lb_true ^ ab_true[index] ? ab[index - 1] : 3);
        Sum += (lb_true ^ ab_true[index] ? ab[index - 1] : -5);
        Sum += (lb_true ^ ab_true[index] ? ab[index - 1] : local_int);
        Sum += (lb_true ^ ab_true[index] ? ab[index - 1] : static_field_int);
        Sum += (lb_true ^ ab_true[index] ? ab[index - 1] : t1_i.mfi);
        Sum += (lb_true ^ ab_true[index] ? ab[index - 1] : simple_func_int());
        Sum += (lb_true ^ ab_true[index] ? ab[index - 1] : ab[index]);
        Sum += (lb_true ^ ab_true[index] ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_112()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ ab_false[index] ? 3 : 3);
        Sum += (lb_true ^ ab_false[index] ? 3 : -5);
        Sum += (lb_true ^ ab_false[index] ? 3 : local_int);
        Sum += (lb_true ^ ab_false[index] ? 3 : static_field_int);
        Sum += (lb_true ^ ab_false[index] ? 3 : t1_i.mfi);
        Sum += (lb_true ^ ab_false[index] ? 3 : simple_func_int());
        Sum += (lb_true ^ ab_false[index] ? 3 : ab[index]);
        Sum += (lb_true ^ ab_false[index] ? 3 : ab[index - 1]);
        Sum += (lb_true ^ ab_false[index] ? -5 : 3);
        Sum += (lb_true ^ ab_false[index] ? -5 : -5);
        Sum += (lb_true ^ ab_false[index] ? -5 : local_int);
        Sum += (lb_true ^ ab_false[index] ? -5 : static_field_int);
        Sum += (lb_true ^ ab_false[index] ? -5 : t1_i.mfi);
        Sum += (lb_true ^ ab_false[index] ? -5 : simple_func_int());
        Sum += (lb_true ^ ab_false[index] ? -5 : ab[index]);
        Sum += (lb_true ^ ab_false[index] ? -5 : ab[index - 1]);
        Sum += (lb_true ^ ab_false[index] ? local_int : 3);
        Sum += (lb_true ^ ab_false[index] ? local_int : -5);
        Sum += (lb_true ^ ab_false[index] ? local_int : local_int);
        Sum += (lb_true ^ ab_false[index] ? local_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_113()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ ab_false[index] ? local_int : t1_i.mfi);
        Sum += (lb_true ^ ab_false[index] ? local_int : simple_func_int());
        Sum += (lb_true ^ ab_false[index] ? local_int : ab[index]);
        Sum += (lb_true ^ ab_false[index] ? local_int : ab[index - 1]);
        Sum += (lb_true ^ ab_false[index] ? static_field_int : 3);
        Sum += (lb_true ^ ab_false[index] ? static_field_int : -5);
        Sum += (lb_true ^ ab_false[index] ? static_field_int : local_int);
        Sum += (lb_true ^ ab_false[index] ? static_field_int : static_field_int);
        Sum += (lb_true ^ ab_false[index] ? static_field_int : t1_i.mfi);
        Sum += (lb_true ^ ab_false[index] ? static_field_int : simple_func_int());
        Sum += (lb_true ^ ab_false[index] ? static_field_int : ab[index]);
        Sum += (lb_true ^ ab_false[index] ? static_field_int : ab[index - 1]);
        Sum += (lb_true ^ ab_false[index] ? t1_i.mfi : 3);
        Sum += (lb_true ^ ab_false[index] ? t1_i.mfi : -5);
        Sum += (lb_true ^ ab_false[index] ? t1_i.mfi : local_int);
        Sum += (lb_true ^ ab_false[index] ? t1_i.mfi : static_field_int);
        Sum += (lb_true ^ ab_false[index] ? t1_i.mfi : t1_i.mfi);
        Sum += (lb_true ^ ab_false[index] ? t1_i.mfi : simple_func_int());
        Sum += (lb_true ^ ab_false[index] ? t1_i.mfi : ab[index]);
        Sum += (lb_true ^ ab_false[index] ? t1_i.mfi : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_114()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ ab_false[index] ? simple_func_int() : 3);
        Sum += (lb_true ^ ab_false[index] ? simple_func_int() : -5);
        Sum += (lb_true ^ ab_false[index] ? simple_func_int() : local_int);
        Sum += (lb_true ^ ab_false[index] ? simple_func_int() : static_field_int);
        Sum += (lb_true ^ ab_false[index] ? simple_func_int() : t1_i.mfi);
        Sum += (lb_true ^ ab_false[index] ? simple_func_int() : simple_func_int());
        Sum += (lb_true ^ ab_false[index] ? simple_func_int() : ab[index]);
        Sum += (lb_true ^ ab_false[index] ? simple_func_int() : ab[index - 1]);
        Sum += (lb_true ^ ab_false[index] ? ab[index] : 3);
        Sum += (lb_true ^ ab_false[index] ? ab[index] : -5);
        Sum += (lb_true ^ ab_false[index] ? ab[index] : local_int);
        Sum += (lb_true ^ ab_false[index] ? ab[index] : static_field_int);
        Sum += (lb_true ^ ab_false[index] ? ab[index] : t1_i.mfi);
        Sum += (lb_true ^ ab_false[index] ? ab[index] : simple_func_int());
        Sum += (lb_true ^ ab_false[index] ? ab[index] : ab[index]);
        Sum += (lb_true ^ ab_false[index] ? ab[index] : ab[index - 1]);
        Sum += (lb_true ^ ab_false[index] ? ab[index - 1] : 3);
        Sum += (lb_true ^ ab_false[index] ? ab[index - 1] : -5);
        Sum += (lb_true ^ ab_false[index] ? ab[index - 1] : local_int);
        Sum += (lb_true ^ ab_false[index] ? ab[index - 1] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_115()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ^ ab_false[index] ? ab[index - 1] : t1_i.mfi);
        Sum += (lb_true ^ ab_false[index] ? ab[index - 1] : simple_func_int());
        Sum += (lb_true ^ ab_false[index] ? ab[index - 1] : ab[index]);
        Sum += (lb_true ^ ab_false[index] ? ab[index - 1] : ab[index - 1]);
        Sum += (lb_false ^ true ? 3 : 3);
        Sum += (lb_false ^ true ? 3 : -5);
        Sum += (lb_false ^ true ? 3 : local_int);
        Sum += (lb_false ^ true ? 3 : static_field_int);
        Sum += (lb_false ^ true ? 3 : t1_i.mfi);
        Sum += (lb_false ^ true ? 3 : simple_func_int());
        Sum += (lb_false ^ true ? 3 : ab[index]);
        Sum += (lb_false ^ true ? 3 : ab[index - 1]);
        Sum += (lb_false ^ true ? -5 : 3);
        Sum += (lb_false ^ true ? -5 : -5);
        Sum += (lb_false ^ true ? -5 : local_int);
        Sum += (lb_false ^ true ? -5 : static_field_int);
        Sum += (lb_false ^ true ? -5 : t1_i.mfi);
        Sum += (lb_false ^ true ? -5 : simple_func_int());
        Sum += (lb_false ^ true ? -5 : ab[index]);
        Sum += (lb_false ^ true ? -5 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_116()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ true ? local_int : 3);
        Sum += (lb_false ^ true ? local_int : -5);
        Sum += (lb_false ^ true ? local_int : local_int);
        Sum += (lb_false ^ true ? local_int : static_field_int);
        Sum += (lb_false ^ true ? local_int : t1_i.mfi);
        Sum += (lb_false ^ true ? local_int : simple_func_int());
        Sum += (lb_false ^ true ? local_int : ab[index]);
        Sum += (lb_false ^ true ? local_int : ab[index - 1]);
        Sum += (lb_false ^ true ? static_field_int : 3);
        Sum += (lb_false ^ true ? static_field_int : -5);
        Sum += (lb_false ^ true ? static_field_int : local_int);
        Sum += (lb_false ^ true ? static_field_int : static_field_int);
        Sum += (lb_false ^ true ? static_field_int : t1_i.mfi);
        Sum += (lb_false ^ true ? static_field_int : simple_func_int());
        Sum += (lb_false ^ true ? static_field_int : ab[index]);
        Sum += (lb_false ^ true ? static_field_int : ab[index - 1]);
        Sum += (lb_false ^ true ? t1_i.mfi : 3);
        Sum += (lb_false ^ true ? t1_i.mfi : -5);
        Sum += (lb_false ^ true ? t1_i.mfi : local_int);
        Sum += (lb_false ^ true ? t1_i.mfi : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_117()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ true ? t1_i.mfi : t1_i.mfi);
        Sum += (lb_false ^ true ? t1_i.mfi : simple_func_int());
        Sum += (lb_false ^ true ? t1_i.mfi : ab[index]);
        Sum += (lb_false ^ true ? t1_i.mfi : ab[index - 1]);
        Sum += (lb_false ^ true ? simple_func_int() : 3);
        Sum += (lb_false ^ true ? simple_func_int() : -5);
        Sum += (lb_false ^ true ? simple_func_int() : local_int);
        Sum += (lb_false ^ true ? simple_func_int() : static_field_int);
        Sum += (lb_false ^ true ? simple_func_int() : t1_i.mfi);
        Sum += (lb_false ^ true ? simple_func_int() : simple_func_int());
        Sum += (lb_false ^ true ? simple_func_int() : ab[index]);
        Sum += (lb_false ^ true ? simple_func_int() : ab[index - 1]);
        Sum += (lb_false ^ true ? ab[index] : 3);
        Sum += (lb_false ^ true ? ab[index] : -5);
        Sum += (lb_false ^ true ? ab[index] : local_int);
        Sum += (lb_false ^ true ? ab[index] : static_field_int);
        Sum += (lb_false ^ true ? ab[index] : t1_i.mfi);
        Sum += (lb_false ^ true ? ab[index] : simple_func_int());
        Sum += (lb_false ^ true ? ab[index] : ab[index]);
        Sum += (lb_false ^ true ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_118()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ true ? ab[index - 1] : 3);
        Sum += (lb_false ^ true ? ab[index - 1] : -5);
        Sum += (lb_false ^ true ? ab[index - 1] : local_int);
        Sum += (lb_false ^ true ? ab[index - 1] : static_field_int);
        Sum += (lb_false ^ true ? ab[index - 1] : t1_i.mfi);
        Sum += (lb_false ^ true ? ab[index - 1] : simple_func_int());
        Sum += (lb_false ^ true ? ab[index - 1] : ab[index]);
        Sum += (lb_false ^ true ? ab[index - 1] : ab[index - 1]);
        Sum += (lb_false ^ false ? 3 : 3);
        Sum += (lb_false ^ false ? 3 : -5);
        Sum += (lb_false ^ false ? 3 : local_int);
        Sum += (lb_false ^ false ? 3 : static_field_int);
        Sum += (lb_false ^ false ? 3 : t1_i.mfi);
        Sum += (lb_false ^ false ? 3 : simple_func_int());
        Sum += (lb_false ^ false ? 3 : ab[index]);
        Sum += (lb_false ^ false ? 3 : ab[index - 1]);
        Sum += (lb_false ^ false ? -5 : 3);
        Sum += (lb_false ^ false ? -5 : -5);
        Sum += (lb_false ^ false ? -5 : local_int);
        Sum += (lb_false ^ false ? -5 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_119()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ false ? -5 : t1_i.mfi);
        Sum += (lb_false ^ false ? -5 : simple_func_int());
        Sum += (lb_false ^ false ? -5 : ab[index]);
        Sum += (lb_false ^ false ? -5 : ab[index - 1]);
        Sum += (lb_false ^ false ? local_int : 3);
        Sum += (lb_false ^ false ? local_int : -5);
        Sum += (lb_false ^ false ? local_int : local_int);
        Sum += (lb_false ^ false ? local_int : static_field_int);
        Sum += (lb_false ^ false ? local_int : t1_i.mfi);
        Sum += (lb_false ^ false ? local_int : simple_func_int());
        Sum += (lb_false ^ false ? local_int : ab[index]);
        Sum += (lb_false ^ false ? local_int : ab[index - 1]);
        Sum += (lb_false ^ false ? static_field_int : 3);
        Sum += (lb_false ^ false ? static_field_int : -5);
        Sum += (lb_false ^ false ? static_field_int : local_int);
        Sum += (lb_false ^ false ? static_field_int : static_field_int);
        Sum += (lb_false ^ false ? static_field_int : t1_i.mfi);
        Sum += (lb_false ^ false ? static_field_int : simple_func_int());
        Sum += (lb_false ^ false ? static_field_int : ab[index]);
        Sum += (lb_false ^ false ? static_field_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_120()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ false ? t1_i.mfi : 3);
        Sum += (lb_false ^ false ? t1_i.mfi : -5);
        Sum += (lb_false ^ false ? t1_i.mfi : local_int);
        Sum += (lb_false ^ false ? t1_i.mfi : static_field_int);
        Sum += (lb_false ^ false ? t1_i.mfi : t1_i.mfi);
        Sum += (lb_false ^ false ? t1_i.mfi : simple_func_int());
        Sum += (lb_false ^ false ? t1_i.mfi : ab[index]);
        Sum += (lb_false ^ false ? t1_i.mfi : ab[index - 1]);
        Sum += (lb_false ^ false ? simple_func_int() : 3);
        Sum += (lb_false ^ false ? simple_func_int() : -5);
        Sum += (lb_false ^ false ? simple_func_int() : local_int);
        Sum += (lb_false ^ false ? simple_func_int() : static_field_int);
        Sum += (lb_false ^ false ? simple_func_int() : t1_i.mfi);
        Sum += (lb_false ^ false ? simple_func_int() : simple_func_int());
        Sum += (lb_false ^ false ? simple_func_int() : ab[index]);
        Sum += (lb_false ^ false ? simple_func_int() : ab[index - 1]);
        Sum += (lb_false ^ false ? ab[index] : 3);
        Sum += (lb_false ^ false ? ab[index] : -5);
        Sum += (lb_false ^ false ? ab[index] : local_int);
        Sum += (lb_false ^ false ? ab[index] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_121()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ false ? ab[index] : t1_i.mfi);
        Sum += (lb_false ^ false ? ab[index] : simple_func_int());
        Sum += (lb_false ^ false ? ab[index] : ab[index]);
        Sum += (lb_false ^ false ? ab[index] : ab[index - 1]);
        Sum += (lb_false ^ false ? ab[index - 1] : 3);
        Sum += (lb_false ^ false ? ab[index - 1] : -5);
        Sum += (lb_false ^ false ? ab[index - 1] : local_int);
        Sum += (lb_false ^ false ? ab[index - 1] : static_field_int);
        Sum += (lb_false ^ false ? ab[index - 1] : t1_i.mfi);
        Sum += (lb_false ^ false ? ab[index - 1] : simple_func_int());
        Sum += (lb_false ^ false ? ab[index - 1] : ab[index]);
        Sum += (lb_false ^ false ? ab[index - 1] : ab[index - 1]);
        Sum += (lb_false ^ lb_true ? 3 : 3);
        Sum += (lb_false ^ lb_true ? 3 : -5);
        Sum += (lb_false ^ lb_true ? 3 : local_int);
        Sum += (lb_false ^ lb_true ? 3 : static_field_int);
        Sum += (lb_false ^ lb_true ? 3 : t1_i.mfi);
        Sum += (lb_false ^ lb_true ? 3 : simple_func_int());
        Sum += (lb_false ^ lb_true ? 3 : ab[index]);
        Sum += (lb_false ^ lb_true ? 3 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_122()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ lb_true ? -5 : 3);
        Sum += (lb_false ^ lb_true ? -5 : -5);
        Sum += (lb_false ^ lb_true ? -5 : local_int);
        Sum += (lb_false ^ lb_true ? -5 : static_field_int);
        Sum += (lb_false ^ lb_true ? -5 : t1_i.mfi);
        Sum += (lb_false ^ lb_true ? -5 : simple_func_int());
        Sum += (lb_false ^ lb_true ? -5 : ab[index]);
        Sum += (lb_false ^ lb_true ? -5 : ab[index - 1]);
        Sum += (lb_false ^ lb_true ? local_int : 3);
        Sum += (lb_false ^ lb_true ? local_int : -5);
        Sum += (lb_false ^ lb_true ? local_int : local_int);
        Sum += (lb_false ^ lb_true ? local_int : static_field_int);
        Sum += (lb_false ^ lb_true ? local_int : t1_i.mfi);
        Sum += (lb_false ^ lb_true ? local_int : simple_func_int());
        Sum += (lb_false ^ lb_true ? local_int : ab[index]);
        Sum += (lb_false ^ lb_true ? local_int : ab[index - 1]);
        Sum += (lb_false ^ lb_true ? static_field_int : 3);
        Sum += (lb_false ^ lb_true ? static_field_int : -5);
        Sum += (lb_false ^ lb_true ? static_field_int : local_int);
        Sum += (lb_false ^ lb_true ? static_field_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_123()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ lb_true ? static_field_int : t1_i.mfi);
        Sum += (lb_false ^ lb_true ? static_field_int : simple_func_int());
        Sum += (lb_false ^ lb_true ? static_field_int : ab[index]);
        Sum += (lb_false ^ lb_true ? static_field_int : ab[index - 1]);
        Sum += (lb_false ^ lb_true ? t1_i.mfi : 3);
        Sum += (lb_false ^ lb_true ? t1_i.mfi : -5);
        Sum += (lb_false ^ lb_true ? t1_i.mfi : local_int);
        Sum += (lb_false ^ lb_true ? t1_i.mfi : static_field_int);
        Sum += (lb_false ^ lb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (lb_false ^ lb_true ? t1_i.mfi : simple_func_int());
        Sum += (lb_false ^ lb_true ? t1_i.mfi : ab[index]);
        Sum += (lb_false ^ lb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (lb_false ^ lb_true ? simple_func_int() : 3);
        Sum += (lb_false ^ lb_true ? simple_func_int() : -5);
        Sum += (lb_false ^ lb_true ? simple_func_int() : local_int);
        Sum += (lb_false ^ lb_true ? simple_func_int() : static_field_int);
        Sum += (lb_false ^ lb_true ? simple_func_int() : t1_i.mfi);
        Sum += (lb_false ^ lb_true ? simple_func_int() : simple_func_int());
        Sum += (lb_false ^ lb_true ? simple_func_int() : ab[index]);
        Sum += (lb_false ^ lb_true ? simple_func_int() : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_124()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ lb_true ? ab[index] : 3);
        Sum += (lb_false ^ lb_true ? ab[index] : -5);
        Sum += (lb_false ^ lb_true ? ab[index] : local_int);
        Sum += (lb_false ^ lb_true ? ab[index] : static_field_int);
        Sum += (lb_false ^ lb_true ? ab[index] : t1_i.mfi);
        Sum += (lb_false ^ lb_true ? ab[index] : simple_func_int());
        Sum += (lb_false ^ lb_true ? ab[index] : ab[index]);
        Sum += (lb_false ^ lb_true ? ab[index] : ab[index - 1]);
        Sum += (lb_false ^ lb_true ? ab[index - 1] : 3);
        Sum += (lb_false ^ lb_true ? ab[index - 1] : -5);
        Sum += (lb_false ^ lb_true ? ab[index - 1] : local_int);
        Sum += (lb_false ^ lb_true ? ab[index - 1] : static_field_int);
        Sum += (lb_false ^ lb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (lb_false ^ lb_true ? ab[index - 1] : simple_func_int());
        Sum += (lb_false ^ lb_true ? ab[index - 1] : ab[index]);
        Sum += (lb_false ^ lb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (lb_false ^ lb_false ? 3 : 3);
        Sum += (lb_false ^ lb_false ? 3 : -5);
        Sum += (lb_false ^ lb_false ? 3 : local_int);
        Sum += (lb_false ^ lb_false ? 3 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_125()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ lb_false ? 3 : t1_i.mfi);
        Sum += (lb_false ^ lb_false ? 3 : simple_func_int());
        Sum += (lb_false ^ lb_false ? 3 : ab[index]);
        Sum += (lb_false ^ lb_false ? 3 : ab[index - 1]);
        Sum += (lb_false ^ lb_false ? -5 : 3);
        Sum += (lb_false ^ lb_false ? -5 : -5);
        Sum += (lb_false ^ lb_false ? -5 : local_int);
        Sum += (lb_false ^ lb_false ? -5 : static_field_int);
        Sum += (lb_false ^ lb_false ? -5 : t1_i.mfi);
        Sum += (lb_false ^ lb_false ? -5 : simple_func_int());
        Sum += (lb_false ^ lb_false ? -5 : ab[index]);
        Sum += (lb_false ^ lb_false ? -5 : ab[index - 1]);
        Sum += (lb_false ^ lb_false ? local_int : 3);
        Sum += (lb_false ^ lb_false ? local_int : -5);
        Sum += (lb_false ^ lb_false ? local_int : local_int);
        Sum += (lb_false ^ lb_false ? local_int : static_field_int);
        Sum += (lb_false ^ lb_false ? local_int : t1_i.mfi);
        Sum += (lb_false ^ lb_false ? local_int : simple_func_int());
        Sum += (lb_false ^ lb_false ? local_int : ab[index]);
        Sum += (lb_false ^ lb_false ? local_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_126()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ lb_false ? static_field_int : 3);
        Sum += (lb_false ^ lb_false ? static_field_int : -5);
        Sum += (lb_false ^ lb_false ? static_field_int : local_int);
        Sum += (lb_false ^ lb_false ? static_field_int : static_field_int);
        Sum += (lb_false ^ lb_false ? static_field_int : t1_i.mfi);
        Sum += (lb_false ^ lb_false ? static_field_int : simple_func_int());
        Sum += (lb_false ^ lb_false ? static_field_int : ab[index]);
        Sum += (lb_false ^ lb_false ? static_field_int : ab[index - 1]);
        Sum += (lb_false ^ lb_false ? t1_i.mfi : 3);
        Sum += (lb_false ^ lb_false ? t1_i.mfi : -5);
        Sum += (lb_false ^ lb_false ? t1_i.mfi : local_int);
        Sum += (lb_false ^ lb_false ? t1_i.mfi : static_field_int);
        Sum += (lb_false ^ lb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (lb_false ^ lb_false ? t1_i.mfi : simple_func_int());
        Sum += (lb_false ^ lb_false ? t1_i.mfi : ab[index]);
        Sum += (lb_false ^ lb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (lb_false ^ lb_false ? simple_func_int() : 3);
        Sum += (lb_false ^ lb_false ? simple_func_int() : -5);
        Sum += (lb_false ^ lb_false ? simple_func_int() : local_int);
        Sum += (lb_false ^ lb_false ? simple_func_int() : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_127()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ lb_false ? simple_func_int() : t1_i.mfi);
        Sum += (lb_false ^ lb_false ? simple_func_int() : simple_func_int());
        Sum += (lb_false ^ lb_false ? simple_func_int() : ab[index]);
        Sum += (lb_false ^ lb_false ? simple_func_int() : ab[index - 1]);
        Sum += (lb_false ^ lb_false ? ab[index] : 3);
        Sum += (lb_false ^ lb_false ? ab[index] : -5);
        Sum += (lb_false ^ lb_false ? ab[index] : local_int);
        Sum += (lb_false ^ lb_false ? ab[index] : static_field_int);
        Sum += (lb_false ^ lb_false ? ab[index] : t1_i.mfi);
        Sum += (lb_false ^ lb_false ? ab[index] : simple_func_int());
        Sum += (lb_false ^ lb_false ? ab[index] : ab[index]);
        Sum += (lb_false ^ lb_false ? ab[index] : ab[index - 1]);
        Sum += (lb_false ^ lb_false ? ab[index - 1] : 3);
        Sum += (lb_false ^ lb_false ? ab[index - 1] : -5);
        Sum += (lb_false ^ lb_false ? ab[index - 1] : local_int);
        Sum += (lb_false ^ lb_false ? ab[index - 1] : static_field_int);
        Sum += (lb_false ^ lb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (lb_false ^ lb_false ? ab[index - 1] : simple_func_int());
        Sum += (lb_false ^ lb_false ? ab[index - 1] : ab[index]);
        Sum += (lb_false ^ lb_false ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_128()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ sfb_true ? 3 : 3);
        Sum += (lb_false ^ sfb_true ? 3 : -5);
        Sum += (lb_false ^ sfb_true ? 3 : local_int);
        Sum += (lb_false ^ sfb_true ? 3 : static_field_int);
        Sum += (lb_false ^ sfb_true ? 3 : t1_i.mfi);
        Sum += (lb_false ^ sfb_true ? 3 : simple_func_int());
        Sum += (lb_false ^ sfb_true ? 3 : ab[index]);
        Sum += (lb_false ^ sfb_true ? 3 : ab[index - 1]);
        Sum += (lb_false ^ sfb_true ? -5 : 3);
        Sum += (lb_false ^ sfb_true ? -5 : -5);
        Sum += (lb_false ^ sfb_true ? -5 : local_int);
        Sum += (lb_false ^ sfb_true ? -5 : static_field_int);
        Sum += (lb_false ^ sfb_true ? -5 : t1_i.mfi);
        Sum += (lb_false ^ sfb_true ? -5 : simple_func_int());
        Sum += (lb_false ^ sfb_true ? -5 : ab[index]);
        Sum += (lb_false ^ sfb_true ? -5 : ab[index - 1]);
        Sum += (lb_false ^ sfb_true ? local_int : 3);
        Sum += (lb_false ^ sfb_true ? local_int : -5);
        Sum += (lb_false ^ sfb_true ? local_int : local_int);
        Sum += (lb_false ^ sfb_true ? local_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_129()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ sfb_true ? local_int : t1_i.mfi);
        Sum += (lb_false ^ sfb_true ? local_int : simple_func_int());
        Sum += (lb_false ^ sfb_true ? local_int : ab[index]);
        Sum += (lb_false ^ sfb_true ? local_int : ab[index - 1]);
        Sum += (lb_false ^ sfb_true ? static_field_int : 3);
        Sum += (lb_false ^ sfb_true ? static_field_int : -5);
        Sum += (lb_false ^ sfb_true ? static_field_int : local_int);
        Sum += (lb_false ^ sfb_true ? static_field_int : static_field_int);
        Sum += (lb_false ^ sfb_true ? static_field_int : t1_i.mfi);
        Sum += (lb_false ^ sfb_true ? static_field_int : simple_func_int());
        Sum += (lb_false ^ sfb_true ? static_field_int : ab[index]);
        Sum += (lb_false ^ sfb_true ? static_field_int : ab[index - 1]);
        Sum += (lb_false ^ sfb_true ? t1_i.mfi : 3);
        Sum += (lb_false ^ sfb_true ? t1_i.mfi : -5);
        Sum += (lb_false ^ sfb_true ? t1_i.mfi : local_int);
        Sum += (lb_false ^ sfb_true ? t1_i.mfi : static_field_int);
        Sum += (lb_false ^ sfb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (lb_false ^ sfb_true ? t1_i.mfi : simple_func_int());
        Sum += (lb_false ^ sfb_true ? t1_i.mfi : ab[index]);
        Sum += (lb_false ^ sfb_true ? t1_i.mfi : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_130()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ sfb_true ? simple_func_int() : 3);
        Sum += (lb_false ^ sfb_true ? simple_func_int() : -5);
        Sum += (lb_false ^ sfb_true ? simple_func_int() : local_int);
        Sum += (lb_false ^ sfb_true ? simple_func_int() : static_field_int);
        Sum += (lb_false ^ sfb_true ? simple_func_int() : t1_i.mfi);
        Sum += (lb_false ^ sfb_true ? simple_func_int() : simple_func_int());
        Sum += (lb_false ^ sfb_true ? simple_func_int() : ab[index]);
        Sum += (lb_false ^ sfb_true ? simple_func_int() : ab[index - 1]);
        Sum += (lb_false ^ sfb_true ? ab[index] : 3);
        Sum += (lb_false ^ sfb_true ? ab[index] : -5);
        Sum += (lb_false ^ sfb_true ? ab[index] : local_int);
        Sum += (lb_false ^ sfb_true ? ab[index] : static_field_int);
        Sum += (lb_false ^ sfb_true ? ab[index] : t1_i.mfi);
        Sum += (lb_false ^ sfb_true ? ab[index] : simple_func_int());
        Sum += (lb_false ^ sfb_true ? ab[index] : ab[index]);
        Sum += (lb_false ^ sfb_true ? ab[index] : ab[index - 1]);
        Sum += (lb_false ^ sfb_true ? ab[index - 1] : 3);
        Sum += (lb_false ^ sfb_true ? ab[index - 1] : -5);
        Sum += (lb_false ^ sfb_true ? ab[index - 1] : local_int);
        Sum += (lb_false ^ sfb_true ? ab[index - 1] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_131()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ sfb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (lb_false ^ sfb_true ? ab[index - 1] : simple_func_int());
        Sum += (lb_false ^ sfb_true ? ab[index - 1] : ab[index]);
        Sum += (lb_false ^ sfb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (lb_false ^ sfb_false ? 3 : 3);
        Sum += (lb_false ^ sfb_false ? 3 : -5);
        Sum += (lb_false ^ sfb_false ? 3 : local_int);
        Sum += (lb_false ^ sfb_false ? 3 : static_field_int);
        Sum += (lb_false ^ sfb_false ? 3 : t1_i.mfi);
        Sum += (lb_false ^ sfb_false ? 3 : simple_func_int());
        Sum += (lb_false ^ sfb_false ? 3 : ab[index]);
        Sum += (lb_false ^ sfb_false ? 3 : ab[index - 1]);
        Sum += (lb_false ^ sfb_false ? -5 : 3);
        Sum += (lb_false ^ sfb_false ? -5 : -5);
        Sum += (lb_false ^ sfb_false ? -5 : local_int);
        Sum += (lb_false ^ sfb_false ? -5 : static_field_int);
        Sum += (lb_false ^ sfb_false ? -5 : t1_i.mfi);
        Sum += (lb_false ^ sfb_false ? -5 : simple_func_int());
        Sum += (lb_false ^ sfb_false ? -5 : ab[index]);
        Sum += (lb_false ^ sfb_false ? -5 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_132()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ sfb_false ? local_int : 3);
        Sum += (lb_false ^ sfb_false ? local_int : -5);
        Sum += (lb_false ^ sfb_false ? local_int : local_int);
        Sum += (lb_false ^ sfb_false ? local_int : static_field_int);
        Sum += (lb_false ^ sfb_false ? local_int : t1_i.mfi);
        Sum += (lb_false ^ sfb_false ? local_int : simple_func_int());
        Sum += (lb_false ^ sfb_false ? local_int : ab[index]);
        Sum += (lb_false ^ sfb_false ? local_int : ab[index - 1]);
        Sum += (lb_false ^ sfb_false ? static_field_int : 3);
        Sum += (lb_false ^ sfb_false ? static_field_int : -5);
        Sum += (lb_false ^ sfb_false ? static_field_int : local_int);
        Sum += (lb_false ^ sfb_false ? static_field_int : static_field_int);
        Sum += (lb_false ^ sfb_false ? static_field_int : t1_i.mfi);
        Sum += (lb_false ^ sfb_false ? static_field_int : simple_func_int());
        Sum += (lb_false ^ sfb_false ? static_field_int : ab[index]);
        Sum += (lb_false ^ sfb_false ? static_field_int : ab[index - 1]);
        Sum += (lb_false ^ sfb_false ? t1_i.mfi : 3);
        Sum += (lb_false ^ sfb_false ? t1_i.mfi : -5);
        Sum += (lb_false ^ sfb_false ? t1_i.mfi : local_int);
        Sum += (lb_false ^ sfb_false ? t1_i.mfi : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_133()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ sfb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (lb_false ^ sfb_false ? t1_i.mfi : simple_func_int());
        Sum += (lb_false ^ sfb_false ? t1_i.mfi : ab[index]);
        Sum += (lb_false ^ sfb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (lb_false ^ sfb_false ? simple_func_int() : 3);
        Sum += (lb_false ^ sfb_false ? simple_func_int() : -5);
        Sum += (lb_false ^ sfb_false ? simple_func_int() : local_int);
        Sum += (lb_false ^ sfb_false ? simple_func_int() : static_field_int);
        Sum += (lb_false ^ sfb_false ? simple_func_int() : t1_i.mfi);
        Sum += (lb_false ^ sfb_false ? simple_func_int() : simple_func_int());
        Sum += (lb_false ^ sfb_false ? simple_func_int() : ab[index]);
        Sum += (lb_false ^ sfb_false ? simple_func_int() : ab[index - 1]);
        Sum += (lb_false ^ sfb_false ? ab[index] : 3);
        Sum += (lb_false ^ sfb_false ? ab[index] : -5);
        Sum += (lb_false ^ sfb_false ? ab[index] : local_int);
        Sum += (lb_false ^ sfb_false ? ab[index] : static_field_int);
        Sum += (lb_false ^ sfb_false ? ab[index] : t1_i.mfi);
        Sum += (lb_false ^ sfb_false ? ab[index] : simple_func_int());
        Sum += (lb_false ^ sfb_false ? ab[index] : ab[index]);
        Sum += (lb_false ^ sfb_false ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_134()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ sfb_false ? ab[index - 1] : 3);
        Sum += (lb_false ^ sfb_false ? ab[index - 1] : -5);
        Sum += (lb_false ^ sfb_false ? ab[index - 1] : local_int);
        Sum += (lb_false ^ sfb_false ? ab[index - 1] : static_field_int);
        Sum += (lb_false ^ sfb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (lb_false ^ sfb_false ? ab[index - 1] : simple_func_int());
        Sum += (lb_false ^ sfb_false ? ab[index - 1] : ab[index]);
        Sum += (lb_false ^ sfb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (lb_false ^ t1_i.mfb_true ? 3 : 3);
        Sum += (lb_false ^ t1_i.mfb_true ? 3 : -5);
        Sum += (lb_false ^ t1_i.mfb_true ? 3 : local_int);
        Sum += (lb_false ^ t1_i.mfb_true ? 3 : static_field_int);
        Sum += (lb_false ^ t1_i.mfb_true ? 3 : t1_i.mfi);
        Sum += (lb_false ^ t1_i.mfb_true ? 3 : simple_func_int());
        Sum += (lb_false ^ t1_i.mfb_true ? 3 : ab[index]);
        Sum += (lb_false ^ t1_i.mfb_true ? 3 : ab[index - 1]);
        Sum += (lb_false ^ t1_i.mfb_true ? -5 : 3);
        Sum += (lb_false ^ t1_i.mfb_true ? -5 : -5);
        Sum += (lb_false ^ t1_i.mfb_true ? -5 : local_int);
        Sum += (lb_false ^ t1_i.mfb_true ? -5 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_135()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ t1_i.mfb_true ? -5 : t1_i.mfi);
        Sum += (lb_false ^ t1_i.mfb_true ? -5 : simple_func_int());
        Sum += (lb_false ^ t1_i.mfb_true ? -5 : ab[index]);
        Sum += (lb_false ^ t1_i.mfb_true ? -5 : ab[index - 1]);
        Sum += (lb_false ^ t1_i.mfb_true ? local_int : 3);
        Sum += (lb_false ^ t1_i.mfb_true ? local_int : -5);
        Sum += (lb_false ^ t1_i.mfb_true ? local_int : local_int);
        Sum += (lb_false ^ t1_i.mfb_true ? local_int : static_field_int);
        Sum += (lb_false ^ t1_i.mfb_true ? local_int : t1_i.mfi);
        Sum += (lb_false ^ t1_i.mfb_true ? local_int : simple_func_int());
        Sum += (lb_false ^ t1_i.mfb_true ? local_int : ab[index]);
        Sum += (lb_false ^ t1_i.mfb_true ? local_int : ab[index - 1]);
        Sum += (lb_false ^ t1_i.mfb_true ? static_field_int : 3);
        Sum += (lb_false ^ t1_i.mfb_true ? static_field_int : -5);
        Sum += (lb_false ^ t1_i.mfb_true ? static_field_int : local_int);
        Sum += (lb_false ^ t1_i.mfb_true ? static_field_int : static_field_int);
        Sum += (lb_false ^ t1_i.mfb_true ? static_field_int : t1_i.mfi);
        Sum += (lb_false ^ t1_i.mfb_true ? static_field_int : simple_func_int());
        Sum += (lb_false ^ t1_i.mfb_true ? static_field_int : ab[index]);
        Sum += (lb_false ^ t1_i.mfb_true ? static_field_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_136()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ t1_i.mfb_true ? t1_i.mfi : 3);
        Sum += (lb_false ^ t1_i.mfb_true ? t1_i.mfi : -5);
        Sum += (lb_false ^ t1_i.mfb_true ? t1_i.mfi : local_int);
        Sum += (lb_false ^ t1_i.mfb_true ? t1_i.mfi : static_field_int);
        Sum += (lb_false ^ t1_i.mfb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (lb_false ^ t1_i.mfb_true ? t1_i.mfi : simple_func_int());
        Sum += (lb_false ^ t1_i.mfb_true ? t1_i.mfi : ab[index]);
        Sum += (lb_false ^ t1_i.mfb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (lb_false ^ t1_i.mfb_true ? simple_func_int() : 3);
        Sum += (lb_false ^ t1_i.mfb_true ? simple_func_int() : -5);
        Sum += (lb_false ^ t1_i.mfb_true ? simple_func_int() : local_int);
        Sum += (lb_false ^ t1_i.mfb_true ? simple_func_int() : static_field_int);
        Sum += (lb_false ^ t1_i.mfb_true ? simple_func_int() : t1_i.mfi);
        Sum += (lb_false ^ t1_i.mfb_true ? simple_func_int() : simple_func_int());
        Sum += (lb_false ^ t1_i.mfb_true ? simple_func_int() : ab[index]);
        Sum += (lb_false ^ t1_i.mfb_true ? simple_func_int() : ab[index - 1]);
        Sum += (lb_false ^ t1_i.mfb_true ? ab[index] : 3);
        Sum += (lb_false ^ t1_i.mfb_true ? ab[index] : -5);
        Sum += (lb_false ^ t1_i.mfb_true ? ab[index] : local_int);
        Sum += (lb_false ^ t1_i.mfb_true ? ab[index] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_137()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ t1_i.mfb_true ? ab[index] : t1_i.mfi);
        Sum += (lb_false ^ t1_i.mfb_true ? ab[index] : simple_func_int());
        Sum += (lb_false ^ t1_i.mfb_true ? ab[index] : ab[index]);
        Sum += (lb_false ^ t1_i.mfb_true ? ab[index] : ab[index - 1]);
        Sum += (lb_false ^ t1_i.mfb_true ? ab[index - 1] : 3);
        Sum += (lb_false ^ t1_i.mfb_true ? ab[index - 1] : -5);
        Sum += (lb_false ^ t1_i.mfb_true ? ab[index - 1] : local_int);
        Sum += (lb_false ^ t1_i.mfb_true ? ab[index - 1] : static_field_int);
        Sum += (lb_false ^ t1_i.mfb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (lb_false ^ t1_i.mfb_true ? ab[index - 1] : simple_func_int());
        Sum += (lb_false ^ t1_i.mfb_true ? ab[index - 1] : ab[index]);
        Sum += (lb_false ^ t1_i.mfb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (lb_false ^ t1_i.mfb_false ? 3 : 3);
        Sum += (lb_false ^ t1_i.mfb_false ? 3 : -5);
        Sum += (lb_false ^ t1_i.mfb_false ? 3 : local_int);
        Sum += (lb_false ^ t1_i.mfb_false ? 3 : static_field_int);
        Sum += (lb_false ^ t1_i.mfb_false ? 3 : t1_i.mfi);
        Sum += (lb_false ^ t1_i.mfb_false ? 3 : simple_func_int());
        Sum += (lb_false ^ t1_i.mfb_false ? 3 : ab[index]);
        Sum += (lb_false ^ t1_i.mfb_false ? 3 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_138()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ t1_i.mfb_false ? -5 : 3);
        Sum += (lb_false ^ t1_i.mfb_false ? -5 : -5);
        Sum += (lb_false ^ t1_i.mfb_false ? -5 : local_int);
        Sum += (lb_false ^ t1_i.mfb_false ? -5 : static_field_int);
        Sum += (lb_false ^ t1_i.mfb_false ? -5 : t1_i.mfi);
        Sum += (lb_false ^ t1_i.mfb_false ? -5 : simple_func_int());
        Sum += (lb_false ^ t1_i.mfb_false ? -5 : ab[index]);
        Sum += (lb_false ^ t1_i.mfb_false ? -5 : ab[index - 1]);
        Sum += (lb_false ^ t1_i.mfb_false ? local_int : 3);
        Sum += (lb_false ^ t1_i.mfb_false ? local_int : -5);
        Sum += (lb_false ^ t1_i.mfb_false ? local_int : local_int);
        Sum += (lb_false ^ t1_i.mfb_false ? local_int : static_field_int);
        Sum += (lb_false ^ t1_i.mfb_false ? local_int : t1_i.mfi);
        Sum += (lb_false ^ t1_i.mfb_false ? local_int : simple_func_int());
        Sum += (lb_false ^ t1_i.mfb_false ? local_int : ab[index]);
        Sum += (lb_false ^ t1_i.mfb_false ? local_int : ab[index - 1]);
        Sum += (lb_false ^ t1_i.mfb_false ? static_field_int : 3);
        Sum += (lb_false ^ t1_i.mfb_false ? static_field_int : -5);
        Sum += (lb_false ^ t1_i.mfb_false ? static_field_int : local_int);
        Sum += (lb_false ^ t1_i.mfb_false ? static_field_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_139()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ t1_i.mfb_false ? static_field_int : t1_i.mfi);
        Sum += (lb_false ^ t1_i.mfb_false ? static_field_int : simple_func_int());
        Sum += (lb_false ^ t1_i.mfb_false ? static_field_int : ab[index]);
        Sum += (lb_false ^ t1_i.mfb_false ? static_field_int : ab[index - 1]);
        Sum += (lb_false ^ t1_i.mfb_false ? t1_i.mfi : 3);
        Sum += (lb_false ^ t1_i.mfb_false ? t1_i.mfi : -5);
        Sum += (lb_false ^ t1_i.mfb_false ? t1_i.mfi : local_int);
        Sum += (lb_false ^ t1_i.mfb_false ? t1_i.mfi : static_field_int);
        Sum += (lb_false ^ t1_i.mfb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (lb_false ^ t1_i.mfb_false ? t1_i.mfi : simple_func_int());
        Sum += (lb_false ^ t1_i.mfb_false ? t1_i.mfi : ab[index]);
        Sum += (lb_false ^ t1_i.mfb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (lb_false ^ t1_i.mfb_false ? simple_func_int() : 3);
        Sum += (lb_false ^ t1_i.mfb_false ? simple_func_int() : -5);
        Sum += (lb_false ^ t1_i.mfb_false ? simple_func_int() : local_int);
        Sum += (lb_false ^ t1_i.mfb_false ? simple_func_int() : static_field_int);
        Sum += (lb_false ^ t1_i.mfb_false ? simple_func_int() : t1_i.mfi);
        Sum += (lb_false ^ t1_i.mfb_false ? simple_func_int() : simple_func_int());
        Sum += (lb_false ^ t1_i.mfb_false ? simple_func_int() : ab[index]);
        Sum += (lb_false ^ t1_i.mfb_false ? simple_func_int() : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_140()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ t1_i.mfb_false ? ab[index] : 3);
        Sum += (lb_false ^ t1_i.mfb_false ? ab[index] : -5);
        Sum += (lb_false ^ t1_i.mfb_false ? ab[index] : local_int);
        Sum += (lb_false ^ t1_i.mfb_false ? ab[index] : static_field_int);
        Sum += (lb_false ^ t1_i.mfb_false ? ab[index] : t1_i.mfi);
        Sum += (lb_false ^ t1_i.mfb_false ? ab[index] : simple_func_int());
        Sum += (lb_false ^ t1_i.mfb_false ? ab[index] : ab[index]);
        Sum += (lb_false ^ t1_i.mfb_false ? ab[index] : ab[index - 1]);
        Sum += (lb_false ^ t1_i.mfb_false ? ab[index - 1] : 3);
        Sum += (lb_false ^ t1_i.mfb_false ? ab[index - 1] : -5);
        Sum += (lb_false ^ t1_i.mfb_false ? ab[index - 1] : local_int);
        Sum += (lb_false ^ t1_i.mfb_false ? ab[index - 1] : static_field_int);
        Sum += (lb_false ^ t1_i.mfb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (lb_false ^ t1_i.mfb_false ? ab[index - 1] : simple_func_int());
        Sum += (lb_false ^ t1_i.mfb_false ? ab[index - 1] : ab[index]);
        Sum += (lb_false ^ t1_i.mfb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (lb_false ^ func_sb_true() ? 3 : 3);
        Sum += (lb_false ^ func_sb_true() ? 3 : -5);
        Sum += (lb_false ^ func_sb_true() ? 3 : local_int);
        Sum += (lb_false ^ func_sb_true() ? 3 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_141()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ func_sb_true() ? 3 : t1_i.mfi);
        Sum += (lb_false ^ func_sb_true() ? 3 : simple_func_int());
        Sum += (lb_false ^ func_sb_true() ? 3 : ab[index]);
        Sum += (lb_false ^ func_sb_true() ? 3 : ab[index - 1]);
        Sum += (lb_false ^ func_sb_true() ? -5 : 3);
        Sum += (lb_false ^ func_sb_true() ? -5 : -5);
        Sum += (lb_false ^ func_sb_true() ? -5 : local_int);
        Sum += (lb_false ^ func_sb_true() ? -5 : static_field_int);
        Sum += (lb_false ^ func_sb_true() ? -5 : t1_i.mfi);
        Sum += (lb_false ^ func_sb_true() ? -5 : simple_func_int());
        Sum += (lb_false ^ func_sb_true() ? -5 : ab[index]);
        Sum += (lb_false ^ func_sb_true() ? -5 : ab[index - 1]);
        Sum += (lb_false ^ func_sb_true() ? local_int : 3);
        Sum += (lb_false ^ func_sb_true() ? local_int : -5);
        Sum += (lb_false ^ func_sb_true() ? local_int : local_int);
        Sum += (lb_false ^ func_sb_true() ? local_int : static_field_int);
        Sum += (lb_false ^ func_sb_true() ? local_int : t1_i.mfi);
        Sum += (lb_false ^ func_sb_true() ? local_int : simple_func_int());
        Sum += (lb_false ^ func_sb_true() ? local_int : ab[index]);
        Sum += (lb_false ^ func_sb_true() ? local_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_142()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ func_sb_true() ? static_field_int : 3);
        Sum += (lb_false ^ func_sb_true() ? static_field_int : -5);
        Sum += (lb_false ^ func_sb_true() ? static_field_int : local_int);
        Sum += (lb_false ^ func_sb_true() ? static_field_int : static_field_int);
        Sum += (lb_false ^ func_sb_true() ? static_field_int : t1_i.mfi);
        Sum += (lb_false ^ func_sb_true() ? static_field_int : simple_func_int());
        Sum += (lb_false ^ func_sb_true() ? static_field_int : ab[index]);
        Sum += (lb_false ^ func_sb_true() ? static_field_int : ab[index - 1]);
        Sum += (lb_false ^ func_sb_true() ? t1_i.mfi : 3);
        Sum += (lb_false ^ func_sb_true() ? t1_i.mfi : -5);
        Sum += (lb_false ^ func_sb_true() ? t1_i.mfi : local_int);
        Sum += (lb_false ^ func_sb_true() ? t1_i.mfi : static_field_int);
        Sum += (lb_false ^ func_sb_true() ? t1_i.mfi : t1_i.mfi);
        Sum += (lb_false ^ func_sb_true() ? t1_i.mfi : simple_func_int());
        Sum += (lb_false ^ func_sb_true() ? t1_i.mfi : ab[index]);
        Sum += (lb_false ^ func_sb_true() ? t1_i.mfi : ab[index - 1]);
        Sum += (lb_false ^ func_sb_true() ? simple_func_int() : 3);
        Sum += (lb_false ^ func_sb_true() ? simple_func_int() : -5);
        Sum += (lb_false ^ func_sb_true() ? simple_func_int() : local_int);
        Sum += (lb_false ^ func_sb_true() ? simple_func_int() : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_143()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ func_sb_true() ? simple_func_int() : t1_i.mfi);
        Sum += (lb_false ^ func_sb_true() ? simple_func_int() : simple_func_int());
        Sum += (lb_false ^ func_sb_true() ? simple_func_int() : ab[index]);
        Sum += (lb_false ^ func_sb_true() ? simple_func_int() : ab[index - 1]);
        Sum += (lb_false ^ func_sb_true() ? ab[index] : 3);
        Sum += (lb_false ^ func_sb_true() ? ab[index] : -5);
        Sum += (lb_false ^ func_sb_true() ? ab[index] : local_int);
        Sum += (lb_false ^ func_sb_true() ? ab[index] : static_field_int);
        Sum += (lb_false ^ func_sb_true() ? ab[index] : t1_i.mfi);
        Sum += (lb_false ^ func_sb_true() ? ab[index] : simple_func_int());
        Sum += (lb_false ^ func_sb_true() ? ab[index] : ab[index]);
        Sum += (lb_false ^ func_sb_true() ? ab[index] : ab[index - 1]);
        Sum += (lb_false ^ func_sb_true() ? ab[index - 1] : 3);
        Sum += (lb_false ^ func_sb_true() ? ab[index - 1] : -5);
        Sum += (lb_false ^ func_sb_true() ? ab[index - 1] : local_int);
        Sum += (lb_false ^ func_sb_true() ? ab[index - 1] : static_field_int);
        Sum += (lb_false ^ func_sb_true() ? ab[index - 1] : t1_i.mfi);
        Sum += (lb_false ^ func_sb_true() ? ab[index - 1] : simple_func_int());
        Sum += (lb_false ^ func_sb_true() ? ab[index - 1] : ab[index]);
        Sum += (lb_false ^ func_sb_true() ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_144()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ func_sb_false() ? 3 : 3);
        Sum += (lb_false ^ func_sb_false() ? 3 : -5);
        Sum += (lb_false ^ func_sb_false() ? 3 : local_int);
        Sum += (lb_false ^ func_sb_false() ? 3 : static_field_int);
        Sum += (lb_false ^ func_sb_false() ? 3 : t1_i.mfi);
        Sum += (lb_false ^ func_sb_false() ? 3 : simple_func_int());
        Sum += (lb_false ^ func_sb_false() ? 3 : ab[index]);
        Sum += (lb_false ^ func_sb_false() ? 3 : ab[index - 1]);
        Sum += (lb_false ^ func_sb_false() ? -5 : 3);
        Sum += (lb_false ^ func_sb_false() ? -5 : -5);
        Sum += (lb_false ^ func_sb_false() ? -5 : local_int);
        Sum += (lb_false ^ func_sb_false() ? -5 : static_field_int);
        Sum += (lb_false ^ func_sb_false() ? -5 : t1_i.mfi);
        Sum += (lb_false ^ func_sb_false() ? -5 : simple_func_int());
        Sum += (lb_false ^ func_sb_false() ? -5 : ab[index]);
        Sum += (lb_false ^ func_sb_false() ? -5 : ab[index - 1]);
        Sum += (lb_false ^ func_sb_false() ? local_int : 3);
        Sum += (lb_false ^ func_sb_false() ? local_int : -5);
        Sum += (lb_false ^ func_sb_false() ? local_int : local_int);
        Sum += (lb_false ^ func_sb_false() ? local_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_145()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ func_sb_false() ? local_int : t1_i.mfi);
        Sum += (lb_false ^ func_sb_false() ? local_int : simple_func_int());
        Sum += (lb_false ^ func_sb_false() ? local_int : ab[index]);
        Sum += (lb_false ^ func_sb_false() ? local_int : ab[index - 1]);
        Sum += (lb_false ^ func_sb_false() ? static_field_int : 3);
        Sum += (lb_false ^ func_sb_false() ? static_field_int : -5);
        Sum += (lb_false ^ func_sb_false() ? static_field_int : local_int);
        Sum += (lb_false ^ func_sb_false() ? static_field_int : static_field_int);
        Sum += (lb_false ^ func_sb_false() ? static_field_int : t1_i.mfi);
        Sum += (lb_false ^ func_sb_false() ? static_field_int : simple_func_int());
        Sum += (lb_false ^ func_sb_false() ? static_field_int : ab[index]);
        Sum += (lb_false ^ func_sb_false() ? static_field_int : ab[index - 1]);
        Sum += (lb_false ^ func_sb_false() ? t1_i.mfi : 3);
        Sum += (lb_false ^ func_sb_false() ? t1_i.mfi : -5);
        Sum += (lb_false ^ func_sb_false() ? t1_i.mfi : local_int);
        Sum += (lb_false ^ func_sb_false() ? t1_i.mfi : static_field_int);
        Sum += (lb_false ^ func_sb_false() ? t1_i.mfi : t1_i.mfi);
        Sum += (lb_false ^ func_sb_false() ? t1_i.mfi : simple_func_int());
        Sum += (lb_false ^ func_sb_false() ? t1_i.mfi : ab[index]);
        Sum += (lb_false ^ func_sb_false() ? t1_i.mfi : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_146()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ func_sb_false() ? simple_func_int() : 3);
        Sum += (lb_false ^ func_sb_false() ? simple_func_int() : -5);
        Sum += (lb_false ^ func_sb_false() ? simple_func_int() : local_int);
        Sum += (lb_false ^ func_sb_false() ? simple_func_int() : static_field_int);
        Sum += (lb_false ^ func_sb_false() ? simple_func_int() : t1_i.mfi);
        Sum += (lb_false ^ func_sb_false() ? simple_func_int() : simple_func_int());
        Sum += (lb_false ^ func_sb_false() ? simple_func_int() : ab[index]);
        Sum += (lb_false ^ func_sb_false() ? simple_func_int() : ab[index - 1]);
        Sum += (lb_false ^ func_sb_false() ? ab[index] : 3);
        Sum += (lb_false ^ func_sb_false() ? ab[index] : -5);
        Sum += (lb_false ^ func_sb_false() ? ab[index] : local_int);
        Sum += (lb_false ^ func_sb_false() ? ab[index] : static_field_int);
        Sum += (lb_false ^ func_sb_false() ? ab[index] : t1_i.mfi);
        Sum += (lb_false ^ func_sb_false() ? ab[index] : simple_func_int());
        Sum += (lb_false ^ func_sb_false() ? ab[index] : ab[index]);
        Sum += (lb_false ^ func_sb_false() ? ab[index] : ab[index - 1]);
        Sum += (lb_false ^ func_sb_false() ? ab[index - 1] : 3);
        Sum += (lb_false ^ func_sb_false() ? ab[index - 1] : -5);
        Sum += (lb_false ^ func_sb_false() ? ab[index - 1] : local_int);
        Sum += (lb_false ^ func_sb_false() ? ab[index - 1] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_147()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ func_sb_false() ? ab[index - 1] : t1_i.mfi);
        Sum += (lb_false ^ func_sb_false() ? ab[index - 1] : simple_func_int());
        Sum += (lb_false ^ func_sb_false() ? ab[index - 1] : ab[index]);
        Sum += (lb_false ^ func_sb_false() ? ab[index - 1] : ab[index - 1]);
        Sum += (lb_false ^ ab_true[index] ? 3 : 3);
        Sum += (lb_false ^ ab_true[index] ? 3 : -5);
        Sum += (lb_false ^ ab_true[index] ? 3 : local_int);
        Sum += (lb_false ^ ab_true[index] ? 3 : static_field_int);
        Sum += (lb_false ^ ab_true[index] ? 3 : t1_i.mfi);
        Sum += (lb_false ^ ab_true[index] ? 3 : simple_func_int());
        Sum += (lb_false ^ ab_true[index] ? 3 : ab[index]);
        Sum += (lb_false ^ ab_true[index] ? 3 : ab[index - 1]);
        Sum += (lb_false ^ ab_true[index] ? -5 : 3);
        Sum += (lb_false ^ ab_true[index] ? -5 : -5);
        Sum += (lb_false ^ ab_true[index] ? -5 : local_int);
        Sum += (lb_false ^ ab_true[index] ? -5 : static_field_int);
        Sum += (lb_false ^ ab_true[index] ? -5 : t1_i.mfi);
        Sum += (lb_false ^ ab_true[index] ? -5 : simple_func_int());
        Sum += (lb_false ^ ab_true[index] ? -5 : ab[index]);
        Sum += (lb_false ^ ab_true[index] ? -5 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_148()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ ab_true[index] ? local_int : 3);
        Sum += (lb_false ^ ab_true[index] ? local_int : -5);
        Sum += (lb_false ^ ab_true[index] ? local_int : local_int);
        Sum += (lb_false ^ ab_true[index] ? local_int : static_field_int);
        Sum += (lb_false ^ ab_true[index] ? local_int : t1_i.mfi);
        Sum += (lb_false ^ ab_true[index] ? local_int : simple_func_int());
        Sum += (lb_false ^ ab_true[index] ? local_int : ab[index]);
        Sum += (lb_false ^ ab_true[index] ? local_int : ab[index - 1]);
        Sum += (lb_false ^ ab_true[index] ? static_field_int : 3);
        Sum += (lb_false ^ ab_true[index] ? static_field_int : -5);
        Sum += (lb_false ^ ab_true[index] ? static_field_int : local_int);
        Sum += (lb_false ^ ab_true[index] ? static_field_int : static_field_int);
        Sum += (lb_false ^ ab_true[index] ? static_field_int : t1_i.mfi);
        Sum += (lb_false ^ ab_true[index] ? static_field_int : simple_func_int());
        Sum += (lb_false ^ ab_true[index] ? static_field_int : ab[index]);
        Sum += (lb_false ^ ab_true[index] ? static_field_int : ab[index - 1]);
        Sum += (lb_false ^ ab_true[index] ? t1_i.mfi : 3);
        Sum += (lb_false ^ ab_true[index] ? t1_i.mfi : -5);
        Sum += (lb_false ^ ab_true[index] ? t1_i.mfi : local_int);
        Sum += (lb_false ^ ab_true[index] ? t1_i.mfi : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_149()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ ab_true[index] ? t1_i.mfi : t1_i.mfi);
        Sum += (lb_false ^ ab_true[index] ? t1_i.mfi : simple_func_int());
        Sum += (lb_false ^ ab_true[index] ? t1_i.mfi : ab[index]);
        Sum += (lb_false ^ ab_true[index] ? t1_i.mfi : ab[index - 1]);
        Sum += (lb_false ^ ab_true[index] ? simple_func_int() : 3);
        Sum += (lb_false ^ ab_true[index] ? simple_func_int() : -5);
        Sum += (lb_false ^ ab_true[index] ? simple_func_int() : local_int);
        Sum += (lb_false ^ ab_true[index] ? simple_func_int() : static_field_int);
        Sum += (lb_false ^ ab_true[index] ? simple_func_int() : t1_i.mfi);
        Sum += (lb_false ^ ab_true[index] ? simple_func_int() : simple_func_int());
        Sum += (lb_false ^ ab_true[index] ? simple_func_int() : ab[index]);
        Sum += (lb_false ^ ab_true[index] ? simple_func_int() : ab[index - 1]);
        Sum += (lb_false ^ ab_true[index] ? ab[index] : 3);
        Sum += (lb_false ^ ab_true[index] ? ab[index] : -5);
        Sum += (lb_false ^ ab_true[index] ? ab[index] : local_int);
        Sum += (lb_false ^ ab_true[index] ? ab[index] : static_field_int);
        Sum += (lb_false ^ ab_true[index] ? ab[index] : t1_i.mfi);
        Sum += (lb_false ^ ab_true[index] ? ab[index] : simple_func_int());
        Sum += (lb_false ^ ab_true[index] ? ab[index] : ab[index]);
        Sum += (lb_false ^ ab_true[index] ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_150()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ ab_true[index] ? ab[index - 1] : 3);
        Sum += (lb_false ^ ab_true[index] ? ab[index - 1] : -5);
        Sum += (lb_false ^ ab_true[index] ? ab[index - 1] : local_int);
        Sum += (lb_false ^ ab_true[index] ? ab[index - 1] : static_field_int);
        Sum += (lb_false ^ ab_true[index] ? ab[index - 1] : t1_i.mfi);
        Sum += (lb_false ^ ab_true[index] ? ab[index - 1] : simple_func_int());
        Sum += (lb_false ^ ab_true[index] ? ab[index - 1] : ab[index]);
        Sum += (lb_false ^ ab_true[index] ? ab[index - 1] : ab[index - 1]);
        Sum += (lb_false ^ ab_false[index] ? 3 : 3);
        Sum += (lb_false ^ ab_false[index] ? 3 : -5);
        Sum += (lb_false ^ ab_false[index] ? 3 : local_int);
        Sum += (lb_false ^ ab_false[index] ? 3 : static_field_int);
        Sum += (lb_false ^ ab_false[index] ? 3 : t1_i.mfi);
        Sum += (lb_false ^ ab_false[index] ? 3 : simple_func_int());
        Sum += (lb_false ^ ab_false[index] ? 3 : ab[index]);
        Sum += (lb_false ^ ab_false[index] ? 3 : ab[index - 1]);
        Sum += (lb_false ^ ab_false[index] ? -5 : 3);
        Sum += (lb_false ^ ab_false[index] ? -5 : -5);
        Sum += (lb_false ^ ab_false[index] ? -5 : local_int);
        Sum += (lb_false ^ ab_false[index] ? -5 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_151()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ ab_false[index] ? -5 : t1_i.mfi);
        Sum += (lb_false ^ ab_false[index] ? -5 : simple_func_int());
        Sum += (lb_false ^ ab_false[index] ? -5 : ab[index]);
        Sum += (lb_false ^ ab_false[index] ? -5 : ab[index - 1]);
        Sum += (lb_false ^ ab_false[index] ? local_int : 3);
        Sum += (lb_false ^ ab_false[index] ? local_int : -5);
        Sum += (lb_false ^ ab_false[index] ? local_int : local_int);
        Sum += (lb_false ^ ab_false[index] ? local_int : static_field_int);
        Sum += (lb_false ^ ab_false[index] ? local_int : t1_i.mfi);
        Sum += (lb_false ^ ab_false[index] ? local_int : simple_func_int());
        Sum += (lb_false ^ ab_false[index] ? local_int : ab[index]);
        Sum += (lb_false ^ ab_false[index] ? local_int : ab[index - 1]);
        Sum += (lb_false ^ ab_false[index] ? static_field_int : 3);
        Sum += (lb_false ^ ab_false[index] ? static_field_int : -5);
        Sum += (lb_false ^ ab_false[index] ? static_field_int : local_int);
        Sum += (lb_false ^ ab_false[index] ? static_field_int : static_field_int);
        Sum += (lb_false ^ ab_false[index] ? static_field_int : t1_i.mfi);
        Sum += (lb_false ^ ab_false[index] ? static_field_int : simple_func_int());
        Sum += (lb_false ^ ab_false[index] ? static_field_int : ab[index]);
        Sum += (lb_false ^ ab_false[index] ? static_field_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_152()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ ab_false[index] ? t1_i.mfi : 3);
        Sum += (lb_false ^ ab_false[index] ? t1_i.mfi : -5);
        Sum += (lb_false ^ ab_false[index] ? t1_i.mfi : local_int);
        Sum += (lb_false ^ ab_false[index] ? t1_i.mfi : static_field_int);
        Sum += (lb_false ^ ab_false[index] ? t1_i.mfi : t1_i.mfi);
        Sum += (lb_false ^ ab_false[index] ? t1_i.mfi : simple_func_int());
        Sum += (lb_false ^ ab_false[index] ? t1_i.mfi : ab[index]);
        Sum += (lb_false ^ ab_false[index] ? t1_i.mfi : ab[index - 1]);
        Sum += (lb_false ^ ab_false[index] ? simple_func_int() : 3);
        Sum += (lb_false ^ ab_false[index] ? simple_func_int() : -5);
        Sum += (lb_false ^ ab_false[index] ? simple_func_int() : local_int);
        Sum += (lb_false ^ ab_false[index] ? simple_func_int() : static_field_int);
        Sum += (lb_false ^ ab_false[index] ? simple_func_int() : t1_i.mfi);
        Sum += (lb_false ^ ab_false[index] ? simple_func_int() : simple_func_int());
        Sum += (lb_false ^ ab_false[index] ? simple_func_int() : ab[index]);
        Sum += (lb_false ^ ab_false[index] ? simple_func_int() : ab[index - 1]);
        Sum += (lb_false ^ ab_false[index] ? ab[index] : 3);
        Sum += (lb_false ^ ab_false[index] ? ab[index] : -5);
        Sum += (lb_false ^ ab_false[index] ? ab[index] : local_int);
        Sum += (lb_false ^ ab_false[index] ? ab[index] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_153()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ^ ab_false[index] ? ab[index] : t1_i.mfi);
        Sum += (lb_false ^ ab_false[index] ? ab[index] : simple_func_int());
        Sum += (lb_false ^ ab_false[index] ? ab[index] : ab[index]);
        Sum += (lb_false ^ ab_false[index] ? ab[index] : ab[index - 1]);
        Sum += (lb_false ^ ab_false[index] ? ab[index - 1] : 3);
        Sum += (lb_false ^ ab_false[index] ? ab[index - 1] : -5);
        Sum += (lb_false ^ ab_false[index] ? ab[index - 1] : local_int);
        Sum += (lb_false ^ ab_false[index] ? ab[index - 1] : static_field_int);
        Sum += (lb_false ^ ab_false[index] ? ab[index - 1] : t1_i.mfi);
        Sum += (lb_false ^ ab_false[index] ? ab[index - 1] : simple_func_int());
        Sum += (lb_false ^ ab_false[index] ? ab[index - 1] : ab[index]);
        Sum += (lb_false ^ ab_false[index] ? ab[index - 1] : ab[index - 1]);
        Sum += (sfb_true ^ true ? 3 : 3);
        Sum += (sfb_true ^ true ? 3 : -5);
        Sum += (sfb_true ^ true ? 3 : local_int);
        Sum += (sfb_true ^ true ? 3 : static_field_int);
        Sum += (sfb_true ^ true ? 3 : t1_i.mfi);
        Sum += (sfb_true ^ true ? 3 : simple_func_int());
        Sum += (sfb_true ^ true ? 3 : ab[index]);
        Sum += (sfb_true ^ true ? 3 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_154()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ true ? -5 : 3);
        Sum += (sfb_true ^ true ? -5 : -5);
        Sum += (sfb_true ^ true ? -5 : local_int);
        Sum += (sfb_true ^ true ? -5 : static_field_int);
        Sum += (sfb_true ^ true ? -5 : t1_i.mfi);
        Sum += (sfb_true ^ true ? -5 : simple_func_int());
        Sum += (sfb_true ^ true ? -5 : ab[index]);
        Sum += (sfb_true ^ true ? -5 : ab[index - 1]);
        Sum += (sfb_true ^ true ? local_int : 3);
        Sum += (sfb_true ^ true ? local_int : -5);
        Sum += (sfb_true ^ true ? local_int : local_int);
        Sum += (sfb_true ^ true ? local_int : static_field_int);
        Sum += (sfb_true ^ true ? local_int : t1_i.mfi);
        Sum += (sfb_true ^ true ? local_int : simple_func_int());
        Sum += (sfb_true ^ true ? local_int : ab[index]);
        Sum += (sfb_true ^ true ? local_int : ab[index - 1]);
        Sum += (sfb_true ^ true ? static_field_int : 3);
        Sum += (sfb_true ^ true ? static_field_int : -5);
        Sum += (sfb_true ^ true ? static_field_int : local_int);
        Sum += (sfb_true ^ true ? static_field_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_155()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ true ? static_field_int : t1_i.mfi);
        Sum += (sfb_true ^ true ? static_field_int : simple_func_int());
        Sum += (sfb_true ^ true ? static_field_int : ab[index]);
        Sum += (sfb_true ^ true ? static_field_int : ab[index - 1]);
        Sum += (sfb_true ^ true ? t1_i.mfi : 3);
        Sum += (sfb_true ^ true ? t1_i.mfi : -5);
        Sum += (sfb_true ^ true ? t1_i.mfi : local_int);
        Sum += (sfb_true ^ true ? t1_i.mfi : static_field_int);
        Sum += (sfb_true ^ true ? t1_i.mfi : t1_i.mfi);
        Sum += (sfb_true ^ true ? t1_i.mfi : simple_func_int());
        Sum += (sfb_true ^ true ? t1_i.mfi : ab[index]);
        Sum += (sfb_true ^ true ? t1_i.mfi : ab[index - 1]);
        Sum += (sfb_true ^ true ? simple_func_int() : 3);
        Sum += (sfb_true ^ true ? simple_func_int() : -5);
        Sum += (sfb_true ^ true ? simple_func_int() : local_int);
        Sum += (sfb_true ^ true ? simple_func_int() : static_field_int);
        Sum += (sfb_true ^ true ? simple_func_int() : t1_i.mfi);
        Sum += (sfb_true ^ true ? simple_func_int() : simple_func_int());
        Sum += (sfb_true ^ true ? simple_func_int() : ab[index]);
        Sum += (sfb_true ^ true ? simple_func_int() : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_156()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ true ? ab[index] : 3);
        Sum += (sfb_true ^ true ? ab[index] : -5);
        Sum += (sfb_true ^ true ? ab[index] : local_int);
        Sum += (sfb_true ^ true ? ab[index] : static_field_int);
        Sum += (sfb_true ^ true ? ab[index] : t1_i.mfi);
        Sum += (sfb_true ^ true ? ab[index] : simple_func_int());
        Sum += (sfb_true ^ true ? ab[index] : ab[index]);
        Sum += (sfb_true ^ true ? ab[index] : ab[index - 1]);
        Sum += (sfb_true ^ true ? ab[index - 1] : 3);
        Sum += (sfb_true ^ true ? ab[index - 1] : -5);
        Sum += (sfb_true ^ true ? ab[index - 1] : local_int);
        Sum += (sfb_true ^ true ? ab[index - 1] : static_field_int);
        Sum += (sfb_true ^ true ? ab[index - 1] : t1_i.mfi);
        Sum += (sfb_true ^ true ? ab[index - 1] : simple_func_int());
        Sum += (sfb_true ^ true ? ab[index - 1] : ab[index]);
        Sum += (sfb_true ^ true ? ab[index - 1] : ab[index - 1]);
        Sum += (sfb_true ^ false ? 3 : 3);
        Sum += (sfb_true ^ false ? 3 : -5);
        Sum += (sfb_true ^ false ? 3 : local_int);
        Sum += (sfb_true ^ false ? 3 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_157()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ false ? 3 : t1_i.mfi);
        Sum += (sfb_true ^ false ? 3 : simple_func_int());
        Sum += (sfb_true ^ false ? 3 : ab[index]);
        Sum += (sfb_true ^ false ? 3 : ab[index - 1]);
        Sum += (sfb_true ^ false ? -5 : 3);
        Sum += (sfb_true ^ false ? -5 : -5);
        Sum += (sfb_true ^ false ? -5 : local_int);
        Sum += (sfb_true ^ false ? -5 : static_field_int);
        Sum += (sfb_true ^ false ? -5 : t1_i.mfi);
        Sum += (sfb_true ^ false ? -5 : simple_func_int());
        Sum += (sfb_true ^ false ? -5 : ab[index]);
        Sum += (sfb_true ^ false ? -5 : ab[index - 1]);
        Sum += (sfb_true ^ false ? local_int : 3);
        Sum += (sfb_true ^ false ? local_int : -5);
        Sum += (sfb_true ^ false ? local_int : local_int);
        Sum += (sfb_true ^ false ? local_int : static_field_int);
        Sum += (sfb_true ^ false ? local_int : t1_i.mfi);
        Sum += (sfb_true ^ false ? local_int : simple_func_int());
        Sum += (sfb_true ^ false ? local_int : ab[index]);
        Sum += (sfb_true ^ false ? local_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_158()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ false ? static_field_int : 3);
        Sum += (sfb_true ^ false ? static_field_int : -5);
        Sum += (sfb_true ^ false ? static_field_int : local_int);
        Sum += (sfb_true ^ false ? static_field_int : static_field_int);
        Sum += (sfb_true ^ false ? static_field_int : t1_i.mfi);
        Sum += (sfb_true ^ false ? static_field_int : simple_func_int());
        Sum += (sfb_true ^ false ? static_field_int : ab[index]);
        Sum += (sfb_true ^ false ? static_field_int : ab[index - 1]);
        Sum += (sfb_true ^ false ? t1_i.mfi : 3);
        Sum += (sfb_true ^ false ? t1_i.mfi : -5);
        Sum += (sfb_true ^ false ? t1_i.mfi : local_int);
        Sum += (sfb_true ^ false ? t1_i.mfi : static_field_int);
        Sum += (sfb_true ^ false ? t1_i.mfi : t1_i.mfi);
        Sum += (sfb_true ^ false ? t1_i.mfi : simple_func_int());
        Sum += (sfb_true ^ false ? t1_i.mfi : ab[index]);
        Sum += (sfb_true ^ false ? t1_i.mfi : ab[index - 1]);
        Sum += (sfb_true ^ false ? simple_func_int() : 3);
        Sum += (sfb_true ^ false ? simple_func_int() : -5);
        Sum += (sfb_true ^ false ? simple_func_int() : local_int);
        Sum += (sfb_true ^ false ? simple_func_int() : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_159()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ false ? simple_func_int() : t1_i.mfi);
        Sum += (sfb_true ^ false ? simple_func_int() : simple_func_int());
        Sum += (sfb_true ^ false ? simple_func_int() : ab[index]);
        Sum += (sfb_true ^ false ? simple_func_int() : ab[index - 1]);
        Sum += (sfb_true ^ false ? ab[index] : 3);
        Sum += (sfb_true ^ false ? ab[index] : -5);
        Sum += (sfb_true ^ false ? ab[index] : local_int);
        Sum += (sfb_true ^ false ? ab[index] : static_field_int);
        Sum += (sfb_true ^ false ? ab[index] : t1_i.mfi);
        Sum += (sfb_true ^ false ? ab[index] : simple_func_int());
        Sum += (sfb_true ^ false ? ab[index] : ab[index]);
        Sum += (sfb_true ^ false ? ab[index] : ab[index - 1]);
        Sum += (sfb_true ^ false ? ab[index - 1] : 3);
        Sum += (sfb_true ^ false ? ab[index - 1] : -5);
        Sum += (sfb_true ^ false ? ab[index - 1] : local_int);
        Sum += (sfb_true ^ false ? ab[index - 1] : static_field_int);
        Sum += (sfb_true ^ false ? ab[index - 1] : t1_i.mfi);
        Sum += (sfb_true ^ false ? ab[index - 1] : simple_func_int());
        Sum += (sfb_true ^ false ? ab[index - 1] : ab[index]);
        Sum += (sfb_true ^ false ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_160()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ lb_true ? 3 : 3);
        Sum += (sfb_true ^ lb_true ? 3 : -5);
        Sum += (sfb_true ^ lb_true ? 3 : local_int);
        Sum += (sfb_true ^ lb_true ? 3 : static_field_int);
        Sum += (sfb_true ^ lb_true ? 3 : t1_i.mfi);
        Sum += (sfb_true ^ lb_true ? 3 : simple_func_int());
        Sum += (sfb_true ^ lb_true ? 3 : ab[index]);
        Sum += (sfb_true ^ lb_true ? 3 : ab[index - 1]);
        Sum += (sfb_true ^ lb_true ? -5 : 3);
        Sum += (sfb_true ^ lb_true ? -5 : -5);
        Sum += (sfb_true ^ lb_true ? -5 : local_int);
        Sum += (sfb_true ^ lb_true ? -5 : static_field_int);
        Sum += (sfb_true ^ lb_true ? -5 : t1_i.mfi);
        Sum += (sfb_true ^ lb_true ? -5 : simple_func_int());
        Sum += (sfb_true ^ lb_true ? -5 : ab[index]);
        Sum += (sfb_true ^ lb_true ? -5 : ab[index - 1]);
        Sum += (sfb_true ^ lb_true ? local_int : 3);
        Sum += (sfb_true ^ lb_true ? local_int : -5);
        Sum += (sfb_true ^ lb_true ? local_int : local_int);
        Sum += (sfb_true ^ lb_true ? local_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_161()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ lb_true ? local_int : t1_i.mfi);
        Sum += (sfb_true ^ lb_true ? local_int : simple_func_int());
        Sum += (sfb_true ^ lb_true ? local_int : ab[index]);
        Sum += (sfb_true ^ lb_true ? local_int : ab[index - 1]);
        Sum += (sfb_true ^ lb_true ? static_field_int : 3);
        Sum += (sfb_true ^ lb_true ? static_field_int : -5);
        Sum += (sfb_true ^ lb_true ? static_field_int : local_int);
        Sum += (sfb_true ^ lb_true ? static_field_int : static_field_int);
        Sum += (sfb_true ^ lb_true ? static_field_int : t1_i.mfi);
        Sum += (sfb_true ^ lb_true ? static_field_int : simple_func_int());
        Sum += (sfb_true ^ lb_true ? static_field_int : ab[index]);
        Sum += (sfb_true ^ lb_true ? static_field_int : ab[index - 1]);
        Sum += (sfb_true ^ lb_true ? t1_i.mfi : 3);
        Sum += (sfb_true ^ lb_true ? t1_i.mfi : -5);
        Sum += (sfb_true ^ lb_true ? t1_i.mfi : local_int);
        Sum += (sfb_true ^ lb_true ? t1_i.mfi : static_field_int);
        Sum += (sfb_true ^ lb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (sfb_true ^ lb_true ? t1_i.mfi : simple_func_int());
        Sum += (sfb_true ^ lb_true ? t1_i.mfi : ab[index]);
        Sum += (sfb_true ^ lb_true ? t1_i.mfi : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_162()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ lb_true ? simple_func_int() : 3);
        Sum += (sfb_true ^ lb_true ? simple_func_int() : -5);
        Sum += (sfb_true ^ lb_true ? simple_func_int() : local_int);
        Sum += (sfb_true ^ lb_true ? simple_func_int() : static_field_int);
        Sum += (sfb_true ^ lb_true ? simple_func_int() : t1_i.mfi);
        Sum += (sfb_true ^ lb_true ? simple_func_int() : simple_func_int());
        Sum += (sfb_true ^ lb_true ? simple_func_int() : ab[index]);
        Sum += (sfb_true ^ lb_true ? simple_func_int() : ab[index - 1]);
        Sum += (sfb_true ^ lb_true ? ab[index] : 3);
        Sum += (sfb_true ^ lb_true ? ab[index] : -5);
        Sum += (sfb_true ^ lb_true ? ab[index] : local_int);
        Sum += (sfb_true ^ lb_true ? ab[index] : static_field_int);
        Sum += (sfb_true ^ lb_true ? ab[index] : t1_i.mfi);
        Sum += (sfb_true ^ lb_true ? ab[index] : simple_func_int());
        Sum += (sfb_true ^ lb_true ? ab[index] : ab[index]);
        Sum += (sfb_true ^ lb_true ? ab[index] : ab[index - 1]);
        Sum += (sfb_true ^ lb_true ? ab[index - 1] : 3);
        Sum += (sfb_true ^ lb_true ? ab[index - 1] : -5);
        Sum += (sfb_true ^ lb_true ? ab[index - 1] : local_int);
        Sum += (sfb_true ^ lb_true ? ab[index - 1] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_163()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ lb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (sfb_true ^ lb_true ? ab[index - 1] : simple_func_int());
        Sum += (sfb_true ^ lb_true ? ab[index - 1] : ab[index]);
        Sum += (sfb_true ^ lb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (sfb_true ^ lb_false ? 3 : 3);
        Sum += (sfb_true ^ lb_false ? 3 : -5);
        Sum += (sfb_true ^ lb_false ? 3 : local_int);
        Sum += (sfb_true ^ lb_false ? 3 : static_field_int);
        Sum += (sfb_true ^ lb_false ? 3 : t1_i.mfi);
        Sum += (sfb_true ^ lb_false ? 3 : simple_func_int());
        Sum += (sfb_true ^ lb_false ? 3 : ab[index]);
        Sum += (sfb_true ^ lb_false ? 3 : ab[index - 1]);
        Sum += (sfb_true ^ lb_false ? -5 : 3);
        Sum += (sfb_true ^ lb_false ? -5 : -5);
        Sum += (sfb_true ^ lb_false ? -5 : local_int);
        Sum += (sfb_true ^ lb_false ? -5 : static_field_int);
        Sum += (sfb_true ^ lb_false ? -5 : t1_i.mfi);
        Sum += (sfb_true ^ lb_false ? -5 : simple_func_int());
        Sum += (sfb_true ^ lb_false ? -5 : ab[index]);
        Sum += (sfb_true ^ lb_false ? -5 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_164()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ lb_false ? local_int : 3);
        Sum += (sfb_true ^ lb_false ? local_int : -5);
        Sum += (sfb_true ^ lb_false ? local_int : local_int);
        Sum += (sfb_true ^ lb_false ? local_int : static_field_int);
        Sum += (sfb_true ^ lb_false ? local_int : t1_i.mfi);
        Sum += (sfb_true ^ lb_false ? local_int : simple_func_int());
        Sum += (sfb_true ^ lb_false ? local_int : ab[index]);
        Sum += (sfb_true ^ lb_false ? local_int : ab[index - 1]);
        Sum += (sfb_true ^ lb_false ? static_field_int : 3);
        Sum += (sfb_true ^ lb_false ? static_field_int : -5);
        Sum += (sfb_true ^ lb_false ? static_field_int : local_int);
        Sum += (sfb_true ^ lb_false ? static_field_int : static_field_int);
        Sum += (sfb_true ^ lb_false ? static_field_int : t1_i.mfi);
        Sum += (sfb_true ^ lb_false ? static_field_int : simple_func_int());
        Sum += (sfb_true ^ lb_false ? static_field_int : ab[index]);
        Sum += (sfb_true ^ lb_false ? static_field_int : ab[index - 1]);
        Sum += (sfb_true ^ lb_false ? t1_i.mfi : 3);
        Sum += (sfb_true ^ lb_false ? t1_i.mfi : -5);
        Sum += (sfb_true ^ lb_false ? t1_i.mfi : local_int);
        Sum += (sfb_true ^ lb_false ? t1_i.mfi : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_165()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ lb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (sfb_true ^ lb_false ? t1_i.mfi : simple_func_int());
        Sum += (sfb_true ^ lb_false ? t1_i.mfi : ab[index]);
        Sum += (sfb_true ^ lb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (sfb_true ^ lb_false ? simple_func_int() : 3);
        Sum += (sfb_true ^ lb_false ? simple_func_int() : -5);
        Sum += (sfb_true ^ lb_false ? simple_func_int() : local_int);
        Sum += (sfb_true ^ lb_false ? simple_func_int() : static_field_int);
        Sum += (sfb_true ^ lb_false ? simple_func_int() : t1_i.mfi);
        Sum += (sfb_true ^ lb_false ? simple_func_int() : simple_func_int());
        Sum += (sfb_true ^ lb_false ? simple_func_int() : ab[index]);
        Sum += (sfb_true ^ lb_false ? simple_func_int() : ab[index - 1]);
        Sum += (sfb_true ^ lb_false ? ab[index] : 3);
        Sum += (sfb_true ^ lb_false ? ab[index] : -5);
        Sum += (sfb_true ^ lb_false ? ab[index] : local_int);
        Sum += (sfb_true ^ lb_false ? ab[index] : static_field_int);
        Sum += (sfb_true ^ lb_false ? ab[index] : t1_i.mfi);
        Sum += (sfb_true ^ lb_false ? ab[index] : simple_func_int());
        Sum += (sfb_true ^ lb_false ? ab[index] : ab[index]);
        Sum += (sfb_true ^ lb_false ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_166()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ lb_false ? ab[index - 1] : 3);
        Sum += (sfb_true ^ lb_false ? ab[index - 1] : -5);
        Sum += (sfb_true ^ lb_false ? ab[index - 1] : local_int);
        Sum += (sfb_true ^ lb_false ? ab[index - 1] : static_field_int);
        Sum += (sfb_true ^ lb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (sfb_true ^ lb_false ? ab[index - 1] : simple_func_int());
        Sum += (sfb_true ^ lb_false ? ab[index - 1] : ab[index]);
        Sum += (sfb_true ^ lb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (sfb_true ^ sfb_true ? 3 : 3);
        Sum += (sfb_true ^ sfb_true ? 3 : -5);
        Sum += (sfb_true ^ sfb_true ? 3 : local_int);
        Sum += (sfb_true ^ sfb_true ? 3 : static_field_int);
        Sum += (sfb_true ^ sfb_true ? 3 : t1_i.mfi);
        Sum += (sfb_true ^ sfb_true ? 3 : simple_func_int());
        Sum += (sfb_true ^ sfb_true ? 3 : ab[index]);
        Sum += (sfb_true ^ sfb_true ? 3 : ab[index - 1]);
        Sum += (sfb_true ^ sfb_true ? -5 : 3);
        Sum += (sfb_true ^ sfb_true ? -5 : -5);
        Sum += (sfb_true ^ sfb_true ? -5 : local_int);
        Sum += (sfb_true ^ sfb_true ? -5 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_167()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ sfb_true ? -5 : t1_i.mfi);
        Sum += (sfb_true ^ sfb_true ? -5 : simple_func_int());
        Sum += (sfb_true ^ sfb_true ? -5 : ab[index]);
        Sum += (sfb_true ^ sfb_true ? -5 : ab[index - 1]);
        Sum += (sfb_true ^ sfb_true ? local_int : 3);
        Sum += (sfb_true ^ sfb_true ? local_int : -5);
        Sum += (sfb_true ^ sfb_true ? local_int : local_int);
        Sum += (sfb_true ^ sfb_true ? local_int : static_field_int);
        Sum += (sfb_true ^ sfb_true ? local_int : t1_i.mfi);
        Sum += (sfb_true ^ sfb_true ? local_int : simple_func_int());
        Sum += (sfb_true ^ sfb_true ? local_int : ab[index]);
        Sum += (sfb_true ^ sfb_true ? local_int : ab[index - 1]);
        Sum += (sfb_true ^ sfb_true ? static_field_int : 3);
        Sum += (sfb_true ^ sfb_true ? static_field_int : -5);
        Sum += (sfb_true ^ sfb_true ? static_field_int : local_int);
        Sum += (sfb_true ^ sfb_true ? static_field_int : static_field_int);
        Sum += (sfb_true ^ sfb_true ? static_field_int : t1_i.mfi);
        Sum += (sfb_true ^ sfb_true ? static_field_int : simple_func_int());
        Sum += (sfb_true ^ sfb_true ? static_field_int : ab[index]);
        Sum += (sfb_true ^ sfb_true ? static_field_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_168()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ sfb_true ? t1_i.mfi : 3);
        Sum += (sfb_true ^ sfb_true ? t1_i.mfi : -5);
        Sum += (sfb_true ^ sfb_true ? t1_i.mfi : local_int);
        Sum += (sfb_true ^ sfb_true ? t1_i.mfi : static_field_int);
        Sum += (sfb_true ^ sfb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (sfb_true ^ sfb_true ? t1_i.mfi : simple_func_int());
        Sum += (sfb_true ^ sfb_true ? t1_i.mfi : ab[index]);
        Sum += (sfb_true ^ sfb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (sfb_true ^ sfb_true ? simple_func_int() : 3);
        Sum += (sfb_true ^ sfb_true ? simple_func_int() : -5);
        Sum += (sfb_true ^ sfb_true ? simple_func_int() : local_int);
        Sum += (sfb_true ^ sfb_true ? simple_func_int() : static_field_int);
        Sum += (sfb_true ^ sfb_true ? simple_func_int() : t1_i.mfi);
        Sum += (sfb_true ^ sfb_true ? simple_func_int() : simple_func_int());
        Sum += (sfb_true ^ sfb_true ? simple_func_int() : ab[index]);
        Sum += (sfb_true ^ sfb_true ? simple_func_int() : ab[index - 1]);
        Sum += (sfb_true ^ sfb_true ? ab[index] : 3);
        Sum += (sfb_true ^ sfb_true ? ab[index] : -5);
        Sum += (sfb_true ^ sfb_true ? ab[index] : local_int);
        Sum += (sfb_true ^ sfb_true ? ab[index] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_169()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ sfb_true ? ab[index] : t1_i.mfi);
        Sum += (sfb_true ^ sfb_true ? ab[index] : simple_func_int());
        Sum += (sfb_true ^ sfb_true ? ab[index] : ab[index]);
        Sum += (sfb_true ^ sfb_true ? ab[index] : ab[index - 1]);
        Sum += (sfb_true ^ sfb_true ? ab[index - 1] : 3);
        Sum += (sfb_true ^ sfb_true ? ab[index - 1] : -5);
        Sum += (sfb_true ^ sfb_true ? ab[index - 1] : local_int);
        Sum += (sfb_true ^ sfb_true ? ab[index - 1] : static_field_int);
        Sum += (sfb_true ^ sfb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (sfb_true ^ sfb_true ? ab[index - 1] : simple_func_int());
        Sum += (sfb_true ^ sfb_true ? ab[index - 1] : ab[index]);
        Sum += (sfb_true ^ sfb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (sfb_true ^ sfb_false ? 3 : 3);
        Sum += (sfb_true ^ sfb_false ? 3 : -5);
        Sum += (sfb_true ^ sfb_false ? 3 : local_int);
        Sum += (sfb_true ^ sfb_false ? 3 : static_field_int);
        Sum += (sfb_true ^ sfb_false ? 3 : t1_i.mfi);
        Sum += (sfb_true ^ sfb_false ? 3 : simple_func_int());
        Sum += (sfb_true ^ sfb_false ? 3 : ab[index]);
        Sum += (sfb_true ^ sfb_false ? 3 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_170()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ sfb_false ? -5 : 3);
        Sum += (sfb_true ^ sfb_false ? -5 : -5);
        Sum += (sfb_true ^ sfb_false ? -5 : local_int);
        Sum += (sfb_true ^ sfb_false ? -5 : static_field_int);
        Sum += (sfb_true ^ sfb_false ? -5 : t1_i.mfi);
        Sum += (sfb_true ^ sfb_false ? -5 : simple_func_int());
        Sum += (sfb_true ^ sfb_false ? -5 : ab[index]);
        Sum += (sfb_true ^ sfb_false ? -5 : ab[index - 1]);
        Sum += (sfb_true ^ sfb_false ? local_int : 3);
        Sum += (sfb_true ^ sfb_false ? local_int : -5);
        Sum += (sfb_true ^ sfb_false ? local_int : local_int);
        Sum += (sfb_true ^ sfb_false ? local_int : static_field_int);
        Sum += (sfb_true ^ sfb_false ? local_int : t1_i.mfi);
        Sum += (sfb_true ^ sfb_false ? local_int : simple_func_int());
        Sum += (sfb_true ^ sfb_false ? local_int : ab[index]);
        Sum += (sfb_true ^ sfb_false ? local_int : ab[index - 1]);
        Sum += (sfb_true ^ sfb_false ? static_field_int : 3);
        Sum += (sfb_true ^ sfb_false ? static_field_int : -5);
        Sum += (sfb_true ^ sfb_false ? static_field_int : local_int);
        Sum += (sfb_true ^ sfb_false ? static_field_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_171()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ sfb_false ? static_field_int : t1_i.mfi);
        Sum += (sfb_true ^ sfb_false ? static_field_int : simple_func_int());
        Sum += (sfb_true ^ sfb_false ? static_field_int : ab[index]);
        Sum += (sfb_true ^ sfb_false ? static_field_int : ab[index - 1]);
        Sum += (sfb_true ^ sfb_false ? t1_i.mfi : 3);
        Sum += (sfb_true ^ sfb_false ? t1_i.mfi : -5);
        Sum += (sfb_true ^ sfb_false ? t1_i.mfi : local_int);
        Sum += (sfb_true ^ sfb_false ? t1_i.mfi : static_field_int);
        Sum += (sfb_true ^ sfb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (sfb_true ^ sfb_false ? t1_i.mfi : simple_func_int());
        Sum += (sfb_true ^ sfb_false ? t1_i.mfi : ab[index]);
        Sum += (sfb_true ^ sfb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (sfb_true ^ sfb_false ? simple_func_int() : 3);
        Sum += (sfb_true ^ sfb_false ? simple_func_int() : -5);
        Sum += (sfb_true ^ sfb_false ? simple_func_int() : local_int);
        Sum += (sfb_true ^ sfb_false ? simple_func_int() : static_field_int);
        Sum += (sfb_true ^ sfb_false ? simple_func_int() : t1_i.mfi);
        Sum += (sfb_true ^ sfb_false ? simple_func_int() : simple_func_int());
        Sum += (sfb_true ^ sfb_false ? simple_func_int() : ab[index]);
        Sum += (sfb_true ^ sfb_false ? simple_func_int() : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_172()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ sfb_false ? ab[index] : 3);
        Sum += (sfb_true ^ sfb_false ? ab[index] : -5);
        Sum += (sfb_true ^ sfb_false ? ab[index] : local_int);
        Sum += (sfb_true ^ sfb_false ? ab[index] : static_field_int);
        Sum += (sfb_true ^ sfb_false ? ab[index] : t1_i.mfi);
        Sum += (sfb_true ^ sfb_false ? ab[index] : simple_func_int());
        Sum += (sfb_true ^ sfb_false ? ab[index] : ab[index]);
        Sum += (sfb_true ^ sfb_false ? ab[index] : ab[index - 1]);
        Sum += (sfb_true ^ sfb_false ? ab[index - 1] : 3);
        Sum += (sfb_true ^ sfb_false ? ab[index - 1] : -5);
        Sum += (sfb_true ^ sfb_false ? ab[index - 1] : local_int);
        Sum += (sfb_true ^ sfb_false ? ab[index - 1] : static_field_int);
        Sum += (sfb_true ^ sfb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (sfb_true ^ sfb_false ? ab[index - 1] : simple_func_int());
        Sum += (sfb_true ^ sfb_false ? ab[index - 1] : ab[index]);
        Sum += (sfb_true ^ sfb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (sfb_true ^ t1_i.mfb_true ? 3 : 3);
        Sum += (sfb_true ^ t1_i.mfb_true ? 3 : -5);
        Sum += (sfb_true ^ t1_i.mfb_true ? 3 : local_int);
        Sum += (sfb_true ^ t1_i.mfb_true ? 3 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_173()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ t1_i.mfb_true ? 3 : t1_i.mfi);
        Sum += (sfb_true ^ t1_i.mfb_true ? 3 : simple_func_int());
        Sum += (sfb_true ^ t1_i.mfb_true ? 3 : ab[index]);
        Sum += (sfb_true ^ t1_i.mfb_true ? 3 : ab[index - 1]);
        Sum += (sfb_true ^ t1_i.mfb_true ? -5 : 3);
        Sum += (sfb_true ^ t1_i.mfb_true ? -5 : -5);
        Sum += (sfb_true ^ t1_i.mfb_true ? -5 : local_int);
        Sum += (sfb_true ^ t1_i.mfb_true ? -5 : static_field_int);
        Sum += (sfb_true ^ t1_i.mfb_true ? -5 : t1_i.mfi);
        Sum += (sfb_true ^ t1_i.mfb_true ? -5 : simple_func_int());
        Sum += (sfb_true ^ t1_i.mfb_true ? -5 : ab[index]);
        Sum += (sfb_true ^ t1_i.mfb_true ? -5 : ab[index - 1]);
        Sum += (sfb_true ^ t1_i.mfb_true ? local_int : 3);
        Sum += (sfb_true ^ t1_i.mfb_true ? local_int : -5);
        Sum += (sfb_true ^ t1_i.mfb_true ? local_int : local_int);
        Sum += (sfb_true ^ t1_i.mfb_true ? local_int : static_field_int);
        Sum += (sfb_true ^ t1_i.mfb_true ? local_int : t1_i.mfi);
        Sum += (sfb_true ^ t1_i.mfb_true ? local_int : simple_func_int());
        Sum += (sfb_true ^ t1_i.mfb_true ? local_int : ab[index]);
        Sum += (sfb_true ^ t1_i.mfb_true ? local_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_174()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ t1_i.mfb_true ? static_field_int : 3);
        Sum += (sfb_true ^ t1_i.mfb_true ? static_field_int : -5);
        Sum += (sfb_true ^ t1_i.mfb_true ? static_field_int : local_int);
        Sum += (sfb_true ^ t1_i.mfb_true ? static_field_int : static_field_int);
        Sum += (sfb_true ^ t1_i.mfb_true ? static_field_int : t1_i.mfi);
        Sum += (sfb_true ^ t1_i.mfb_true ? static_field_int : simple_func_int());
        Sum += (sfb_true ^ t1_i.mfb_true ? static_field_int : ab[index]);
        Sum += (sfb_true ^ t1_i.mfb_true ? static_field_int : ab[index - 1]);
        Sum += (sfb_true ^ t1_i.mfb_true ? t1_i.mfi : 3);
        Sum += (sfb_true ^ t1_i.mfb_true ? t1_i.mfi : -5);
        Sum += (sfb_true ^ t1_i.mfb_true ? t1_i.mfi : local_int);
        Sum += (sfb_true ^ t1_i.mfb_true ? t1_i.mfi : static_field_int);
        Sum += (sfb_true ^ t1_i.mfb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (sfb_true ^ t1_i.mfb_true ? t1_i.mfi : simple_func_int());
        Sum += (sfb_true ^ t1_i.mfb_true ? t1_i.mfi : ab[index]);
        Sum += (sfb_true ^ t1_i.mfb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (sfb_true ^ t1_i.mfb_true ? simple_func_int() : 3);
        Sum += (sfb_true ^ t1_i.mfb_true ? simple_func_int() : -5);
        Sum += (sfb_true ^ t1_i.mfb_true ? simple_func_int() : local_int);
        Sum += (sfb_true ^ t1_i.mfb_true ? simple_func_int() : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_175()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ t1_i.mfb_true ? simple_func_int() : t1_i.mfi);
        Sum += (sfb_true ^ t1_i.mfb_true ? simple_func_int() : simple_func_int());
        Sum += (sfb_true ^ t1_i.mfb_true ? simple_func_int() : ab[index]);
        Sum += (sfb_true ^ t1_i.mfb_true ? simple_func_int() : ab[index - 1]);
        Sum += (sfb_true ^ t1_i.mfb_true ? ab[index] : 3);
        Sum += (sfb_true ^ t1_i.mfb_true ? ab[index] : -5);
        Sum += (sfb_true ^ t1_i.mfb_true ? ab[index] : local_int);
        Sum += (sfb_true ^ t1_i.mfb_true ? ab[index] : static_field_int);
        Sum += (sfb_true ^ t1_i.mfb_true ? ab[index] : t1_i.mfi);
        Sum += (sfb_true ^ t1_i.mfb_true ? ab[index] : simple_func_int());
        Sum += (sfb_true ^ t1_i.mfb_true ? ab[index] : ab[index]);
        Sum += (sfb_true ^ t1_i.mfb_true ? ab[index] : ab[index - 1]);
        Sum += (sfb_true ^ t1_i.mfb_true ? ab[index - 1] : 3);
        Sum += (sfb_true ^ t1_i.mfb_true ? ab[index - 1] : -5);
        Sum += (sfb_true ^ t1_i.mfb_true ? ab[index - 1] : local_int);
        Sum += (sfb_true ^ t1_i.mfb_true ? ab[index - 1] : static_field_int);
        Sum += (sfb_true ^ t1_i.mfb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (sfb_true ^ t1_i.mfb_true ? ab[index - 1] : simple_func_int());
        Sum += (sfb_true ^ t1_i.mfb_true ? ab[index - 1] : ab[index]);
        Sum += (sfb_true ^ t1_i.mfb_true ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_176()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ t1_i.mfb_false ? 3 : 3);
        Sum += (sfb_true ^ t1_i.mfb_false ? 3 : -5);
        Sum += (sfb_true ^ t1_i.mfb_false ? 3 : local_int);
        Sum += (sfb_true ^ t1_i.mfb_false ? 3 : static_field_int);
        Sum += (sfb_true ^ t1_i.mfb_false ? 3 : t1_i.mfi);
        Sum += (sfb_true ^ t1_i.mfb_false ? 3 : simple_func_int());
        Sum += (sfb_true ^ t1_i.mfb_false ? 3 : ab[index]);
        Sum += (sfb_true ^ t1_i.mfb_false ? 3 : ab[index - 1]);
        Sum += (sfb_true ^ t1_i.mfb_false ? -5 : 3);
        Sum += (sfb_true ^ t1_i.mfb_false ? -5 : -5);
        Sum += (sfb_true ^ t1_i.mfb_false ? -5 : local_int);
        Sum += (sfb_true ^ t1_i.mfb_false ? -5 : static_field_int);
        Sum += (sfb_true ^ t1_i.mfb_false ? -5 : t1_i.mfi);
        Sum += (sfb_true ^ t1_i.mfb_false ? -5 : simple_func_int());
        Sum += (sfb_true ^ t1_i.mfb_false ? -5 : ab[index]);
        Sum += (sfb_true ^ t1_i.mfb_false ? -5 : ab[index - 1]);
        Sum += (sfb_true ^ t1_i.mfb_false ? local_int : 3);
        Sum += (sfb_true ^ t1_i.mfb_false ? local_int : -5);
        Sum += (sfb_true ^ t1_i.mfb_false ? local_int : local_int);
        Sum += (sfb_true ^ t1_i.mfb_false ? local_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_177()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ t1_i.mfb_false ? local_int : t1_i.mfi);
        Sum += (sfb_true ^ t1_i.mfb_false ? local_int : simple_func_int());
        Sum += (sfb_true ^ t1_i.mfb_false ? local_int : ab[index]);
        Sum += (sfb_true ^ t1_i.mfb_false ? local_int : ab[index - 1]);
        Sum += (sfb_true ^ t1_i.mfb_false ? static_field_int : 3);
        Sum += (sfb_true ^ t1_i.mfb_false ? static_field_int : -5);
        Sum += (sfb_true ^ t1_i.mfb_false ? static_field_int : local_int);
        Sum += (sfb_true ^ t1_i.mfb_false ? static_field_int : static_field_int);
        Sum += (sfb_true ^ t1_i.mfb_false ? static_field_int : t1_i.mfi);
        Sum += (sfb_true ^ t1_i.mfb_false ? static_field_int : simple_func_int());
        Sum += (sfb_true ^ t1_i.mfb_false ? static_field_int : ab[index]);
        Sum += (sfb_true ^ t1_i.mfb_false ? static_field_int : ab[index - 1]);
        Sum += (sfb_true ^ t1_i.mfb_false ? t1_i.mfi : 3);
        Sum += (sfb_true ^ t1_i.mfb_false ? t1_i.mfi : -5);
        Sum += (sfb_true ^ t1_i.mfb_false ? t1_i.mfi : local_int);
        Sum += (sfb_true ^ t1_i.mfb_false ? t1_i.mfi : static_field_int);
        Sum += (sfb_true ^ t1_i.mfb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (sfb_true ^ t1_i.mfb_false ? t1_i.mfi : simple_func_int());
        Sum += (sfb_true ^ t1_i.mfb_false ? t1_i.mfi : ab[index]);
        Sum += (sfb_true ^ t1_i.mfb_false ? t1_i.mfi : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_178()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ t1_i.mfb_false ? simple_func_int() : 3);
        Sum += (sfb_true ^ t1_i.mfb_false ? simple_func_int() : -5);
        Sum += (sfb_true ^ t1_i.mfb_false ? simple_func_int() : local_int);
        Sum += (sfb_true ^ t1_i.mfb_false ? simple_func_int() : static_field_int);
        Sum += (sfb_true ^ t1_i.mfb_false ? simple_func_int() : t1_i.mfi);
        Sum += (sfb_true ^ t1_i.mfb_false ? simple_func_int() : simple_func_int());
        Sum += (sfb_true ^ t1_i.mfb_false ? simple_func_int() : ab[index]);
        Sum += (sfb_true ^ t1_i.mfb_false ? simple_func_int() : ab[index - 1]);
        Sum += (sfb_true ^ t1_i.mfb_false ? ab[index] : 3);
        Sum += (sfb_true ^ t1_i.mfb_false ? ab[index] : -5);
        Sum += (sfb_true ^ t1_i.mfb_false ? ab[index] : local_int);
        Sum += (sfb_true ^ t1_i.mfb_false ? ab[index] : static_field_int);
        Sum += (sfb_true ^ t1_i.mfb_false ? ab[index] : t1_i.mfi);
        Sum += (sfb_true ^ t1_i.mfb_false ? ab[index] : simple_func_int());
        Sum += (sfb_true ^ t1_i.mfb_false ? ab[index] : ab[index]);
        Sum += (sfb_true ^ t1_i.mfb_false ? ab[index] : ab[index - 1]);
        Sum += (sfb_true ^ t1_i.mfb_false ? ab[index - 1] : 3);
        Sum += (sfb_true ^ t1_i.mfb_false ? ab[index - 1] : -5);
        Sum += (sfb_true ^ t1_i.mfb_false ? ab[index - 1] : local_int);
        Sum += (sfb_true ^ t1_i.mfb_false ? ab[index - 1] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_179()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ t1_i.mfb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (sfb_true ^ t1_i.mfb_false ? ab[index - 1] : simple_func_int());
        Sum += (sfb_true ^ t1_i.mfb_false ? ab[index - 1] : ab[index]);
        Sum += (sfb_true ^ t1_i.mfb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (sfb_true ^ func_sb_true() ? 3 : 3);
        Sum += (sfb_true ^ func_sb_true() ? 3 : -5);
        Sum += (sfb_true ^ func_sb_true() ? 3 : local_int);
        Sum += (sfb_true ^ func_sb_true() ? 3 : static_field_int);
        Sum += (sfb_true ^ func_sb_true() ? 3 : t1_i.mfi);
        Sum += (sfb_true ^ func_sb_true() ? 3 : simple_func_int());
        Sum += (sfb_true ^ func_sb_true() ? 3 : ab[index]);
        Sum += (sfb_true ^ func_sb_true() ? 3 : ab[index - 1]);
        Sum += (sfb_true ^ func_sb_true() ? -5 : 3);
        Sum += (sfb_true ^ func_sb_true() ? -5 : -5);
        Sum += (sfb_true ^ func_sb_true() ? -5 : local_int);
        Sum += (sfb_true ^ func_sb_true() ? -5 : static_field_int);
        Sum += (sfb_true ^ func_sb_true() ? -5 : t1_i.mfi);
        Sum += (sfb_true ^ func_sb_true() ? -5 : simple_func_int());
        Sum += (sfb_true ^ func_sb_true() ? -5 : ab[index]);
        Sum += (sfb_true ^ func_sb_true() ? -5 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_180()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ func_sb_true() ? local_int : 3);
        Sum += (sfb_true ^ func_sb_true() ? local_int : -5);
        Sum += (sfb_true ^ func_sb_true() ? local_int : local_int);
        Sum += (sfb_true ^ func_sb_true() ? local_int : static_field_int);
        Sum += (sfb_true ^ func_sb_true() ? local_int : t1_i.mfi);
        Sum += (sfb_true ^ func_sb_true() ? local_int : simple_func_int());
        Sum += (sfb_true ^ func_sb_true() ? local_int : ab[index]);
        Sum += (sfb_true ^ func_sb_true() ? local_int : ab[index - 1]);
        Sum += (sfb_true ^ func_sb_true() ? static_field_int : 3);
        Sum += (sfb_true ^ func_sb_true() ? static_field_int : -5);
        Sum += (sfb_true ^ func_sb_true() ? static_field_int : local_int);
        Sum += (sfb_true ^ func_sb_true() ? static_field_int : static_field_int);
        Sum += (sfb_true ^ func_sb_true() ? static_field_int : t1_i.mfi);
        Sum += (sfb_true ^ func_sb_true() ? static_field_int : simple_func_int());
        Sum += (sfb_true ^ func_sb_true() ? static_field_int : ab[index]);
        Sum += (sfb_true ^ func_sb_true() ? static_field_int : ab[index - 1]);
        Sum += (sfb_true ^ func_sb_true() ? t1_i.mfi : 3);
        Sum += (sfb_true ^ func_sb_true() ? t1_i.mfi : -5);
        Sum += (sfb_true ^ func_sb_true() ? t1_i.mfi : local_int);
        Sum += (sfb_true ^ func_sb_true() ? t1_i.mfi : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_181()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ func_sb_true() ? t1_i.mfi : t1_i.mfi);
        Sum += (sfb_true ^ func_sb_true() ? t1_i.mfi : simple_func_int());
        Sum += (sfb_true ^ func_sb_true() ? t1_i.mfi : ab[index]);
        Sum += (sfb_true ^ func_sb_true() ? t1_i.mfi : ab[index - 1]);
        Sum += (sfb_true ^ func_sb_true() ? simple_func_int() : 3);
        Sum += (sfb_true ^ func_sb_true() ? simple_func_int() : -5);
        Sum += (sfb_true ^ func_sb_true() ? simple_func_int() : local_int);
        Sum += (sfb_true ^ func_sb_true() ? simple_func_int() : static_field_int);
        Sum += (sfb_true ^ func_sb_true() ? simple_func_int() : t1_i.mfi);
        Sum += (sfb_true ^ func_sb_true() ? simple_func_int() : simple_func_int());
        Sum += (sfb_true ^ func_sb_true() ? simple_func_int() : ab[index]);
        Sum += (sfb_true ^ func_sb_true() ? simple_func_int() : ab[index - 1]);
        Sum += (sfb_true ^ func_sb_true() ? ab[index] : 3);
        Sum += (sfb_true ^ func_sb_true() ? ab[index] : -5);
        Sum += (sfb_true ^ func_sb_true() ? ab[index] : local_int);
        Sum += (sfb_true ^ func_sb_true() ? ab[index] : static_field_int);
        Sum += (sfb_true ^ func_sb_true() ? ab[index] : t1_i.mfi);
        Sum += (sfb_true ^ func_sb_true() ? ab[index] : simple_func_int());
        Sum += (sfb_true ^ func_sb_true() ? ab[index] : ab[index]);
        Sum += (sfb_true ^ func_sb_true() ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_182()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ func_sb_true() ? ab[index - 1] : 3);
        Sum += (sfb_true ^ func_sb_true() ? ab[index - 1] : -5);
        Sum += (sfb_true ^ func_sb_true() ? ab[index - 1] : local_int);
        Sum += (sfb_true ^ func_sb_true() ? ab[index - 1] : static_field_int);
        Sum += (sfb_true ^ func_sb_true() ? ab[index - 1] : t1_i.mfi);
        Sum += (sfb_true ^ func_sb_true() ? ab[index - 1] : simple_func_int());
        Sum += (sfb_true ^ func_sb_true() ? ab[index - 1] : ab[index]);
        Sum += (sfb_true ^ func_sb_true() ? ab[index - 1] : ab[index - 1]);
        Sum += (sfb_true ^ func_sb_false() ? 3 : 3);
        Sum += (sfb_true ^ func_sb_false() ? 3 : -5);
        Sum += (sfb_true ^ func_sb_false() ? 3 : local_int);
        Sum += (sfb_true ^ func_sb_false() ? 3 : static_field_int);
        Sum += (sfb_true ^ func_sb_false() ? 3 : t1_i.mfi);
        Sum += (sfb_true ^ func_sb_false() ? 3 : simple_func_int());
        Sum += (sfb_true ^ func_sb_false() ? 3 : ab[index]);
        Sum += (sfb_true ^ func_sb_false() ? 3 : ab[index - 1]);
        Sum += (sfb_true ^ func_sb_false() ? -5 : 3);
        Sum += (sfb_true ^ func_sb_false() ? -5 : -5);
        Sum += (sfb_true ^ func_sb_false() ? -5 : local_int);
        Sum += (sfb_true ^ func_sb_false() ? -5 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_183()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ func_sb_false() ? -5 : t1_i.mfi);
        Sum += (sfb_true ^ func_sb_false() ? -5 : simple_func_int());
        Sum += (sfb_true ^ func_sb_false() ? -5 : ab[index]);
        Sum += (sfb_true ^ func_sb_false() ? -5 : ab[index - 1]);
        Sum += (sfb_true ^ func_sb_false() ? local_int : 3);
        Sum += (sfb_true ^ func_sb_false() ? local_int : -5);
        Sum += (sfb_true ^ func_sb_false() ? local_int : local_int);
        Sum += (sfb_true ^ func_sb_false() ? local_int : static_field_int);
        Sum += (sfb_true ^ func_sb_false() ? local_int : t1_i.mfi);
        Sum += (sfb_true ^ func_sb_false() ? local_int : simple_func_int());
        Sum += (sfb_true ^ func_sb_false() ? local_int : ab[index]);
        Sum += (sfb_true ^ func_sb_false() ? local_int : ab[index - 1]);
        Sum += (sfb_true ^ func_sb_false() ? static_field_int : 3);
        Sum += (sfb_true ^ func_sb_false() ? static_field_int : -5);
        Sum += (sfb_true ^ func_sb_false() ? static_field_int : local_int);
        Sum += (sfb_true ^ func_sb_false() ? static_field_int : static_field_int);
        Sum += (sfb_true ^ func_sb_false() ? static_field_int : t1_i.mfi);
        Sum += (sfb_true ^ func_sb_false() ? static_field_int : simple_func_int());
        Sum += (sfb_true ^ func_sb_false() ? static_field_int : ab[index]);
        Sum += (sfb_true ^ func_sb_false() ? static_field_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_184()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ func_sb_false() ? t1_i.mfi : 3);
        Sum += (sfb_true ^ func_sb_false() ? t1_i.mfi : -5);
        Sum += (sfb_true ^ func_sb_false() ? t1_i.mfi : local_int);
        Sum += (sfb_true ^ func_sb_false() ? t1_i.mfi : static_field_int);
        Sum += (sfb_true ^ func_sb_false() ? t1_i.mfi : t1_i.mfi);
        Sum += (sfb_true ^ func_sb_false() ? t1_i.mfi : simple_func_int());
        Sum += (sfb_true ^ func_sb_false() ? t1_i.mfi : ab[index]);
        Sum += (sfb_true ^ func_sb_false() ? t1_i.mfi : ab[index - 1]);
        Sum += (sfb_true ^ func_sb_false() ? simple_func_int() : 3);
        Sum += (sfb_true ^ func_sb_false() ? simple_func_int() : -5);
        Sum += (sfb_true ^ func_sb_false() ? simple_func_int() : local_int);
        Sum += (sfb_true ^ func_sb_false() ? simple_func_int() : static_field_int);
        Sum += (sfb_true ^ func_sb_false() ? simple_func_int() : t1_i.mfi);
        Sum += (sfb_true ^ func_sb_false() ? simple_func_int() : simple_func_int());
        Sum += (sfb_true ^ func_sb_false() ? simple_func_int() : ab[index]);
        Sum += (sfb_true ^ func_sb_false() ? simple_func_int() : ab[index - 1]);
        Sum += (sfb_true ^ func_sb_false() ? ab[index] : 3);
        Sum += (sfb_true ^ func_sb_false() ? ab[index] : -5);
        Sum += (sfb_true ^ func_sb_false() ? ab[index] : local_int);
        Sum += (sfb_true ^ func_sb_false() ? ab[index] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_185()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ func_sb_false() ? ab[index] : t1_i.mfi);
        Sum += (sfb_true ^ func_sb_false() ? ab[index] : simple_func_int());
        Sum += (sfb_true ^ func_sb_false() ? ab[index] : ab[index]);
        Sum += (sfb_true ^ func_sb_false() ? ab[index] : ab[index - 1]);
        Sum += (sfb_true ^ func_sb_false() ? ab[index - 1] : 3);
        Sum += (sfb_true ^ func_sb_false() ? ab[index - 1] : -5);
        Sum += (sfb_true ^ func_sb_false() ? ab[index - 1] : local_int);
        Sum += (sfb_true ^ func_sb_false() ? ab[index - 1] : static_field_int);
        Sum += (sfb_true ^ func_sb_false() ? ab[index - 1] : t1_i.mfi);
        Sum += (sfb_true ^ func_sb_false() ? ab[index - 1] : simple_func_int());
        Sum += (sfb_true ^ func_sb_false() ? ab[index - 1] : ab[index]);
        Sum += (sfb_true ^ func_sb_false() ? ab[index - 1] : ab[index - 1]);
        Sum += (sfb_true ^ ab_true[index] ? 3 : 3);
        Sum += (sfb_true ^ ab_true[index] ? 3 : -5);
        Sum += (sfb_true ^ ab_true[index] ? 3 : local_int);
        Sum += (sfb_true ^ ab_true[index] ? 3 : static_field_int);
        Sum += (sfb_true ^ ab_true[index] ? 3 : t1_i.mfi);
        Sum += (sfb_true ^ ab_true[index] ? 3 : simple_func_int());
        Sum += (sfb_true ^ ab_true[index] ? 3 : ab[index]);
        Sum += (sfb_true ^ ab_true[index] ? 3 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_186()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ ab_true[index] ? -5 : 3);
        Sum += (sfb_true ^ ab_true[index] ? -5 : -5);
        Sum += (sfb_true ^ ab_true[index] ? -5 : local_int);
        Sum += (sfb_true ^ ab_true[index] ? -5 : static_field_int);
        Sum += (sfb_true ^ ab_true[index] ? -5 : t1_i.mfi);
        Sum += (sfb_true ^ ab_true[index] ? -5 : simple_func_int());
        Sum += (sfb_true ^ ab_true[index] ? -5 : ab[index]);
        Sum += (sfb_true ^ ab_true[index] ? -5 : ab[index - 1]);
        Sum += (sfb_true ^ ab_true[index] ? local_int : 3);
        Sum += (sfb_true ^ ab_true[index] ? local_int : -5);
        Sum += (sfb_true ^ ab_true[index] ? local_int : local_int);
        Sum += (sfb_true ^ ab_true[index] ? local_int : static_field_int);
        Sum += (sfb_true ^ ab_true[index] ? local_int : t1_i.mfi);
        Sum += (sfb_true ^ ab_true[index] ? local_int : simple_func_int());
        Sum += (sfb_true ^ ab_true[index] ? local_int : ab[index]);
        Sum += (sfb_true ^ ab_true[index] ? local_int : ab[index - 1]);
        Sum += (sfb_true ^ ab_true[index] ? static_field_int : 3);
        Sum += (sfb_true ^ ab_true[index] ? static_field_int : -5);
        Sum += (sfb_true ^ ab_true[index] ? static_field_int : local_int);
        Sum += (sfb_true ^ ab_true[index] ? static_field_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_187()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ ab_true[index] ? static_field_int : t1_i.mfi);
        Sum += (sfb_true ^ ab_true[index] ? static_field_int : simple_func_int());
        Sum += (sfb_true ^ ab_true[index] ? static_field_int : ab[index]);
        Sum += (sfb_true ^ ab_true[index] ? static_field_int : ab[index - 1]);
        Sum += (sfb_true ^ ab_true[index] ? t1_i.mfi : 3);
        Sum += (sfb_true ^ ab_true[index] ? t1_i.mfi : -5);
        Sum += (sfb_true ^ ab_true[index] ? t1_i.mfi : local_int);
        Sum += (sfb_true ^ ab_true[index] ? t1_i.mfi : static_field_int);
        Sum += (sfb_true ^ ab_true[index] ? t1_i.mfi : t1_i.mfi);
        Sum += (sfb_true ^ ab_true[index] ? t1_i.mfi : simple_func_int());
        Sum += (sfb_true ^ ab_true[index] ? t1_i.mfi : ab[index]);
        Sum += (sfb_true ^ ab_true[index] ? t1_i.mfi : ab[index - 1]);
        Sum += (sfb_true ^ ab_true[index] ? simple_func_int() : 3);
        Sum += (sfb_true ^ ab_true[index] ? simple_func_int() : -5);
        Sum += (sfb_true ^ ab_true[index] ? simple_func_int() : local_int);
        Sum += (sfb_true ^ ab_true[index] ? simple_func_int() : static_field_int);
        Sum += (sfb_true ^ ab_true[index] ? simple_func_int() : t1_i.mfi);
        Sum += (sfb_true ^ ab_true[index] ? simple_func_int() : simple_func_int());
        Sum += (sfb_true ^ ab_true[index] ? simple_func_int() : ab[index]);
        Sum += (sfb_true ^ ab_true[index] ? simple_func_int() : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_188()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ ab_true[index] ? ab[index] : 3);
        Sum += (sfb_true ^ ab_true[index] ? ab[index] : -5);
        Sum += (sfb_true ^ ab_true[index] ? ab[index] : local_int);
        Sum += (sfb_true ^ ab_true[index] ? ab[index] : static_field_int);
        Sum += (sfb_true ^ ab_true[index] ? ab[index] : t1_i.mfi);
        Sum += (sfb_true ^ ab_true[index] ? ab[index] : simple_func_int());
        Sum += (sfb_true ^ ab_true[index] ? ab[index] : ab[index]);
        Sum += (sfb_true ^ ab_true[index] ? ab[index] : ab[index - 1]);
        Sum += (sfb_true ^ ab_true[index] ? ab[index - 1] : 3);
        Sum += (sfb_true ^ ab_true[index] ? ab[index - 1] : -5);
        Sum += (sfb_true ^ ab_true[index] ? ab[index - 1] : local_int);
        Sum += (sfb_true ^ ab_true[index] ? ab[index - 1] : static_field_int);
        Sum += (sfb_true ^ ab_true[index] ? ab[index - 1] : t1_i.mfi);
        Sum += (sfb_true ^ ab_true[index] ? ab[index - 1] : simple_func_int());
        Sum += (sfb_true ^ ab_true[index] ? ab[index - 1] : ab[index]);
        Sum += (sfb_true ^ ab_true[index] ? ab[index - 1] : ab[index - 1]);
        Sum += (sfb_true ^ ab_false[index] ? 3 : 3);
        Sum += (sfb_true ^ ab_false[index] ? 3 : -5);
        Sum += (sfb_true ^ ab_false[index] ? 3 : local_int);
        Sum += (sfb_true ^ ab_false[index] ? 3 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_189()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ ab_false[index] ? 3 : t1_i.mfi);
        Sum += (sfb_true ^ ab_false[index] ? 3 : simple_func_int());
        Sum += (sfb_true ^ ab_false[index] ? 3 : ab[index]);
        Sum += (sfb_true ^ ab_false[index] ? 3 : ab[index - 1]);
        Sum += (sfb_true ^ ab_false[index] ? -5 : 3);
        Sum += (sfb_true ^ ab_false[index] ? -5 : -5);
        Sum += (sfb_true ^ ab_false[index] ? -5 : local_int);
        Sum += (sfb_true ^ ab_false[index] ? -5 : static_field_int);
        Sum += (sfb_true ^ ab_false[index] ? -5 : t1_i.mfi);
        Sum += (sfb_true ^ ab_false[index] ? -5 : simple_func_int());
        Sum += (sfb_true ^ ab_false[index] ? -5 : ab[index]);
        Sum += (sfb_true ^ ab_false[index] ? -5 : ab[index - 1]);
        Sum += (sfb_true ^ ab_false[index] ? local_int : 3);
        Sum += (sfb_true ^ ab_false[index] ? local_int : -5);
        Sum += (sfb_true ^ ab_false[index] ? local_int : local_int);
        Sum += (sfb_true ^ ab_false[index] ? local_int : static_field_int);
        Sum += (sfb_true ^ ab_false[index] ? local_int : t1_i.mfi);
        Sum += (sfb_true ^ ab_false[index] ? local_int : simple_func_int());
        Sum += (sfb_true ^ ab_false[index] ? local_int : ab[index]);
        Sum += (sfb_true ^ ab_false[index] ? local_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_190()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ ab_false[index] ? static_field_int : 3);
        Sum += (sfb_true ^ ab_false[index] ? static_field_int : -5);
        Sum += (sfb_true ^ ab_false[index] ? static_field_int : local_int);
        Sum += (sfb_true ^ ab_false[index] ? static_field_int : static_field_int);
        Sum += (sfb_true ^ ab_false[index] ? static_field_int : t1_i.mfi);
        Sum += (sfb_true ^ ab_false[index] ? static_field_int : simple_func_int());
        Sum += (sfb_true ^ ab_false[index] ? static_field_int : ab[index]);
        Sum += (sfb_true ^ ab_false[index] ? static_field_int : ab[index - 1]);
        Sum += (sfb_true ^ ab_false[index] ? t1_i.mfi : 3);
        Sum += (sfb_true ^ ab_false[index] ? t1_i.mfi : -5);
        Sum += (sfb_true ^ ab_false[index] ? t1_i.mfi : local_int);
        Sum += (sfb_true ^ ab_false[index] ? t1_i.mfi : static_field_int);
        Sum += (sfb_true ^ ab_false[index] ? t1_i.mfi : t1_i.mfi);
        Sum += (sfb_true ^ ab_false[index] ? t1_i.mfi : simple_func_int());
        Sum += (sfb_true ^ ab_false[index] ? t1_i.mfi : ab[index]);
        Sum += (sfb_true ^ ab_false[index] ? t1_i.mfi : ab[index - 1]);
        Sum += (sfb_true ^ ab_false[index] ? simple_func_int() : 3);
        Sum += (sfb_true ^ ab_false[index] ? simple_func_int() : -5);
        Sum += (sfb_true ^ ab_false[index] ? simple_func_int() : local_int);
        Sum += (sfb_true ^ ab_false[index] ? simple_func_int() : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_191()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ^ ab_false[index] ? simple_func_int() : t1_i.mfi);
        Sum += (sfb_true ^ ab_false[index] ? simple_func_int() : simple_func_int());
        Sum += (sfb_true ^ ab_false[index] ? simple_func_int() : ab[index]);
        Sum += (sfb_true ^ ab_false[index] ? simple_func_int() : ab[index - 1]);
        Sum += (sfb_true ^ ab_false[index] ? ab[index] : 3);
        Sum += (sfb_true ^ ab_false[index] ? ab[index] : -5);
        Sum += (sfb_true ^ ab_false[index] ? ab[index] : local_int);
        Sum += (sfb_true ^ ab_false[index] ? ab[index] : static_field_int);
        Sum += (sfb_true ^ ab_false[index] ? ab[index] : t1_i.mfi);
        Sum += (sfb_true ^ ab_false[index] ? ab[index] : simple_func_int());
        Sum += (sfb_true ^ ab_false[index] ? ab[index] : ab[index]);
        Sum += (sfb_true ^ ab_false[index] ? ab[index] : ab[index - 1]);
        Sum += (sfb_true ^ ab_false[index] ? ab[index - 1] : 3);
        Sum += (sfb_true ^ ab_false[index] ? ab[index - 1] : -5);
        Sum += (sfb_true ^ ab_false[index] ? ab[index - 1] : local_int);
        Sum += (sfb_true ^ ab_false[index] ? ab[index - 1] : static_field_int);
        Sum += (sfb_true ^ ab_false[index] ? ab[index - 1] : t1_i.mfi);
        Sum += (sfb_true ^ ab_false[index] ? ab[index - 1] : simple_func_int());
        Sum += (sfb_true ^ ab_false[index] ? ab[index - 1] : ab[index]);
        Sum += (sfb_true ^ ab_false[index] ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_192()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ true ? 3 : 3);
        Sum += (sfb_false ^ true ? 3 : -5);
        Sum += (sfb_false ^ true ? 3 : local_int);
        Sum += (sfb_false ^ true ? 3 : static_field_int);
        Sum += (sfb_false ^ true ? 3 : t1_i.mfi);
        Sum += (sfb_false ^ true ? 3 : simple_func_int());
        Sum += (sfb_false ^ true ? 3 : ab[index]);
        Sum += (sfb_false ^ true ? 3 : ab[index - 1]);
        Sum += (sfb_false ^ true ? -5 : 3);
        Sum += (sfb_false ^ true ? -5 : -5);
        Sum += (sfb_false ^ true ? -5 : local_int);
        Sum += (sfb_false ^ true ? -5 : static_field_int);
        Sum += (sfb_false ^ true ? -5 : t1_i.mfi);
        Sum += (sfb_false ^ true ? -5 : simple_func_int());
        Sum += (sfb_false ^ true ? -5 : ab[index]);
        Sum += (sfb_false ^ true ? -5 : ab[index - 1]);
        Sum += (sfb_false ^ true ? local_int : 3);
        Sum += (sfb_false ^ true ? local_int : -5);
        Sum += (sfb_false ^ true ? local_int : local_int);
        Sum += (sfb_false ^ true ? local_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_193()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ true ? local_int : t1_i.mfi);
        Sum += (sfb_false ^ true ? local_int : simple_func_int());
        Sum += (sfb_false ^ true ? local_int : ab[index]);
        Sum += (sfb_false ^ true ? local_int : ab[index - 1]);
        Sum += (sfb_false ^ true ? static_field_int : 3);
        Sum += (sfb_false ^ true ? static_field_int : -5);
        Sum += (sfb_false ^ true ? static_field_int : local_int);
        Sum += (sfb_false ^ true ? static_field_int : static_field_int);
        Sum += (sfb_false ^ true ? static_field_int : t1_i.mfi);
        Sum += (sfb_false ^ true ? static_field_int : simple_func_int());
        Sum += (sfb_false ^ true ? static_field_int : ab[index]);
        Sum += (sfb_false ^ true ? static_field_int : ab[index - 1]);
        Sum += (sfb_false ^ true ? t1_i.mfi : 3);
        Sum += (sfb_false ^ true ? t1_i.mfi : -5);
        Sum += (sfb_false ^ true ? t1_i.mfi : local_int);
        Sum += (sfb_false ^ true ? t1_i.mfi : static_field_int);
        Sum += (sfb_false ^ true ? t1_i.mfi : t1_i.mfi);
        Sum += (sfb_false ^ true ? t1_i.mfi : simple_func_int());
        Sum += (sfb_false ^ true ? t1_i.mfi : ab[index]);
        Sum += (sfb_false ^ true ? t1_i.mfi : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_194()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ true ? simple_func_int() : 3);
        Sum += (sfb_false ^ true ? simple_func_int() : -5);
        Sum += (sfb_false ^ true ? simple_func_int() : local_int);
        Sum += (sfb_false ^ true ? simple_func_int() : static_field_int);
        Sum += (sfb_false ^ true ? simple_func_int() : t1_i.mfi);
        Sum += (sfb_false ^ true ? simple_func_int() : simple_func_int());
        Sum += (sfb_false ^ true ? simple_func_int() : ab[index]);
        Sum += (sfb_false ^ true ? simple_func_int() : ab[index - 1]);
        Sum += (sfb_false ^ true ? ab[index] : 3);
        Sum += (sfb_false ^ true ? ab[index] : -5);
        Sum += (sfb_false ^ true ? ab[index] : local_int);
        Sum += (sfb_false ^ true ? ab[index] : static_field_int);
        Sum += (sfb_false ^ true ? ab[index] : t1_i.mfi);
        Sum += (sfb_false ^ true ? ab[index] : simple_func_int());
        Sum += (sfb_false ^ true ? ab[index] : ab[index]);
        Sum += (sfb_false ^ true ? ab[index] : ab[index - 1]);
        Sum += (sfb_false ^ true ? ab[index - 1] : 3);
        Sum += (sfb_false ^ true ? ab[index - 1] : -5);
        Sum += (sfb_false ^ true ? ab[index - 1] : local_int);
        Sum += (sfb_false ^ true ? ab[index - 1] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_195()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ true ? ab[index - 1] : t1_i.mfi);
        Sum += (sfb_false ^ true ? ab[index - 1] : simple_func_int());
        Sum += (sfb_false ^ true ? ab[index - 1] : ab[index]);
        Sum += (sfb_false ^ true ? ab[index - 1] : ab[index - 1]);
        Sum += (sfb_false ^ false ? 3 : 3);
        Sum += (sfb_false ^ false ? 3 : -5);
        Sum += (sfb_false ^ false ? 3 : local_int);
        Sum += (sfb_false ^ false ? 3 : static_field_int);
        Sum += (sfb_false ^ false ? 3 : t1_i.mfi);
        Sum += (sfb_false ^ false ? 3 : simple_func_int());
        Sum += (sfb_false ^ false ? 3 : ab[index]);
        Sum += (sfb_false ^ false ? 3 : ab[index - 1]);
        Sum += (sfb_false ^ false ? -5 : 3);
        Sum += (sfb_false ^ false ? -5 : -5);
        Sum += (sfb_false ^ false ? -5 : local_int);
        Sum += (sfb_false ^ false ? -5 : static_field_int);
        Sum += (sfb_false ^ false ? -5 : t1_i.mfi);
        Sum += (sfb_false ^ false ? -5 : simple_func_int());
        Sum += (sfb_false ^ false ? -5 : ab[index]);
        Sum += (sfb_false ^ false ? -5 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_196()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ false ? local_int : 3);
        Sum += (sfb_false ^ false ? local_int : -5);
        Sum += (sfb_false ^ false ? local_int : local_int);
        Sum += (sfb_false ^ false ? local_int : static_field_int);
        Sum += (sfb_false ^ false ? local_int : t1_i.mfi);
        Sum += (sfb_false ^ false ? local_int : simple_func_int());
        Sum += (sfb_false ^ false ? local_int : ab[index]);
        Sum += (sfb_false ^ false ? local_int : ab[index - 1]);
        Sum += (sfb_false ^ false ? static_field_int : 3);
        Sum += (sfb_false ^ false ? static_field_int : -5);
        Sum += (sfb_false ^ false ? static_field_int : local_int);
        Sum += (sfb_false ^ false ? static_field_int : static_field_int);
        Sum += (sfb_false ^ false ? static_field_int : t1_i.mfi);
        Sum += (sfb_false ^ false ? static_field_int : simple_func_int());
        Sum += (sfb_false ^ false ? static_field_int : ab[index]);
        Sum += (sfb_false ^ false ? static_field_int : ab[index - 1]);
        Sum += (sfb_false ^ false ? t1_i.mfi : 3);
        Sum += (sfb_false ^ false ? t1_i.mfi : -5);
        Sum += (sfb_false ^ false ? t1_i.mfi : local_int);
        Sum += (sfb_false ^ false ? t1_i.mfi : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_197()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ false ? t1_i.mfi : t1_i.mfi);
        Sum += (sfb_false ^ false ? t1_i.mfi : simple_func_int());
        Sum += (sfb_false ^ false ? t1_i.mfi : ab[index]);
        Sum += (sfb_false ^ false ? t1_i.mfi : ab[index - 1]);
        Sum += (sfb_false ^ false ? simple_func_int() : 3);
        Sum += (sfb_false ^ false ? simple_func_int() : -5);
        Sum += (sfb_false ^ false ? simple_func_int() : local_int);
        Sum += (sfb_false ^ false ? simple_func_int() : static_field_int);
        Sum += (sfb_false ^ false ? simple_func_int() : t1_i.mfi);
        Sum += (sfb_false ^ false ? simple_func_int() : simple_func_int());
        Sum += (sfb_false ^ false ? simple_func_int() : ab[index]);
        Sum += (sfb_false ^ false ? simple_func_int() : ab[index - 1]);
        Sum += (sfb_false ^ false ? ab[index] : 3);
        Sum += (sfb_false ^ false ? ab[index] : -5);
        Sum += (sfb_false ^ false ? ab[index] : local_int);
        Sum += (sfb_false ^ false ? ab[index] : static_field_int);
        Sum += (sfb_false ^ false ? ab[index] : t1_i.mfi);
        Sum += (sfb_false ^ false ? ab[index] : simple_func_int());
        Sum += (sfb_false ^ false ? ab[index] : ab[index]);
        Sum += (sfb_false ^ false ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_198()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ false ? ab[index - 1] : 3);
        Sum += (sfb_false ^ false ? ab[index - 1] : -5);
        Sum += (sfb_false ^ false ? ab[index - 1] : local_int);
        Sum += (sfb_false ^ false ? ab[index - 1] : static_field_int);
        Sum += (sfb_false ^ false ? ab[index - 1] : t1_i.mfi);
        Sum += (sfb_false ^ false ? ab[index - 1] : simple_func_int());
        Sum += (sfb_false ^ false ? ab[index - 1] : ab[index]);
        Sum += (sfb_false ^ false ? ab[index - 1] : ab[index - 1]);
        Sum += (sfb_false ^ lb_true ? 3 : 3);
        Sum += (sfb_false ^ lb_true ? 3 : -5);
        Sum += (sfb_false ^ lb_true ? 3 : local_int);
        Sum += (sfb_false ^ lb_true ? 3 : static_field_int);
        Sum += (sfb_false ^ lb_true ? 3 : t1_i.mfi);
        Sum += (sfb_false ^ lb_true ? 3 : simple_func_int());
        Sum += (sfb_false ^ lb_true ? 3 : ab[index]);
        Sum += (sfb_false ^ lb_true ? 3 : ab[index - 1]);
        Sum += (sfb_false ^ lb_true ? -5 : 3);
        Sum += (sfb_false ^ lb_true ? -5 : -5);
        Sum += (sfb_false ^ lb_true ? -5 : local_int);
        Sum += (sfb_false ^ lb_true ? -5 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_199()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ lb_true ? -5 : t1_i.mfi);
        Sum += (sfb_false ^ lb_true ? -5 : simple_func_int());
        Sum += (sfb_false ^ lb_true ? -5 : ab[index]);
        Sum += (sfb_false ^ lb_true ? -5 : ab[index - 1]);
        Sum += (sfb_false ^ lb_true ? local_int : 3);
        Sum += (sfb_false ^ lb_true ? local_int : -5);
        Sum += (sfb_false ^ lb_true ? local_int : local_int);
        Sum += (sfb_false ^ lb_true ? local_int : static_field_int);
        Sum += (sfb_false ^ lb_true ? local_int : t1_i.mfi);
        Sum += (sfb_false ^ lb_true ? local_int : simple_func_int());
        Sum += (sfb_false ^ lb_true ? local_int : ab[index]);
        Sum += (sfb_false ^ lb_true ? local_int : ab[index - 1]);
        Sum += (sfb_false ^ lb_true ? static_field_int : 3);
        Sum += (sfb_false ^ lb_true ? static_field_int : -5);
        Sum += (sfb_false ^ lb_true ? static_field_int : local_int);
        Sum += (sfb_false ^ lb_true ? static_field_int : static_field_int);
        Sum += (sfb_false ^ lb_true ? static_field_int : t1_i.mfi);
        Sum += (sfb_false ^ lb_true ? static_field_int : simple_func_int());
        Sum += (sfb_false ^ lb_true ? static_field_int : ab[index]);
        Sum += (sfb_false ^ lb_true ? static_field_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_200()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ lb_true ? t1_i.mfi : 3);
        Sum += (sfb_false ^ lb_true ? t1_i.mfi : -5);
        Sum += (sfb_false ^ lb_true ? t1_i.mfi : local_int);
        Sum += (sfb_false ^ lb_true ? t1_i.mfi : static_field_int);
        Sum += (sfb_false ^ lb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (sfb_false ^ lb_true ? t1_i.mfi : simple_func_int());
        Sum += (sfb_false ^ lb_true ? t1_i.mfi : ab[index]);
        Sum += (sfb_false ^ lb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (sfb_false ^ lb_true ? simple_func_int() : 3);
        Sum += (sfb_false ^ lb_true ? simple_func_int() : -5);
        Sum += (sfb_false ^ lb_true ? simple_func_int() : local_int);
        Sum += (sfb_false ^ lb_true ? simple_func_int() : static_field_int);
        Sum += (sfb_false ^ lb_true ? simple_func_int() : t1_i.mfi);
        Sum += (sfb_false ^ lb_true ? simple_func_int() : simple_func_int());
        Sum += (sfb_false ^ lb_true ? simple_func_int() : ab[index]);
        Sum += (sfb_false ^ lb_true ? simple_func_int() : ab[index - 1]);
        Sum += (sfb_false ^ lb_true ? ab[index] : 3);
        Sum += (sfb_false ^ lb_true ? ab[index] : -5);
        Sum += (sfb_false ^ lb_true ? ab[index] : local_int);
        Sum += (sfb_false ^ lb_true ? ab[index] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_201()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ lb_true ? ab[index] : t1_i.mfi);
        Sum += (sfb_false ^ lb_true ? ab[index] : simple_func_int());
        Sum += (sfb_false ^ lb_true ? ab[index] : ab[index]);
        Sum += (sfb_false ^ lb_true ? ab[index] : ab[index - 1]);
        Sum += (sfb_false ^ lb_true ? ab[index - 1] : 3);
        Sum += (sfb_false ^ lb_true ? ab[index - 1] : -5);
        Sum += (sfb_false ^ lb_true ? ab[index - 1] : local_int);
        Sum += (sfb_false ^ lb_true ? ab[index - 1] : static_field_int);
        Sum += (sfb_false ^ lb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (sfb_false ^ lb_true ? ab[index - 1] : simple_func_int());
        Sum += (sfb_false ^ lb_true ? ab[index - 1] : ab[index]);
        Sum += (sfb_false ^ lb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (sfb_false ^ lb_false ? 3 : 3);
        Sum += (sfb_false ^ lb_false ? 3 : -5);
        Sum += (sfb_false ^ lb_false ? 3 : local_int);
        Sum += (sfb_false ^ lb_false ? 3 : static_field_int);
        Sum += (sfb_false ^ lb_false ? 3 : t1_i.mfi);
        Sum += (sfb_false ^ lb_false ? 3 : simple_func_int());
        Sum += (sfb_false ^ lb_false ? 3 : ab[index]);
        Sum += (sfb_false ^ lb_false ? 3 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_202()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ lb_false ? -5 : 3);
        Sum += (sfb_false ^ lb_false ? -5 : -5);
        Sum += (sfb_false ^ lb_false ? -5 : local_int);
        Sum += (sfb_false ^ lb_false ? -5 : static_field_int);
        Sum += (sfb_false ^ lb_false ? -5 : t1_i.mfi);
        Sum += (sfb_false ^ lb_false ? -5 : simple_func_int());
        Sum += (sfb_false ^ lb_false ? -5 : ab[index]);
        Sum += (sfb_false ^ lb_false ? -5 : ab[index - 1]);
        Sum += (sfb_false ^ lb_false ? local_int : 3);
        Sum += (sfb_false ^ lb_false ? local_int : -5);
        Sum += (sfb_false ^ lb_false ? local_int : local_int);
        Sum += (sfb_false ^ lb_false ? local_int : static_field_int);
        Sum += (sfb_false ^ lb_false ? local_int : t1_i.mfi);
        Sum += (sfb_false ^ lb_false ? local_int : simple_func_int());
        Sum += (sfb_false ^ lb_false ? local_int : ab[index]);
        Sum += (sfb_false ^ lb_false ? local_int : ab[index - 1]);
        Sum += (sfb_false ^ lb_false ? static_field_int : 3);
        Sum += (sfb_false ^ lb_false ? static_field_int : -5);
        Sum += (sfb_false ^ lb_false ? static_field_int : local_int);
        Sum += (sfb_false ^ lb_false ? static_field_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_203()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ lb_false ? static_field_int : t1_i.mfi);
        Sum += (sfb_false ^ lb_false ? static_field_int : simple_func_int());
        Sum += (sfb_false ^ lb_false ? static_field_int : ab[index]);
        Sum += (sfb_false ^ lb_false ? static_field_int : ab[index - 1]);
        Sum += (sfb_false ^ lb_false ? t1_i.mfi : 3);
        Sum += (sfb_false ^ lb_false ? t1_i.mfi : -5);
        Sum += (sfb_false ^ lb_false ? t1_i.mfi : local_int);
        Sum += (sfb_false ^ lb_false ? t1_i.mfi : static_field_int);
        Sum += (sfb_false ^ lb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (sfb_false ^ lb_false ? t1_i.mfi : simple_func_int());
        Sum += (sfb_false ^ lb_false ? t1_i.mfi : ab[index]);
        Sum += (sfb_false ^ lb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (sfb_false ^ lb_false ? simple_func_int() : 3);
        Sum += (sfb_false ^ lb_false ? simple_func_int() : -5);
        Sum += (sfb_false ^ lb_false ? simple_func_int() : local_int);
        Sum += (sfb_false ^ lb_false ? simple_func_int() : static_field_int);
        Sum += (sfb_false ^ lb_false ? simple_func_int() : t1_i.mfi);
        Sum += (sfb_false ^ lb_false ? simple_func_int() : simple_func_int());
        Sum += (sfb_false ^ lb_false ? simple_func_int() : ab[index]);
        Sum += (sfb_false ^ lb_false ? simple_func_int() : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_204()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ lb_false ? ab[index] : 3);
        Sum += (sfb_false ^ lb_false ? ab[index] : -5);
        Sum += (sfb_false ^ lb_false ? ab[index] : local_int);
        Sum += (sfb_false ^ lb_false ? ab[index] : static_field_int);
        Sum += (sfb_false ^ lb_false ? ab[index] : t1_i.mfi);
        Sum += (sfb_false ^ lb_false ? ab[index] : simple_func_int());
        Sum += (sfb_false ^ lb_false ? ab[index] : ab[index]);
        Sum += (sfb_false ^ lb_false ? ab[index] : ab[index - 1]);
        Sum += (sfb_false ^ lb_false ? ab[index - 1] : 3);
        Sum += (sfb_false ^ lb_false ? ab[index - 1] : -5);
        Sum += (sfb_false ^ lb_false ? ab[index - 1] : local_int);
        Sum += (sfb_false ^ lb_false ? ab[index - 1] : static_field_int);
        Sum += (sfb_false ^ lb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (sfb_false ^ lb_false ? ab[index - 1] : simple_func_int());
        Sum += (sfb_false ^ lb_false ? ab[index - 1] : ab[index]);
        Sum += (sfb_false ^ lb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (sfb_false ^ sfb_true ? 3 : 3);
        Sum += (sfb_false ^ sfb_true ? 3 : -5);
        Sum += (sfb_false ^ sfb_true ? 3 : local_int);
        Sum += (sfb_false ^ sfb_true ? 3 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_205()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ sfb_true ? 3 : t1_i.mfi);
        Sum += (sfb_false ^ sfb_true ? 3 : simple_func_int());
        Sum += (sfb_false ^ sfb_true ? 3 : ab[index]);
        Sum += (sfb_false ^ sfb_true ? 3 : ab[index - 1]);
        Sum += (sfb_false ^ sfb_true ? -5 : 3);
        Sum += (sfb_false ^ sfb_true ? -5 : -5);
        Sum += (sfb_false ^ sfb_true ? -5 : local_int);
        Sum += (sfb_false ^ sfb_true ? -5 : static_field_int);
        Sum += (sfb_false ^ sfb_true ? -5 : t1_i.mfi);
        Sum += (sfb_false ^ sfb_true ? -5 : simple_func_int());
        Sum += (sfb_false ^ sfb_true ? -5 : ab[index]);
        Sum += (sfb_false ^ sfb_true ? -5 : ab[index - 1]);
        Sum += (sfb_false ^ sfb_true ? local_int : 3);
        Sum += (sfb_false ^ sfb_true ? local_int : -5);
        Sum += (sfb_false ^ sfb_true ? local_int : local_int);
        Sum += (sfb_false ^ sfb_true ? local_int : static_field_int);
        Sum += (sfb_false ^ sfb_true ? local_int : t1_i.mfi);
        Sum += (sfb_false ^ sfb_true ? local_int : simple_func_int());
        Sum += (sfb_false ^ sfb_true ? local_int : ab[index]);
        Sum += (sfb_false ^ sfb_true ? local_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_206()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ sfb_true ? static_field_int : 3);
        Sum += (sfb_false ^ sfb_true ? static_field_int : -5);
        Sum += (sfb_false ^ sfb_true ? static_field_int : local_int);
        Sum += (sfb_false ^ sfb_true ? static_field_int : static_field_int);
        Sum += (sfb_false ^ sfb_true ? static_field_int : t1_i.mfi);
        Sum += (sfb_false ^ sfb_true ? static_field_int : simple_func_int());
        Sum += (sfb_false ^ sfb_true ? static_field_int : ab[index]);
        Sum += (sfb_false ^ sfb_true ? static_field_int : ab[index - 1]);
        Sum += (sfb_false ^ sfb_true ? t1_i.mfi : 3);
        Sum += (sfb_false ^ sfb_true ? t1_i.mfi : -5);
        Sum += (sfb_false ^ sfb_true ? t1_i.mfi : local_int);
        Sum += (sfb_false ^ sfb_true ? t1_i.mfi : static_field_int);
        Sum += (sfb_false ^ sfb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (sfb_false ^ sfb_true ? t1_i.mfi : simple_func_int());
        Sum += (sfb_false ^ sfb_true ? t1_i.mfi : ab[index]);
        Sum += (sfb_false ^ sfb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (sfb_false ^ sfb_true ? simple_func_int() : 3);
        Sum += (sfb_false ^ sfb_true ? simple_func_int() : -5);
        Sum += (sfb_false ^ sfb_true ? simple_func_int() : local_int);
        Sum += (sfb_false ^ sfb_true ? simple_func_int() : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_207()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ sfb_true ? simple_func_int() : t1_i.mfi);
        Sum += (sfb_false ^ sfb_true ? simple_func_int() : simple_func_int());
        Sum += (sfb_false ^ sfb_true ? simple_func_int() : ab[index]);
        Sum += (sfb_false ^ sfb_true ? simple_func_int() : ab[index - 1]);
        Sum += (sfb_false ^ sfb_true ? ab[index] : 3);
        Sum += (sfb_false ^ sfb_true ? ab[index] : -5);
        Sum += (sfb_false ^ sfb_true ? ab[index] : local_int);
        Sum += (sfb_false ^ sfb_true ? ab[index] : static_field_int);
        Sum += (sfb_false ^ sfb_true ? ab[index] : t1_i.mfi);
        Sum += (sfb_false ^ sfb_true ? ab[index] : simple_func_int());
        Sum += (sfb_false ^ sfb_true ? ab[index] : ab[index]);
        Sum += (sfb_false ^ sfb_true ? ab[index] : ab[index - 1]);
        Sum += (sfb_false ^ sfb_true ? ab[index - 1] : 3);
        Sum += (sfb_false ^ sfb_true ? ab[index - 1] : -5);
        Sum += (sfb_false ^ sfb_true ? ab[index - 1] : local_int);
        Sum += (sfb_false ^ sfb_true ? ab[index - 1] : static_field_int);
        Sum += (sfb_false ^ sfb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (sfb_false ^ sfb_true ? ab[index - 1] : simple_func_int());
        Sum += (sfb_false ^ sfb_true ? ab[index - 1] : ab[index]);
        Sum += (sfb_false ^ sfb_true ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_208()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ sfb_false ? 3 : 3);
        Sum += (sfb_false ^ sfb_false ? 3 : -5);
        Sum += (sfb_false ^ sfb_false ? 3 : local_int);
        Sum += (sfb_false ^ sfb_false ? 3 : static_field_int);
        Sum += (sfb_false ^ sfb_false ? 3 : t1_i.mfi);
        Sum += (sfb_false ^ sfb_false ? 3 : simple_func_int());
        Sum += (sfb_false ^ sfb_false ? 3 : ab[index]);
        Sum += (sfb_false ^ sfb_false ? 3 : ab[index - 1]);
        Sum += (sfb_false ^ sfb_false ? -5 : 3);
        Sum += (sfb_false ^ sfb_false ? -5 : -5);
        Sum += (sfb_false ^ sfb_false ? -5 : local_int);
        Sum += (sfb_false ^ sfb_false ? -5 : static_field_int);
        Sum += (sfb_false ^ sfb_false ? -5 : t1_i.mfi);
        Sum += (sfb_false ^ sfb_false ? -5 : simple_func_int());
        Sum += (sfb_false ^ sfb_false ? -5 : ab[index]);
        Sum += (sfb_false ^ sfb_false ? -5 : ab[index - 1]);
        Sum += (sfb_false ^ sfb_false ? local_int : 3);
        Sum += (sfb_false ^ sfb_false ? local_int : -5);
        Sum += (sfb_false ^ sfb_false ? local_int : local_int);
        Sum += (sfb_false ^ sfb_false ? local_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_209()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ sfb_false ? local_int : t1_i.mfi);
        Sum += (sfb_false ^ sfb_false ? local_int : simple_func_int());
        Sum += (sfb_false ^ sfb_false ? local_int : ab[index]);
        Sum += (sfb_false ^ sfb_false ? local_int : ab[index - 1]);
        Sum += (sfb_false ^ sfb_false ? static_field_int : 3);
        Sum += (sfb_false ^ sfb_false ? static_field_int : -5);
        Sum += (sfb_false ^ sfb_false ? static_field_int : local_int);
        Sum += (sfb_false ^ sfb_false ? static_field_int : static_field_int);
        Sum += (sfb_false ^ sfb_false ? static_field_int : t1_i.mfi);
        Sum += (sfb_false ^ sfb_false ? static_field_int : simple_func_int());
        Sum += (sfb_false ^ sfb_false ? static_field_int : ab[index]);
        Sum += (sfb_false ^ sfb_false ? static_field_int : ab[index - 1]);
        Sum += (sfb_false ^ sfb_false ? t1_i.mfi : 3);
        Sum += (sfb_false ^ sfb_false ? t1_i.mfi : -5);
        Sum += (sfb_false ^ sfb_false ? t1_i.mfi : local_int);
        Sum += (sfb_false ^ sfb_false ? t1_i.mfi : static_field_int);
        Sum += (sfb_false ^ sfb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (sfb_false ^ sfb_false ? t1_i.mfi : simple_func_int());
        Sum += (sfb_false ^ sfb_false ? t1_i.mfi : ab[index]);
        Sum += (sfb_false ^ sfb_false ? t1_i.mfi : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_210()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ sfb_false ? simple_func_int() : 3);
        Sum += (sfb_false ^ sfb_false ? simple_func_int() : -5);
        Sum += (sfb_false ^ sfb_false ? simple_func_int() : local_int);
        Sum += (sfb_false ^ sfb_false ? simple_func_int() : static_field_int);
        Sum += (sfb_false ^ sfb_false ? simple_func_int() : t1_i.mfi);
        Sum += (sfb_false ^ sfb_false ? simple_func_int() : simple_func_int());
        Sum += (sfb_false ^ sfb_false ? simple_func_int() : ab[index]);
        Sum += (sfb_false ^ sfb_false ? simple_func_int() : ab[index - 1]);
        Sum += (sfb_false ^ sfb_false ? ab[index] : 3);
        Sum += (sfb_false ^ sfb_false ? ab[index] : -5);
        Sum += (sfb_false ^ sfb_false ? ab[index] : local_int);
        Sum += (sfb_false ^ sfb_false ? ab[index] : static_field_int);
        Sum += (sfb_false ^ sfb_false ? ab[index] : t1_i.mfi);
        Sum += (sfb_false ^ sfb_false ? ab[index] : simple_func_int());
        Sum += (sfb_false ^ sfb_false ? ab[index] : ab[index]);
        Sum += (sfb_false ^ sfb_false ? ab[index] : ab[index - 1]);
        Sum += (sfb_false ^ sfb_false ? ab[index - 1] : 3);
        Sum += (sfb_false ^ sfb_false ? ab[index - 1] : -5);
        Sum += (sfb_false ^ sfb_false ? ab[index - 1] : local_int);
        Sum += (sfb_false ^ sfb_false ? ab[index - 1] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_211()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ sfb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (sfb_false ^ sfb_false ? ab[index - 1] : simple_func_int());
        Sum += (sfb_false ^ sfb_false ? ab[index - 1] : ab[index]);
        Sum += (sfb_false ^ sfb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (sfb_false ^ t1_i.mfb_true ? 3 : 3);
        Sum += (sfb_false ^ t1_i.mfb_true ? 3 : -5);
        Sum += (sfb_false ^ t1_i.mfb_true ? 3 : local_int);
        Sum += (sfb_false ^ t1_i.mfb_true ? 3 : static_field_int);
        Sum += (sfb_false ^ t1_i.mfb_true ? 3 : t1_i.mfi);
        Sum += (sfb_false ^ t1_i.mfb_true ? 3 : simple_func_int());
        Sum += (sfb_false ^ t1_i.mfb_true ? 3 : ab[index]);
        Sum += (sfb_false ^ t1_i.mfb_true ? 3 : ab[index - 1]);
        Sum += (sfb_false ^ t1_i.mfb_true ? -5 : 3);
        Sum += (sfb_false ^ t1_i.mfb_true ? -5 : -5);
        Sum += (sfb_false ^ t1_i.mfb_true ? -5 : local_int);
        Sum += (sfb_false ^ t1_i.mfb_true ? -5 : static_field_int);
        Sum += (sfb_false ^ t1_i.mfb_true ? -5 : t1_i.mfi);
        Sum += (sfb_false ^ t1_i.mfb_true ? -5 : simple_func_int());
        Sum += (sfb_false ^ t1_i.mfb_true ? -5 : ab[index]);
        Sum += (sfb_false ^ t1_i.mfb_true ? -5 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_212()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ t1_i.mfb_true ? local_int : 3);
        Sum += (sfb_false ^ t1_i.mfb_true ? local_int : -5);
        Sum += (sfb_false ^ t1_i.mfb_true ? local_int : local_int);
        Sum += (sfb_false ^ t1_i.mfb_true ? local_int : static_field_int);
        Sum += (sfb_false ^ t1_i.mfb_true ? local_int : t1_i.mfi);
        Sum += (sfb_false ^ t1_i.mfb_true ? local_int : simple_func_int());
        Sum += (sfb_false ^ t1_i.mfb_true ? local_int : ab[index]);
        Sum += (sfb_false ^ t1_i.mfb_true ? local_int : ab[index - 1]);
        Sum += (sfb_false ^ t1_i.mfb_true ? static_field_int : 3);
        Sum += (sfb_false ^ t1_i.mfb_true ? static_field_int : -5);
        Sum += (sfb_false ^ t1_i.mfb_true ? static_field_int : local_int);
        Sum += (sfb_false ^ t1_i.mfb_true ? static_field_int : static_field_int);
        Sum += (sfb_false ^ t1_i.mfb_true ? static_field_int : t1_i.mfi);
        Sum += (sfb_false ^ t1_i.mfb_true ? static_field_int : simple_func_int());
        Sum += (sfb_false ^ t1_i.mfb_true ? static_field_int : ab[index]);
        Sum += (sfb_false ^ t1_i.mfb_true ? static_field_int : ab[index - 1]);
        Sum += (sfb_false ^ t1_i.mfb_true ? t1_i.mfi : 3);
        Sum += (sfb_false ^ t1_i.mfb_true ? t1_i.mfi : -5);
        Sum += (sfb_false ^ t1_i.mfb_true ? t1_i.mfi : local_int);
        Sum += (sfb_false ^ t1_i.mfb_true ? t1_i.mfi : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_213()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ t1_i.mfb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (sfb_false ^ t1_i.mfb_true ? t1_i.mfi : simple_func_int());
        Sum += (sfb_false ^ t1_i.mfb_true ? t1_i.mfi : ab[index]);
        Sum += (sfb_false ^ t1_i.mfb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (sfb_false ^ t1_i.mfb_true ? simple_func_int() : 3);
        Sum += (sfb_false ^ t1_i.mfb_true ? simple_func_int() : -5);
        Sum += (sfb_false ^ t1_i.mfb_true ? simple_func_int() : local_int);
        Sum += (sfb_false ^ t1_i.mfb_true ? simple_func_int() : static_field_int);
        Sum += (sfb_false ^ t1_i.mfb_true ? simple_func_int() : t1_i.mfi);
        Sum += (sfb_false ^ t1_i.mfb_true ? simple_func_int() : simple_func_int());
        Sum += (sfb_false ^ t1_i.mfb_true ? simple_func_int() : ab[index]);
        Sum += (sfb_false ^ t1_i.mfb_true ? simple_func_int() : ab[index - 1]);
        Sum += (sfb_false ^ t1_i.mfb_true ? ab[index] : 3);
        Sum += (sfb_false ^ t1_i.mfb_true ? ab[index] : -5);
        Sum += (sfb_false ^ t1_i.mfb_true ? ab[index] : local_int);
        Sum += (sfb_false ^ t1_i.mfb_true ? ab[index] : static_field_int);
        Sum += (sfb_false ^ t1_i.mfb_true ? ab[index] : t1_i.mfi);
        Sum += (sfb_false ^ t1_i.mfb_true ? ab[index] : simple_func_int());
        Sum += (sfb_false ^ t1_i.mfb_true ? ab[index] : ab[index]);
        Sum += (sfb_false ^ t1_i.mfb_true ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_214()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ t1_i.mfb_true ? ab[index - 1] : 3);
        Sum += (sfb_false ^ t1_i.mfb_true ? ab[index - 1] : -5);
        Sum += (sfb_false ^ t1_i.mfb_true ? ab[index - 1] : local_int);
        Sum += (sfb_false ^ t1_i.mfb_true ? ab[index - 1] : static_field_int);
        Sum += (sfb_false ^ t1_i.mfb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (sfb_false ^ t1_i.mfb_true ? ab[index - 1] : simple_func_int());
        Sum += (sfb_false ^ t1_i.mfb_true ? ab[index - 1] : ab[index]);
        Sum += (sfb_false ^ t1_i.mfb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (sfb_false ^ t1_i.mfb_false ? 3 : 3);
        Sum += (sfb_false ^ t1_i.mfb_false ? 3 : -5);
        Sum += (sfb_false ^ t1_i.mfb_false ? 3 : local_int);
        Sum += (sfb_false ^ t1_i.mfb_false ? 3 : static_field_int);
        Sum += (sfb_false ^ t1_i.mfb_false ? 3 : t1_i.mfi);
        Sum += (sfb_false ^ t1_i.mfb_false ? 3 : simple_func_int());
        Sum += (sfb_false ^ t1_i.mfb_false ? 3 : ab[index]);
        Sum += (sfb_false ^ t1_i.mfb_false ? 3 : ab[index - 1]);
        Sum += (sfb_false ^ t1_i.mfb_false ? -5 : 3);
        Sum += (sfb_false ^ t1_i.mfb_false ? -5 : -5);
        Sum += (sfb_false ^ t1_i.mfb_false ? -5 : local_int);
        Sum += (sfb_false ^ t1_i.mfb_false ? -5 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_215()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ t1_i.mfb_false ? -5 : t1_i.mfi);
        Sum += (sfb_false ^ t1_i.mfb_false ? -5 : simple_func_int());
        Sum += (sfb_false ^ t1_i.mfb_false ? -5 : ab[index]);
        Sum += (sfb_false ^ t1_i.mfb_false ? -5 : ab[index - 1]);
        Sum += (sfb_false ^ t1_i.mfb_false ? local_int : 3);
        Sum += (sfb_false ^ t1_i.mfb_false ? local_int : -5);
        Sum += (sfb_false ^ t1_i.mfb_false ? local_int : local_int);
        Sum += (sfb_false ^ t1_i.mfb_false ? local_int : static_field_int);
        Sum += (sfb_false ^ t1_i.mfb_false ? local_int : t1_i.mfi);
        Sum += (sfb_false ^ t1_i.mfb_false ? local_int : simple_func_int());
        Sum += (sfb_false ^ t1_i.mfb_false ? local_int : ab[index]);
        Sum += (sfb_false ^ t1_i.mfb_false ? local_int : ab[index - 1]);
        Sum += (sfb_false ^ t1_i.mfb_false ? static_field_int : 3);
        Sum += (sfb_false ^ t1_i.mfb_false ? static_field_int : -5);
        Sum += (sfb_false ^ t1_i.mfb_false ? static_field_int : local_int);
        Sum += (sfb_false ^ t1_i.mfb_false ? static_field_int : static_field_int);
        Sum += (sfb_false ^ t1_i.mfb_false ? static_field_int : t1_i.mfi);
        Sum += (sfb_false ^ t1_i.mfb_false ? static_field_int : simple_func_int());
        Sum += (sfb_false ^ t1_i.mfb_false ? static_field_int : ab[index]);
        Sum += (sfb_false ^ t1_i.mfb_false ? static_field_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_216()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ t1_i.mfb_false ? t1_i.mfi : 3);
        Sum += (sfb_false ^ t1_i.mfb_false ? t1_i.mfi : -5);
        Sum += (sfb_false ^ t1_i.mfb_false ? t1_i.mfi : local_int);
        Sum += (sfb_false ^ t1_i.mfb_false ? t1_i.mfi : static_field_int);
        Sum += (sfb_false ^ t1_i.mfb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (sfb_false ^ t1_i.mfb_false ? t1_i.mfi : simple_func_int());
        Sum += (sfb_false ^ t1_i.mfb_false ? t1_i.mfi : ab[index]);
        Sum += (sfb_false ^ t1_i.mfb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (sfb_false ^ t1_i.mfb_false ? simple_func_int() : 3);
        Sum += (sfb_false ^ t1_i.mfb_false ? simple_func_int() : -5);
        Sum += (sfb_false ^ t1_i.mfb_false ? simple_func_int() : local_int);
        Sum += (sfb_false ^ t1_i.mfb_false ? simple_func_int() : static_field_int);
        Sum += (sfb_false ^ t1_i.mfb_false ? simple_func_int() : t1_i.mfi);
        Sum += (sfb_false ^ t1_i.mfb_false ? simple_func_int() : simple_func_int());
        Sum += (sfb_false ^ t1_i.mfb_false ? simple_func_int() : ab[index]);
        Sum += (sfb_false ^ t1_i.mfb_false ? simple_func_int() : ab[index - 1]);
        Sum += (sfb_false ^ t1_i.mfb_false ? ab[index] : 3);
        Sum += (sfb_false ^ t1_i.mfb_false ? ab[index] : -5);
        Sum += (sfb_false ^ t1_i.mfb_false ? ab[index] : local_int);
        Sum += (sfb_false ^ t1_i.mfb_false ? ab[index] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_217()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ t1_i.mfb_false ? ab[index] : t1_i.mfi);
        Sum += (sfb_false ^ t1_i.mfb_false ? ab[index] : simple_func_int());
        Sum += (sfb_false ^ t1_i.mfb_false ? ab[index] : ab[index]);
        Sum += (sfb_false ^ t1_i.mfb_false ? ab[index] : ab[index - 1]);
        Sum += (sfb_false ^ t1_i.mfb_false ? ab[index - 1] : 3);
        Sum += (sfb_false ^ t1_i.mfb_false ? ab[index - 1] : -5);
        Sum += (sfb_false ^ t1_i.mfb_false ? ab[index - 1] : local_int);
        Sum += (sfb_false ^ t1_i.mfb_false ? ab[index - 1] : static_field_int);
        Sum += (sfb_false ^ t1_i.mfb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (sfb_false ^ t1_i.mfb_false ? ab[index - 1] : simple_func_int());
        Sum += (sfb_false ^ t1_i.mfb_false ? ab[index - 1] : ab[index]);
        Sum += (sfb_false ^ t1_i.mfb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (sfb_false ^ func_sb_true() ? 3 : 3);
        Sum += (sfb_false ^ func_sb_true() ? 3 : -5);
        Sum += (sfb_false ^ func_sb_true() ? 3 : local_int);
        Sum += (sfb_false ^ func_sb_true() ? 3 : static_field_int);
        Sum += (sfb_false ^ func_sb_true() ? 3 : t1_i.mfi);
        Sum += (sfb_false ^ func_sb_true() ? 3 : simple_func_int());
        Sum += (sfb_false ^ func_sb_true() ? 3 : ab[index]);
        Sum += (sfb_false ^ func_sb_true() ? 3 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_218()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ func_sb_true() ? -5 : 3);
        Sum += (sfb_false ^ func_sb_true() ? -5 : -5);
        Sum += (sfb_false ^ func_sb_true() ? -5 : local_int);
        Sum += (sfb_false ^ func_sb_true() ? -5 : static_field_int);
        Sum += (sfb_false ^ func_sb_true() ? -5 : t1_i.mfi);
        Sum += (sfb_false ^ func_sb_true() ? -5 : simple_func_int());
        Sum += (sfb_false ^ func_sb_true() ? -5 : ab[index]);
        Sum += (sfb_false ^ func_sb_true() ? -5 : ab[index - 1]);
        Sum += (sfb_false ^ func_sb_true() ? local_int : 3);
        Sum += (sfb_false ^ func_sb_true() ? local_int : -5);
        Sum += (sfb_false ^ func_sb_true() ? local_int : local_int);
        Sum += (sfb_false ^ func_sb_true() ? local_int : static_field_int);
        Sum += (sfb_false ^ func_sb_true() ? local_int : t1_i.mfi);
        Sum += (sfb_false ^ func_sb_true() ? local_int : simple_func_int());
        Sum += (sfb_false ^ func_sb_true() ? local_int : ab[index]);
        Sum += (sfb_false ^ func_sb_true() ? local_int : ab[index - 1]);
        Sum += (sfb_false ^ func_sb_true() ? static_field_int : 3);
        Sum += (sfb_false ^ func_sb_true() ? static_field_int : -5);
        Sum += (sfb_false ^ func_sb_true() ? static_field_int : local_int);
        Sum += (sfb_false ^ func_sb_true() ? static_field_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_219()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ func_sb_true() ? static_field_int : t1_i.mfi);
        Sum += (sfb_false ^ func_sb_true() ? static_field_int : simple_func_int());
        Sum += (sfb_false ^ func_sb_true() ? static_field_int : ab[index]);
        Sum += (sfb_false ^ func_sb_true() ? static_field_int : ab[index - 1]);
        Sum += (sfb_false ^ func_sb_true() ? t1_i.mfi : 3);
        Sum += (sfb_false ^ func_sb_true() ? t1_i.mfi : -5);
        Sum += (sfb_false ^ func_sb_true() ? t1_i.mfi : local_int);
        Sum += (sfb_false ^ func_sb_true() ? t1_i.mfi : static_field_int);
        Sum += (sfb_false ^ func_sb_true() ? t1_i.mfi : t1_i.mfi);
        Sum += (sfb_false ^ func_sb_true() ? t1_i.mfi : simple_func_int());
        Sum += (sfb_false ^ func_sb_true() ? t1_i.mfi : ab[index]);
        Sum += (sfb_false ^ func_sb_true() ? t1_i.mfi : ab[index - 1]);
        Sum += (sfb_false ^ func_sb_true() ? simple_func_int() : 3);
        Sum += (sfb_false ^ func_sb_true() ? simple_func_int() : -5);
        Sum += (sfb_false ^ func_sb_true() ? simple_func_int() : local_int);
        Sum += (sfb_false ^ func_sb_true() ? simple_func_int() : static_field_int);
        Sum += (sfb_false ^ func_sb_true() ? simple_func_int() : t1_i.mfi);
        Sum += (sfb_false ^ func_sb_true() ? simple_func_int() : simple_func_int());
        Sum += (sfb_false ^ func_sb_true() ? simple_func_int() : ab[index]);
        Sum += (sfb_false ^ func_sb_true() ? simple_func_int() : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_220()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ func_sb_true() ? ab[index] : 3);
        Sum += (sfb_false ^ func_sb_true() ? ab[index] : -5);
        Sum += (sfb_false ^ func_sb_true() ? ab[index] : local_int);
        Sum += (sfb_false ^ func_sb_true() ? ab[index] : static_field_int);
        Sum += (sfb_false ^ func_sb_true() ? ab[index] : t1_i.mfi);
        Sum += (sfb_false ^ func_sb_true() ? ab[index] : simple_func_int());
        Sum += (sfb_false ^ func_sb_true() ? ab[index] : ab[index]);
        Sum += (sfb_false ^ func_sb_true() ? ab[index] : ab[index - 1]);
        Sum += (sfb_false ^ func_sb_true() ? ab[index - 1] : 3);
        Sum += (sfb_false ^ func_sb_true() ? ab[index - 1] : -5);
        Sum += (sfb_false ^ func_sb_true() ? ab[index - 1] : local_int);
        Sum += (sfb_false ^ func_sb_true() ? ab[index - 1] : static_field_int);
        Sum += (sfb_false ^ func_sb_true() ? ab[index - 1] : t1_i.mfi);
        Sum += (sfb_false ^ func_sb_true() ? ab[index - 1] : simple_func_int());
        Sum += (sfb_false ^ func_sb_true() ? ab[index - 1] : ab[index]);
        Sum += (sfb_false ^ func_sb_true() ? ab[index - 1] : ab[index - 1]);
        Sum += (sfb_false ^ func_sb_false() ? 3 : 3);
        Sum += (sfb_false ^ func_sb_false() ? 3 : -5);
        Sum += (sfb_false ^ func_sb_false() ? 3 : local_int);
        Sum += (sfb_false ^ func_sb_false() ? 3 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_221()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ func_sb_false() ? 3 : t1_i.mfi);
        Sum += (sfb_false ^ func_sb_false() ? 3 : simple_func_int());
        Sum += (sfb_false ^ func_sb_false() ? 3 : ab[index]);
        Sum += (sfb_false ^ func_sb_false() ? 3 : ab[index - 1]);
        Sum += (sfb_false ^ func_sb_false() ? -5 : 3);
        Sum += (sfb_false ^ func_sb_false() ? -5 : -5);
        Sum += (sfb_false ^ func_sb_false() ? -5 : local_int);
        Sum += (sfb_false ^ func_sb_false() ? -5 : static_field_int);
        Sum += (sfb_false ^ func_sb_false() ? -5 : t1_i.mfi);
        Sum += (sfb_false ^ func_sb_false() ? -5 : simple_func_int());
        Sum += (sfb_false ^ func_sb_false() ? -5 : ab[index]);
        Sum += (sfb_false ^ func_sb_false() ? -5 : ab[index - 1]);
        Sum += (sfb_false ^ func_sb_false() ? local_int : 3);
        Sum += (sfb_false ^ func_sb_false() ? local_int : -5);
        Sum += (sfb_false ^ func_sb_false() ? local_int : local_int);
        Sum += (sfb_false ^ func_sb_false() ? local_int : static_field_int);
        Sum += (sfb_false ^ func_sb_false() ? local_int : t1_i.mfi);
        Sum += (sfb_false ^ func_sb_false() ? local_int : simple_func_int());
        Sum += (sfb_false ^ func_sb_false() ? local_int : ab[index]);
        Sum += (sfb_false ^ func_sb_false() ? local_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_222()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ func_sb_false() ? static_field_int : 3);
        Sum += (sfb_false ^ func_sb_false() ? static_field_int : -5);
        Sum += (sfb_false ^ func_sb_false() ? static_field_int : local_int);
        Sum += (sfb_false ^ func_sb_false() ? static_field_int : static_field_int);
        Sum += (sfb_false ^ func_sb_false() ? static_field_int : t1_i.mfi);
        Sum += (sfb_false ^ func_sb_false() ? static_field_int : simple_func_int());
        Sum += (sfb_false ^ func_sb_false() ? static_field_int : ab[index]);
        Sum += (sfb_false ^ func_sb_false() ? static_field_int : ab[index - 1]);
        Sum += (sfb_false ^ func_sb_false() ? t1_i.mfi : 3);
        Sum += (sfb_false ^ func_sb_false() ? t1_i.mfi : -5);
        Sum += (sfb_false ^ func_sb_false() ? t1_i.mfi : local_int);
        Sum += (sfb_false ^ func_sb_false() ? t1_i.mfi : static_field_int);
        Sum += (sfb_false ^ func_sb_false() ? t1_i.mfi : t1_i.mfi);
        Sum += (sfb_false ^ func_sb_false() ? t1_i.mfi : simple_func_int());
        Sum += (sfb_false ^ func_sb_false() ? t1_i.mfi : ab[index]);
        Sum += (sfb_false ^ func_sb_false() ? t1_i.mfi : ab[index - 1]);
        Sum += (sfb_false ^ func_sb_false() ? simple_func_int() : 3);
        Sum += (sfb_false ^ func_sb_false() ? simple_func_int() : -5);
        Sum += (sfb_false ^ func_sb_false() ? simple_func_int() : local_int);
        Sum += (sfb_false ^ func_sb_false() ? simple_func_int() : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_223()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ func_sb_false() ? simple_func_int() : t1_i.mfi);
        Sum += (sfb_false ^ func_sb_false() ? simple_func_int() : simple_func_int());
        Sum += (sfb_false ^ func_sb_false() ? simple_func_int() : ab[index]);
        Sum += (sfb_false ^ func_sb_false() ? simple_func_int() : ab[index - 1]);
        Sum += (sfb_false ^ func_sb_false() ? ab[index] : 3);
        Sum += (sfb_false ^ func_sb_false() ? ab[index] : -5);
        Sum += (sfb_false ^ func_sb_false() ? ab[index] : local_int);
        Sum += (sfb_false ^ func_sb_false() ? ab[index] : static_field_int);
        Sum += (sfb_false ^ func_sb_false() ? ab[index] : t1_i.mfi);
        Sum += (sfb_false ^ func_sb_false() ? ab[index] : simple_func_int());
        Sum += (sfb_false ^ func_sb_false() ? ab[index] : ab[index]);
        Sum += (sfb_false ^ func_sb_false() ? ab[index] : ab[index - 1]);
        Sum += (sfb_false ^ func_sb_false() ? ab[index - 1] : 3);
        Sum += (sfb_false ^ func_sb_false() ? ab[index - 1] : -5);
        Sum += (sfb_false ^ func_sb_false() ? ab[index - 1] : local_int);
        Sum += (sfb_false ^ func_sb_false() ? ab[index - 1] : static_field_int);
        Sum += (sfb_false ^ func_sb_false() ? ab[index - 1] : t1_i.mfi);
        Sum += (sfb_false ^ func_sb_false() ? ab[index - 1] : simple_func_int());
        Sum += (sfb_false ^ func_sb_false() ? ab[index - 1] : ab[index]);
        Sum += (sfb_false ^ func_sb_false() ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_224()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ ab_true[index] ? 3 : 3);
        Sum += (sfb_false ^ ab_true[index] ? 3 : -5);
        Sum += (sfb_false ^ ab_true[index] ? 3 : local_int);
        Sum += (sfb_false ^ ab_true[index] ? 3 : static_field_int);
        Sum += (sfb_false ^ ab_true[index] ? 3 : t1_i.mfi);
        Sum += (sfb_false ^ ab_true[index] ? 3 : simple_func_int());
        Sum += (sfb_false ^ ab_true[index] ? 3 : ab[index]);
        Sum += (sfb_false ^ ab_true[index] ? 3 : ab[index - 1]);
        Sum += (sfb_false ^ ab_true[index] ? -5 : 3);
        Sum += (sfb_false ^ ab_true[index] ? -5 : -5);
        Sum += (sfb_false ^ ab_true[index] ? -5 : local_int);
        Sum += (sfb_false ^ ab_true[index] ? -5 : static_field_int);
        Sum += (sfb_false ^ ab_true[index] ? -5 : t1_i.mfi);
        Sum += (sfb_false ^ ab_true[index] ? -5 : simple_func_int());
        Sum += (sfb_false ^ ab_true[index] ? -5 : ab[index]);
        Sum += (sfb_false ^ ab_true[index] ? -5 : ab[index - 1]);
        Sum += (sfb_false ^ ab_true[index] ? local_int : 3);
        Sum += (sfb_false ^ ab_true[index] ? local_int : -5);
        Sum += (sfb_false ^ ab_true[index] ? local_int : local_int);
        Sum += (sfb_false ^ ab_true[index] ? local_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_225()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ ab_true[index] ? local_int : t1_i.mfi);
        Sum += (sfb_false ^ ab_true[index] ? local_int : simple_func_int());
        Sum += (sfb_false ^ ab_true[index] ? local_int : ab[index]);
        Sum += (sfb_false ^ ab_true[index] ? local_int : ab[index - 1]);
        Sum += (sfb_false ^ ab_true[index] ? static_field_int : 3);
        Sum += (sfb_false ^ ab_true[index] ? static_field_int : -5);
        Sum += (sfb_false ^ ab_true[index] ? static_field_int : local_int);
        Sum += (sfb_false ^ ab_true[index] ? static_field_int : static_field_int);
        Sum += (sfb_false ^ ab_true[index] ? static_field_int : t1_i.mfi);
        Sum += (sfb_false ^ ab_true[index] ? static_field_int : simple_func_int());
        Sum += (sfb_false ^ ab_true[index] ? static_field_int : ab[index]);
        Sum += (sfb_false ^ ab_true[index] ? static_field_int : ab[index - 1]);
        Sum += (sfb_false ^ ab_true[index] ? t1_i.mfi : 3);
        Sum += (sfb_false ^ ab_true[index] ? t1_i.mfi : -5);
        Sum += (sfb_false ^ ab_true[index] ? t1_i.mfi : local_int);
        Sum += (sfb_false ^ ab_true[index] ? t1_i.mfi : static_field_int);
        Sum += (sfb_false ^ ab_true[index] ? t1_i.mfi : t1_i.mfi);
        Sum += (sfb_false ^ ab_true[index] ? t1_i.mfi : simple_func_int());
        Sum += (sfb_false ^ ab_true[index] ? t1_i.mfi : ab[index]);
        Sum += (sfb_false ^ ab_true[index] ? t1_i.mfi : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_226()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ ab_true[index] ? simple_func_int() : 3);
        Sum += (sfb_false ^ ab_true[index] ? simple_func_int() : -5);
        Sum += (sfb_false ^ ab_true[index] ? simple_func_int() : local_int);
        Sum += (sfb_false ^ ab_true[index] ? simple_func_int() : static_field_int);
        Sum += (sfb_false ^ ab_true[index] ? simple_func_int() : t1_i.mfi);
        Sum += (sfb_false ^ ab_true[index] ? simple_func_int() : simple_func_int());
        Sum += (sfb_false ^ ab_true[index] ? simple_func_int() : ab[index]);
        Sum += (sfb_false ^ ab_true[index] ? simple_func_int() : ab[index - 1]);
        Sum += (sfb_false ^ ab_true[index] ? ab[index] : 3);
        Sum += (sfb_false ^ ab_true[index] ? ab[index] : -5);
        Sum += (sfb_false ^ ab_true[index] ? ab[index] : local_int);
        Sum += (sfb_false ^ ab_true[index] ? ab[index] : static_field_int);
        Sum += (sfb_false ^ ab_true[index] ? ab[index] : t1_i.mfi);
        Sum += (sfb_false ^ ab_true[index] ? ab[index] : simple_func_int());
        Sum += (sfb_false ^ ab_true[index] ? ab[index] : ab[index]);
        Sum += (sfb_false ^ ab_true[index] ? ab[index] : ab[index - 1]);
        Sum += (sfb_false ^ ab_true[index] ? ab[index - 1] : 3);
        Sum += (sfb_false ^ ab_true[index] ? ab[index - 1] : -5);
        Sum += (sfb_false ^ ab_true[index] ? ab[index - 1] : local_int);
        Sum += (sfb_false ^ ab_true[index] ? ab[index - 1] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_227()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ ab_true[index] ? ab[index - 1] : t1_i.mfi);
        Sum += (sfb_false ^ ab_true[index] ? ab[index - 1] : simple_func_int());
        Sum += (sfb_false ^ ab_true[index] ? ab[index - 1] : ab[index]);
        Sum += (sfb_false ^ ab_true[index] ? ab[index - 1] : ab[index - 1]);
        Sum += (sfb_false ^ ab_false[index] ? 3 : 3);
        Sum += (sfb_false ^ ab_false[index] ? 3 : -5);
        Sum += (sfb_false ^ ab_false[index] ? 3 : local_int);
        Sum += (sfb_false ^ ab_false[index] ? 3 : static_field_int);
        Sum += (sfb_false ^ ab_false[index] ? 3 : t1_i.mfi);
        Sum += (sfb_false ^ ab_false[index] ? 3 : simple_func_int());
        Sum += (sfb_false ^ ab_false[index] ? 3 : ab[index]);
        Sum += (sfb_false ^ ab_false[index] ? 3 : ab[index - 1]);
        Sum += (sfb_false ^ ab_false[index] ? -5 : 3);
        Sum += (sfb_false ^ ab_false[index] ? -5 : -5);
        Sum += (sfb_false ^ ab_false[index] ? -5 : local_int);
        Sum += (sfb_false ^ ab_false[index] ? -5 : static_field_int);
        Sum += (sfb_false ^ ab_false[index] ? -5 : t1_i.mfi);
        Sum += (sfb_false ^ ab_false[index] ? -5 : simple_func_int());
        Sum += (sfb_false ^ ab_false[index] ? -5 : ab[index]);
        Sum += (sfb_false ^ ab_false[index] ? -5 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_228()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ ab_false[index] ? local_int : 3);
        Sum += (sfb_false ^ ab_false[index] ? local_int : -5);
        Sum += (sfb_false ^ ab_false[index] ? local_int : local_int);
        Sum += (sfb_false ^ ab_false[index] ? local_int : static_field_int);
        Sum += (sfb_false ^ ab_false[index] ? local_int : t1_i.mfi);
        Sum += (sfb_false ^ ab_false[index] ? local_int : simple_func_int());
        Sum += (sfb_false ^ ab_false[index] ? local_int : ab[index]);
        Sum += (sfb_false ^ ab_false[index] ? local_int : ab[index - 1]);
        Sum += (sfb_false ^ ab_false[index] ? static_field_int : 3);
        Sum += (sfb_false ^ ab_false[index] ? static_field_int : -5);
        Sum += (sfb_false ^ ab_false[index] ? static_field_int : local_int);
        Sum += (sfb_false ^ ab_false[index] ? static_field_int : static_field_int);
        Sum += (sfb_false ^ ab_false[index] ? static_field_int : t1_i.mfi);
        Sum += (sfb_false ^ ab_false[index] ? static_field_int : simple_func_int());
        Sum += (sfb_false ^ ab_false[index] ? static_field_int : ab[index]);
        Sum += (sfb_false ^ ab_false[index] ? static_field_int : ab[index - 1]);
        Sum += (sfb_false ^ ab_false[index] ? t1_i.mfi : 3);
        Sum += (sfb_false ^ ab_false[index] ? t1_i.mfi : -5);
        Sum += (sfb_false ^ ab_false[index] ? t1_i.mfi : local_int);
        Sum += (sfb_false ^ ab_false[index] ? t1_i.mfi : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_229()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ ab_false[index] ? t1_i.mfi : t1_i.mfi);
        Sum += (sfb_false ^ ab_false[index] ? t1_i.mfi : simple_func_int());
        Sum += (sfb_false ^ ab_false[index] ? t1_i.mfi : ab[index]);
        Sum += (sfb_false ^ ab_false[index] ? t1_i.mfi : ab[index - 1]);
        Sum += (sfb_false ^ ab_false[index] ? simple_func_int() : 3);
        Sum += (sfb_false ^ ab_false[index] ? simple_func_int() : -5);
        Sum += (sfb_false ^ ab_false[index] ? simple_func_int() : local_int);
        Sum += (sfb_false ^ ab_false[index] ? simple_func_int() : static_field_int);
        Sum += (sfb_false ^ ab_false[index] ? simple_func_int() : t1_i.mfi);
        Sum += (sfb_false ^ ab_false[index] ? simple_func_int() : simple_func_int());
        Sum += (sfb_false ^ ab_false[index] ? simple_func_int() : ab[index]);
        Sum += (sfb_false ^ ab_false[index] ? simple_func_int() : ab[index - 1]);
        Sum += (sfb_false ^ ab_false[index] ? ab[index] : 3);
        Sum += (sfb_false ^ ab_false[index] ? ab[index] : -5);
        Sum += (sfb_false ^ ab_false[index] ? ab[index] : local_int);
        Sum += (sfb_false ^ ab_false[index] ? ab[index] : static_field_int);
        Sum += (sfb_false ^ ab_false[index] ? ab[index] : t1_i.mfi);
        Sum += (sfb_false ^ ab_false[index] ? ab[index] : simple_func_int());
        Sum += (sfb_false ^ ab_false[index] ? ab[index] : ab[index]);
        Sum += (sfb_false ^ ab_false[index] ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_230()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ^ ab_false[index] ? ab[index - 1] : 3);
        Sum += (sfb_false ^ ab_false[index] ? ab[index - 1] : -5);
        Sum += (sfb_false ^ ab_false[index] ? ab[index - 1] : local_int);
        Sum += (sfb_false ^ ab_false[index] ? ab[index - 1] : static_field_int);
        Sum += (sfb_false ^ ab_false[index] ? ab[index - 1] : t1_i.mfi);
        Sum += (sfb_false ^ ab_false[index] ? ab[index - 1] : simple_func_int());
        Sum += (sfb_false ^ ab_false[index] ? ab[index - 1] : ab[index]);
        Sum += (sfb_false ^ ab_false[index] ? ab[index - 1] : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ true ? 3 : 3);
        Sum += (t1_i.mfb_true ^ true ? 3 : -5);
        Sum += (t1_i.mfb_true ^ true ? 3 : local_int);
        Sum += (t1_i.mfb_true ^ true ? 3 : static_field_int);
        Sum += (t1_i.mfb_true ^ true ? 3 : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ true ? 3 : simple_func_int());
        Sum += (t1_i.mfb_true ^ true ? 3 : ab[index]);
        Sum += (t1_i.mfb_true ^ true ? 3 : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ true ? -5 : 3);
        Sum += (t1_i.mfb_true ^ true ? -5 : -5);
        Sum += (t1_i.mfb_true ^ true ? -5 : local_int);
        Sum += (t1_i.mfb_true ^ true ? -5 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_231()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ true ? -5 : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ true ? -5 : simple_func_int());
        Sum += (t1_i.mfb_true ^ true ? -5 : ab[index]);
        Sum += (t1_i.mfb_true ^ true ? -5 : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ true ? local_int : 3);
        Sum += (t1_i.mfb_true ^ true ? local_int : -5);
        Sum += (t1_i.mfb_true ^ true ? local_int : local_int);
        Sum += (t1_i.mfb_true ^ true ? local_int : static_field_int);
        Sum += (t1_i.mfb_true ^ true ? local_int : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ true ? local_int : simple_func_int());
        Sum += (t1_i.mfb_true ^ true ? local_int : ab[index]);
        Sum += (t1_i.mfb_true ^ true ? local_int : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ true ? static_field_int : 3);
        Sum += (t1_i.mfb_true ^ true ? static_field_int : -5);
        Sum += (t1_i.mfb_true ^ true ? static_field_int : local_int);
        Sum += (t1_i.mfb_true ^ true ? static_field_int : static_field_int);
        Sum += (t1_i.mfb_true ^ true ? static_field_int : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ true ? static_field_int : simple_func_int());
        Sum += (t1_i.mfb_true ^ true ? static_field_int : ab[index]);
        Sum += (t1_i.mfb_true ^ true ? static_field_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_232()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ true ? t1_i.mfi : 3);
        Sum += (t1_i.mfb_true ^ true ? t1_i.mfi : -5);
        Sum += (t1_i.mfb_true ^ true ? t1_i.mfi : local_int);
        Sum += (t1_i.mfb_true ^ true ? t1_i.mfi : static_field_int);
        Sum += (t1_i.mfb_true ^ true ? t1_i.mfi : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ true ? t1_i.mfi : simple_func_int());
        Sum += (t1_i.mfb_true ^ true ? t1_i.mfi : ab[index]);
        Sum += (t1_i.mfb_true ^ true ? t1_i.mfi : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ true ? simple_func_int() : 3);
        Sum += (t1_i.mfb_true ^ true ? simple_func_int() : -5);
        Sum += (t1_i.mfb_true ^ true ? simple_func_int() : local_int);
        Sum += (t1_i.mfb_true ^ true ? simple_func_int() : static_field_int);
        Sum += (t1_i.mfb_true ^ true ? simple_func_int() : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ true ? simple_func_int() : simple_func_int());
        Sum += (t1_i.mfb_true ^ true ? simple_func_int() : ab[index]);
        Sum += (t1_i.mfb_true ^ true ? simple_func_int() : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ true ? ab[index] : 3);
        Sum += (t1_i.mfb_true ^ true ? ab[index] : -5);
        Sum += (t1_i.mfb_true ^ true ? ab[index] : local_int);
        Sum += (t1_i.mfb_true ^ true ? ab[index] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_233()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ true ? ab[index] : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ true ? ab[index] : simple_func_int());
        Sum += (t1_i.mfb_true ^ true ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_true ^ true ? ab[index] : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ true ? ab[index - 1] : 3);
        Sum += (t1_i.mfb_true ^ true ? ab[index - 1] : -5);
        Sum += (t1_i.mfb_true ^ true ? ab[index - 1] : local_int);
        Sum += (t1_i.mfb_true ^ true ? ab[index - 1] : static_field_int);
        Sum += (t1_i.mfb_true ^ true ? ab[index - 1] : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ true ? ab[index - 1] : simple_func_int());
        Sum += (t1_i.mfb_true ^ true ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_true ^ true ? ab[index - 1] : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ false ? 3 : 3);
        Sum += (t1_i.mfb_true ^ false ? 3 : -5);
        Sum += (t1_i.mfb_true ^ false ? 3 : local_int);
        Sum += (t1_i.mfb_true ^ false ? 3 : static_field_int);
        Sum += (t1_i.mfb_true ^ false ? 3 : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ false ? 3 : simple_func_int());
        Sum += (t1_i.mfb_true ^ false ? 3 : ab[index]);
        Sum += (t1_i.mfb_true ^ false ? 3 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_234()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ false ? -5 : 3);
        Sum += (t1_i.mfb_true ^ false ? -5 : -5);
        Sum += (t1_i.mfb_true ^ false ? -5 : local_int);
        Sum += (t1_i.mfb_true ^ false ? -5 : static_field_int);
        Sum += (t1_i.mfb_true ^ false ? -5 : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ false ? -5 : simple_func_int());
        Sum += (t1_i.mfb_true ^ false ? -5 : ab[index]);
        Sum += (t1_i.mfb_true ^ false ? -5 : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ false ? local_int : 3);
        Sum += (t1_i.mfb_true ^ false ? local_int : -5);
        Sum += (t1_i.mfb_true ^ false ? local_int : local_int);
        Sum += (t1_i.mfb_true ^ false ? local_int : static_field_int);
        Sum += (t1_i.mfb_true ^ false ? local_int : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ false ? local_int : simple_func_int());
        Sum += (t1_i.mfb_true ^ false ? local_int : ab[index]);
        Sum += (t1_i.mfb_true ^ false ? local_int : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ false ? static_field_int : 3);
        Sum += (t1_i.mfb_true ^ false ? static_field_int : -5);
        Sum += (t1_i.mfb_true ^ false ? static_field_int : local_int);
        Sum += (t1_i.mfb_true ^ false ? static_field_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_235()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ false ? static_field_int : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ false ? static_field_int : simple_func_int());
        Sum += (t1_i.mfb_true ^ false ? static_field_int : ab[index]);
        Sum += (t1_i.mfb_true ^ false ? static_field_int : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ false ? t1_i.mfi : 3);
        Sum += (t1_i.mfb_true ^ false ? t1_i.mfi : -5);
        Sum += (t1_i.mfb_true ^ false ? t1_i.mfi : local_int);
        Sum += (t1_i.mfb_true ^ false ? t1_i.mfi : static_field_int);
        Sum += (t1_i.mfb_true ^ false ? t1_i.mfi : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ false ? t1_i.mfi : simple_func_int());
        Sum += (t1_i.mfb_true ^ false ? t1_i.mfi : ab[index]);
        Sum += (t1_i.mfb_true ^ false ? t1_i.mfi : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ false ? simple_func_int() : 3);
        Sum += (t1_i.mfb_true ^ false ? simple_func_int() : -5);
        Sum += (t1_i.mfb_true ^ false ? simple_func_int() : local_int);
        Sum += (t1_i.mfb_true ^ false ? simple_func_int() : static_field_int);
        Sum += (t1_i.mfb_true ^ false ? simple_func_int() : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ false ? simple_func_int() : simple_func_int());
        Sum += (t1_i.mfb_true ^ false ? simple_func_int() : ab[index]);
        Sum += (t1_i.mfb_true ^ false ? simple_func_int() : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_236()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ false ? ab[index] : 3);
        Sum += (t1_i.mfb_true ^ false ? ab[index] : -5);
        Sum += (t1_i.mfb_true ^ false ? ab[index] : local_int);
        Sum += (t1_i.mfb_true ^ false ? ab[index] : static_field_int);
        Sum += (t1_i.mfb_true ^ false ? ab[index] : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ false ? ab[index] : simple_func_int());
        Sum += (t1_i.mfb_true ^ false ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_true ^ false ? ab[index] : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ false ? ab[index - 1] : 3);
        Sum += (t1_i.mfb_true ^ false ? ab[index - 1] : -5);
        Sum += (t1_i.mfb_true ^ false ? ab[index - 1] : local_int);
        Sum += (t1_i.mfb_true ^ false ? ab[index - 1] : static_field_int);
        Sum += (t1_i.mfb_true ^ false ? ab[index - 1] : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ false ? ab[index - 1] : simple_func_int());
        Sum += (t1_i.mfb_true ^ false ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_true ^ false ? ab[index - 1] : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ lb_true ? 3 : 3);
        Sum += (t1_i.mfb_true ^ lb_true ? 3 : -5);
        Sum += (t1_i.mfb_true ^ lb_true ? 3 : local_int);
        Sum += (t1_i.mfb_true ^ lb_true ? 3 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_237()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ lb_true ? 3 : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ lb_true ? 3 : simple_func_int());
        Sum += (t1_i.mfb_true ^ lb_true ? 3 : ab[index]);
        Sum += (t1_i.mfb_true ^ lb_true ? 3 : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ lb_true ? -5 : 3);
        Sum += (t1_i.mfb_true ^ lb_true ? -5 : -5);
        Sum += (t1_i.mfb_true ^ lb_true ? -5 : local_int);
        Sum += (t1_i.mfb_true ^ lb_true ? -5 : static_field_int);
        Sum += (t1_i.mfb_true ^ lb_true ? -5 : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ lb_true ? -5 : simple_func_int());
        Sum += (t1_i.mfb_true ^ lb_true ? -5 : ab[index]);
        Sum += (t1_i.mfb_true ^ lb_true ? -5 : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ lb_true ? local_int : 3);
        Sum += (t1_i.mfb_true ^ lb_true ? local_int : -5);
        Sum += (t1_i.mfb_true ^ lb_true ? local_int : local_int);
        Sum += (t1_i.mfb_true ^ lb_true ? local_int : static_field_int);
        Sum += (t1_i.mfb_true ^ lb_true ? local_int : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ lb_true ? local_int : simple_func_int());
        Sum += (t1_i.mfb_true ^ lb_true ? local_int : ab[index]);
        Sum += (t1_i.mfb_true ^ lb_true ? local_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_238()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ lb_true ? static_field_int : 3);
        Sum += (t1_i.mfb_true ^ lb_true ? static_field_int : -5);
        Sum += (t1_i.mfb_true ^ lb_true ? static_field_int : local_int);
        Sum += (t1_i.mfb_true ^ lb_true ? static_field_int : static_field_int);
        Sum += (t1_i.mfb_true ^ lb_true ? static_field_int : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ lb_true ? static_field_int : simple_func_int());
        Sum += (t1_i.mfb_true ^ lb_true ? static_field_int : ab[index]);
        Sum += (t1_i.mfb_true ^ lb_true ? static_field_int : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ lb_true ? t1_i.mfi : 3);
        Sum += (t1_i.mfb_true ^ lb_true ? t1_i.mfi : -5);
        Sum += (t1_i.mfb_true ^ lb_true ? t1_i.mfi : local_int);
        Sum += (t1_i.mfb_true ^ lb_true ? t1_i.mfi : static_field_int);
        Sum += (t1_i.mfb_true ^ lb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ lb_true ? t1_i.mfi : simple_func_int());
        Sum += (t1_i.mfb_true ^ lb_true ? t1_i.mfi : ab[index]);
        Sum += (t1_i.mfb_true ^ lb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ lb_true ? simple_func_int() : 3);
        Sum += (t1_i.mfb_true ^ lb_true ? simple_func_int() : -5);
        Sum += (t1_i.mfb_true ^ lb_true ? simple_func_int() : local_int);
        Sum += (t1_i.mfb_true ^ lb_true ? simple_func_int() : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_239()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ lb_true ? simple_func_int() : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ lb_true ? simple_func_int() : simple_func_int());
        Sum += (t1_i.mfb_true ^ lb_true ? simple_func_int() : ab[index]);
        Sum += (t1_i.mfb_true ^ lb_true ? simple_func_int() : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ lb_true ? ab[index] : 3);
        Sum += (t1_i.mfb_true ^ lb_true ? ab[index] : -5);
        Sum += (t1_i.mfb_true ^ lb_true ? ab[index] : local_int);
        Sum += (t1_i.mfb_true ^ lb_true ? ab[index] : static_field_int);
        Sum += (t1_i.mfb_true ^ lb_true ? ab[index] : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ lb_true ? ab[index] : simple_func_int());
        Sum += (t1_i.mfb_true ^ lb_true ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_true ^ lb_true ? ab[index] : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ lb_true ? ab[index - 1] : 3);
        Sum += (t1_i.mfb_true ^ lb_true ? ab[index - 1] : -5);
        Sum += (t1_i.mfb_true ^ lb_true ? ab[index - 1] : local_int);
        Sum += (t1_i.mfb_true ^ lb_true ? ab[index - 1] : static_field_int);
        Sum += (t1_i.mfb_true ^ lb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ lb_true ? ab[index - 1] : simple_func_int());
        Sum += (t1_i.mfb_true ^ lb_true ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_true ^ lb_true ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_240()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ lb_false ? 3 : 3);
        Sum += (t1_i.mfb_true ^ lb_false ? 3 : -5);
        Sum += (t1_i.mfb_true ^ lb_false ? 3 : local_int);
        Sum += (t1_i.mfb_true ^ lb_false ? 3 : static_field_int);
        Sum += (t1_i.mfb_true ^ lb_false ? 3 : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ lb_false ? 3 : simple_func_int());
        Sum += (t1_i.mfb_true ^ lb_false ? 3 : ab[index]);
        Sum += (t1_i.mfb_true ^ lb_false ? 3 : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ lb_false ? -5 : 3);
        Sum += (t1_i.mfb_true ^ lb_false ? -5 : -5);
        Sum += (t1_i.mfb_true ^ lb_false ? -5 : local_int);
        Sum += (t1_i.mfb_true ^ lb_false ? -5 : static_field_int);
        Sum += (t1_i.mfb_true ^ lb_false ? -5 : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ lb_false ? -5 : simple_func_int());
        Sum += (t1_i.mfb_true ^ lb_false ? -5 : ab[index]);
        Sum += (t1_i.mfb_true ^ lb_false ? -5 : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ lb_false ? local_int : 3);
        Sum += (t1_i.mfb_true ^ lb_false ? local_int : -5);
        Sum += (t1_i.mfb_true ^ lb_false ? local_int : local_int);
        Sum += (t1_i.mfb_true ^ lb_false ? local_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_241()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ lb_false ? local_int : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ lb_false ? local_int : simple_func_int());
        Sum += (t1_i.mfb_true ^ lb_false ? local_int : ab[index]);
        Sum += (t1_i.mfb_true ^ lb_false ? local_int : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ lb_false ? static_field_int : 3);
        Sum += (t1_i.mfb_true ^ lb_false ? static_field_int : -5);
        Sum += (t1_i.mfb_true ^ lb_false ? static_field_int : local_int);
        Sum += (t1_i.mfb_true ^ lb_false ? static_field_int : static_field_int);
        Sum += (t1_i.mfb_true ^ lb_false ? static_field_int : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ lb_false ? static_field_int : simple_func_int());
        Sum += (t1_i.mfb_true ^ lb_false ? static_field_int : ab[index]);
        Sum += (t1_i.mfb_true ^ lb_false ? static_field_int : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ lb_false ? t1_i.mfi : 3);
        Sum += (t1_i.mfb_true ^ lb_false ? t1_i.mfi : -5);
        Sum += (t1_i.mfb_true ^ lb_false ? t1_i.mfi : local_int);
        Sum += (t1_i.mfb_true ^ lb_false ? t1_i.mfi : static_field_int);
        Sum += (t1_i.mfb_true ^ lb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ lb_false ? t1_i.mfi : simple_func_int());
        Sum += (t1_i.mfb_true ^ lb_false ? t1_i.mfi : ab[index]);
        Sum += (t1_i.mfb_true ^ lb_false ? t1_i.mfi : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_242()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ lb_false ? simple_func_int() : 3);
        Sum += (t1_i.mfb_true ^ lb_false ? simple_func_int() : -5);
        Sum += (t1_i.mfb_true ^ lb_false ? simple_func_int() : local_int);
        Sum += (t1_i.mfb_true ^ lb_false ? simple_func_int() : static_field_int);
        Sum += (t1_i.mfb_true ^ lb_false ? simple_func_int() : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ lb_false ? simple_func_int() : simple_func_int());
        Sum += (t1_i.mfb_true ^ lb_false ? simple_func_int() : ab[index]);
        Sum += (t1_i.mfb_true ^ lb_false ? simple_func_int() : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ lb_false ? ab[index] : 3);
        Sum += (t1_i.mfb_true ^ lb_false ? ab[index] : -5);
        Sum += (t1_i.mfb_true ^ lb_false ? ab[index] : local_int);
        Sum += (t1_i.mfb_true ^ lb_false ? ab[index] : static_field_int);
        Sum += (t1_i.mfb_true ^ lb_false ? ab[index] : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ lb_false ? ab[index] : simple_func_int());
        Sum += (t1_i.mfb_true ^ lb_false ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_true ^ lb_false ? ab[index] : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ lb_false ? ab[index - 1] : 3);
        Sum += (t1_i.mfb_true ^ lb_false ? ab[index - 1] : -5);
        Sum += (t1_i.mfb_true ^ lb_false ? ab[index - 1] : local_int);
        Sum += (t1_i.mfb_true ^ lb_false ? ab[index - 1] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_243()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ lb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ lb_false ? ab[index - 1] : simple_func_int());
        Sum += (t1_i.mfb_true ^ lb_false ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_true ^ lb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ sfb_true ? 3 : 3);
        Sum += (t1_i.mfb_true ^ sfb_true ? 3 : -5);
        Sum += (t1_i.mfb_true ^ sfb_true ? 3 : local_int);
        Sum += (t1_i.mfb_true ^ sfb_true ? 3 : static_field_int);
        Sum += (t1_i.mfb_true ^ sfb_true ? 3 : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ sfb_true ? 3 : simple_func_int());
        Sum += (t1_i.mfb_true ^ sfb_true ? 3 : ab[index]);
        Sum += (t1_i.mfb_true ^ sfb_true ? 3 : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ sfb_true ? -5 : 3);
        Sum += (t1_i.mfb_true ^ sfb_true ? -5 : -5);
        Sum += (t1_i.mfb_true ^ sfb_true ? -5 : local_int);
        Sum += (t1_i.mfb_true ^ sfb_true ? -5 : static_field_int);
        Sum += (t1_i.mfb_true ^ sfb_true ? -5 : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ sfb_true ? -5 : simple_func_int());
        Sum += (t1_i.mfb_true ^ sfb_true ? -5 : ab[index]);
        Sum += (t1_i.mfb_true ^ sfb_true ? -5 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_244()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ sfb_true ? local_int : 3);
        Sum += (t1_i.mfb_true ^ sfb_true ? local_int : -5);
        Sum += (t1_i.mfb_true ^ sfb_true ? local_int : local_int);
        Sum += (t1_i.mfb_true ^ sfb_true ? local_int : static_field_int);
        Sum += (t1_i.mfb_true ^ sfb_true ? local_int : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ sfb_true ? local_int : simple_func_int());
        Sum += (t1_i.mfb_true ^ sfb_true ? local_int : ab[index]);
        Sum += (t1_i.mfb_true ^ sfb_true ? local_int : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ sfb_true ? static_field_int : 3);
        Sum += (t1_i.mfb_true ^ sfb_true ? static_field_int : -5);
        Sum += (t1_i.mfb_true ^ sfb_true ? static_field_int : local_int);
        Sum += (t1_i.mfb_true ^ sfb_true ? static_field_int : static_field_int);
        Sum += (t1_i.mfb_true ^ sfb_true ? static_field_int : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ sfb_true ? static_field_int : simple_func_int());
        Sum += (t1_i.mfb_true ^ sfb_true ? static_field_int : ab[index]);
        Sum += (t1_i.mfb_true ^ sfb_true ? static_field_int : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ sfb_true ? t1_i.mfi : 3);
        Sum += (t1_i.mfb_true ^ sfb_true ? t1_i.mfi : -5);
        Sum += (t1_i.mfb_true ^ sfb_true ? t1_i.mfi : local_int);
        Sum += (t1_i.mfb_true ^ sfb_true ? t1_i.mfi : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_245()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ sfb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ sfb_true ? t1_i.mfi : simple_func_int());
        Sum += (t1_i.mfb_true ^ sfb_true ? t1_i.mfi : ab[index]);
        Sum += (t1_i.mfb_true ^ sfb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ sfb_true ? simple_func_int() : 3);
        Sum += (t1_i.mfb_true ^ sfb_true ? simple_func_int() : -5);
        Sum += (t1_i.mfb_true ^ sfb_true ? simple_func_int() : local_int);
        Sum += (t1_i.mfb_true ^ sfb_true ? simple_func_int() : static_field_int);
        Sum += (t1_i.mfb_true ^ sfb_true ? simple_func_int() : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ sfb_true ? simple_func_int() : simple_func_int());
        Sum += (t1_i.mfb_true ^ sfb_true ? simple_func_int() : ab[index]);
        Sum += (t1_i.mfb_true ^ sfb_true ? simple_func_int() : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ sfb_true ? ab[index] : 3);
        Sum += (t1_i.mfb_true ^ sfb_true ? ab[index] : -5);
        Sum += (t1_i.mfb_true ^ sfb_true ? ab[index] : local_int);
        Sum += (t1_i.mfb_true ^ sfb_true ? ab[index] : static_field_int);
        Sum += (t1_i.mfb_true ^ sfb_true ? ab[index] : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ sfb_true ? ab[index] : simple_func_int());
        Sum += (t1_i.mfb_true ^ sfb_true ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_true ^ sfb_true ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_246()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ sfb_true ? ab[index - 1] : 3);
        Sum += (t1_i.mfb_true ^ sfb_true ? ab[index - 1] : -5);
        Sum += (t1_i.mfb_true ^ sfb_true ? ab[index - 1] : local_int);
        Sum += (t1_i.mfb_true ^ sfb_true ? ab[index - 1] : static_field_int);
        Sum += (t1_i.mfb_true ^ sfb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ sfb_true ? ab[index - 1] : simple_func_int());
        Sum += (t1_i.mfb_true ^ sfb_true ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_true ^ sfb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ sfb_false ? 3 : 3);
        Sum += (t1_i.mfb_true ^ sfb_false ? 3 : -5);
        Sum += (t1_i.mfb_true ^ sfb_false ? 3 : local_int);
        Sum += (t1_i.mfb_true ^ sfb_false ? 3 : static_field_int);
        Sum += (t1_i.mfb_true ^ sfb_false ? 3 : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ sfb_false ? 3 : simple_func_int());
        Sum += (t1_i.mfb_true ^ sfb_false ? 3 : ab[index]);
        Sum += (t1_i.mfb_true ^ sfb_false ? 3 : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ sfb_false ? -5 : 3);
        Sum += (t1_i.mfb_true ^ sfb_false ? -5 : -5);
        Sum += (t1_i.mfb_true ^ sfb_false ? -5 : local_int);
        Sum += (t1_i.mfb_true ^ sfb_false ? -5 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_247()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ sfb_false ? -5 : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ sfb_false ? -5 : simple_func_int());
        Sum += (t1_i.mfb_true ^ sfb_false ? -5 : ab[index]);
        Sum += (t1_i.mfb_true ^ sfb_false ? -5 : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ sfb_false ? local_int : 3);
        Sum += (t1_i.mfb_true ^ sfb_false ? local_int : -5);
        Sum += (t1_i.mfb_true ^ sfb_false ? local_int : local_int);
        Sum += (t1_i.mfb_true ^ sfb_false ? local_int : static_field_int);
        Sum += (t1_i.mfb_true ^ sfb_false ? local_int : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ sfb_false ? local_int : simple_func_int());
        Sum += (t1_i.mfb_true ^ sfb_false ? local_int : ab[index]);
        Sum += (t1_i.mfb_true ^ sfb_false ? local_int : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ sfb_false ? static_field_int : 3);
        Sum += (t1_i.mfb_true ^ sfb_false ? static_field_int : -5);
        Sum += (t1_i.mfb_true ^ sfb_false ? static_field_int : local_int);
        Sum += (t1_i.mfb_true ^ sfb_false ? static_field_int : static_field_int);
        Sum += (t1_i.mfb_true ^ sfb_false ? static_field_int : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ sfb_false ? static_field_int : simple_func_int());
        Sum += (t1_i.mfb_true ^ sfb_false ? static_field_int : ab[index]);
        Sum += (t1_i.mfb_true ^ sfb_false ? static_field_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_248()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ sfb_false ? t1_i.mfi : 3);
        Sum += (t1_i.mfb_true ^ sfb_false ? t1_i.mfi : -5);
        Sum += (t1_i.mfb_true ^ sfb_false ? t1_i.mfi : local_int);
        Sum += (t1_i.mfb_true ^ sfb_false ? t1_i.mfi : static_field_int);
        Sum += (t1_i.mfb_true ^ sfb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ sfb_false ? t1_i.mfi : simple_func_int());
        Sum += (t1_i.mfb_true ^ sfb_false ? t1_i.mfi : ab[index]);
        Sum += (t1_i.mfb_true ^ sfb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ sfb_false ? simple_func_int() : 3);
        Sum += (t1_i.mfb_true ^ sfb_false ? simple_func_int() : -5);
        Sum += (t1_i.mfb_true ^ sfb_false ? simple_func_int() : local_int);
        Sum += (t1_i.mfb_true ^ sfb_false ? simple_func_int() : static_field_int);
        Sum += (t1_i.mfb_true ^ sfb_false ? simple_func_int() : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ sfb_false ? simple_func_int() : simple_func_int());
        Sum += (t1_i.mfb_true ^ sfb_false ? simple_func_int() : ab[index]);
        Sum += (t1_i.mfb_true ^ sfb_false ? simple_func_int() : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ sfb_false ? ab[index] : 3);
        Sum += (t1_i.mfb_true ^ sfb_false ? ab[index] : -5);
        Sum += (t1_i.mfb_true ^ sfb_false ? ab[index] : local_int);
        Sum += (t1_i.mfb_true ^ sfb_false ? ab[index] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_249()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ sfb_false ? ab[index] : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ sfb_false ? ab[index] : simple_func_int());
        Sum += (t1_i.mfb_true ^ sfb_false ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_true ^ sfb_false ? ab[index] : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ sfb_false ? ab[index - 1] : 3);
        Sum += (t1_i.mfb_true ^ sfb_false ? ab[index - 1] : -5);
        Sum += (t1_i.mfb_true ^ sfb_false ? ab[index - 1] : local_int);
        Sum += (t1_i.mfb_true ^ sfb_false ? ab[index - 1] : static_field_int);
        Sum += (t1_i.mfb_true ^ sfb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ sfb_false ? ab[index - 1] : simple_func_int());
        Sum += (t1_i.mfb_true ^ sfb_false ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_true ^ sfb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? 3 : 3);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? 3 : -5);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? 3 : local_int);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? 3 : static_field_int);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? 3 : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? 3 : simple_func_int());
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? 3 : ab[index]);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? 3 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_250()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? -5 : 3);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? -5 : -5);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? -5 : local_int);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? -5 : static_field_int);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? -5 : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? -5 : simple_func_int());
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? -5 : ab[index]);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? -5 : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? local_int : 3);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? local_int : -5);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? local_int : local_int);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? local_int : static_field_int);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? local_int : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? local_int : simple_func_int());
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? local_int : ab[index]);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? local_int : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? static_field_int : 3);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? static_field_int : -5);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? static_field_int : local_int);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? static_field_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_251()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? static_field_int : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? static_field_int : simple_func_int());
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? static_field_int : ab[index]);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? static_field_int : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? t1_i.mfi : 3);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? t1_i.mfi : -5);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? t1_i.mfi : local_int);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? t1_i.mfi : static_field_int);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? t1_i.mfi : simple_func_int());
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? t1_i.mfi : ab[index]);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? simple_func_int() : 3);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? simple_func_int() : -5);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? simple_func_int() : local_int);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? simple_func_int() : static_field_int);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? simple_func_int() : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? simple_func_int() : simple_func_int());
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? simple_func_int() : ab[index]);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? simple_func_int() : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_252()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? ab[index] : 3);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? ab[index] : -5);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? ab[index] : local_int);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? ab[index] : static_field_int);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? ab[index] : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? ab[index] : simple_func_int());
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? ab[index] : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? ab[index - 1] : 3);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? ab[index - 1] : -5);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? ab[index - 1] : local_int);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? ab[index - 1] : static_field_int);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? ab[index - 1] : simple_func_int());
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? 3 : 3);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? 3 : -5);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? 3 : local_int);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? 3 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_253()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? 3 : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? 3 : simple_func_int());
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? 3 : ab[index]);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? 3 : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? -5 : 3);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? -5 : -5);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? -5 : local_int);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? -5 : static_field_int);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? -5 : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? -5 : simple_func_int());
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? -5 : ab[index]);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? -5 : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? local_int : 3);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? local_int : -5);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? local_int : local_int);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? local_int : static_field_int);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? local_int : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? local_int : simple_func_int());
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? local_int : ab[index]);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? local_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_254()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? static_field_int : 3);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? static_field_int : -5);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? static_field_int : local_int);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? static_field_int : static_field_int);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? static_field_int : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? static_field_int : simple_func_int());
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? static_field_int : ab[index]);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? static_field_int : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? t1_i.mfi : 3);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? t1_i.mfi : -5);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? t1_i.mfi : local_int);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? t1_i.mfi : static_field_int);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? t1_i.mfi : simple_func_int());
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? t1_i.mfi : ab[index]);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? simple_func_int() : 3);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? simple_func_int() : -5);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? simple_func_int() : local_int);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? simple_func_int() : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_255()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? simple_func_int() : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? simple_func_int() : simple_func_int());
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? simple_func_int() : ab[index]);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? simple_func_int() : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? ab[index] : 3);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? ab[index] : -5);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? ab[index] : local_int);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? ab[index] : static_field_int);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? ab[index] : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? ab[index] : simple_func_int());
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? ab[index] : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? ab[index - 1] : 3);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? ab[index - 1] : -5);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? ab[index - 1] : local_int);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? ab[index - 1] : static_field_int);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? ab[index - 1] : simple_func_int());
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_true ^ t1_i.mfb_false ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_256()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ func_sb_true() ? 3 : 3);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? 3 : -5);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? 3 : local_int);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? 3 : static_field_int);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? 3 : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? 3 : simple_func_int());
        Sum += (t1_i.mfb_true ^ func_sb_true() ? 3 : ab[index]);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? 3 : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? -5 : 3);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? -5 : -5);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? -5 : local_int);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? -5 : static_field_int);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? -5 : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? -5 : simple_func_int());
        Sum += (t1_i.mfb_true ^ func_sb_true() ? -5 : ab[index]);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? -5 : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? local_int : 3);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? local_int : -5);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? local_int : local_int);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? local_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_257()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ func_sb_true() ? local_int : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? local_int : simple_func_int());
        Sum += (t1_i.mfb_true ^ func_sb_true() ? local_int : ab[index]);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? local_int : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? static_field_int : 3);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? static_field_int : -5);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? static_field_int : local_int);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? static_field_int : static_field_int);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? static_field_int : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? static_field_int : simple_func_int());
        Sum += (t1_i.mfb_true ^ func_sb_true() ? static_field_int : ab[index]);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? static_field_int : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? t1_i.mfi : 3);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? t1_i.mfi : -5);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? t1_i.mfi : local_int);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? t1_i.mfi : static_field_int);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? t1_i.mfi : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? t1_i.mfi : simple_func_int());
        Sum += (t1_i.mfb_true ^ func_sb_true() ? t1_i.mfi : ab[index]);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? t1_i.mfi : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_258()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ func_sb_true() ? simple_func_int() : 3);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? simple_func_int() : -5);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? simple_func_int() : local_int);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? simple_func_int() : static_field_int);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? simple_func_int() : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? simple_func_int() : simple_func_int());
        Sum += (t1_i.mfb_true ^ func_sb_true() ? simple_func_int() : ab[index]);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? simple_func_int() : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? ab[index] : 3);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? ab[index] : -5);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? ab[index] : local_int);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? ab[index] : static_field_int);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? ab[index] : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? ab[index] : simple_func_int());
        Sum += (t1_i.mfb_true ^ func_sb_true() ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? ab[index] : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? ab[index - 1] : 3);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? ab[index - 1] : -5);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? ab[index - 1] : local_int);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? ab[index - 1] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_259()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ func_sb_true() ? ab[index - 1] : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? ab[index - 1] : simple_func_int());
        Sum += (t1_i.mfb_true ^ func_sb_true() ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_true ^ func_sb_true() ? ab[index - 1] : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? 3 : 3);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? 3 : -5);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? 3 : local_int);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? 3 : static_field_int);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? 3 : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? 3 : simple_func_int());
        Sum += (t1_i.mfb_true ^ func_sb_false() ? 3 : ab[index]);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? 3 : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? -5 : 3);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? -5 : -5);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? -5 : local_int);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? -5 : static_field_int);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? -5 : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? -5 : simple_func_int());
        Sum += (t1_i.mfb_true ^ func_sb_false() ? -5 : ab[index]);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? -5 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_260()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ func_sb_false() ? local_int : 3);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? local_int : -5);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? local_int : local_int);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? local_int : static_field_int);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? local_int : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? local_int : simple_func_int());
        Sum += (t1_i.mfb_true ^ func_sb_false() ? local_int : ab[index]);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? local_int : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? static_field_int : 3);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? static_field_int : -5);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? static_field_int : local_int);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? static_field_int : static_field_int);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? static_field_int : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? static_field_int : simple_func_int());
        Sum += (t1_i.mfb_true ^ func_sb_false() ? static_field_int : ab[index]);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? static_field_int : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? t1_i.mfi : 3);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? t1_i.mfi : -5);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? t1_i.mfi : local_int);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? t1_i.mfi : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_261()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ func_sb_false() ? t1_i.mfi : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? t1_i.mfi : simple_func_int());
        Sum += (t1_i.mfb_true ^ func_sb_false() ? t1_i.mfi : ab[index]);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? t1_i.mfi : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? simple_func_int() : 3);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? simple_func_int() : -5);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? simple_func_int() : local_int);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? simple_func_int() : static_field_int);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? simple_func_int() : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? simple_func_int() : simple_func_int());
        Sum += (t1_i.mfb_true ^ func_sb_false() ? simple_func_int() : ab[index]);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? simple_func_int() : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? ab[index] : 3);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? ab[index] : -5);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? ab[index] : local_int);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? ab[index] : static_field_int);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? ab[index] : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? ab[index] : simple_func_int());
        Sum += (t1_i.mfb_true ^ func_sb_false() ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_262()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ func_sb_false() ? ab[index - 1] : 3);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? ab[index - 1] : -5);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? ab[index - 1] : local_int);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? ab[index - 1] : static_field_int);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? ab[index - 1] : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? ab[index - 1] : simple_func_int());
        Sum += (t1_i.mfb_true ^ func_sb_false() ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_true ^ func_sb_false() ? ab[index - 1] : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? 3 : 3);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? 3 : -5);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? 3 : local_int);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? 3 : static_field_int);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? 3 : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? 3 : simple_func_int());
        Sum += (t1_i.mfb_true ^ ab_true[index] ? 3 : ab[index]);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? 3 : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? -5 : 3);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? -5 : -5);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? -5 : local_int);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? -5 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_263()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ ab_true[index] ? -5 : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? -5 : simple_func_int());
        Sum += (t1_i.mfb_true ^ ab_true[index] ? -5 : ab[index]);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? -5 : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? local_int : 3);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? local_int : -5);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? local_int : local_int);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? local_int : static_field_int);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? local_int : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? local_int : simple_func_int());
        Sum += (t1_i.mfb_true ^ ab_true[index] ? local_int : ab[index]);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? local_int : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? static_field_int : 3);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? static_field_int : -5);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? static_field_int : local_int);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? static_field_int : static_field_int);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? static_field_int : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? static_field_int : simple_func_int());
        Sum += (t1_i.mfb_true ^ ab_true[index] ? static_field_int : ab[index]);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? static_field_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_264()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ ab_true[index] ? t1_i.mfi : 3);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? t1_i.mfi : -5);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? t1_i.mfi : local_int);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? t1_i.mfi : static_field_int);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? t1_i.mfi : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? t1_i.mfi : simple_func_int());
        Sum += (t1_i.mfb_true ^ ab_true[index] ? t1_i.mfi : ab[index]);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? t1_i.mfi : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? simple_func_int() : 3);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? simple_func_int() : -5);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? simple_func_int() : local_int);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? simple_func_int() : static_field_int);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? simple_func_int() : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? simple_func_int() : simple_func_int());
        Sum += (t1_i.mfb_true ^ ab_true[index] ? simple_func_int() : ab[index]);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? simple_func_int() : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? ab[index] : 3);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? ab[index] : -5);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? ab[index] : local_int);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? ab[index] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_265()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ ab_true[index] ? ab[index] : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? ab[index] : simple_func_int());
        Sum += (t1_i.mfb_true ^ ab_true[index] ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? ab[index] : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? ab[index - 1] : 3);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? ab[index - 1] : -5);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? ab[index - 1] : local_int);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? ab[index - 1] : static_field_int);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? ab[index - 1] : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? ab[index - 1] : simple_func_int());
        Sum += (t1_i.mfb_true ^ ab_true[index] ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_true ^ ab_true[index] ? ab[index - 1] : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? 3 : 3);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? 3 : -5);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? 3 : local_int);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? 3 : static_field_int);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? 3 : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? 3 : simple_func_int());
        Sum += (t1_i.mfb_true ^ ab_false[index] ? 3 : ab[index]);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? 3 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_266()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ ab_false[index] ? -5 : 3);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? -5 : -5);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? -5 : local_int);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? -5 : static_field_int);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? -5 : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? -5 : simple_func_int());
        Sum += (t1_i.mfb_true ^ ab_false[index] ? -5 : ab[index]);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? -5 : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? local_int : 3);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? local_int : -5);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? local_int : local_int);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? local_int : static_field_int);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? local_int : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? local_int : simple_func_int());
        Sum += (t1_i.mfb_true ^ ab_false[index] ? local_int : ab[index]);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? local_int : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? static_field_int : 3);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? static_field_int : -5);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? static_field_int : local_int);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? static_field_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_267()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ ab_false[index] ? static_field_int : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? static_field_int : simple_func_int());
        Sum += (t1_i.mfb_true ^ ab_false[index] ? static_field_int : ab[index]);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? static_field_int : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? t1_i.mfi : 3);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? t1_i.mfi : -5);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? t1_i.mfi : local_int);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? t1_i.mfi : static_field_int);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? t1_i.mfi : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? t1_i.mfi : simple_func_int());
        Sum += (t1_i.mfb_true ^ ab_false[index] ? t1_i.mfi : ab[index]);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? t1_i.mfi : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? simple_func_int() : 3);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? simple_func_int() : -5);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? simple_func_int() : local_int);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? simple_func_int() : static_field_int);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? simple_func_int() : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? simple_func_int() : simple_func_int());
        Sum += (t1_i.mfb_true ^ ab_false[index] ? simple_func_int() : ab[index]);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? simple_func_int() : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_268()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ^ ab_false[index] ? ab[index] : 3);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? ab[index] : -5);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? ab[index] : local_int);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? ab[index] : static_field_int);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? ab[index] : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? ab[index] : simple_func_int());
        Sum += (t1_i.mfb_true ^ ab_false[index] ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? ab[index] : ab[index - 1]);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? ab[index - 1] : 3);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? ab[index - 1] : -5);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? ab[index - 1] : local_int);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? ab[index - 1] : static_field_int);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? ab[index - 1] : t1_i.mfi);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? ab[index - 1] : simple_func_int());
        Sum += (t1_i.mfb_true ^ ab_false[index] ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_true ^ ab_false[index] ? ab[index - 1] : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ true ? 3 : 3);
        Sum += (t1_i.mfb_false ^ true ? 3 : -5);
        Sum += (t1_i.mfb_false ^ true ? 3 : local_int);
        Sum += (t1_i.mfb_false ^ true ? 3 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_269()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ true ? 3 : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ true ? 3 : simple_func_int());
        Sum += (t1_i.mfb_false ^ true ? 3 : ab[index]);
        Sum += (t1_i.mfb_false ^ true ? 3 : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ true ? -5 : 3);
        Sum += (t1_i.mfb_false ^ true ? -5 : -5);
        Sum += (t1_i.mfb_false ^ true ? -5 : local_int);
        Sum += (t1_i.mfb_false ^ true ? -5 : static_field_int);
        Sum += (t1_i.mfb_false ^ true ? -5 : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ true ? -5 : simple_func_int());
        Sum += (t1_i.mfb_false ^ true ? -5 : ab[index]);
        Sum += (t1_i.mfb_false ^ true ? -5 : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ true ? local_int : 3);
        Sum += (t1_i.mfb_false ^ true ? local_int : -5);
        Sum += (t1_i.mfb_false ^ true ? local_int : local_int);
        Sum += (t1_i.mfb_false ^ true ? local_int : static_field_int);
        Sum += (t1_i.mfb_false ^ true ? local_int : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ true ? local_int : simple_func_int());
        Sum += (t1_i.mfb_false ^ true ? local_int : ab[index]);
        Sum += (t1_i.mfb_false ^ true ? local_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_270()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ true ? static_field_int : 3);
        Sum += (t1_i.mfb_false ^ true ? static_field_int : -5);
        Sum += (t1_i.mfb_false ^ true ? static_field_int : local_int);
        Sum += (t1_i.mfb_false ^ true ? static_field_int : static_field_int);
        Sum += (t1_i.mfb_false ^ true ? static_field_int : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ true ? static_field_int : simple_func_int());
        Sum += (t1_i.mfb_false ^ true ? static_field_int : ab[index]);
        Sum += (t1_i.mfb_false ^ true ? static_field_int : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ true ? t1_i.mfi : 3);
        Sum += (t1_i.mfb_false ^ true ? t1_i.mfi : -5);
        Sum += (t1_i.mfb_false ^ true ? t1_i.mfi : local_int);
        Sum += (t1_i.mfb_false ^ true ? t1_i.mfi : static_field_int);
        Sum += (t1_i.mfb_false ^ true ? t1_i.mfi : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ true ? t1_i.mfi : simple_func_int());
        Sum += (t1_i.mfb_false ^ true ? t1_i.mfi : ab[index]);
        Sum += (t1_i.mfb_false ^ true ? t1_i.mfi : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ true ? simple_func_int() : 3);
        Sum += (t1_i.mfb_false ^ true ? simple_func_int() : -5);
        Sum += (t1_i.mfb_false ^ true ? simple_func_int() : local_int);
        Sum += (t1_i.mfb_false ^ true ? simple_func_int() : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_271()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ true ? simple_func_int() : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ true ? simple_func_int() : simple_func_int());
        Sum += (t1_i.mfb_false ^ true ? simple_func_int() : ab[index]);
        Sum += (t1_i.mfb_false ^ true ? simple_func_int() : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ true ? ab[index] : 3);
        Sum += (t1_i.mfb_false ^ true ? ab[index] : -5);
        Sum += (t1_i.mfb_false ^ true ? ab[index] : local_int);
        Sum += (t1_i.mfb_false ^ true ? ab[index] : static_field_int);
        Sum += (t1_i.mfb_false ^ true ? ab[index] : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ true ? ab[index] : simple_func_int());
        Sum += (t1_i.mfb_false ^ true ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_false ^ true ? ab[index] : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ true ? ab[index - 1] : 3);
        Sum += (t1_i.mfb_false ^ true ? ab[index - 1] : -5);
        Sum += (t1_i.mfb_false ^ true ? ab[index - 1] : local_int);
        Sum += (t1_i.mfb_false ^ true ? ab[index - 1] : static_field_int);
        Sum += (t1_i.mfb_false ^ true ? ab[index - 1] : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ true ? ab[index - 1] : simple_func_int());
        Sum += (t1_i.mfb_false ^ true ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_false ^ true ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_272()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ false ? 3 : 3);
        Sum += (t1_i.mfb_false ^ false ? 3 : -5);
        Sum += (t1_i.mfb_false ^ false ? 3 : local_int);
        Sum += (t1_i.mfb_false ^ false ? 3 : static_field_int);
        Sum += (t1_i.mfb_false ^ false ? 3 : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ false ? 3 : simple_func_int());
        Sum += (t1_i.mfb_false ^ false ? 3 : ab[index]);
        Sum += (t1_i.mfb_false ^ false ? 3 : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ false ? -5 : 3);
        Sum += (t1_i.mfb_false ^ false ? -5 : -5);
        Sum += (t1_i.mfb_false ^ false ? -5 : local_int);
        Sum += (t1_i.mfb_false ^ false ? -5 : static_field_int);
        Sum += (t1_i.mfb_false ^ false ? -5 : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ false ? -5 : simple_func_int());
        Sum += (t1_i.mfb_false ^ false ? -5 : ab[index]);
        Sum += (t1_i.mfb_false ^ false ? -5 : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ false ? local_int : 3);
        Sum += (t1_i.mfb_false ^ false ? local_int : -5);
        Sum += (t1_i.mfb_false ^ false ? local_int : local_int);
        Sum += (t1_i.mfb_false ^ false ? local_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_273()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ false ? local_int : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ false ? local_int : simple_func_int());
        Sum += (t1_i.mfb_false ^ false ? local_int : ab[index]);
        Sum += (t1_i.mfb_false ^ false ? local_int : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ false ? static_field_int : 3);
        Sum += (t1_i.mfb_false ^ false ? static_field_int : -5);
        Sum += (t1_i.mfb_false ^ false ? static_field_int : local_int);
        Sum += (t1_i.mfb_false ^ false ? static_field_int : static_field_int);
        Sum += (t1_i.mfb_false ^ false ? static_field_int : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ false ? static_field_int : simple_func_int());
        Sum += (t1_i.mfb_false ^ false ? static_field_int : ab[index]);
        Sum += (t1_i.mfb_false ^ false ? static_field_int : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ false ? t1_i.mfi : 3);
        Sum += (t1_i.mfb_false ^ false ? t1_i.mfi : -5);
        Sum += (t1_i.mfb_false ^ false ? t1_i.mfi : local_int);
        Sum += (t1_i.mfb_false ^ false ? t1_i.mfi : static_field_int);
        Sum += (t1_i.mfb_false ^ false ? t1_i.mfi : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ false ? t1_i.mfi : simple_func_int());
        Sum += (t1_i.mfb_false ^ false ? t1_i.mfi : ab[index]);
        Sum += (t1_i.mfb_false ^ false ? t1_i.mfi : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_274()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ false ? simple_func_int() : 3);
        Sum += (t1_i.mfb_false ^ false ? simple_func_int() : -5);
        Sum += (t1_i.mfb_false ^ false ? simple_func_int() : local_int);
        Sum += (t1_i.mfb_false ^ false ? simple_func_int() : static_field_int);
        Sum += (t1_i.mfb_false ^ false ? simple_func_int() : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ false ? simple_func_int() : simple_func_int());
        Sum += (t1_i.mfb_false ^ false ? simple_func_int() : ab[index]);
        Sum += (t1_i.mfb_false ^ false ? simple_func_int() : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ false ? ab[index] : 3);
        Sum += (t1_i.mfb_false ^ false ? ab[index] : -5);
        Sum += (t1_i.mfb_false ^ false ? ab[index] : local_int);
        Sum += (t1_i.mfb_false ^ false ? ab[index] : static_field_int);
        Sum += (t1_i.mfb_false ^ false ? ab[index] : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ false ? ab[index] : simple_func_int());
        Sum += (t1_i.mfb_false ^ false ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_false ^ false ? ab[index] : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ false ? ab[index - 1] : 3);
        Sum += (t1_i.mfb_false ^ false ? ab[index - 1] : -5);
        Sum += (t1_i.mfb_false ^ false ? ab[index - 1] : local_int);
        Sum += (t1_i.mfb_false ^ false ? ab[index - 1] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_275()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ false ? ab[index - 1] : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ false ? ab[index - 1] : simple_func_int());
        Sum += (t1_i.mfb_false ^ false ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_false ^ false ? ab[index - 1] : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ lb_true ? 3 : 3);
        Sum += (t1_i.mfb_false ^ lb_true ? 3 : -5);
        Sum += (t1_i.mfb_false ^ lb_true ? 3 : local_int);
        Sum += (t1_i.mfb_false ^ lb_true ? 3 : static_field_int);
        Sum += (t1_i.mfb_false ^ lb_true ? 3 : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ lb_true ? 3 : simple_func_int());
        Sum += (t1_i.mfb_false ^ lb_true ? 3 : ab[index]);
        Sum += (t1_i.mfb_false ^ lb_true ? 3 : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ lb_true ? -5 : 3);
        Sum += (t1_i.mfb_false ^ lb_true ? -5 : -5);
        Sum += (t1_i.mfb_false ^ lb_true ? -5 : local_int);
        Sum += (t1_i.mfb_false ^ lb_true ? -5 : static_field_int);
        Sum += (t1_i.mfb_false ^ lb_true ? -5 : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ lb_true ? -5 : simple_func_int());
        Sum += (t1_i.mfb_false ^ lb_true ? -5 : ab[index]);
        Sum += (t1_i.mfb_false ^ lb_true ? -5 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_276()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ lb_true ? local_int : 3);
        Sum += (t1_i.mfb_false ^ lb_true ? local_int : -5);
        Sum += (t1_i.mfb_false ^ lb_true ? local_int : local_int);
        Sum += (t1_i.mfb_false ^ lb_true ? local_int : static_field_int);
        Sum += (t1_i.mfb_false ^ lb_true ? local_int : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ lb_true ? local_int : simple_func_int());
        Sum += (t1_i.mfb_false ^ lb_true ? local_int : ab[index]);
        Sum += (t1_i.mfb_false ^ lb_true ? local_int : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ lb_true ? static_field_int : 3);
        Sum += (t1_i.mfb_false ^ lb_true ? static_field_int : -5);
        Sum += (t1_i.mfb_false ^ lb_true ? static_field_int : local_int);
        Sum += (t1_i.mfb_false ^ lb_true ? static_field_int : static_field_int);
        Sum += (t1_i.mfb_false ^ lb_true ? static_field_int : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ lb_true ? static_field_int : simple_func_int());
        Sum += (t1_i.mfb_false ^ lb_true ? static_field_int : ab[index]);
        Sum += (t1_i.mfb_false ^ lb_true ? static_field_int : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ lb_true ? t1_i.mfi : 3);
        Sum += (t1_i.mfb_false ^ lb_true ? t1_i.mfi : -5);
        Sum += (t1_i.mfb_false ^ lb_true ? t1_i.mfi : local_int);
        Sum += (t1_i.mfb_false ^ lb_true ? t1_i.mfi : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_277()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ lb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ lb_true ? t1_i.mfi : simple_func_int());
        Sum += (t1_i.mfb_false ^ lb_true ? t1_i.mfi : ab[index]);
        Sum += (t1_i.mfb_false ^ lb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ lb_true ? simple_func_int() : 3);
        Sum += (t1_i.mfb_false ^ lb_true ? simple_func_int() : -5);
        Sum += (t1_i.mfb_false ^ lb_true ? simple_func_int() : local_int);
        Sum += (t1_i.mfb_false ^ lb_true ? simple_func_int() : static_field_int);
        Sum += (t1_i.mfb_false ^ lb_true ? simple_func_int() : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ lb_true ? simple_func_int() : simple_func_int());
        Sum += (t1_i.mfb_false ^ lb_true ? simple_func_int() : ab[index]);
        Sum += (t1_i.mfb_false ^ lb_true ? simple_func_int() : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ lb_true ? ab[index] : 3);
        Sum += (t1_i.mfb_false ^ lb_true ? ab[index] : -5);
        Sum += (t1_i.mfb_false ^ lb_true ? ab[index] : local_int);
        Sum += (t1_i.mfb_false ^ lb_true ? ab[index] : static_field_int);
        Sum += (t1_i.mfb_false ^ lb_true ? ab[index] : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ lb_true ? ab[index] : simple_func_int());
        Sum += (t1_i.mfb_false ^ lb_true ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_false ^ lb_true ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_278()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ lb_true ? ab[index - 1] : 3);
        Sum += (t1_i.mfb_false ^ lb_true ? ab[index - 1] : -5);
        Sum += (t1_i.mfb_false ^ lb_true ? ab[index - 1] : local_int);
        Sum += (t1_i.mfb_false ^ lb_true ? ab[index - 1] : static_field_int);
        Sum += (t1_i.mfb_false ^ lb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ lb_true ? ab[index - 1] : simple_func_int());
        Sum += (t1_i.mfb_false ^ lb_true ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_false ^ lb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ lb_false ? 3 : 3);
        Sum += (t1_i.mfb_false ^ lb_false ? 3 : -5);
        Sum += (t1_i.mfb_false ^ lb_false ? 3 : local_int);
        Sum += (t1_i.mfb_false ^ lb_false ? 3 : static_field_int);
        Sum += (t1_i.mfb_false ^ lb_false ? 3 : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ lb_false ? 3 : simple_func_int());
        Sum += (t1_i.mfb_false ^ lb_false ? 3 : ab[index]);
        Sum += (t1_i.mfb_false ^ lb_false ? 3 : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ lb_false ? -5 : 3);
        Sum += (t1_i.mfb_false ^ lb_false ? -5 : -5);
        Sum += (t1_i.mfb_false ^ lb_false ? -5 : local_int);
        Sum += (t1_i.mfb_false ^ lb_false ? -5 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_279()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ lb_false ? -5 : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ lb_false ? -5 : simple_func_int());
        Sum += (t1_i.mfb_false ^ lb_false ? -5 : ab[index]);
        Sum += (t1_i.mfb_false ^ lb_false ? -5 : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ lb_false ? local_int : 3);
        Sum += (t1_i.mfb_false ^ lb_false ? local_int : -5);
        Sum += (t1_i.mfb_false ^ lb_false ? local_int : local_int);
        Sum += (t1_i.mfb_false ^ lb_false ? local_int : static_field_int);
        Sum += (t1_i.mfb_false ^ lb_false ? local_int : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ lb_false ? local_int : simple_func_int());
        Sum += (t1_i.mfb_false ^ lb_false ? local_int : ab[index]);
        Sum += (t1_i.mfb_false ^ lb_false ? local_int : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ lb_false ? static_field_int : 3);
        Sum += (t1_i.mfb_false ^ lb_false ? static_field_int : -5);
        Sum += (t1_i.mfb_false ^ lb_false ? static_field_int : local_int);
        Sum += (t1_i.mfb_false ^ lb_false ? static_field_int : static_field_int);
        Sum += (t1_i.mfb_false ^ lb_false ? static_field_int : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ lb_false ? static_field_int : simple_func_int());
        Sum += (t1_i.mfb_false ^ lb_false ? static_field_int : ab[index]);
        Sum += (t1_i.mfb_false ^ lb_false ? static_field_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_280()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ lb_false ? t1_i.mfi : 3);
        Sum += (t1_i.mfb_false ^ lb_false ? t1_i.mfi : -5);
        Sum += (t1_i.mfb_false ^ lb_false ? t1_i.mfi : local_int);
        Sum += (t1_i.mfb_false ^ lb_false ? t1_i.mfi : static_field_int);
        Sum += (t1_i.mfb_false ^ lb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ lb_false ? t1_i.mfi : simple_func_int());
        Sum += (t1_i.mfb_false ^ lb_false ? t1_i.mfi : ab[index]);
        Sum += (t1_i.mfb_false ^ lb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ lb_false ? simple_func_int() : 3);
        Sum += (t1_i.mfb_false ^ lb_false ? simple_func_int() : -5);
        Sum += (t1_i.mfb_false ^ lb_false ? simple_func_int() : local_int);
        Sum += (t1_i.mfb_false ^ lb_false ? simple_func_int() : static_field_int);
        Sum += (t1_i.mfb_false ^ lb_false ? simple_func_int() : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ lb_false ? simple_func_int() : simple_func_int());
        Sum += (t1_i.mfb_false ^ lb_false ? simple_func_int() : ab[index]);
        Sum += (t1_i.mfb_false ^ lb_false ? simple_func_int() : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ lb_false ? ab[index] : 3);
        Sum += (t1_i.mfb_false ^ lb_false ? ab[index] : -5);
        Sum += (t1_i.mfb_false ^ lb_false ? ab[index] : local_int);
        Sum += (t1_i.mfb_false ^ lb_false ? ab[index] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_281()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ lb_false ? ab[index] : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ lb_false ? ab[index] : simple_func_int());
        Sum += (t1_i.mfb_false ^ lb_false ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_false ^ lb_false ? ab[index] : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ lb_false ? ab[index - 1] : 3);
        Sum += (t1_i.mfb_false ^ lb_false ? ab[index - 1] : -5);
        Sum += (t1_i.mfb_false ^ lb_false ? ab[index - 1] : local_int);
        Sum += (t1_i.mfb_false ^ lb_false ? ab[index - 1] : static_field_int);
        Sum += (t1_i.mfb_false ^ lb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ lb_false ? ab[index - 1] : simple_func_int());
        Sum += (t1_i.mfb_false ^ lb_false ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_false ^ lb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ sfb_true ? 3 : 3);
        Sum += (t1_i.mfb_false ^ sfb_true ? 3 : -5);
        Sum += (t1_i.mfb_false ^ sfb_true ? 3 : local_int);
        Sum += (t1_i.mfb_false ^ sfb_true ? 3 : static_field_int);
        Sum += (t1_i.mfb_false ^ sfb_true ? 3 : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ sfb_true ? 3 : simple_func_int());
        Sum += (t1_i.mfb_false ^ sfb_true ? 3 : ab[index]);
        Sum += (t1_i.mfb_false ^ sfb_true ? 3 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_282()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ sfb_true ? -5 : 3);
        Sum += (t1_i.mfb_false ^ sfb_true ? -5 : -5);
        Sum += (t1_i.mfb_false ^ sfb_true ? -5 : local_int);
        Sum += (t1_i.mfb_false ^ sfb_true ? -5 : static_field_int);
        Sum += (t1_i.mfb_false ^ sfb_true ? -5 : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ sfb_true ? -5 : simple_func_int());
        Sum += (t1_i.mfb_false ^ sfb_true ? -5 : ab[index]);
        Sum += (t1_i.mfb_false ^ sfb_true ? -5 : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ sfb_true ? local_int : 3);
        Sum += (t1_i.mfb_false ^ sfb_true ? local_int : -5);
        Sum += (t1_i.mfb_false ^ sfb_true ? local_int : local_int);
        Sum += (t1_i.mfb_false ^ sfb_true ? local_int : static_field_int);
        Sum += (t1_i.mfb_false ^ sfb_true ? local_int : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ sfb_true ? local_int : simple_func_int());
        Sum += (t1_i.mfb_false ^ sfb_true ? local_int : ab[index]);
        Sum += (t1_i.mfb_false ^ sfb_true ? local_int : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ sfb_true ? static_field_int : 3);
        Sum += (t1_i.mfb_false ^ sfb_true ? static_field_int : -5);
        Sum += (t1_i.mfb_false ^ sfb_true ? static_field_int : local_int);
        Sum += (t1_i.mfb_false ^ sfb_true ? static_field_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_283()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ sfb_true ? static_field_int : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ sfb_true ? static_field_int : simple_func_int());
        Sum += (t1_i.mfb_false ^ sfb_true ? static_field_int : ab[index]);
        Sum += (t1_i.mfb_false ^ sfb_true ? static_field_int : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ sfb_true ? t1_i.mfi : 3);
        Sum += (t1_i.mfb_false ^ sfb_true ? t1_i.mfi : -5);
        Sum += (t1_i.mfb_false ^ sfb_true ? t1_i.mfi : local_int);
        Sum += (t1_i.mfb_false ^ sfb_true ? t1_i.mfi : static_field_int);
        Sum += (t1_i.mfb_false ^ sfb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ sfb_true ? t1_i.mfi : simple_func_int());
        Sum += (t1_i.mfb_false ^ sfb_true ? t1_i.mfi : ab[index]);
        Sum += (t1_i.mfb_false ^ sfb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ sfb_true ? simple_func_int() : 3);
        Sum += (t1_i.mfb_false ^ sfb_true ? simple_func_int() : -5);
        Sum += (t1_i.mfb_false ^ sfb_true ? simple_func_int() : local_int);
        Sum += (t1_i.mfb_false ^ sfb_true ? simple_func_int() : static_field_int);
        Sum += (t1_i.mfb_false ^ sfb_true ? simple_func_int() : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ sfb_true ? simple_func_int() : simple_func_int());
        Sum += (t1_i.mfb_false ^ sfb_true ? simple_func_int() : ab[index]);
        Sum += (t1_i.mfb_false ^ sfb_true ? simple_func_int() : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_284()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ sfb_true ? ab[index] : 3);
        Sum += (t1_i.mfb_false ^ sfb_true ? ab[index] : -5);
        Sum += (t1_i.mfb_false ^ sfb_true ? ab[index] : local_int);
        Sum += (t1_i.mfb_false ^ sfb_true ? ab[index] : static_field_int);
        Sum += (t1_i.mfb_false ^ sfb_true ? ab[index] : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ sfb_true ? ab[index] : simple_func_int());
        Sum += (t1_i.mfb_false ^ sfb_true ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_false ^ sfb_true ? ab[index] : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ sfb_true ? ab[index - 1] : 3);
        Sum += (t1_i.mfb_false ^ sfb_true ? ab[index - 1] : -5);
        Sum += (t1_i.mfb_false ^ sfb_true ? ab[index - 1] : local_int);
        Sum += (t1_i.mfb_false ^ sfb_true ? ab[index - 1] : static_field_int);
        Sum += (t1_i.mfb_false ^ sfb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ sfb_true ? ab[index - 1] : simple_func_int());
        Sum += (t1_i.mfb_false ^ sfb_true ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_false ^ sfb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ sfb_false ? 3 : 3);
        Sum += (t1_i.mfb_false ^ sfb_false ? 3 : -5);
        Sum += (t1_i.mfb_false ^ sfb_false ? 3 : local_int);
        Sum += (t1_i.mfb_false ^ sfb_false ? 3 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_285()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ sfb_false ? 3 : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ sfb_false ? 3 : simple_func_int());
        Sum += (t1_i.mfb_false ^ sfb_false ? 3 : ab[index]);
        Sum += (t1_i.mfb_false ^ sfb_false ? 3 : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ sfb_false ? -5 : 3);
        Sum += (t1_i.mfb_false ^ sfb_false ? -5 : -5);
        Sum += (t1_i.mfb_false ^ sfb_false ? -5 : local_int);
        Sum += (t1_i.mfb_false ^ sfb_false ? -5 : static_field_int);
        Sum += (t1_i.mfb_false ^ sfb_false ? -5 : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ sfb_false ? -5 : simple_func_int());
        Sum += (t1_i.mfb_false ^ sfb_false ? -5 : ab[index]);
        Sum += (t1_i.mfb_false ^ sfb_false ? -5 : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ sfb_false ? local_int : 3);
        Sum += (t1_i.mfb_false ^ sfb_false ? local_int : -5);
        Sum += (t1_i.mfb_false ^ sfb_false ? local_int : local_int);
        Sum += (t1_i.mfb_false ^ sfb_false ? local_int : static_field_int);
        Sum += (t1_i.mfb_false ^ sfb_false ? local_int : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ sfb_false ? local_int : simple_func_int());
        Sum += (t1_i.mfb_false ^ sfb_false ? local_int : ab[index]);
        Sum += (t1_i.mfb_false ^ sfb_false ? local_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_286()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ sfb_false ? static_field_int : 3);
        Sum += (t1_i.mfb_false ^ sfb_false ? static_field_int : -5);
        Sum += (t1_i.mfb_false ^ sfb_false ? static_field_int : local_int);
        Sum += (t1_i.mfb_false ^ sfb_false ? static_field_int : static_field_int);
        Sum += (t1_i.mfb_false ^ sfb_false ? static_field_int : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ sfb_false ? static_field_int : simple_func_int());
        Sum += (t1_i.mfb_false ^ sfb_false ? static_field_int : ab[index]);
        Sum += (t1_i.mfb_false ^ sfb_false ? static_field_int : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ sfb_false ? t1_i.mfi : 3);
        Sum += (t1_i.mfb_false ^ sfb_false ? t1_i.mfi : -5);
        Sum += (t1_i.mfb_false ^ sfb_false ? t1_i.mfi : local_int);
        Sum += (t1_i.mfb_false ^ sfb_false ? t1_i.mfi : static_field_int);
        Sum += (t1_i.mfb_false ^ sfb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ sfb_false ? t1_i.mfi : simple_func_int());
        Sum += (t1_i.mfb_false ^ sfb_false ? t1_i.mfi : ab[index]);
        Sum += (t1_i.mfb_false ^ sfb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ sfb_false ? simple_func_int() : 3);
        Sum += (t1_i.mfb_false ^ sfb_false ? simple_func_int() : -5);
        Sum += (t1_i.mfb_false ^ sfb_false ? simple_func_int() : local_int);
        Sum += (t1_i.mfb_false ^ sfb_false ? simple_func_int() : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_287()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ sfb_false ? simple_func_int() : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ sfb_false ? simple_func_int() : simple_func_int());
        Sum += (t1_i.mfb_false ^ sfb_false ? simple_func_int() : ab[index]);
        Sum += (t1_i.mfb_false ^ sfb_false ? simple_func_int() : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ sfb_false ? ab[index] : 3);
        Sum += (t1_i.mfb_false ^ sfb_false ? ab[index] : -5);
        Sum += (t1_i.mfb_false ^ sfb_false ? ab[index] : local_int);
        Sum += (t1_i.mfb_false ^ sfb_false ? ab[index] : static_field_int);
        Sum += (t1_i.mfb_false ^ sfb_false ? ab[index] : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ sfb_false ? ab[index] : simple_func_int());
        Sum += (t1_i.mfb_false ^ sfb_false ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_false ^ sfb_false ? ab[index] : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ sfb_false ? ab[index - 1] : 3);
        Sum += (t1_i.mfb_false ^ sfb_false ? ab[index - 1] : -5);
        Sum += (t1_i.mfb_false ^ sfb_false ? ab[index - 1] : local_int);
        Sum += (t1_i.mfb_false ^ sfb_false ? ab[index - 1] : static_field_int);
        Sum += (t1_i.mfb_false ^ sfb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ sfb_false ? ab[index - 1] : simple_func_int());
        Sum += (t1_i.mfb_false ^ sfb_false ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_false ^ sfb_false ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_288()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? 3 : 3);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? 3 : -5);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? 3 : local_int);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? 3 : static_field_int);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? 3 : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? 3 : simple_func_int());
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? 3 : ab[index]);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? 3 : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? -5 : 3);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? -5 : -5);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? -5 : local_int);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? -5 : static_field_int);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? -5 : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? -5 : simple_func_int());
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? -5 : ab[index]);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? -5 : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? local_int : 3);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? local_int : -5);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? local_int : local_int);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? local_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_289()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? local_int : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? local_int : simple_func_int());
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? local_int : ab[index]);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? local_int : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? static_field_int : 3);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? static_field_int : -5);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? static_field_int : local_int);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? static_field_int : static_field_int);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? static_field_int : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? static_field_int : simple_func_int());
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? static_field_int : ab[index]);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? static_field_int : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? t1_i.mfi : 3);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? t1_i.mfi : -5);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? t1_i.mfi : local_int);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? t1_i.mfi : static_field_int);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? t1_i.mfi : simple_func_int());
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? t1_i.mfi : ab[index]);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? t1_i.mfi : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_290()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? simple_func_int() : 3);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? simple_func_int() : -5);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? simple_func_int() : local_int);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? simple_func_int() : static_field_int);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? simple_func_int() : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? simple_func_int() : simple_func_int());
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? simple_func_int() : ab[index]);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? simple_func_int() : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? ab[index] : 3);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? ab[index] : -5);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? ab[index] : local_int);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? ab[index] : static_field_int);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? ab[index] : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? ab[index] : simple_func_int());
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? ab[index] : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? ab[index - 1] : 3);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? ab[index - 1] : -5);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? ab[index - 1] : local_int);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? ab[index - 1] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_291()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? ab[index - 1] : simple_func_int());
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? 3 : 3);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? 3 : -5);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? 3 : local_int);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? 3 : static_field_int);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? 3 : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? 3 : simple_func_int());
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? 3 : ab[index]);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? 3 : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? -5 : 3);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? -5 : -5);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? -5 : local_int);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? -5 : static_field_int);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? -5 : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? -5 : simple_func_int());
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? -5 : ab[index]);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? -5 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_292()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? local_int : 3);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? local_int : -5);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? local_int : local_int);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? local_int : static_field_int);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? local_int : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? local_int : simple_func_int());
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? local_int : ab[index]);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? local_int : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? static_field_int : 3);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? static_field_int : -5);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? static_field_int : local_int);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? static_field_int : static_field_int);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? static_field_int : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? static_field_int : simple_func_int());
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? static_field_int : ab[index]);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? static_field_int : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? t1_i.mfi : 3);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? t1_i.mfi : -5);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? t1_i.mfi : local_int);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? t1_i.mfi : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_293()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? t1_i.mfi : simple_func_int());
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? t1_i.mfi : ab[index]);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? simple_func_int() : 3);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? simple_func_int() : -5);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? simple_func_int() : local_int);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? simple_func_int() : static_field_int);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? simple_func_int() : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? simple_func_int() : simple_func_int());
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? simple_func_int() : ab[index]);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? simple_func_int() : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? ab[index] : 3);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? ab[index] : -5);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? ab[index] : local_int);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? ab[index] : static_field_int);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? ab[index] : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? ab[index] : simple_func_int());
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_294()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? ab[index - 1] : 3);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? ab[index - 1] : -5);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? ab[index - 1] : local_int);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? ab[index - 1] : static_field_int);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? ab[index - 1] : simple_func_int());
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_false ^ t1_i.mfb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? 3 : 3);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? 3 : -5);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? 3 : local_int);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? 3 : static_field_int);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? 3 : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? 3 : simple_func_int());
        Sum += (t1_i.mfb_false ^ func_sb_true() ? 3 : ab[index]);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? 3 : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? -5 : 3);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? -5 : -5);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? -5 : local_int);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? -5 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_295()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ func_sb_true() ? -5 : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? -5 : simple_func_int());
        Sum += (t1_i.mfb_false ^ func_sb_true() ? -5 : ab[index]);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? -5 : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? local_int : 3);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? local_int : -5);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? local_int : local_int);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? local_int : static_field_int);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? local_int : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? local_int : simple_func_int());
        Sum += (t1_i.mfb_false ^ func_sb_true() ? local_int : ab[index]);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? local_int : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? static_field_int : 3);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? static_field_int : -5);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? static_field_int : local_int);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? static_field_int : static_field_int);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? static_field_int : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? static_field_int : simple_func_int());
        Sum += (t1_i.mfb_false ^ func_sb_true() ? static_field_int : ab[index]);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? static_field_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_296()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ func_sb_true() ? t1_i.mfi : 3);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? t1_i.mfi : -5);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? t1_i.mfi : local_int);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? t1_i.mfi : static_field_int);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? t1_i.mfi : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? t1_i.mfi : simple_func_int());
        Sum += (t1_i.mfb_false ^ func_sb_true() ? t1_i.mfi : ab[index]);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? t1_i.mfi : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? simple_func_int() : 3);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? simple_func_int() : -5);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? simple_func_int() : local_int);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? simple_func_int() : static_field_int);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? simple_func_int() : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? simple_func_int() : simple_func_int());
        Sum += (t1_i.mfb_false ^ func_sb_true() ? simple_func_int() : ab[index]);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? simple_func_int() : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? ab[index] : 3);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? ab[index] : -5);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? ab[index] : local_int);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? ab[index] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_297()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ func_sb_true() ? ab[index] : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? ab[index] : simple_func_int());
        Sum += (t1_i.mfb_false ^ func_sb_true() ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? ab[index] : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? ab[index - 1] : 3);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? ab[index - 1] : -5);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? ab[index - 1] : local_int);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? ab[index - 1] : static_field_int);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? ab[index - 1] : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? ab[index - 1] : simple_func_int());
        Sum += (t1_i.mfb_false ^ func_sb_true() ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_false ^ func_sb_true() ? ab[index - 1] : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? 3 : 3);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? 3 : -5);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? 3 : local_int);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? 3 : static_field_int);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? 3 : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? 3 : simple_func_int());
        Sum += (t1_i.mfb_false ^ func_sb_false() ? 3 : ab[index]);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? 3 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_298()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ func_sb_false() ? -5 : 3);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? -5 : -5);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? -5 : local_int);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? -5 : static_field_int);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? -5 : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? -5 : simple_func_int());
        Sum += (t1_i.mfb_false ^ func_sb_false() ? -5 : ab[index]);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? -5 : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? local_int : 3);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? local_int : -5);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? local_int : local_int);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? local_int : static_field_int);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? local_int : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? local_int : simple_func_int());
        Sum += (t1_i.mfb_false ^ func_sb_false() ? local_int : ab[index]);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? local_int : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? static_field_int : 3);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? static_field_int : -5);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? static_field_int : local_int);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? static_field_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_299()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ func_sb_false() ? static_field_int : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? static_field_int : simple_func_int());
        Sum += (t1_i.mfb_false ^ func_sb_false() ? static_field_int : ab[index]);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? static_field_int : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? t1_i.mfi : 3);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? t1_i.mfi : -5);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? t1_i.mfi : local_int);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? t1_i.mfi : static_field_int);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? t1_i.mfi : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? t1_i.mfi : simple_func_int());
        Sum += (t1_i.mfb_false ^ func_sb_false() ? t1_i.mfi : ab[index]);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? t1_i.mfi : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? simple_func_int() : 3);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? simple_func_int() : -5);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? simple_func_int() : local_int);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? simple_func_int() : static_field_int);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? simple_func_int() : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? simple_func_int() : simple_func_int());
        Sum += (t1_i.mfb_false ^ func_sb_false() ? simple_func_int() : ab[index]);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? simple_func_int() : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_300()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ func_sb_false() ? ab[index] : 3);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? ab[index] : -5);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? ab[index] : local_int);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? ab[index] : static_field_int);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? ab[index] : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? ab[index] : simple_func_int());
        Sum += (t1_i.mfb_false ^ func_sb_false() ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? ab[index] : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? ab[index - 1] : 3);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? ab[index - 1] : -5);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? ab[index - 1] : local_int);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? ab[index - 1] : static_field_int);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? ab[index - 1] : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? ab[index - 1] : simple_func_int());
        Sum += (t1_i.mfb_false ^ func_sb_false() ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_false ^ func_sb_false() ? ab[index - 1] : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? 3 : 3);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? 3 : -5);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? 3 : local_int);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? 3 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_301()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ ab_true[index] ? 3 : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? 3 : simple_func_int());
        Sum += (t1_i.mfb_false ^ ab_true[index] ? 3 : ab[index]);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? 3 : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? -5 : 3);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? -5 : -5);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? -5 : local_int);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? -5 : static_field_int);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? -5 : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? -5 : simple_func_int());
        Sum += (t1_i.mfb_false ^ ab_true[index] ? -5 : ab[index]);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? -5 : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? local_int : 3);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? local_int : -5);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? local_int : local_int);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? local_int : static_field_int);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? local_int : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? local_int : simple_func_int());
        Sum += (t1_i.mfb_false ^ ab_true[index] ? local_int : ab[index]);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? local_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_302()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ ab_true[index] ? static_field_int : 3);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? static_field_int : -5);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? static_field_int : local_int);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? static_field_int : static_field_int);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? static_field_int : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? static_field_int : simple_func_int());
        Sum += (t1_i.mfb_false ^ ab_true[index] ? static_field_int : ab[index]);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? static_field_int : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? t1_i.mfi : 3);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? t1_i.mfi : -5);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? t1_i.mfi : local_int);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? t1_i.mfi : static_field_int);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? t1_i.mfi : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? t1_i.mfi : simple_func_int());
        Sum += (t1_i.mfb_false ^ ab_true[index] ? t1_i.mfi : ab[index]);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? t1_i.mfi : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? simple_func_int() : 3);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? simple_func_int() : -5);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? simple_func_int() : local_int);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? simple_func_int() : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_303()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ ab_true[index] ? simple_func_int() : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? simple_func_int() : simple_func_int());
        Sum += (t1_i.mfb_false ^ ab_true[index] ? simple_func_int() : ab[index]);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? simple_func_int() : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? ab[index] : 3);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? ab[index] : -5);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? ab[index] : local_int);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? ab[index] : static_field_int);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? ab[index] : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? ab[index] : simple_func_int());
        Sum += (t1_i.mfb_false ^ ab_true[index] ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? ab[index] : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? ab[index - 1] : 3);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? ab[index - 1] : -5);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? ab[index - 1] : local_int);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? ab[index - 1] : static_field_int);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? ab[index - 1] : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? ab[index - 1] : simple_func_int());
        Sum += (t1_i.mfb_false ^ ab_true[index] ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_false ^ ab_true[index] ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_304()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ ab_false[index] ? 3 : 3);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? 3 : -5);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? 3 : local_int);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? 3 : static_field_int);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? 3 : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? 3 : simple_func_int());
        Sum += (t1_i.mfb_false ^ ab_false[index] ? 3 : ab[index]);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? 3 : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? -5 : 3);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? -5 : -5);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? -5 : local_int);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? -5 : static_field_int);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? -5 : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? -5 : simple_func_int());
        Sum += (t1_i.mfb_false ^ ab_false[index] ? -5 : ab[index]);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? -5 : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? local_int : 3);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? local_int : -5);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? local_int : local_int);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? local_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_305()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ ab_false[index] ? local_int : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? local_int : simple_func_int());
        Sum += (t1_i.mfb_false ^ ab_false[index] ? local_int : ab[index]);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? local_int : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? static_field_int : 3);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? static_field_int : -5);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? static_field_int : local_int);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? static_field_int : static_field_int);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? static_field_int : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? static_field_int : simple_func_int());
        Sum += (t1_i.mfb_false ^ ab_false[index] ? static_field_int : ab[index]);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? static_field_int : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? t1_i.mfi : 3);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? t1_i.mfi : -5);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? t1_i.mfi : local_int);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? t1_i.mfi : static_field_int);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? t1_i.mfi : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? t1_i.mfi : simple_func_int());
        Sum += (t1_i.mfb_false ^ ab_false[index] ? t1_i.mfi : ab[index]);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? t1_i.mfi : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_306()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ ab_false[index] ? simple_func_int() : 3);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? simple_func_int() : -5);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? simple_func_int() : local_int);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? simple_func_int() : static_field_int);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? simple_func_int() : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? simple_func_int() : simple_func_int());
        Sum += (t1_i.mfb_false ^ ab_false[index] ? simple_func_int() : ab[index]);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? simple_func_int() : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? ab[index] : 3);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? ab[index] : -5);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? ab[index] : local_int);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? ab[index] : static_field_int);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? ab[index] : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? ab[index] : simple_func_int());
        Sum += (t1_i.mfb_false ^ ab_false[index] ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? ab[index] : ab[index - 1]);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? ab[index - 1] : 3);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? ab[index - 1] : -5);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? ab[index - 1] : local_int);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? ab[index - 1] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_307()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ^ ab_false[index] ? ab[index - 1] : t1_i.mfi);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? ab[index - 1] : simple_func_int());
        Sum += (t1_i.mfb_false ^ ab_false[index] ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_false ^ ab_false[index] ? ab[index - 1] : ab[index - 1]);
        Sum += (func_sb_true() ^ true ? 3 : 3);
        Sum += (func_sb_true() ^ true ? 3 : -5);
        Sum += (func_sb_true() ^ true ? 3 : local_int);
        Sum += (func_sb_true() ^ true ? 3 : static_field_int);
        Sum += (func_sb_true() ^ true ? 3 : t1_i.mfi);
        Sum += (func_sb_true() ^ true ? 3 : simple_func_int());
        Sum += (func_sb_true() ^ true ? 3 : ab[index]);
        Sum += (func_sb_true() ^ true ? 3 : ab[index - 1]);
        Sum += (func_sb_true() ^ true ? -5 : 3);
        Sum += (func_sb_true() ^ true ? -5 : -5);
        Sum += (func_sb_true() ^ true ? -5 : local_int);
        Sum += (func_sb_true() ^ true ? -5 : static_field_int);
        Sum += (func_sb_true() ^ true ? -5 : t1_i.mfi);
        Sum += (func_sb_true() ^ true ? -5 : simple_func_int());
        Sum += (func_sb_true() ^ true ? -5 : ab[index]);
        Sum += (func_sb_true() ^ true ? -5 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_308()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ true ? local_int : 3);
        Sum += (func_sb_true() ^ true ? local_int : -5);
        Sum += (func_sb_true() ^ true ? local_int : local_int);
        Sum += (func_sb_true() ^ true ? local_int : static_field_int);
        Sum += (func_sb_true() ^ true ? local_int : t1_i.mfi);
        Sum += (func_sb_true() ^ true ? local_int : simple_func_int());
        Sum += (func_sb_true() ^ true ? local_int : ab[index]);
        Sum += (func_sb_true() ^ true ? local_int : ab[index - 1]);
        Sum += (func_sb_true() ^ true ? static_field_int : 3);
        Sum += (func_sb_true() ^ true ? static_field_int : -5);
        Sum += (func_sb_true() ^ true ? static_field_int : local_int);
        Sum += (func_sb_true() ^ true ? static_field_int : static_field_int);
        Sum += (func_sb_true() ^ true ? static_field_int : t1_i.mfi);
        Sum += (func_sb_true() ^ true ? static_field_int : simple_func_int());
        Sum += (func_sb_true() ^ true ? static_field_int : ab[index]);
        Sum += (func_sb_true() ^ true ? static_field_int : ab[index - 1]);
        Sum += (func_sb_true() ^ true ? t1_i.mfi : 3);
        Sum += (func_sb_true() ^ true ? t1_i.mfi : -5);
        Sum += (func_sb_true() ^ true ? t1_i.mfi : local_int);
        Sum += (func_sb_true() ^ true ? t1_i.mfi : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_309()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ true ? t1_i.mfi : t1_i.mfi);
        Sum += (func_sb_true() ^ true ? t1_i.mfi : simple_func_int());
        Sum += (func_sb_true() ^ true ? t1_i.mfi : ab[index]);
        Sum += (func_sb_true() ^ true ? t1_i.mfi : ab[index - 1]);
        Sum += (func_sb_true() ^ true ? simple_func_int() : 3);
        Sum += (func_sb_true() ^ true ? simple_func_int() : -5);
        Sum += (func_sb_true() ^ true ? simple_func_int() : local_int);
        Sum += (func_sb_true() ^ true ? simple_func_int() : static_field_int);
        Sum += (func_sb_true() ^ true ? simple_func_int() : t1_i.mfi);
        Sum += (func_sb_true() ^ true ? simple_func_int() : simple_func_int());
        Sum += (func_sb_true() ^ true ? simple_func_int() : ab[index]);
        Sum += (func_sb_true() ^ true ? simple_func_int() : ab[index - 1]);
        Sum += (func_sb_true() ^ true ? ab[index] : 3);
        Sum += (func_sb_true() ^ true ? ab[index] : -5);
        Sum += (func_sb_true() ^ true ? ab[index] : local_int);
        Sum += (func_sb_true() ^ true ? ab[index] : static_field_int);
        Sum += (func_sb_true() ^ true ? ab[index] : t1_i.mfi);
        Sum += (func_sb_true() ^ true ? ab[index] : simple_func_int());
        Sum += (func_sb_true() ^ true ? ab[index] : ab[index]);
        Sum += (func_sb_true() ^ true ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_310()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ true ? ab[index - 1] : 3);
        Sum += (func_sb_true() ^ true ? ab[index - 1] : -5);
        Sum += (func_sb_true() ^ true ? ab[index - 1] : local_int);
        Sum += (func_sb_true() ^ true ? ab[index - 1] : static_field_int);
        Sum += (func_sb_true() ^ true ? ab[index - 1] : t1_i.mfi);
        Sum += (func_sb_true() ^ true ? ab[index - 1] : simple_func_int());
        Sum += (func_sb_true() ^ true ? ab[index - 1] : ab[index]);
        Sum += (func_sb_true() ^ true ? ab[index - 1] : ab[index - 1]);
        Sum += (func_sb_true() ^ false ? 3 : 3);
        Sum += (func_sb_true() ^ false ? 3 : -5);
        Sum += (func_sb_true() ^ false ? 3 : local_int);
        Sum += (func_sb_true() ^ false ? 3 : static_field_int);
        Sum += (func_sb_true() ^ false ? 3 : t1_i.mfi);
        Sum += (func_sb_true() ^ false ? 3 : simple_func_int());
        Sum += (func_sb_true() ^ false ? 3 : ab[index]);
        Sum += (func_sb_true() ^ false ? 3 : ab[index - 1]);
        Sum += (func_sb_true() ^ false ? -5 : 3);
        Sum += (func_sb_true() ^ false ? -5 : -5);
        Sum += (func_sb_true() ^ false ? -5 : local_int);
        Sum += (func_sb_true() ^ false ? -5 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_311()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ false ? -5 : t1_i.mfi);
        Sum += (func_sb_true() ^ false ? -5 : simple_func_int());
        Sum += (func_sb_true() ^ false ? -5 : ab[index]);
        Sum += (func_sb_true() ^ false ? -5 : ab[index - 1]);
        Sum += (func_sb_true() ^ false ? local_int : 3);
        Sum += (func_sb_true() ^ false ? local_int : -5);
        Sum += (func_sb_true() ^ false ? local_int : local_int);
        Sum += (func_sb_true() ^ false ? local_int : static_field_int);
        Sum += (func_sb_true() ^ false ? local_int : t1_i.mfi);
        Sum += (func_sb_true() ^ false ? local_int : simple_func_int());
        Sum += (func_sb_true() ^ false ? local_int : ab[index]);
        Sum += (func_sb_true() ^ false ? local_int : ab[index - 1]);
        Sum += (func_sb_true() ^ false ? static_field_int : 3);
        Sum += (func_sb_true() ^ false ? static_field_int : -5);
        Sum += (func_sb_true() ^ false ? static_field_int : local_int);
        Sum += (func_sb_true() ^ false ? static_field_int : static_field_int);
        Sum += (func_sb_true() ^ false ? static_field_int : t1_i.mfi);
        Sum += (func_sb_true() ^ false ? static_field_int : simple_func_int());
        Sum += (func_sb_true() ^ false ? static_field_int : ab[index]);
        Sum += (func_sb_true() ^ false ? static_field_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_312()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ false ? t1_i.mfi : 3);
        Sum += (func_sb_true() ^ false ? t1_i.mfi : -5);
        Sum += (func_sb_true() ^ false ? t1_i.mfi : local_int);
        Sum += (func_sb_true() ^ false ? t1_i.mfi : static_field_int);
        Sum += (func_sb_true() ^ false ? t1_i.mfi : t1_i.mfi);
        Sum += (func_sb_true() ^ false ? t1_i.mfi : simple_func_int());
        Sum += (func_sb_true() ^ false ? t1_i.mfi : ab[index]);
        Sum += (func_sb_true() ^ false ? t1_i.mfi : ab[index - 1]);
        Sum += (func_sb_true() ^ false ? simple_func_int() : 3);
        Sum += (func_sb_true() ^ false ? simple_func_int() : -5);
        Sum += (func_sb_true() ^ false ? simple_func_int() : local_int);
        Sum += (func_sb_true() ^ false ? simple_func_int() : static_field_int);
        Sum += (func_sb_true() ^ false ? simple_func_int() : t1_i.mfi);
        Sum += (func_sb_true() ^ false ? simple_func_int() : simple_func_int());
        Sum += (func_sb_true() ^ false ? simple_func_int() : ab[index]);
        Sum += (func_sb_true() ^ false ? simple_func_int() : ab[index - 1]);
        Sum += (func_sb_true() ^ false ? ab[index] : 3);
        Sum += (func_sb_true() ^ false ? ab[index] : -5);
        Sum += (func_sb_true() ^ false ? ab[index] : local_int);
        Sum += (func_sb_true() ^ false ? ab[index] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_313()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ false ? ab[index] : t1_i.mfi);
        Sum += (func_sb_true() ^ false ? ab[index] : simple_func_int());
        Sum += (func_sb_true() ^ false ? ab[index] : ab[index]);
        Sum += (func_sb_true() ^ false ? ab[index] : ab[index - 1]);
        Sum += (func_sb_true() ^ false ? ab[index - 1] : 3);
        Sum += (func_sb_true() ^ false ? ab[index - 1] : -5);
        Sum += (func_sb_true() ^ false ? ab[index - 1] : local_int);
        Sum += (func_sb_true() ^ false ? ab[index - 1] : static_field_int);
        Sum += (func_sb_true() ^ false ? ab[index - 1] : t1_i.mfi);
        Sum += (func_sb_true() ^ false ? ab[index - 1] : simple_func_int());
        Sum += (func_sb_true() ^ false ? ab[index - 1] : ab[index]);
        Sum += (func_sb_true() ^ false ? ab[index - 1] : ab[index - 1]);
        Sum += (func_sb_true() ^ lb_true ? 3 : 3);
        Sum += (func_sb_true() ^ lb_true ? 3 : -5);
        Sum += (func_sb_true() ^ lb_true ? 3 : local_int);
        Sum += (func_sb_true() ^ lb_true ? 3 : static_field_int);
        Sum += (func_sb_true() ^ lb_true ? 3 : t1_i.mfi);
        Sum += (func_sb_true() ^ lb_true ? 3 : simple_func_int());
        Sum += (func_sb_true() ^ lb_true ? 3 : ab[index]);
        Sum += (func_sb_true() ^ lb_true ? 3 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_314()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ lb_true ? -5 : 3);
        Sum += (func_sb_true() ^ lb_true ? -5 : -5);
        Sum += (func_sb_true() ^ lb_true ? -5 : local_int);
        Sum += (func_sb_true() ^ lb_true ? -5 : static_field_int);
        Sum += (func_sb_true() ^ lb_true ? -5 : t1_i.mfi);
        Sum += (func_sb_true() ^ lb_true ? -5 : simple_func_int());
        Sum += (func_sb_true() ^ lb_true ? -5 : ab[index]);
        Sum += (func_sb_true() ^ lb_true ? -5 : ab[index - 1]);
        Sum += (func_sb_true() ^ lb_true ? local_int : 3);
        Sum += (func_sb_true() ^ lb_true ? local_int : -5);
        Sum += (func_sb_true() ^ lb_true ? local_int : local_int);
        Sum += (func_sb_true() ^ lb_true ? local_int : static_field_int);
        Sum += (func_sb_true() ^ lb_true ? local_int : t1_i.mfi);
        Sum += (func_sb_true() ^ lb_true ? local_int : simple_func_int());
        Sum += (func_sb_true() ^ lb_true ? local_int : ab[index]);
        Sum += (func_sb_true() ^ lb_true ? local_int : ab[index - 1]);
        Sum += (func_sb_true() ^ lb_true ? static_field_int : 3);
        Sum += (func_sb_true() ^ lb_true ? static_field_int : -5);
        Sum += (func_sb_true() ^ lb_true ? static_field_int : local_int);
        Sum += (func_sb_true() ^ lb_true ? static_field_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_315()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ lb_true ? static_field_int : t1_i.mfi);
        Sum += (func_sb_true() ^ lb_true ? static_field_int : simple_func_int());
        Sum += (func_sb_true() ^ lb_true ? static_field_int : ab[index]);
        Sum += (func_sb_true() ^ lb_true ? static_field_int : ab[index - 1]);
        Sum += (func_sb_true() ^ lb_true ? t1_i.mfi : 3);
        Sum += (func_sb_true() ^ lb_true ? t1_i.mfi : -5);
        Sum += (func_sb_true() ^ lb_true ? t1_i.mfi : local_int);
        Sum += (func_sb_true() ^ lb_true ? t1_i.mfi : static_field_int);
        Sum += (func_sb_true() ^ lb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (func_sb_true() ^ lb_true ? t1_i.mfi : simple_func_int());
        Sum += (func_sb_true() ^ lb_true ? t1_i.mfi : ab[index]);
        Sum += (func_sb_true() ^ lb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (func_sb_true() ^ lb_true ? simple_func_int() : 3);
        Sum += (func_sb_true() ^ lb_true ? simple_func_int() : -5);
        Sum += (func_sb_true() ^ lb_true ? simple_func_int() : local_int);
        Sum += (func_sb_true() ^ lb_true ? simple_func_int() : static_field_int);
        Sum += (func_sb_true() ^ lb_true ? simple_func_int() : t1_i.mfi);
        Sum += (func_sb_true() ^ lb_true ? simple_func_int() : simple_func_int());
        Sum += (func_sb_true() ^ lb_true ? simple_func_int() : ab[index]);
        Sum += (func_sb_true() ^ lb_true ? simple_func_int() : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_316()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ lb_true ? ab[index] : 3);
        Sum += (func_sb_true() ^ lb_true ? ab[index] : -5);
        Sum += (func_sb_true() ^ lb_true ? ab[index] : local_int);
        Sum += (func_sb_true() ^ lb_true ? ab[index] : static_field_int);
        Sum += (func_sb_true() ^ lb_true ? ab[index] : t1_i.mfi);
        Sum += (func_sb_true() ^ lb_true ? ab[index] : simple_func_int());
        Sum += (func_sb_true() ^ lb_true ? ab[index] : ab[index]);
        Sum += (func_sb_true() ^ lb_true ? ab[index] : ab[index - 1]);
        Sum += (func_sb_true() ^ lb_true ? ab[index - 1] : 3);
        Sum += (func_sb_true() ^ lb_true ? ab[index - 1] : -5);
        Sum += (func_sb_true() ^ lb_true ? ab[index - 1] : local_int);
        Sum += (func_sb_true() ^ lb_true ? ab[index - 1] : static_field_int);
        Sum += (func_sb_true() ^ lb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (func_sb_true() ^ lb_true ? ab[index - 1] : simple_func_int());
        Sum += (func_sb_true() ^ lb_true ? ab[index - 1] : ab[index]);
        Sum += (func_sb_true() ^ lb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (func_sb_true() ^ lb_false ? 3 : 3);
        Sum += (func_sb_true() ^ lb_false ? 3 : -5);
        Sum += (func_sb_true() ^ lb_false ? 3 : local_int);
        Sum += (func_sb_true() ^ lb_false ? 3 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_317()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ lb_false ? 3 : t1_i.mfi);
        Sum += (func_sb_true() ^ lb_false ? 3 : simple_func_int());
        Sum += (func_sb_true() ^ lb_false ? 3 : ab[index]);
        Sum += (func_sb_true() ^ lb_false ? 3 : ab[index - 1]);
        Sum += (func_sb_true() ^ lb_false ? -5 : 3);
        Sum += (func_sb_true() ^ lb_false ? -5 : -5);
        Sum += (func_sb_true() ^ lb_false ? -5 : local_int);
        Sum += (func_sb_true() ^ lb_false ? -5 : static_field_int);
        Sum += (func_sb_true() ^ lb_false ? -5 : t1_i.mfi);
        Sum += (func_sb_true() ^ lb_false ? -5 : simple_func_int());
        Sum += (func_sb_true() ^ lb_false ? -5 : ab[index]);
        Sum += (func_sb_true() ^ lb_false ? -5 : ab[index - 1]);
        Sum += (func_sb_true() ^ lb_false ? local_int : 3);
        Sum += (func_sb_true() ^ lb_false ? local_int : -5);
        Sum += (func_sb_true() ^ lb_false ? local_int : local_int);
        Sum += (func_sb_true() ^ lb_false ? local_int : static_field_int);
        Sum += (func_sb_true() ^ lb_false ? local_int : t1_i.mfi);
        Sum += (func_sb_true() ^ lb_false ? local_int : simple_func_int());
        Sum += (func_sb_true() ^ lb_false ? local_int : ab[index]);
        Sum += (func_sb_true() ^ lb_false ? local_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_318()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ lb_false ? static_field_int : 3);
        Sum += (func_sb_true() ^ lb_false ? static_field_int : -5);
        Sum += (func_sb_true() ^ lb_false ? static_field_int : local_int);
        Sum += (func_sb_true() ^ lb_false ? static_field_int : static_field_int);
        Sum += (func_sb_true() ^ lb_false ? static_field_int : t1_i.mfi);
        Sum += (func_sb_true() ^ lb_false ? static_field_int : simple_func_int());
        Sum += (func_sb_true() ^ lb_false ? static_field_int : ab[index]);
        Sum += (func_sb_true() ^ lb_false ? static_field_int : ab[index - 1]);
        Sum += (func_sb_true() ^ lb_false ? t1_i.mfi : 3);
        Sum += (func_sb_true() ^ lb_false ? t1_i.mfi : -5);
        Sum += (func_sb_true() ^ lb_false ? t1_i.mfi : local_int);
        Sum += (func_sb_true() ^ lb_false ? t1_i.mfi : static_field_int);
        Sum += (func_sb_true() ^ lb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (func_sb_true() ^ lb_false ? t1_i.mfi : simple_func_int());
        Sum += (func_sb_true() ^ lb_false ? t1_i.mfi : ab[index]);
        Sum += (func_sb_true() ^ lb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (func_sb_true() ^ lb_false ? simple_func_int() : 3);
        Sum += (func_sb_true() ^ lb_false ? simple_func_int() : -5);
        Sum += (func_sb_true() ^ lb_false ? simple_func_int() : local_int);
        Sum += (func_sb_true() ^ lb_false ? simple_func_int() : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_319()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ lb_false ? simple_func_int() : t1_i.mfi);
        Sum += (func_sb_true() ^ lb_false ? simple_func_int() : simple_func_int());
        Sum += (func_sb_true() ^ lb_false ? simple_func_int() : ab[index]);
        Sum += (func_sb_true() ^ lb_false ? simple_func_int() : ab[index - 1]);
        Sum += (func_sb_true() ^ lb_false ? ab[index] : 3);
        Sum += (func_sb_true() ^ lb_false ? ab[index] : -5);
        Sum += (func_sb_true() ^ lb_false ? ab[index] : local_int);
        Sum += (func_sb_true() ^ lb_false ? ab[index] : static_field_int);
        Sum += (func_sb_true() ^ lb_false ? ab[index] : t1_i.mfi);
        Sum += (func_sb_true() ^ lb_false ? ab[index] : simple_func_int());
        Sum += (func_sb_true() ^ lb_false ? ab[index] : ab[index]);
        Sum += (func_sb_true() ^ lb_false ? ab[index] : ab[index - 1]);
        Sum += (func_sb_true() ^ lb_false ? ab[index - 1] : 3);
        Sum += (func_sb_true() ^ lb_false ? ab[index - 1] : -5);
        Sum += (func_sb_true() ^ lb_false ? ab[index - 1] : local_int);
        Sum += (func_sb_true() ^ lb_false ? ab[index - 1] : static_field_int);
        Sum += (func_sb_true() ^ lb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (func_sb_true() ^ lb_false ? ab[index - 1] : simple_func_int());
        Sum += (func_sb_true() ^ lb_false ? ab[index - 1] : ab[index]);
        Sum += (func_sb_true() ^ lb_false ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_320()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ sfb_true ? 3 : 3);
        Sum += (func_sb_true() ^ sfb_true ? 3 : -5);
        Sum += (func_sb_true() ^ sfb_true ? 3 : local_int);
        Sum += (func_sb_true() ^ sfb_true ? 3 : static_field_int);
        Sum += (func_sb_true() ^ sfb_true ? 3 : t1_i.mfi);
        Sum += (func_sb_true() ^ sfb_true ? 3 : simple_func_int());
        Sum += (func_sb_true() ^ sfb_true ? 3 : ab[index]);
        Sum += (func_sb_true() ^ sfb_true ? 3 : ab[index - 1]);
        Sum += (func_sb_true() ^ sfb_true ? -5 : 3);
        Sum += (func_sb_true() ^ sfb_true ? -5 : -5);
        Sum += (func_sb_true() ^ sfb_true ? -5 : local_int);
        Sum += (func_sb_true() ^ sfb_true ? -5 : static_field_int);
        Sum += (func_sb_true() ^ sfb_true ? -5 : t1_i.mfi);
        Sum += (func_sb_true() ^ sfb_true ? -5 : simple_func_int());
        Sum += (func_sb_true() ^ sfb_true ? -5 : ab[index]);
        Sum += (func_sb_true() ^ sfb_true ? -5 : ab[index - 1]);
        Sum += (func_sb_true() ^ sfb_true ? local_int : 3);
        Sum += (func_sb_true() ^ sfb_true ? local_int : -5);
        Sum += (func_sb_true() ^ sfb_true ? local_int : local_int);
        Sum += (func_sb_true() ^ sfb_true ? local_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_321()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ sfb_true ? local_int : t1_i.mfi);
        Sum += (func_sb_true() ^ sfb_true ? local_int : simple_func_int());
        Sum += (func_sb_true() ^ sfb_true ? local_int : ab[index]);
        Sum += (func_sb_true() ^ sfb_true ? local_int : ab[index - 1]);
        Sum += (func_sb_true() ^ sfb_true ? static_field_int : 3);
        Sum += (func_sb_true() ^ sfb_true ? static_field_int : -5);
        Sum += (func_sb_true() ^ sfb_true ? static_field_int : local_int);
        Sum += (func_sb_true() ^ sfb_true ? static_field_int : static_field_int);
        Sum += (func_sb_true() ^ sfb_true ? static_field_int : t1_i.mfi);
        Sum += (func_sb_true() ^ sfb_true ? static_field_int : simple_func_int());
        Sum += (func_sb_true() ^ sfb_true ? static_field_int : ab[index]);
        Sum += (func_sb_true() ^ sfb_true ? static_field_int : ab[index - 1]);
        Sum += (func_sb_true() ^ sfb_true ? t1_i.mfi : 3);
        Sum += (func_sb_true() ^ sfb_true ? t1_i.mfi : -5);
        Sum += (func_sb_true() ^ sfb_true ? t1_i.mfi : local_int);
        Sum += (func_sb_true() ^ sfb_true ? t1_i.mfi : static_field_int);
        Sum += (func_sb_true() ^ sfb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (func_sb_true() ^ sfb_true ? t1_i.mfi : simple_func_int());
        Sum += (func_sb_true() ^ sfb_true ? t1_i.mfi : ab[index]);
        Sum += (func_sb_true() ^ sfb_true ? t1_i.mfi : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_322()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ sfb_true ? simple_func_int() : 3);
        Sum += (func_sb_true() ^ sfb_true ? simple_func_int() : -5);
        Sum += (func_sb_true() ^ sfb_true ? simple_func_int() : local_int);
        Sum += (func_sb_true() ^ sfb_true ? simple_func_int() : static_field_int);
        Sum += (func_sb_true() ^ sfb_true ? simple_func_int() : t1_i.mfi);
        Sum += (func_sb_true() ^ sfb_true ? simple_func_int() : simple_func_int());
        Sum += (func_sb_true() ^ sfb_true ? simple_func_int() : ab[index]);
        Sum += (func_sb_true() ^ sfb_true ? simple_func_int() : ab[index - 1]);
        Sum += (func_sb_true() ^ sfb_true ? ab[index] : 3);
        Sum += (func_sb_true() ^ sfb_true ? ab[index] : -5);
        Sum += (func_sb_true() ^ sfb_true ? ab[index] : local_int);
        Sum += (func_sb_true() ^ sfb_true ? ab[index] : static_field_int);
        Sum += (func_sb_true() ^ sfb_true ? ab[index] : t1_i.mfi);
        Sum += (func_sb_true() ^ sfb_true ? ab[index] : simple_func_int());
        Sum += (func_sb_true() ^ sfb_true ? ab[index] : ab[index]);
        Sum += (func_sb_true() ^ sfb_true ? ab[index] : ab[index - 1]);
        Sum += (func_sb_true() ^ sfb_true ? ab[index - 1] : 3);
        Sum += (func_sb_true() ^ sfb_true ? ab[index - 1] : -5);
        Sum += (func_sb_true() ^ sfb_true ? ab[index - 1] : local_int);
        Sum += (func_sb_true() ^ sfb_true ? ab[index - 1] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_323()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ sfb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (func_sb_true() ^ sfb_true ? ab[index - 1] : simple_func_int());
        Sum += (func_sb_true() ^ sfb_true ? ab[index - 1] : ab[index]);
        Sum += (func_sb_true() ^ sfb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (func_sb_true() ^ sfb_false ? 3 : 3);
        Sum += (func_sb_true() ^ sfb_false ? 3 : -5);
        Sum += (func_sb_true() ^ sfb_false ? 3 : local_int);
        Sum += (func_sb_true() ^ sfb_false ? 3 : static_field_int);
        Sum += (func_sb_true() ^ sfb_false ? 3 : t1_i.mfi);
        Sum += (func_sb_true() ^ sfb_false ? 3 : simple_func_int());
        Sum += (func_sb_true() ^ sfb_false ? 3 : ab[index]);
        Sum += (func_sb_true() ^ sfb_false ? 3 : ab[index - 1]);
        Sum += (func_sb_true() ^ sfb_false ? -5 : 3);
        Sum += (func_sb_true() ^ sfb_false ? -5 : -5);
        Sum += (func_sb_true() ^ sfb_false ? -5 : local_int);
        Sum += (func_sb_true() ^ sfb_false ? -5 : static_field_int);
        Sum += (func_sb_true() ^ sfb_false ? -5 : t1_i.mfi);
        Sum += (func_sb_true() ^ sfb_false ? -5 : simple_func_int());
        Sum += (func_sb_true() ^ sfb_false ? -5 : ab[index]);
        Sum += (func_sb_true() ^ sfb_false ? -5 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_324()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ sfb_false ? local_int : 3);
        Sum += (func_sb_true() ^ sfb_false ? local_int : -5);
        Sum += (func_sb_true() ^ sfb_false ? local_int : local_int);
        Sum += (func_sb_true() ^ sfb_false ? local_int : static_field_int);
        Sum += (func_sb_true() ^ sfb_false ? local_int : t1_i.mfi);
        Sum += (func_sb_true() ^ sfb_false ? local_int : simple_func_int());
        Sum += (func_sb_true() ^ sfb_false ? local_int : ab[index]);
        Sum += (func_sb_true() ^ sfb_false ? local_int : ab[index - 1]);
        Sum += (func_sb_true() ^ sfb_false ? static_field_int : 3);
        Sum += (func_sb_true() ^ sfb_false ? static_field_int : -5);
        Sum += (func_sb_true() ^ sfb_false ? static_field_int : local_int);
        Sum += (func_sb_true() ^ sfb_false ? static_field_int : static_field_int);
        Sum += (func_sb_true() ^ sfb_false ? static_field_int : t1_i.mfi);
        Sum += (func_sb_true() ^ sfb_false ? static_field_int : simple_func_int());
        Sum += (func_sb_true() ^ sfb_false ? static_field_int : ab[index]);
        Sum += (func_sb_true() ^ sfb_false ? static_field_int : ab[index - 1]);
        Sum += (func_sb_true() ^ sfb_false ? t1_i.mfi : 3);
        Sum += (func_sb_true() ^ sfb_false ? t1_i.mfi : -5);
        Sum += (func_sb_true() ^ sfb_false ? t1_i.mfi : local_int);
        Sum += (func_sb_true() ^ sfb_false ? t1_i.mfi : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_325()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ sfb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (func_sb_true() ^ sfb_false ? t1_i.mfi : simple_func_int());
        Sum += (func_sb_true() ^ sfb_false ? t1_i.mfi : ab[index]);
        Sum += (func_sb_true() ^ sfb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (func_sb_true() ^ sfb_false ? simple_func_int() : 3);
        Sum += (func_sb_true() ^ sfb_false ? simple_func_int() : -5);
        Sum += (func_sb_true() ^ sfb_false ? simple_func_int() : local_int);
        Sum += (func_sb_true() ^ sfb_false ? simple_func_int() : static_field_int);
        Sum += (func_sb_true() ^ sfb_false ? simple_func_int() : t1_i.mfi);
        Sum += (func_sb_true() ^ sfb_false ? simple_func_int() : simple_func_int());
        Sum += (func_sb_true() ^ sfb_false ? simple_func_int() : ab[index]);
        Sum += (func_sb_true() ^ sfb_false ? simple_func_int() : ab[index - 1]);
        Sum += (func_sb_true() ^ sfb_false ? ab[index] : 3);
        Sum += (func_sb_true() ^ sfb_false ? ab[index] : -5);
        Sum += (func_sb_true() ^ sfb_false ? ab[index] : local_int);
        Sum += (func_sb_true() ^ sfb_false ? ab[index] : static_field_int);
        Sum += (func_sb_true() ^ sfb_false ? ab[index] : t1_i.mfi);
        Sum += (func_sb_true() ^ sfb_false ? ab[index] : simple_func_int());
        Sum += (func_sb_true() ^ sfb_false ? ab[index] : ab[index]);
        Sum += (func_sb_true() ^ sfb_false ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_326()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ sfb_false ? ab[index - 1] : 3);
        Sum += (func_sb_true() ^ sfb_false ? ab[index - 1] : -5);
        Sum += (func_sb_true() ^ sfb_false ? ab[index - 1] : local_int);
        Sum += (func_sb_true() ^ sfb_false ? ab[index - 1] : static_field_int);
        Sum += (func_sb_true() ^ sfb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (func_sb_true() ^ sfb_false ? ab[index - 1] : simple_func_int());
        Sum += (func_sb_true() ^ sfb_false ? ab[index - 1] : ab[index]);
        Sum += (func_sb_true() ^ sfb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? 3 : 3);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? 3 : -5);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? 3 : local_int);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? 3 : static_field_int);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? 3 : t1_i.mfi);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? 3 : simple_func_int());
        Sum += (func_sb_true() ^ t1_i.mfb_true ? 3 : ab[index]);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? 3 : ab[index - 1]);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? -5 : 3);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? -5 : -5);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? -5 : local_int);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? -5 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_327()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ t1_i.mfb_true ? -5 : t1_i.mfi);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? -5 : simple_func_int());
        Sum += (func_sb_true() ^ t1_i.mfb_true ? -5 : ab[index]);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? -5 : ab[index - 1]);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? local_int : 3);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? local_int : -5);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? local_int : local_int);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? local_int : static_field_int);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? local_int : t1_i.mfi);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? local_int : simple_func_int());
        Sum += (func_sb_true() ^ t1_i.mfb_true ? local_int : ab[index]);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? local_int : ab[index - 1]);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? static_field_int : 3);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? static_field_int : -5);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? static_field_int : local_int);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? static_field_int : static_field_int);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? static_field_int : t1_i.mfi);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? static_field_int : simple_func_int());
        Sum += (func_sb_true() ^ t1_i.mfb_true ? static_field_int : ab[index]);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? static_field_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_328()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ t1_i.mfb_true ? t1_i.mfi : 3);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? t1_i.mfi : -5);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? t1_i.mfi : local_int);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? t1_i.mfi : static_field_int);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? t1_i.mfi : simple_func_int());
        Sum += (func_sb_true() ^ t1_i.mfb_true ? t1_i.mfi : ab[index]);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? simple_func_int() : 3);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? simple_func_int() : -5);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? simple_func_int() : local_int);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? simple_func_int() : static_field_int);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? simple_func_int() : t1_i.mfi);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? simple_func_int() : simple_func_int());
        Sum += (func_sb_true() ^ t1_i.mfb_true ? simple_func_int() : ab[index]);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? simple_func_int() : ab[index - 1]);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? ab[index] : 3);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? ab[index] : -5);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? ab[index] : local_int);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? ab[index] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_329()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ t1_i.mfb_true ? ab[index] : t1_i.mfi);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? ab[index] : simple_func_int());
        Sum += (func_sb_true() ^ t1_i.mfb_true ? ab[index] : ab[index]);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? ab[index] : ab[index - 1]);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? ab[index - 1] : 3);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? ab[index - 1] : -5);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? ab[index - 1] : local_int);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? ab[index - 1] : static_field_int);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? ab[index - 1] : simple_func_int());
        Sum += (func_sb_true() ^ t1_i.mfb_true ? ab[index - 1] : ab[index]);
        Sum += (func_sb_true() ^ t1_i.mfb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? 3 : 3);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? 3 : -5);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? 3 : local_int);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? 3 : static_field_int);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? 3 : t1_i.mfi);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? 3 : simple_func_int());
        Sum += (func_sb_true() ^ t1_i.mfb_false ? 3 : ab[index]);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? 3 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_330()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ t1_i.mfb_false ? -5 : 3);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? -5 : -5);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? -5 : local_int);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? -5 : static_field_int);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? -5 : t1_i.mfi);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? -5 : simple_func_int());
        Sum += (func_sb_true() ^ t1_i.mfb_false ? -5 : ab[index]);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? -5 : ab[index - 1]);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? local_int : 3);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? local_int : -5);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? local_int : local_int);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? local_int : static_field_int);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? local_int : t1_i.mfi);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? local_int : simple_func_int());
        Sum += (func_sb_true() ^ t1_i.mfb_false ? local_int : ab[index]);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? local_int : ab[index - 1]);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? static_field_int : 3);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? static_field_int : -5);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? static_field_int : local_int);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? static_field_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_331()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ t1_i.mfb_false ? static_field_int : t1_i.mfi);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? static_field_int : simple_func_int());
        Sum += (func_sb_true() ^ t1_i.mfb_false ? static_field_int : ab[index]);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? static_field_int : ab[index - 1]);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? t1_i.mfi : 3);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? t1_i.mfi : -5);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? t1_i.mfi : local_int);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? t1_i.mfi : static_field_int);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? t1_i.mfi : simple_func_int());
        Sum += (func_sb_true() ^ t1_i.mfb_false ? t1_i.mfi : ab[index]);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? simple_func_int() : 3);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? simple_func_int() : -5);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? simple_func_int() : local_int);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? simple_func_int() : static_field_int);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? simple_func_int() : t1_i.mfi);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? simple_func_int() : simple_func_int());
        Sum += (func_sb_true() ^ t1_i.mfb_false ? simple_func_int() : ab[index]);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? simple_func_int() : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_332()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ t1_i.mfb_false ? ab[index] : 3);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? ab[index] : -5);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? ab[index] : local_int);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? ab[index] : static_field_int);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? ab[index] : t1_i.mfi);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? ab[index] : simple_func_int());
        Sum += (func_sb_true() ^ t1_i.mfb_false ? ab[index] : ab[index]);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? ab[index] : ab[index - 1]);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? ab[index - 1] : 3);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? ab[index - 1] : -5);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? ab[index - 1] : local_int);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? ab[index - 1] : static_field_int);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? ab[index - 1] : simple_func_int());
        Sum += (func_sb_true() ^ t1_i.mfb_false ? ab[index - 1] : ab[index]);
        Sum += (func_sb_true() ^ t1_i.mfb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (func_sb_true() ^ func_sb_true() ? 3 : 3);
        Sum += (func_sb_true() ^ func_sb_true() ? 3 : -5);
        Sum += (func_sb_true() ^ func_sb_true() ? 3 : local_int);
        Sum += (func_sb_true() ^ func_sb_true() ? 3 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_333()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ func_sb_true() ? 3 : t1_i.mfi);
        Sum += (func_sb_true() ^ func_sb_true() ? 3 : simple_func_int());
        Sum += (func_sb_true() ^ func_sb_true() ? 3 : ab[index]);
        Sum += (func_sb_true() ^ func_sb_true() ? 3 : ab[index - 1]);
        Sum += (func_sb_true() ^ func_sb_true() ? -5 : 3);
        Sum += (func_sb_true() ^ func_sb_true() ? -5 : -5);
        Sum += (func_sb_true() ^ func_sb_true() ? -5 : local_int);
        Sum += (func_sb_true() ^ func_sb_true() ? -5 : static_field_int);
        Sum += (func_sb_true() ^ func_sb_true() ? -5 : t1_i.mfi);
        Sum += (func_sb_true() ^ func_sb_true() ? -5 : simple_func_int());
        Sum += (func_sb_true() ^ func_sb_true() ? -5 : ab[index]);
        Sum += (func_sb_true() ^ func_sb_true() ? -5 : ab[index - 1]);
        Sum += (func_sb_true() ^ func_sb_true() ? local_int : 3);
        Sum += (func_sb_true() ^ func_sb_true() ? local_int : -5);
        Sum += (func_sb_true() ^ func_sb_true() ? local_int : local_int);
        Sum += (func_sb_true() ^ func_sb_true() ? local_int : static_field_int);
        Sum += (func_sb_true() ^ func_sb_true() ? local_int : t1_i.mfi);
        Sum += (func_sb_true() ^ func_sb_true() ? local_int : simple_func_int());
        Sum += (func_sb_true() ^ func_sb_true() ? local_int : ab[index]);
        Sum += (func_sb_true() ^ func_sb_true() ? local_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_334()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ func_sb_true() ? static_field_int : 3);
        Sum += (func_sb_true() ^ func_sb_true() ? static_field_int : -5);
        Sum += (func_sb_true() ^ func_sb_true() ? static_field_int : local_int);
        Sum += (func_sb_true() ^ func_sb_true() ? static_field_int : static_field_int);
        Sum += (func_sb_true() ^ func_sb_true() ? static_field_int : t1_i.mfi);
        Sum += (func_sb_true() ^ func_sb_true() ? static_field_int : simple_func_int());
        Sum += (func_sb_true() ^ func_sb_true() ? static_field_int : ab[index]);
        Sum += (func_sb_true() ^ func_sb_true() ? static_field_int : ab[index - 1]);
        Sum += (func_sb_true() ^ func_sb_true() ? t1_i.mfi : 3);
        Sum += (func_sb_true() ^ func_sb_true() ? t1_i.mfi : -5);
        Sum += (func_sb_true() ^ func_sb_true() ? t1_i.mfi : local_int);
        Sum += (func_sb_true() ^ func_sb_true() ? t1_i.mfi : static_field_int);
        Sum += (func_sb_true() ^ func_sb_true() ? t1_i.mfi : t1_i.mfi);
        Sum += (func_sb_true() ^ func_sb_true() ? t1_i.mfi : simple_func_int());
        Sum += (func_sb_true() ^ func_sb_true() ? t1_i.mfi : ab[index]);
        Sum += (func_sb_true() ^ func_sb_true() ? t1_i.mfi : ab[index - 1]);
        Sum += (func_sb_true() ^ func_sb_true() ? simple_func_int() : 3);
        Sum += (func_sb_true() ^ func_sb_true() ? simple_func_int() : -5);
        Sum += (func_sb_true() ^ func_sb_true() ? simple_func_int() : local_int);
        Sum += (func_sb_true() ^ func_sb_true() ? simple_func_int() : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_335()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ func_sb_true() ? simple_func_int() : t1_i.mfi);
        Sum += (func_sb_true() ^ func_sb_true() ? simple_func_int() : simple_func_int());
        Sum += (func_sb_true() ^ func_sb_true() ? simple_func_int() : ab[index]);
        Sum += (func_sb_true() ^ func_sb_true() ? simple_func_int() : ab[index - 1]);
        Sum += (func_sb_true() ^ func_sb_true() ? ab[index] : 3);
        Sum += (func_sb_true() ^ func_sb_true() ? ab[index] : -5);
        Sum += (func_sb_true() ^ func_sb_true() ? ab[index] : local_int);
        Sum += (func_sb_true() ^ func_sb_true() ? ab[index] : static_field_int);
        Sum += (func_sb_true() ^ func_sb_true() ? ab[index] : t1_i.mfi);
        Sum += (func_sb_true() ^ func_sb_true() ? ab[index] : simple_func_int());
        Sum += (func_sb_true() ^ func_sb_true() ? ab[index] : ab[index]);
        Sum += (func_sb_true() ^ func_sb_true() ? ab[index] : ab[index - 1]);
        Sum += (func_sb_true() ^ func_sb_true() ? ab[index - 1] : 3);
        Sum += (func_sb_true() ^ func_sb_true() ? ab[index - 1] : -5);
        Sum += (func_sb_true() ^ func_sb_true() ? ab[index - 1] : local_int);
        Sum += (func_sb_true() ^ func_sb_true() ? ab[index - 1] : static_field_int);
        Sum += (func_sb_true() ^ func_sb_true() ? ab[index - 1] : t1_i.mfi);
        Sum += (func_sb_true() ^ func_sb_true() ? ab[index - 1] : simple_func_int());
        Sum += (func_sb_true() ^ func_sb_true() ? ab[index - 1] : ab[index]);
        Sum += (func_sb_true() ^ func_sb_true() ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_336()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ func_sb_false() ? 3 : 3);
        Sum += (func_sb_true() ^ func_sb_false() ? 3 : -5);
        Sum += (func_sb_true() ^ func_sb_false() ? 3 : local_int);
        Sum += (func_sb_true() ^ func_sb_false() ? 3 : static_field_int);
        Sum += (func_sb_true() ^ func_sb_false() ? 3 : t1_i.mfi);
        Sum += (func_sb_true() ^ func_sb_false() ? 3 : simple_func_int());
        Sum += (func_sb_true() ^ func_sb_false() ? 3 : ab[index]);
        Sum += (func_sb_true() ^ func_sb_false() ? 3 : ab[index - 1]);
        Sum += (func_sb_true() ^ func_sb_false() ? -5 : 3);
        Sum += (func_sb_true() ^ func_sb_false() ? -5 : -5);
        Sum += (func_sb_true() ^ func_sb_false() ? -5 : local_int);
        Sum += (func_sb_true() ^ func_sb_false() ? -5 : static_field_int);
        Sum += (func_sb_true() ^ func_sb_false() ? -5 : t1_i.mfi);
        Sum += (func_sb_true() ^ func_sb_false() ? -5 : simple_func_int());
        Sum += (func_sb_true() ^ func_sb_false() ? -5 : ab[index]);
        Sum += (func_sb_true() ^ func_sb_false() ? -5 : ab[index - 1]);
        Sum += (func_sb_true() ^ func_sb_false() ? local_int : 3);
        Sum += (func_sb_true() ^ func_sb_false() ? local_int : -5);
        Sum += (func_sb_true() ^ func_sb_false() ? local_int : local_int);
        Sum += (func_sb_true() ^ func_sb_false() ? local_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_337()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ func_sb_false() ? local_int : t1_i.mfi);
        Sum += (func_sb_true() ^ func_sb_false() ? local_int : simple_func_int());
        Sum += (func_sb_true() ^ func_sb_false() ? local_int : ab[index]);
        Sum += (func_sb_true() ^ func_sb_false() ? local_int : ab[index - 1]);
        Sum += (func_sb_true() ^ func_sb_false() ? static_field_int : 3);
        Sum += (func_sb_true() ^ func_sb_false() ? static_field_int : -5);
        Sum += (func_sb_true() ^ func_sb_false() ? static_field_int : local_int);
        Sum += (func_sb_true() ^ func_sb_false() ? static_field_int : static_field_int);
        Sum += (func_sb_true() ^ func_sb_false() ? static_field_int : t1_i.mfi);
        Sum += (func_sb_true() ^ func_sb_false() ? static_field_int : simple_func_int());
        Sum += (func_sb_true() ^ func_sb_false() ? static_field_int : ab[index]);
        Sum += (func_sb_true() ^ func_sb_false() ? static_field_int : ab[index - 1]);
        Sum += (func_sb_true() ^ func_sb_false() ? t1_i.mfi : 3);
        Sum += (func_sb_true() ^ func_sb_false() ? t1_i.mfi : -5);
        Sum += (func_sb_true() ^ func_sb_false() ? t1_i.mfi : local_int);
        Sum += (func_sb_true() ^ func_sb_false() ? t1_i.mfi : static_field_int);
        Sum += (func_sb_true() ^ func_sb_false() ? t1_i.mfi : t1_i.mfi);
        Sum += (func_sb_true() ^ func_sb_false() ? t1_i.mfi : simple_func_int());
        Sum += (func_sb_true() ^ func_sb_false() ? t1_i.mfi : ab[index]);
        Sum += (func_sb_true() ^ func_sb_false() ? t1_i.mfi : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_338()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ func_sb_false() ? simple_func_int() : 3);
        Sum += (func_sb_true() ^ func_sb_false() ? simple_func_int() : -5);
        Sum += (func_sb_true() ^ func_sb_false() ? simple_func_int() : local_int);
        Sum += (func_sb_true() ^ func_sb_false() ? simple_func_int() : static_field_int);
        Sum += (func_sb_true() ^ func_sb_false() ? simple_func_int() : t1_i.mfi);
        Sum += (func_sb_true() ^ func_sb_false() ? simple_func_int() : simple_func_int());
        Sum += (func_sb_true() ^ func_sb_false() ? simple_func_int() : ab[index]);
        Sum += (func_sb_true() ^ func_sb_false() ? simple_func_int() : ab[index - 1]);
        Sum += (func_sb_true() ^ func_sb_false() ? ab[index] : 3);
        Sum += (func_sb_true() ^ func_sb_false() ? ab[index] : -5);
        Sum += (func_sb_true() ^ func_sb_false() ? ab[index] : local_int);
        Sum += (func_sb_true() ^ func_sb_false() ? ab[index] : static_field_int);
        Sum += (func_sb_true() ^ func_sb_false() ? ab[index] : t1_i.mfi);
        Sum += (func_sb_true() ^ func_sb_false() ? ab[index] : simple_func_int());
        Sum += (func_sb_true() ^ func_sb_false() ? ab[index] : ab[index]);
        Sum += (func_sb_true() ^ func_sb_false() ? ab[index] : ab[index - 1]);
        Sum += (func_sb_true() ^ func_sb_false() ? ab[index - 1] : 3);
        Sum += (func_sb_true() ^ func_sb_false() ? ab[index - 1] : -5);
        Sum += (func_sb_true() ^ func_sb_false() ? ab[index - 1] : local_int);
        Sum += (func_sb_true() ^ func_sb_false() ? ab[index - 1] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_339()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ func_sb_false() ? ab[index - 1] : t1_i.mfi);
        Sum += (func_sb_true() ^ func_sb_false() ? ab[index - 1] : simple_func_int());
        Sum += (func_sb_true() ^ func_sb_false() ? ab[index - 1] : ab[index]);
        Sum += (func_sb_true() ^ func_sb_false() ? ab[index - 1] : ab[index - 1]);
        Sum += (func_sb_true() ^ ab_true[index] ? 3 : 3);
        Sum += (func_sb_true() ^ ab_true[index] ? 3 : -5);
        Sum += (func_sb_true() ^ ab_true[index] ? 3 : local_int);
        Sum += (func_sb_true() ^ ab_true[index] ? 3 : static_field_int);
        Sum += (func_sb_true() ^ ab_true[index] ? 3 : t1_i.mfi);
        Sum += (func_sb_true() ^ ab_true[index] ? 3 : simple_func_int());
        Sum += (func_sb_true() ^ ab_true[index] ? 3 : ab[index]);
        Sum += (func_sb_true() ^ ab_true[index] ? 3 : ab[index - 1]);
        Sum += (func_sb_true() ^ ab_true[index] ? -5 : 3);
        Sum += (func_sb_true() ^ ab_true[index] ? -5 : -5);
        Sum += (func_sb_true() ^ ab_true[index] ? -5 : local_int);
        Sum += (func_sb_true() ^ ab_true[index] ? -5 : static_field_int);
        Sum += (func_sb_true() ^ ab_true[index] ? -5 : t1_i.mfi);
        Sum += (func_sb_true() ^ ab_true[index] ? -5 : simple_func_int());
        Sum += (func_sb_true() ^ ab_true[index] ? -5 : ab[index]);
        Sum += (func_sb_true() ^ ab_true[index] ? -5 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_340()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ ab_true[index] ? local_int : 3);
        Sum += (func_sb_true() ^ ab_true[index] ? local_int : -5);
        Sum += (func_sb_true() ^ ab_true[index] ? local_int : local_int);
        Sum += (func_sb_true() ^ ab_true[index] ? local_int : static_field_int);
        Sum += (func_sb_true() ^ ab_true[index] ? local_int : t1_i.mfi);
        Sum += (func_sb_true() ^ ab_true[index] ? local_int : simple_func_int());
        Sum += (func_sb_true() ^ ab_true[index] ? local_int : ab[index]);
        Sum += (func_sb_true() ^ ab_true[index] ? local_int : ab[index - 1]);
        Sum += (func_sb_true() ^ ab_true[index] ? static_field_int : 3);
        Sum += (func_sb_true() ^ ab_true[index] ? static_field_int : -5);
        Sum += (func_sb_true() ^ ab_true[index] ? static_field_int : local_int);
        Sum += (func_sb_true() ^ ab_true[index] ? static_field_int : static_field_int);
        Sum += (func_sb_true() ^ ab_true[index] ? static_field_int : t1_i.mfi);
        Sum += (func_sb_true() ^ ab_true[index] ? static_field_int : simple_func_int());
        Sum += (func_sb_true() ^ ab_true[index] ? static_field_int : ab[index]);
        Sum += (func_sb_true() ^ ab_true[index] ? static_field_int : ab[index - 1]);
        Sum += (func_sb_true() ^ ab_true[index] ? t1_i.mfi : 3);
        Sum += (func_sb_true() ^ ab_true[index] ? t1_i.mfi : -5);
        Sum += (func_sb_true() ^ ab_true[index] ? t1_i.mfi : local_int);
        Sum += (func_sb_true() ^ ab_true[index] ? t1_i.mfi : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_341()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ ab_true[index] ? t1_i.mfi : t1_i.mfi);
        Sum += (func_sb_true() ^ ab_true[index] ? t1_i.mfi : simple_func_int());
        Sum += (func_sb_true() ^ ab_true[index] ? t1_i.mfi : ab[index]);
        Sum += (func_sb_true() ^ ab_true[index] ? t1_i.mfi : ab[index - 1]);
        Sum += (func_sb_true() ^ ab_true[index] ? simple_func_int() : 3);
        Sum += (func_sb_true() ^ ab_true[index] ? simple_func_int() : -5);
        Sum += (func_sb_true() ^ ab_true[index] ? simple_func_int() : local_int);
        Sum += (func_sb_true() ^ ab_true[index] ? simple_func_int() : static_field_int);
        Sum += (func_sb_true() ^ ab_true[index] ? simple_func_int() : t1_i.mfi);
        Sum += (func_sb_true() ^ ab_true[index] ? simple_func_int() : simple_func_int());
        Sum += (func_sb_true() ^ ab_true[index] ? simple_func_int() : ab[index]);
        Sum += (func_sb_true() ^ ab_true[index] ? simple_func_int() : ab[index - 1]);
        Sum += (func_sb_true() ^ ab_true[index] ? ab[index] : 3);
        Sum += (func_sb_true() ^ ab_true[index] ? ab[index] : -5);
        Sum += (func_sb_true() ^ ab_true[index] ? ab[index] : local_int);
        Sum += (func_sb_true() ^ ab_true[index] ? ab[index] : static_field_int);
        Sum += (func_sb_true() ^ ab_true[index] ? ab[index] : t1_i.mfi);
        Sum += (func_sb_true() ^ ab_true[index] ? ab[index] : simple_func_int());
        Sum += (func_sb_true() ^ ab_true[index] ? ab[index] : ab[index]);
        Sum += (func_sb_true() ^ ab_true[index] ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_342()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ ab_true[index] ? ab[index - 1] : 3);
        Sum += (func_sb_true() ^ ab_true[index] ? ab[index - 1] : -5);
        Sum += (func_sb_true() ^ ab_true[index] ? ab[index - 1] : local_int);
        Sum += (func_sb_true() ^ ab_true[index] ? ab[index - 1] : static_field_int);
        Sum += (func_sb_true() ^ ab_true[index] ? ab[index - 1] : t1_i.mfi);
        Sum += (func_sb_true() ^ ab_true[index] ? ab[index - 1] : simple_func_int());
        Sum += (func_sb_true() ^ ab_true[index] ? ab[index - 1] : ab[index]);
        Sum += (func_sb_true() ^ ab_true[index] ? ab[index - 1] : ab[index - 1]);
        Sum += (func_sb_true() ^ ab_false[index] ? 3 : 3);
        Sum += (func_sb_true() ^ ab_false[index] ? 3 : -5);
        Sum += (func_sb_true() ^ ab_false[index] ? 3 : local_int);
        Sum += (func_sb_true() ^ ab_false[index] ? 3 : static_field_int);
        Sum += (func_sb_true() ^ ab_false[index] ? 3 : t1_i.mfi);
        Sum += (func_sb_true() ^ ab_false[index] ? 3 : simple_func_int());
        Sum += (func_sb_true() ^ ab_false[index] ? 3 : ab[index]);
        Sum += (func_sb_true() ^ ab_false[index] ? 3 : ab[index - 1]);
        Sum += (func_sb_true() ^ ab_false[index] ? -5 : 3);
        Sum += (func_sb_true() ^ ab_false[index] ? -5 : -5);
        Sum += (func_sb_true() ^ ab_false[index] ? -5 : local_int);
        Sum += (func_sb_true() ^ ab_false[index] ? -5 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_343()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ ab_false[index] ? -5 : t1_i.mfi);
        Sum += (func_sb_true() ^ ab_false[index] ? -5 : simple_func_int());
        Sum += (func_sb_true() ^ ab_false[index] ? -5 : ab[index]);
        Sum += (func_sb_true() ^ ab_false[index] ? -5 : ab[index - 1]);
        Sum += (func_sb_true() ^ ab_false[index] ? local_int : 3);
        Sum += (func_sb_true() ^ ab_false[index] ? local_int : -5);
        Sum += (func_sb_true() ^ ab_false[index] ? local_int : local_int);
        Sum += (func_sb_true() ^ ab_false[index] ? local_int : static_field_int);
        Sum += (func_sb_true() ^ ab_false[index] ? local_int : t1_i.mfi);
        Sum += (func_sb_true() ^ ab_false[index] ? local_int : simple_func_int());
        Sum += (func_sb_true() ^ ab_false[index] ? local_int : ab[index]);
        Sum += (func_sb_true() ^ ab_false[index] ? local_int : ab[index - 1]);
        Sum += (func_sb_true() ^ ab_false[index] ? static_field_int : 3);
        Sum += (func_sb_true() ^ ab_false[index] ? static_field_int : -5);
        Sum += (func_sb_true() ^ ab_false[index] ? static_field_int : local_int);
        Sum += (func_sb_true() ^ ab_false[index] ? static_field_int : static_field_int);
        Sum += (func_sb_true() ^ ab_false[index] ? static_field_int : t1_i.mfi);
        Sum += (func_sb_true() ^ ab_false[index] ? static_field_int : simple_func_int());
        Sum += (func_sb_true() ^ ab_false[index] ? static_field_int : ab[index]);
        Sum += (func_sb_true() ^ ab_false[index] ? static_field_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_344()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ ab_false[index] ? t1_i.mfi : 3);
        Sum += (func_sb_true() ^ ab_false[index] ? t1_i.mfi : -5);
        Sum += (func_sb_true() ^ ab_false[index] ? t1_i.mfi : local_int);
        Sum += (func_sb_true() ^ ab_false[index] ? t1_i.mfi : static_field_int);
        Sum += (func_sb_true() ^ ab_false[index] ? t1_i.mfi : t1_i.mfi);
        Sum += (func_sb_true() ^ ab_false[index] ? t1_i.mfi : simple_func_int());
        Sum += (func_sb_true() ^ ab_false[index] ? t1_i.mfi : ab[index]);
        Sum += (func_sb_true() ^ ab_false[index] ? t1_i.mfi : ab[index - 1]);
        Sum += (func_sb_true() ^ ab_false[index] ? simple_func_int() : 3);
        Sum += (func_sb_true() ^ ab_false[index] ? simple_func_int() : -5);
        Sum += (func_sb_true() ^ ab_false[index] ? simple_func_int() : local_int);
        Sum += (func_sb_true() ^ ab_false[index] ? simple_func_int() : static_field_int);
        Sum += (func_sb_true() ^ ab_false[index] ? simple_func_int() : t1_i.mfi);
        Sum += (func_sb_true() ^ ab_false[index] ? simple_func_int() : simple_func_int());
        Sum += (func_sb_true() ^ ab_false[index] ? simple_func_int() : ab[index]);
        Sum += (func_sb_true() ^ ab_false[index] ? simple_func_int() : ab[index - 1]);
        Sum += (func_sb_true() ^ ab_false[index] ? ab[index] : 3);
        Sum += (func_sb_true() ^ ab_false[index] ? ab[index] : -5);
        Sum += (func_sb_true() ^ ab_false[index] ? ab[index] : local_int);
        Sum += (func_sb_true() ^ ab_false[index] ? ab[index] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_345()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ^ ab_false[index] ? ab[index] : t1_i.mfi);
        Sum += (func_sb_true() ^ ab_false[index] ? ab[index] : simple_func_int());
        Sum += (func_sb_true() ^ ab_false[index] ? ab[index] : ab[index]);
        Sum += (func_sb_true() ^ ab_false[index] ? ab[index] : ab[index - 1]);
        Sum += (func_sb_true() ^ ab_false[index] ? ab[index - 1] : 3);
        Sum += (func_sb_true() ^ ab_false[index] ? ab[index - 1] : -5);
        Sum += (func_sb_true() ^ ab_false[index] ? ab[index - 1] : local_int);
        Sum += (func_sb_true() ^ ab_false[index] ? ab[index - 1] : static_field_int);
        Sum += (func_sb_true() ^ ab_false[index] ? ab[index - 1] : t1_i.mfi);
        Sum += (func_sb_true() ^ ab_false[index] ? ab[index - 1] : simple_func_int());
        Sum += (func_sb_true() ^ ab_false[index] ? ab[index - 1] : ab[index]);
        Sum += (func_sb_true() ^ ab_false[index] ? ab[index - 1] : ab[index - 1]);
        Sum += (func_sb_false() ^ true ? 3 : 3);
        Sum += (func_sb_false() ^ true ? 3 : -5);
        Sum += (func_sb_false() ^ true ? 3 : local_int);
        Sum += (func_sb_false() ^ true ? 3 : static_field_int);
        Sum += (func_sb_false() ^ true ? 3 : t1_i.mfi);
        Sum += (func_sb_false() ^ true ? 3 : simple_func_int());
        Sum += (func_sb_false() ^ true ? 3 : ab[index]);
        Sum += (func_sb_false() ^ true ? 3 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_346()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ true ? -5 : 3);
        Sum += (func_sb_false() ^ true ? -5 : -5);
        Sum += (func_sb_false() ^ true ? -5 : local_int);
        Sum += (func_sb_false() ^ true ? -5 : static_field_int);
        Sum += (func_sb_false() ^ true ? -5 : t1_i.mfi);
        Sum += (func_sb_false() ^ true ? -5 : simple_func_int());
        Sum += (func_sb_false() ^ true ? -5 : ab[index]);
        Sum += (func_sb_false() ^ true ? -5 : ab[index - 1]);
        Sum += (func_sb_false() ^ true ? local_int : 3);
        Sum += (func_sb_false() ^ true ? local_int : -5);
        Sum += (func_sb_false() ^ true ? local_int : local_int);
        Sum += (func_sb_false() ^ true ? local_int : static_field_int);
        Sum += (func_sb_false() ^ true ? local_int : t1_i.mfi);
        Sum += (func_sb_false() ^ true ? local_int : simple_func_int());
        Sum += (func_sb_false() ^ true ? local_int : ab[index]);
        Sum += (func_sb_false() ^ true ? local_int : ab[index - 1]);
        Sum += (func_sb_false() ^ true ? static_field_int : 3);
        Sum += (func_sb_false() ^ true ? static_field_int : -5);
        Sum += (func_sb_false() ^ true ? static_field_int : local_int);
        Sum += (func_sb_false() ^ true ? static_field_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_347()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ true ? static_field_int : t1_i.mfi);
        Sum += (func_sb_false() ^ true ? static_field_int : simple_func_int());
        Sum += (func_sb_false() ^ true ? static_field_int : ab[index]);
        Sum += (func_sb_false() ^ true ? static_field_int : ab[index - 1]);
        Sum += (func_sb_false() ^ true ? t1_i.mfi : 3);
        Sum += (func_sb_false() ^ true ? t1_i.mfi : -5);
        Sum += (func_sb_false() ^ true ? t1_i.mfi : local_int);
        Sum += (func_sb_false() ^ true ? t1_i.mfi : static_field_int);
        Sum += (func_sb_false() ^ true ? t1_i.mfi : t1_i.mfi);
        Sum += (func_sb_false() ^ true ? t1_i.mfi : simple_func_int());
        Sum += (func_sb_false() ^ true ? t1_i.mfi : ab[index]);
        Sum += (func_sb_false() ^ true ? t1_i.mfi : ab[index - 1]);
        Sum += (func_sb_false() ^ true ? simple_func_int() : 3);
        Sum += (func_sb_false() ^ true ? simple_func_int() : -5);
        Sum += (func_sb_false() ^ true ? simple_func_int() : local_int);
        Sum += (func_sb_false() ^ true ? simple_func_int() : static_field_int);
        Sum += (func_sb_false() ^ true ? simple_func_int() : t1_i.mfi);
        Sum += (func_sb_false() ^ true ? simple_func_int() : simple_func_int());
        Sum += (func_sb_false() ^ true ? simple_func_int() : ab[index]);
        Sum += (func_sb_false() ^ true ? simple_func_int() : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_348()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ true ? ab[index] : 3);
        Sum += (func_sb_false() ^ true ? ab[index] : -5);
        Sum += (func_sb_false() ^ true ? ab[index] : local_int);
        Sum += (func_sb_false() ^ true ? ab[index] : static_field_int);
        Sum += (func_sb_false() ^ true ? ab[index] : t1_i.mfi);
        Sum += (func_sb_false() ^ true ? ab[index] : simple_func_int());
        Sum += (func_sb_false() ^ true ? ab[index] : ab[index]);
        Sum += (func_sb_false() ^ true ? ab[index] : ab[index - 1]);
        Sum += (func_sb_false() ^ true ? ab[index - 1] : 3);
        Sum += (func_sb_false() ^ true ? ab[index - 1] : -5);
        Sum += (func_sb_false() ^ true ? ab[index - 1] : local_int);
        Sum += (func_sb_false() ^ true ? ab[index - 1] : static_field_int);
        Sum += (func_sb_false() ^ true ? ab[index - 1] : t1_i.mfi);
        Sum += (func_sb_false() ^ true ? ab[index - 1] : simple_func_int());
        Sum += (func_sb_false() ^ true ? ab[index - 1] : ab[index]);
        Sum += (func_sb_false() ^ true ? ab[index - 1] : ab[index - 1]);
        Sum += (func_sb_false() ^ false ? 3 : 3);
        Sum += (func_sb_false() ^ false ? 3 : -5);
        Sum += (func_sb_false() ^ false ? 3 : local_int);
        Sum += (func_sb_false() ^ false ? 3 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_349()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ false ? 3 : t1_i.mfi);
        Sum += (func_sb_false() ^ false ? 3 : simple_func_int());
        Sum += (func_sb_false() ^ false ? 3 : ab[index]);
        Sum += (func_sb_false() ^ false ? 3 : ab[index - 1]);
        Sum += (func_sb_false() ^ false ? -5 : 3);
        Sum += (func_sb_false() ^ false ? -5 : -5);
        Sum += (func_sb_false() ^ false ? -5 : local_int);
        Sum += (func_sb_false() ^ false ? -5 : static_field_int);
        Sum += (func_sb_false() ^ false ? -5 : t1_i.mfi);
        Sum += (func_sb_false() ^ false ? -5 : simple_func_int());
        Sum += (func_sb_false() ^ false ? -5 : ab[index]);
        Sum += (func_sb_false() ^ false ? -5 : ab[index - 1]);
        Sum += (func_sb_false() ^ false ? local_int : 3);
        Sum += (func_sb_false() ^ false ? local_int : -5);
        Sum += (func_sb_false() ^ false ? local_int : local_int);
        Sum += (func_sb_false() ^ false ? local_int : static_field_int);
        Sum += (func_sb_false() ^ false ? local_int : t1_i.mfi);
        Sum += (func_sb_false() ^ false ? local_int : simple_func_int());
        Sum += (func_sb_false() ^ false ? local_int : ab[index]);
        Sum += (func_sb_false() ^ false ? local_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_350()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ false ? static_field_int : 3);
        Sum += (func_sb_false() ^ false ? static_field_int : -5);
        Sum += (func_sb_false() ^ false ? static_field_int : local_int);
        Sum += (func_sb_false() ^ false ? static_field_int : static_field_int);
        Sum += (func_sb_false() ^ false ? static_field_int : t1_i.mfi);
        Sum += (func_sb_false() ^ false ? static_field_int : simple_func_int());
        Sum += (func_sb_false() ^ false ? static_field_int : ab[index]);
        Sum += (func_sb_false() ^ false ? static_field_int : ab[index - 1]);
        Sum += (func_sb_false() ^ false ? t1_i.mfi : 3);
        Sum += (func_sb_false() ^ false ? t1_i.mfi : -5);
        Sum += (func_sb_false() ^ false ? t1_i.mfi : local_int);
        Sum += (func_sb_false() ^ false ? t1_i.mfi : static_field_int);
        Sum += (func_sb_false() ^ false ? t1_i.mfi : t1_i.mfi);
        Sum += (func_sb_false() ^ false ? t1_i.mfi : simple_func_int());
        Sum += (func_sb_false() ^ false ? t1_i.mfi : ab[index]);
        Sum += (func_sb_false() ^ false ? t1_i.mfi : ab[index - 1]);
        Sum += (func_sb_false() ^ false ? simple_func_int() : 3);
        Sum += (func_sb_false() ^ false ? simple_func_int() : -5);
        Sum += (func_sb_false() ^ false ? simple_func_int() : local_int);
        Sum += (func_sb_false() ^ false ? simple_func_int() : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_351()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ false ? simple_func_int() : t1_i.mfi);
        Sum += (func_sb_false() ^ false ? simple_func_int() : simple_func_int());
        Sum += (func_sb_false() ^ false ? simple_func_int() : ab[index]);
        Sum += (func_sb_false() ^ false ? simple_func_int() : ab[index - 1]);
        Sum += (func_sb_false() ^ false ? ab[index] : 3);
        Sum += (func_sb_false() ^ false ? ab[index] : -5);
        Sum += (func_sb_false() ^ false ? ab[index] : local_int);
        Sum += (func_sb_false() ^ false ? ab[index] : static_field_int);
        Sum += (func_sb_false() ^ false ? ab[index] : t1_i.mfi);
        Sum += (func_sb_false() ^ false ? ab[index] : simple_func_int());
        Sum += (func_sb_false() ^ false ? ab[index] : ab[index]);
        Sum += (func_sb_false() ^ false ? ab[index] : ab[index - 1]);
        Sum += (func_sb_false() ^ false ? ab[index - 1] : 3);
        Sum += (func_sb_false() ^ false ? ab[index - 1] : -5);
        Sum += (func_sb_false() ^ false ? ab[index - 1] : local_int);
        Sum += (func_sb_false() ^ false ? ab[index - 1] : static_field_int);
        Sum += (func_sb_false() ^ false ? ab[index - 1] : t1_i.mfi);
        Sum += (func_sb_false() ^ false ? ab[index - 1] : simple_func_int());
        Sum += (func_sb_false() ^ false ? ab[index - 1] : ab[index]);
        Sum += (func_sb_false() ^ false ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_352()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ lb_true ? 3 : 3);
        Sum += (func_sb_false() ^ lb_true ? 3 : -5);
        Sum += (func_sb_false() ^ lb_true ? 3 : local_int);
        Sum += (func_sb_false() ^ lb_true ? 3 : static_field_int);
        Sum += (func_sb_false() ^ lb_true ? 3 : t1_i.mfi);
        Sum += (func_sb_false() ^ lb_true ? 3 : simple_func_int());
        Sum += (func_sb_false() ^ lb_true ? 3 : ab[index]);
        Sum += (func_sb_false() ^ lb_true ? 3 : ab[index - 1]);
        Sum += (func_sb_false() ^ lb_true ? -5 : 3);
        Sum += (func_sb_false() ^ lb_true ? -5 : -5);
        Sum += (func_sb_false() ^ lb_true ? -5 : local_int);
        Sum += (func_sb_false() ^ lb_true ? -5 : static_field_int);
        Sum += (func_sb_false() ^ lb_true ? -5 : t1_i.mfi);
        Sum += (func_sb_false() ^ lb_true ? -5 : simple_func_int());
        Sum += (func_sb_false() ^ lb_true ? -5 : ab[index]);
        Sum += (func_sb_false() ^ lb_true ? -5 : ab[index - 1]);
        Sum += (func_sb_false() ^ lb_true ? local_int : 3);
        Sum += (func_sb_false() ^ lb_true ? local_int : -5);
        Sum += (func_sb_false() ^ lb_true ? local_int : local_int);
        Sum += (func_sb_false() ^ lb_true ? local_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_353()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ lb_true ? local_int : t1_i.mfi);
        Sum += (func_sb_false() ^ lb_true ? local_int : simple_func_int());
        Sum += (func_sb_false() ^ lb_true ? local_int : ab[index]);
        Sum += (func_sb_false() ^ lb_true ? local_int : ab[index - 1]);
        Sum += (func_sb_false() ^ lb_true ? static_field_int : 3);
        Sum += (func_sb_false() ^ lb_true ? static_field_int : -5);
        Sum += (func_sb_false() ^ lb_true ? static_field_int : local_int);
        Sum += (func_sb_false() ^ lb_true ? static_field_int : static_field_int);
        Sum += (func_sb_false() ^ lb_true ? static_field_int : t1_i.mfi);
        Sum += (func_sb_false() ^ lb_true ? static_field_int : simple_func_int());
        Sum += (func_sb_false() ^ lb_true ? static_field_int : ab[index]);
        Sum += (func_sb_false() ^ lb_true ? static_field_int : ab[index - 1]);
        Sum += (func_sb_false() ^ lb_true ? t1_i.mfi : 3);
        Sum += (func_sb_false() ^ lb_true ? t1_i.mfi : -5);
        Sum += (func_sb_false() ^ lb_true ? t1_i.mfi : local_int);
        Sum += (func_sb_false() ^ lb_true ? t1_i.mfi : static_field_int);
        Sum += (func_sb_false() ^ lb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (func_sb_false() ^ lb_true ? t1_i.mfi : simple_func_int());
        Sum += (func_sb_false() ^ lb_true ? t1_i.mfi : ab[index]);
        Sum += (func_sb_false() ^ lb_true ? t1_i.mfi : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_354()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ lb_true ? simple_func_int() : 3);
        Sum += (func_sb_false() ^ lb_true ? simple_func_int() : -5);
        Sum += (func_sb_false() ^ lb_true ? simple_func_int() : local_int);
        Sum += (func_sb_false() ^ lb_true ? simple_func_int() : static_field_int);
        Sum += (func_sb_false() ^ lb_true ? simple_func_int() : t1_i.mfi);
        Sum += (func_sb_false() ^ lb_true ? simple_func_int() : simple_func_int());
        Sum += (func_sb_false() ^ lb_true ? simple_func_int() : ab[index]);
        Sum += (func_sb_false() ^ lb_true ? simple_func_int() : ab[index - 1]);
        Sum += (func_sb_false() ^ lb_true ? ab[index] : 3);
        Sum += (func_sb_false() ^ lb_true ? ab[index] : -5);
        Sum += (func_sb_false() ^ lb_true ? ab[index] : local_int);
        Sum += (func_sb_false() ^ lb_true ? ab[index] : static_field_int);
        Sum += (func_sb_false() ^ lb_true ? ab[index] : t1_i.mfi);
        Sum += (func_sb_false() ^ lb_true ? ab[index] : simple_func_int());
        Sum += (func_sb_false() ^ lb_true ? ab[index] : ab[index]);
        Sum += (func_sb_false() ^ lb_true ? ab[index] : ab[index - 1]);
        Sum += (func_sb_false() ^ lb_true ? ab[index - 1] : 3);
        Sum += (func_sb_false() ^ lb_true ? ab[index - 1] : -5);
        Sum += (func_sb_false() ^ lb_true ? ab[index - 1] : local_int);
        Sum += (func_sb_false() ^ lb_true ? ab[index - 1] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_355()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ lb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (func_sb_false() ^ lb_true ? ab[index - 1] : simple_func_int());
        Sum += (func_sb_false() ^ lb_true ? ab[index - 1] : ab[index]);
        Sum += (func_sb_false() ^ lb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (func_sb_false() ^ lb_false ? 3 : 3);
        Sum += (func_sb_false() ^ lb_false ? 3 : -5);
        Sum += (func_sb_false() ^ lb_false ? 3 : local_int);
        Sum += (func_sb_false() ^ lb_false ? 3 : static_field_int);
        Sum += (func_sb_false() ^ lb_false ? 3 : t1_i.mfi);
        Sum += (func_sb_false() ^ lb_false ? 3 : simple_func_int());
        Sum += (func_sb_false() ^ lb_false ? 3 : ab[index]);
        Sum += (func_sb_false() ^ lb_false ? 3 : ab[index - 1]);
        Sum += (func_sb_false() ^ lb_false ? -5 : 3);
        Sum += (func_sb_false() ^ lb_false ? -5 : -5);
        Sum += (func_sb_false() ^ lb_false ? -5 : local_int);
        Sum += (func_sb_false() ^ lb_false ? -5 : static_field_int);
        Sum += (func_sb_false() ^ lb_false ? -5 : t1_i.mfi);
        Sum += (func_sb_false() ^ lb_false ? -5 : simple_func_int());
        Sum += (func_sb_false() ^ lb_false ? -5 : ab[index]);
        Sum += (func_sb_false() ^ lb_false ? -5 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_356()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ lb_false ? local_int : 3);
        Sum += (func_sb_false() ^ lb_false ? local_int : -5);
        Sum += (func_sb_false() ^ lb_false ? local_int : local_int);
        Sum += (func_sb_false() ^ lb_false ? local_int : static_field_int);
        Sum += (func_sb_false() ^ lb_false ? local_int : t1_i.mfi);
        Sum += (func_sb_false() ^ lb_false ? local_int : simple_func_int());
        Sum += (func_sb_false() ^ lb_false ? local_int : ab[index]);
        Sum += (func_sb_false() ^ lb_false ? local_int : ab[index - 1]);
        Sum += (func_sb_false() ^ lb_false ? static_field_int : 3);
        Sum += (func_sb_false() ^ lb_false ? static_field_int : -5);
        Sum += (func_sb_false() ^ lb_false ? static_field_int : local_int);
        Sum += (func_sb_false() ^ lb_false ? static_field_int : static_field_int);
        Sum += (func_sb_false() ^ lb_false ? static_field_int : t1_i.mfi);
        Sum += (func_sb_false() ^ lb_false ? static_field_int : simple_func_int());
        Sum += (func_sb_false() ^ lb_false ? static_field_int : ab[index]);
        Sum += (func_sb_false() ^ lb_false ? static_field_int : ab[index - 1]);
        Sum += (func_sb_false() ^ lb_false ? t1_i.mfi : 3);
        Sum += (func_sb_false() ^ lb_false ? t1_i.mfi : -5);
        Sum += (func_sb_false() ^ lb_false ? t1_i.mfi : local_int);
        Sum += (func_sb_false() ^ lb_false ? t1_i.mfi : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_357()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ lb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (func_sb_false() ^ lb_false ? t1_i.mfi : simple_func_int());
        Sum += (func_sb_false() ^ lb_false ? t1_i.mfi : ab[index]);
        Sum += (func_sb_false() ^ lb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (func_sb_false() ^ lb_false ? simple_func_int() : 3);
        Sum += (func_sb_false() ^ lb_false ? simple_func_int() : -5);
        Sum += (func_sb_false() ^ lb_false ? simple_func_int() : local_int);
        Sum += (func_sb_false() ^ lb_false ? simple_func_int() : static_field_int);
        Sum += (func_sb_false() ^ lb_false ? simple_func_int() : t1_i.mfi);
        Sum += (func_sb_false() ^ lb_false ? simple_func_int() : simple_func_int());
        Sum += (func_sb_false() ^ lb_false ? simple_func_int() : ab[index]);
        Sum += (func_sb_false() ^ lb_false ? simple_func_int() : ab[index - 1]);
        Sum += (func_sb_false() ^ lb_false ? ab[index] : 3);
        Sum += (func_sb_false() ^ lb_false ? ab[index] : -5);
        Sum += (func_sb_false() ^ lb_false ? ab[index] : local_int);
        Sum += (func_sb_false() ^ lb_false ? ab[index] : static_field_int);
        Sum += (func_sb_false() ^ lb_false ? ab[index] : t1_i.mfi);
        Sum += (func_sb_false() ^ lb_false ? ab[index] : simple_func_int());
        Sum += (func_sb_false() ^ lb_false ? ab[index] : ab[index]);
        Sum += (func_sb_false() ^ lb_false ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_358()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ lb_false ? ab[index - 1] : 3);
        Sum += (func_sb_false() ^ lb_false ? ab[index - 1] : -5);
        Sum += (func_sb_false() ^ lb_false ? ab[index - 1] : local_int);
        Sum += (func_sb_false() ^ lb_false ? ab[index - 1] : static_field_int);
        Sum += (func_sb_false() ^ lb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (func_sb_false() ^ lb_false ? ab[index - 1] : simple_func_int());
        Sum += (func_sb_false() ^ lb_false ? ab[index - 1] : ab[index]);
        Sum += (func_sb_false() ^ lb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (func_sb_false() ^ sfb_true ? 3 : 3);
        Sum += (func_sb_false() ^ sfb_true ? 3 : -5);
        Sum += (func_sb_false() ^ sfb_true ? 3 : local_int);
        Sum += (func_sb_false() ^ sfb_true ? 3 : static_field_int);
        Sum += (func_sb_false() ^ sfb_true ? 3 : t1_i.mfi);
        Sum += (func_sb_false() ^ sfb_true ? 3 : simple_func_int());
        Sum += (func_sb_false() ^ sfb_true ? 3 : ab[index]);
        Sum += (func_sb_false() ^ sfb_true ? 3 : ab[index - 1]);
        Sum += (func_sb_false() ^ sfb_true ? -5 : 3);
        Sum += (func_sb_false() ^ sfb_true ? -5 : -5);
        Sum += (func_sb_false() ^ sfb_true ? -5 : local_int);
        Sum += (func_sb_false() ^ sfb_true ? -5 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_359()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ sfb_true ? -5 : t1_i.mfi);
        Sum += (func_sb_false() ^ sfb_true ? -5 : simple_func_int());
        Sum += (func_sb_false() ^ sfb_true ? -5 : ab[index]);
        Sum += (func_sb_false() ^ sfb_true ? -5 : ab[index - 1]);
        Sum += (func_sb_false() ^ sfb_true ? local_int : 3);
        Sum += (func_sb_false() ^ sfb_true ? local_int : -5);
        Sum += (func_sb_false() ^ sfb_true ? local_int : local_int);
        Sum += (func_sb_false() ^ sfb_true ? local_int : static_field_int);
        Sum += (func_sb_false() ^ sfb_true ? local_int : t1_i.mfi);
        Sum += (func_sb_false() ^ sfb_true ? local_int : simple_func_int());
        Sum += (func_sb_false() ^ sfb_true ? local_int : ab[index]);
        Sum += (func_sb_false() ^ sfb_true ? local_int : ab[index - 1]);
        Sum += (func_sb_false() ^ sfb_true ? static_field_int : 3);
        Sum += (func_sb_false() ^ sfb_true ? static_field_int : -5);
        Sum += (func_sb_false() ^ sfb_true ? static_field_int : local_int);
        Sum += (func_sb_false() ^ sfb_true ? static_field_int : static_field_int);
        Sum += (func_sb_false() ^ sfb_true ? static_field_int : t1_i.mfi);
        Sum += (func_sb_false() ^ sfb_true ? static_field_int : simple_func_int());
        Sum += (func_sb_false() ^ sfb_true ? static_field_int : ab[index]);
        Sum += (func_sb_false() ^ sfb_true ? static_field_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_360()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ sfb_true ? t1_i.mfi : 3);
        Sum += (func_sb_false() ^ sfb_true ? t1_i.mfi : -5);
        Sum += (func_sb_false() ^ sfb_true ? t1_i.mfi : local_int);
        Sum += (func_sb_false() ^ sfb_true ? t1_i.mfi : static_field_int);
        Sum += (func_sb_false() ^ sfb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (func_sb_false() ^ sfb_true ? t1_i.mfi : simple_func_int());
        Sum += (func_sb_false() ^ sfb_true ? t1_i.mfi : ab[index]);
        Sum += (func_sb_false() ^ sfb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (func_sb_false() ^ sfb_true ? simple_func_int() : 3);
        Sum += (func_sb_false() ^ sfb_true ? simple_func_int() : -5);
        Sum += (func_sb_false() ^ sfb_true ? simple_func_int() : local_int);
        Sum += (func_sb_false() ^ sfb_true ? simple_func_int() : static_field_int);
        Sum += (func_sb_false() ^ sfb_true ? simple_func_int() : t1_i.mfi);
        Sum += (func_sb_false() ^ sfb_true ? simple_func_int() : simple_func_int());
        Sum += (func_sb_false() ^ sfb_true ? simple_func_int() : ab[index]);
        Sum += (func_sb_false() ^ sfb_true ? simple_func_int() : ab[index - 1]);
        Sum += (func_sb_false() ^ sfb_true ? ab[index] : 3);
        Sum += (func_sb_false() ^ sfb_true ? ab[index] : -5);
        Sum += (func_sb_false() ^ sfb_true ? ab[index] : local_int);
        Sum += (func_sb_false() ^ sfb_true ? ab[index] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_361()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ sfb_true ? ab[index] : t1_i.mfi);
        Sum += (func_sb_false() ^ sfb_true ? ab[index] : simple_func_int());
        Sum += (func_sb_false() ^ sfb_true ? ab[index] : ab[index]);
        Sum += (func_sb_false() ^ sfb_true ? ab[index] : ab[index - 1]);
        Sum += (func_sb_false() ^ sfb_true ? ab[index - 1] : 3);
        Sum += (func_sb_false() ^ sfb_true ? ab[index - 1] : -5);
        Sum += (func_sb_false() ^ sfb_true ? ab[index - 1] : local_int);
        Sum += (func_sb_false() ^ sfb_true ? ab[index - 1] : static_field_int);
        Sum += (func_sb_false() ^ sfb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (func_sb_false() ^ sfb_true ? ab[index - 1] : simple_func_int());
        Sum += (func_sb_false() ^ sfb_true ? ab[index - 1] : ab[index]);
        Sum += (func_sb_false() ^ sfb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (func_sb_false() ^ sfb_false ? 3 : 3);
        Sum += (func_sb_false() ^ sfb_false ? 3 : -5);
        Sum += (func_sb_false() ^ sfb_false ? 3 : local_int);
        Sum += (func_sb_false() ^ sfb_false ? 3 : static_field_int);
        Sum += (func_sb_false() ^ sfb_false ? 3 : t1_i.mfi);
        Sum += (func_sb_false() ^ sfb_false ? 3 : simple_func_int());
        Sum += (func_sb_false() ^ sfb_false ? 3 : ab[index]);
        Sum += (func_sb_false() ^ sfb_false ? 3 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_362()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ sfb_false ? -5 : 3);
        Sum += (func_sb_false() ^ sfb_false ? -5 : -5);
        Sum += (func_sb_false() ^ sfb_false ? -5 : local_int);
        Sum += (func_sb_false() ^ sfb_false ? -5 : static_field_int);
        Sum += (func_sb_false() ^ sfb_false ? -5 : t1_i.mfi);
        Sum += (func_sb_false() ^ sfb_false ? -5 : simple_func_int());
        Sum += (func_sb_false() ^ sfb_false ? -5 : ab[index]);
        Sum += (func_sb_false() ^ sfb_false ? -5 : ab[index - 1]);
        Sum += (func_sb_false() ^ sfb_false ? local_int : 3);
        Sum += (func_sb_false() ^ sfb_false ? local_int : -5);
        Sum += (func_sb_false() ^ sfb_false ? local_int : local_int);
        Sum += (func_sb_false() ^ sfb_false ? local_int : static_field_int);
        Sum += (func_sb_false() ^ sfb_false ? local_int : t1_i.mfi);
        Sum += (func_sb_false() ^ sfb_false ? local_int : simple_func_int());
        Sum += (func_sb_false() ^ sfb_false ? local_int : ab[index]);
        Sum += (func_sb_false() ^ sfb_false ? local_int : ab[index - 1]);
        Sum += (func_sb_false() ^ sfb_false ? static_field_int : 3);
        Sum += (func_sb_false() ^ sfb_false ? static_field_int : -5);
        Sum += (func_sb_false() ^ sfb_false ? static_field_int : local_int);
        Sum += (func_sb_false() ^ sfb_false ? static_field_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_363()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ sfb_false ? static_field_int : t1_i.mfi);
        Sum += (func_sb_false() ^ sfb_false ? static_field_int : simple_func_int());
        Sum += (func_sb_false() ^ sfb_false ? static_field_int : ab[index]);
        Sum += (func_sb_false() ^ sfb_false ? static_field_int : ab[index - 1]);
        Sum += (func_sb_false() ^ sfb_false ? t1_i.mfi : 3);
        Sum += (func_sb_false() ^ sfb_false ? t1_i.mfi : -5);
        Sum += (func_sb_false() ^ sfb_false ? t1_i.mfi : local_int);
        Sum += (func_sb_false() ^ sfb_false ? t1_i.mfi : static_field_int);
        Sum += (func_sb_false() ^ sfb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (func_sb_false() ^ sfb_false ? t1_i.mfi : simple_func_int());
        Sum += (func_sb_false() ^ sfb_false ? t1_i.mfi : ab[index]);
        Sum += (func_sb_false() ^ sfb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (func_sb_false() ^ sfb_false ? simple_func_int() : 3);
        Sum += (func_sb_false() ^ sfb_false ? simple_func_int() : -5);
        Sum += (func_sb_false() ^ sfb_false ? simple_func_int() : local_int);
        Sum += (func_sb_false() ^ sfb_false ? simple_func_int() : static_field_int);
        Sum += (func_sb_false() ^ sfb_false ? simple_func_int() : t1_i.mfi);
        Sum += (func_sb_false() ^ sfb_false ? simple_func_int() : simple_func_int());
        Sum += (func_sb_false() ^ sfb_false ? simple_func_int() : ab[index]);
        Sum += (func_sb_false() ^ sfb_false ? simple_func_int() : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_364()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ sfb_false ? ab[index] : 3);
        Sum += (func_sb_false() ^ sfb_false ? ab[index] : -5);
        Sum += (func_sb_false() ^ sfb_false ? ab[index] : local_int);
        Sum += (func_sb_false() ^ sfb_false ? ab[index] : static_field_int);
        Sum += (func_sb_false() ^ sfb_false ? ab[index] : t1_i.mfi);
        Sum += (func_sb_false() ^ sfb_false ? ab[index] : simple_func_int());
        Sum += (func_sb_false() ^ sfb_false ? ab[index] : ab[index]);
        Sum += (func_sb_false() ^ sfb_false ? ab[index] : ab[index - 1]);
        Sum += (func_sb_false() ^ sfb_false ? ab[index - 1] : 3);
        Sum += (func_sb_false() ^ sfb_false ? ab[index - 1] : -5);
        Sum += (func_sb_false() ^ sfb_false ? ab[index - 1] : local_int);
        Sum += (func_sb_false() ^ sfb_false ? ab[index - 1] : static_field_int);
        Sum += (func_sb_false() ^ sfb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (func_sb_false() ^ sfb_false ? ab[index - 1] : simple_func_int());
        Sum += (func_sb_false() ^ sfb_false ? ab[index - 1] : ab[index]);
        Sum += (func_sb_false() ^ sfb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? 3 : 3);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? 3 : -5);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? 3 : local_int);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? 3 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_365()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ t1_i.mfb_true ? 3 : t1_i.mfi);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? 3 : simple_func_int());
        Sum += (func_sb_false() ^ t1_i.mfb_true ? 3 : ab[index]);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? 3 : ab[index - 1]);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? -5 : 3);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? -5 : -5);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? -5 : local_int);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? -5 : static_field_int);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? -5 : t1_i.mfi);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? -5 : simple_func_int());
        Sum += (func_sb_false() ^ t1_i.mfb_true ? -5 : ab[index]);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? -5 : ab[index - 1]);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? local_int : 3);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? local_int : -5);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? local_int : local_int);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? local_int : static_field_int);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? local_int : t1_i.mfi);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? local_int : simple_func_int());
        Sum += (func_sb_false() ^ t1_i.mfb_true ? local_int : ab[index]);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? local_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_366()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ t1_i.mfb_true ? static_field_int : 3);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? static_field_int : -5);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? static_field_int : local_int);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? static_field_int : static_field_int);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? static_field_int : t1_i.mfi);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? static_field_int : simple_func_int());
        Sum += (func_sb_false() ^ t1_i.mfb_true ? static_field_int : ab[index]);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? static_field_int : ab[index - 1]);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? t1_i.mfi : 3);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? t1_i.mfi : -5);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? t1_i.mfi : local_int);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? t1_i.mfi : static_field_int);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? t1_i.mfi : simple_func_int());
        Sum += (func_sb_false() ^ t1_i.mfb_true ? t1_i.mfi : ab[index]);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? simple_func_int() : 3);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? simple_func_int() : -5);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? simple_func_int() : local_int);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? simple_func_int() : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_367()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ t1_i.mfb_true ? simple_func_int() : t1_i.mfi);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? simple_func_int() : simple_func_int());
        Sum += (func_sb_false() ^ t1_i.mfb_true ? simple_func_int() : ab[index]);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? simple_func_int() : ab[index - 1]);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? ab[index] : 3);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? ab[index] : -5);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? ab[index] : local_int);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? ab[index] : static_field_int);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? ab[index] : t1_i.mfi);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? ab[index] : simple_func_int());
        Sum += (func_sb_false() ^ t1_i.mfb_true ? ab[index] : ab[index]);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? ab[index] : ab[index - 1]);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? ab[index - 1] : 3);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? ab[index - 1] : -5);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? ab[index - 1] : local_int);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? ab[index - 1] : static_field_int);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? ab[index - 1] : simple_func_int());
        Sum += (func_sb_false() ^ t1_i.mfb_true ? ab[index - 1] : ab[index]);
        Sum += (func_sb_false() ^ t1_i.mfb_true ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_368()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ t1_i.mfb_false ? 3 : 3);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? 3 : -5);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? 3 : local_int);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? 3 : static_field_int);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? 3 : t1_i.mfi);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? 3 : simple_func_int());
        Sum += (func_sb_false() ^ t1_i.mfb_false ? 3 : ab[index]);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? 3 : ab[index - 1]);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? -5 : 3);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? -5 : -5);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? -5 : local_int);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? -5 : static_field_int);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? -5 : t1_i.mfi);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? -5 : simple_func_int());
        Sum += (func_sb_false() ^ t1_i.mfb_false ? -5 : ab[index]);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? -5 : ab[index - 1]);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? local_int : 3);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? local_int : -5);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? local_int : local_int);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? local_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_369()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ t1_i.mfb_false ? local_int : t1_i.mfi);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? local_int : simple_func_int());
        Sum += (func_sb_false() ^ t1_i.mfb_false ? local_int : ab[index]);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? local_int : ab[index - 1]);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? static_field_int : 3);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? static_field_int : -5);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? static_field_int : local_int);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? static_field_int : static_field_int);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? static_field_int : t1_i.mfi);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? static_field_int : simple_func_int());
        Sum += (func_sb_false() ^ t1_i.mfb_false ? static_field_int : ab[index]);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? static_field_int : ab[index - 1]);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? t1_i.mfi : 3);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? t1_i.mfi : -5);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? t1_i.mfi : local_int);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? t1_i.mfi : static_field_int);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? t1_i.mfi : simple_func_int());
        Sum += (func_sb_false() ^ t1_i.mfb_false ? t1_i.mfi : ab[index]);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? t1_i.mfi : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_370()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ t1_i.mfb_false ? simple_func_int() : 3);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? simple_func_int() : -5);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? simple_func_int() : local_int);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? simple_func_int() : static_field_int);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? simple_func_int() : t1_i.mfi);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? simple_func_int() : simple_func_int());
        Sum += (func_sb_false() ^ t1_i.mfb_false ? simple_func_int() : ab[index]);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? simple_func_int() : ab[index - 1]);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? ab[index] : 3);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? ab[index] : -5);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? ab[index] : local_int);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? ab[index] : static_field_int);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? ab[index] : t1_i.mfi);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? ab[index] : simple_func_int());
        Sum += (func_sb_false() ^ t1_i.mfb_false ? ab[index] : ab[index]);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? ab[index] : ab[index - 1]);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? ab[index - 1] : 3);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? ab[index - 1] : -5);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? ab[index - 1] : local_int);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? ab[index - 1] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_371()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ t1_i.mfb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? ab[index - 1] : simple_func_int());
        Sum += (func_sb_false() ^ t1_i.mfb_false ? ab[index - 1] : ab[index]);
        Sum += (func_sb_false() ^ t1_i.mfb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (func_sb_false() ^ func_sb_true() ? 3 : 3);
        Sum += (func_sb_false() ^ func_sb_true() ? 3 : -5);
        Sum += (func_sb_false() ^ func_sb_true() ? 3 : local_int);
        Sum += (func_sb_false() ^ func_sb_true() ? 3 : static_field_int);
        Sum += (func_sb_false() ^ func_sb_true() ? 3 : t1_i.mfi);
        Sum += (func_sb_false() ^ func_sb_true() ? 3 : simple_func_int());
        Sum += (func_sb_false() ^ func_sb_true() ? 3 : ab[index]);
        Sum += (func_sb_false() ^ func_sb_true() ? 3 : ab[index - 1]);
        Sum += (func_sb_false() ^ func_sb_true() ? -5 : 3);
        Sum += (func_sb_false() ^ func_sb_true() ? -5 : -5);
        Sum += (func_sb_false() ^ func_sb_true() ? -5 : local_int);
        Sum += (func_sb_false() ^ func_sb_true() ? -5 : static_field_int);
        Sum += (func_sb_false() ^ func_sb_true() ? -5 : t1_i.mfi);
        Sum += (func_sb_false() ^ func_sb_true() ? -5 : simple_func_int());
        Sum += (func_sb_false() ^ func_sb_true() ? -5 : ab[index]);
        Sum += (func_sb_false() ^ func_sb_true() ? -5 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_372()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ func_sb_true() ? local_int : 3);
        Sum += (func_sb_false() ^ func_sb_true() ? local_int : -5);
        Sum += (func_sb_false() ^ func_sb_true() ? local_int : local_int);
        Sum += (func_sb_false() ^ func_sb_true() ? local_int : static_field_int);
        Sum += (func_sb_false() ^ func_sb_true() ? local_int : t1_i.mfi);
        Sum += (func_sb_false() ^ func_sb_true() ? local_int : simple_func_int());
        Sum += (func_sb_false() ^ func_sb_true() ? local_int : ab[index]);
        Sum += (func_sb_false() ^ func_sb_true() ? local_int : ab[index - 1]);
        Sum += (func_sb_false() ^ func_sb_true() ? static_field_int : 3);
        Sum += (func_sb_false() ^ func_sb_true() ? static_field_int : -5);
        Sum += (func_sb_false() ^ func_sb_true() ? static_field_int : local_int);
        Sum += (func_sb_false() ^ func_sb_true() ? static_field_int : static_field_int);
        Sum += (func_sb_false() ^ func_sb_true() ? static_field_int : t1_i.mfi);
        Sum += (func_sb_false() ^ func_sb_true() ? static_field_int : simple_func_int());
        Sum += (func_sb_false() ^ func_sb_true() ? static_field_int : ab[index]);
        Sum += (func_sb_false() ^ func_sb_true() ? static_field_int : ab[index - 1]);
        Sum += (func_sb_false() ^ func_sb_true() ? t1_i.mfi : 3);
        Sum += (func_sb_false() ^ func_sb_true() ? t1_i.mfi : -5);
        Sum += (func_sb_false() ^ func_sb_true() ? t1_i.mfi : local_int);
        Sum += (func_sb_false() ^ func_sb_true() ? t1_i.mfi : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_373()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ func_sb_true() ? t1_i.mfi : t1_i.mfi);
        Sum += (func_sb_false() ^ func_sb_true() ? t1_i.mfi : simple_func_int());
        Sum += (func_sb_false() ^ func_sb_true() ? t1_i.mfi : ab[index]);
        Sum += (func_sb_false() ^ func_sb_true() ? t1_i.mfi : ab[index - 1]);
        Sum += (func_sb_false() ^ func_sb_true() ? simple_func_int() : 3);
        Sum += (func_sb_false() ^ func_sb_true() ? simple_func_int() : -5);
        Sum += (func_sb_false() ^ func_sb_true() ? simple_func_int() : local_int);
        Sum += (func_sb_false() ^ func_sb_true() ? simple_func_int() : static_field_int);
        Sum += (func_sb_false() ^ func_sb_true() ? simple_func_int() : t1_i.mfi);
        Sum += (func_sb_false() ^ func_sb_true() ? simple_func_int() : simple_func_int());
        Sum += (func_sb_false() ^ func_sb_true() ? simple_func_int() : ab[index]);
        Sum += (func_sb_false() ^ func_sb_true() ? simple_func_int() : ab[index - 1]);
        Sum += (func_sb_false() ^ func_sb_true() ? ab[index] : 3);
        Sum += (func_sb_false() ^ func_sb_true() ? ab[index] : -5);
        Sum += (func_sb_false() ^ func_sb_true() ? ab[index] : local_int);
        Sum += (func_sb_false() ^ func_sb_true() ? ab[index] : static_field_int);
        Sum += (func_sb_false() ^ func_sb_true() ? ab[index] : t1_i.mfi);
        Sum += (func_sb_false() ^ func_sb_true() ? ab[index] : simple_func_int());
        Sum += (func_sb_false() ^ func_sb_true() ? ab[index] : ab[index]);
        Sum += (func_sb_false() ^ func_sb_true() ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_374()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ func_sb_true() ? ab[index - 1] : 3);
        Sum += (func_sb_false() ^ func_sb_true() ? ab[index - 1] : -5);
        Sum += (func_sb_false() ^ func_sb_true() ? ab[index - 1] : local_int);
        Sum += (func_sb_false() ^ func_sb_true() ? ab[index - 1] : static_field_int);
        Sum += (func_sb_false() ^ func_sb_true() ? ab[index - 1] : t1_i.mfi);
        Sum += (func_sb_false() ^ func_sb_true() ? ab[index - 1] : simple_func_int());
        Sum += (func_sb_false() ^ func_sb_true() ? ab[index - 1] : ab[index]);
        Sum += (func_sb_false() ^ func_sb_true() ? ab[index - 1] : ab[index - 1]);
        Sum += (func_sb_false() ^ func_sb_false() ? 3 : 3);
        Sum += (func_sb_false() ^ func_sb_false() ? 3 : -5);
        Sum += (func_sb_false() ^ func_sb_false() ? 3 : local_int);
        Sum += (func_sb_false() ^ func_sb_false() ? 3 : static_field_int);
        Sum += (func_sb_false() ^ func_sb_false() ? 3 : t1_i.mfi);
        Sum += (func_sb_false() ^ func_sb_false() ? 3 : simple_func_int());
        Sum += (func_sb_false() ^ func_sb_false() ? 3 : ab[index]);
        Sum += (func_sb_false() ^ func_sb_false() ? 3 : ab[index - 1]);
        Sum += (func_sb_false() ^ func_sb_false() ? -5 : 3);
        Sum += (func_sb_false() ^ func_sb_false() ? -5 : -5);
        Sum += (func_sb_false() ^ func_sb_false() ? -5 : local_int);
        Sum += (func_sb_false() ^ func_sb_false() ? -5 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_375()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ func_sb_false() ? -5 : t1_i.mfi);
        Sum += (func_sb_false() ^ func_sb_false() ? -5 : simple_func_int());
        Sum += (func_sb_false() ^ func_sb_false() ? -5 : ab[index]);
        Sum += (func_sb_false() ^ func_sb_false() ? -5 : ab[index - 1]);
        Sum += (func_sb_false() ^ func_sb_false() ? local_int : 3);
        Sum += (func_sb_false() ^ func_sb_false() ? local_int : -5);
        Sum += (func_sb_false() ^ func_sb_false() ? local_int : local_int);
        Sum += (func_sb_false() ^ func_sb_false() ? local_int : static_field_int);
        Sum += (func_sb_false() ^ func_sb_false() ? local_int : t1_i.mfi);
        Sum += (func_sb_false() ^ func_sb_false() ? local_int : simple_func_int());
        Sum += (func_sb_false() ^ func_sb_false() ? local_int : ab[index]);
        Sum += (func_sb_false() ^ func_sb_false() ? local_int : ab[index - 1]);
        Sum += (func_sb_false() ^ func_sb_false() ? static_field_int : 3);
        Sum += (func_sb_false() ^ func_sb_false() ? static_field_int : -5);
        Sum += (func_sb_false() ^ func_sb_false() ? static_field_int : local_int);
        Sum += (func_sb_false() ^ func_sb_false() ? static_field_int : static_field_int);
        Sum += (func_sb_false() ^ func_sb_false() ? static_field_int : t1_i.mfi);
        Sum += (func_sb_false() ^ func_sb_false() ? static_field_int : simple_func_int());
        Sum += (func_sb_false() ^ func_sb_false() ? static_field_int : ab[index]);
        Sum += (func_sb_false() ^ func_sb_false() ? static_field_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_376()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ func_sb_false() ? t1_i.mfi : 3);
        Sum += (func_sb_false() ^ func_sb_false() ? t1_i.mfi : -5);
        Sum += (func_sb_false() ^ func_sb_false() ? t1_i.mfi : local_int);
        Sum += (func_sb_false() ^ func_sb_false() ? t1_i.mfi : static_field_int);
        Sum += (func_sb_false() ^ func_sb_false() ? t1_i.mfi : t1_i.mfi);
        Sum += (func_sb_false() ^ func_sb_false() ? t1_i.mfi : simple_func_int());
        Sum += (func_sb_false() ^ func_sb_false() ? t1_i.mfi : ab[index]);
        Sum += (func_sb_false() ^ func_sb_false() ? t1_i.mfi : ab[index - 1]);
        Sum += (func_sb_false() ^ func_sb_false() ? simple_func_int() : 3);
        Sum += (func_sb_false() ^ func_sb_false() ? simple_func_int() : -5);
        Sum += (func_sb_false() ^ func_sb_false() ? simple_func_int() : local_int);
        Sum += (func_sb_false() ^ func_sb_false() ? simple_func_int() : static_field_int);
        Sum += (func_sb_false() ^ func_sb_false() ? simple_func_int() : t1_i.mfi);
        Sum += (func_sb_false() ^ func_sb_false() ? simple_func_int() : simple_func_int());
        Sum += (func_sb_false() ^ func_sb_false() ? simple_func_int() : ab[index]);
        Sum += (func_sb_false() ^ func_sb_false() ? simple_func_int() : ab[index - 1]);
        Sum += (func_sb_false() ^ func_sb_false() ? ab[index] : 3);
        Sum += (func_sb_false() ^ func_sb_false() ? ab[index] : -5);
        Sum += (func_sb_false() ^ func_sb_false() ? ab[index] : local_int);
        Sum += (func_sb_false() ^ func_sb_false() ? ab[index] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_377()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ func_sb_false() ? ab[index] : t1_i.mfi);
        Sum += (func_sb_false() ^ func_sb_false() ? ab[index] : simple_func_int());
        Sum += (func_sb_false() ^ func_sb_false() ? ab[index] : ab[index]);
        Sum += (func_sb_false() ^ func_sb_false() ? ab[index] : ab[index - 1]);
        Sum += (func_sb_false() ^ func_sb_false() ? ab[index - 1] : 3);
        Sum += (func_sb_false() ^ func_sb_false() ? ab[index - 1] : -5);
        Sum += (func_sb_false() ^ func_sb_false() ? ab[index - 1] : local_int);
        Sum += (func_sb_false() ^ func_sb_false() ? ab[index - 1] : static_field_int);
        Sum += (func_sb_false() ^ func_sb_false() ? ab[index - 1] : t1_i.mfi);
        Sum += (func_sb_false() ^ func_sb_false() ? ab[index - 1] : simple_func_int());
        Sum += (func_sb_false() ^ func_sb_false() ? ab[index - 1] : ab[index]);
        Sum += (func_sb_false() ^ func_sb_false() ? ab[index - 1] : ab[index - 1]);
        Sum += (func_sb_false() ^ ab_true[index] ? 3 : 3);
        Sum += (func_sb_false() ^ ab_true[index] ? 3 : -5);
        Sum += (func_sb_false() ^ ab_true[index] ? 3 : local_int);
        Sum += (func_sb_false() ^ ab_true[index] ? 3 : static_field_int);
        Sum += (func_sb_false() ^ ab_true[index] ? 3 : t1_i.mfi);
        Sum += (func_sb_false() ^ ab_true[index] ? 3 : simple_func_int());
        Sum += (func_sb_false() ^ ab_true[index] ? 3 : ab[index]);
        Sum += (func_sb_false() ^ ab_true[index] ? 3 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_378()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ ab_true[index] ? -5 : 3);
        Sum += (func_sb_false() ^ ab_true[index] ? -5 : -5);
        Sum += (func_sb_false() ^ ab_true[index] ? -5 : local_int);
        Sum += (func_sb_false() ^ ab_true[index] ? -5 : static_field_int);
        Sum += (func_sb_false() ^ ab_true[index] ? -5 : t1_i.mfi);
        Sum += (func_sb_false() ^ ab_true[index] ? -5 : simple_func_int());
        Sum += (func_sb_false() ^ ab_true[index] ? -5 : ab[index]);
        Sum += (func_sb_false() ^ ab_true[index] ? -5 : ab[index - 1]);
        Sum += (func_sb_false() ^ ab_true[index] ? local_int : 3);
        Sum += (func_sb_false() ^ ab_true[index] ? local_int : -5);
        Sum += (func_sb_false() ^ ab_true[index] ? local_int : local_int);
        Sum += (func_sb_false() ^ ab_true[index] ? local_int : static_field_int);
        Sum += (func_sb_false() ^ ab_true[index] ? local_int : t1_i.mfi);
        Sum += (func_sb_false() ^ ab_true[index] ? local_int : simple_func_int());
        Sum += (func_sb_false() ^ ab_true[index] ? local_int : ab[index]);
        Sum += (func_sb_false() ^ ab_true[index] ? local_int : ab[index - 1]);
        Sum += (func_sb_false() ^ ab_true[index] ? static_field_int : 3);
        Sum += (func_sb_false() ^ ab_true[index] ? static_field_int : -5);
        Sum += (func_sb_false() ^ ab_true[index] ? static_field_int : local_int);
        Sum += (func_sb_false() ^ ab_true[index] ? static_field_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_379()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ ab_true[index] ? static_field_int : t1_i.mfi);
        Sum += (func_sb_false() ^ ab_true[index] ? static_field_int : simple_func_int());
        Sum += (func_sb_false() ^ ab_true[index] ? static_field_int : ab[index]);
        Sum += (func_sb_false() ^ ab_true[index] ? static_field_int : ab[index - 1]);
        Sum += (func_sb_false() ^ ab_true[index] ? t1_i.mfi : 3);
        Sum += (func_sb_false() ^ ab_true[index] ? t1_i.mfi : -5);
        Sum += (func_sb_false() ^ ab_true[index] ? t1_i.mfi : local_int);
        Sum += (func_sb_false() ^ ab_true[index] ? t1_i.mfi : static_field_int);
        Sum += (func_sb_false() ^ ab_true[index] ? t1_i.mfi : t1_i.mfi);
        Sum += (func_sb_false() ^ ab_true[index] ? t1_i.mfi : simple_func_int());
        Sum += (func_sb_false() ^ ab_true[index] ? t1_i.mfi : ab[index]);
        Sum += (func_sb_false() ^ ab_true[index] ? t1_i.mfi : ab[index - 1]);
        Sum += (func_sb_false() ^ ab_true[index] ? simple_func_int() : 3);
        Sum += (func_sb_false() ^ ab_true[index] ? simple_func_int() : -5);
        Sum += (func_sb_false() ^ ab_true[index] ? simple_func_int() : local_int);
        Sum += (func_sb_false() ^ ab_true[index] ? simple_func_int() : static_field_int);
        Sum += (func_sb_false() ^ ab_true[index] ? simple_func_int() : t1_i.mfi);
        Sum += (func_sb_false() ^ ab_true[index] ? simple_func_int() : simple_func_int());
        Sum += (func_sb_false() ^ ab_true[index] ? simple_func_int() : ab[index]);
        Sum += (func_sb_false() ^ ab_true[index] ? simple_func_int() : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_380()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ ab_true[index] ? ab[index] : 3);
        Sum += (func_sb_false() ^ ab_true[index] ? ab[index] : -5);
        Sum += (func_sb_false() ^ ab_true[index] ? ab[index] : local_int);
        Sum += (func_sb_false() ^ ab_true[index] ? ab[index] : static_field_int);
        Sum += (func_sb_false() ^ ab_true[index] ? ab[index] : t1_i.mfi);
        Sum += (func_sb_false() ^ ab_true[index] ? ab[index] : simple_func_int());
        Sum += (func_sb_false() ^ ab_true[index] ? ab[index] : ab[index]);
        Sum += (func_sb_false() ^ ab_true[index] ? ab[index] : ab[index - 1]);
        Sum += (func_sb_false() ^ ab_true[index] ? ab[index - 1] : 3);
        Sum += (func_sb_false() ^ ab_true[index] ? ab[index - 1] : -5);
        Sum += (func_sb_false() ^ ab_true[index] ? ab[index - 1] : local_int);
        Sum += (func_sb_false() ^ ab_true[index] ? ab[index - 1] : static_field_int);
        Sum += (func_sb_false() ^ ab_true[index] ? ab[index - 1] : t1_i.mfi);
        Sum += (func_sb_false() ^ ab_true[index] ? ab[index - 1] : simple_func_int());
        Sum += (func_sb_false() ^ ab_true[index] ? ab[index - 1] : ab[index]);
        Sum += (func_sb_false() ^ ab_true[index] ? ab[index - 1] : ab[index - 1]);
        Sum += (func_sb_false() ^ ab_false[index] ? 3 : 3);
        Sum += (func_sb_false() ^ ab_false[index] ? 3 : -5);
        Sum += (func_sb_false() ^ ab_false[index] ? 3 : local_int);
        Sum += (func_sb_false() ^ ab_false[index] ? 3 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_381()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ ab_false[index] ? 3 : t1_i.mfi);
        Sum += (func_sb_false() ^ ab_false[index] ? 3 : simple_func_int());
        Sum += (func_sb_false() ^ ab_false[index] ? 3 : ab[index]);
        Sum += (func_sb_false() ^ ab_false[index] ? 3 : ab[index - 1]);
        Sum += (func_sb_false() ^ ab_false[index] ? -5 : 3);
        Sum += (func_sb_false() ^ ab_false[index] ? -5 : -5);
        Sum += (func_sb_false() ^ ab_false[index] ? -5 : local_int);
        Sum += (func_sb_false() ^ ab_false[index] ? -5 : static_field_int);
        Sum += (func_sb_false() ^ ab_false[index] ? -5 : t1_i.mfi);
        Sum += (func_sb_false() ^ ab_false[index] ? -5 : simple_func_int());
        Sum += (func_sb_false() ^ ab_false[index] ? -5 : ab[index]);
        Sum += (func_sb_false() ^ ab_false[index] ? -5 : ab[index - 1]);
        Sum += (func_sb_false() ^ ab_false[index] ? local_int : 3);
        Sum += (func_sb_false() ^ ab_false[index] ? local_int : -5);
        Sum += (func_sb_false() ^ ab_false[index] ? local_int : local_int);
        Sum += (func_sb_false() ^ ab_false[index] ? local_int : static_field_int);
        Sum += (func_sb_false() ^ ab_false[index] ? local_int : t1_i.mfi);
        Sum += (func_sb_false() ^ ab_false[index] ? local_int : simple_func_int());
        Sum += (func_sb_false() ^ ab_false[index] ? local_int : ab[index]);
        Sum += (func_sb_false() ^ ab_false[index] ? local_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_382()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ ab_false[index] ? static_field_int : 3);
        Sum += (func_sb_false() ^ ab_false[index] ? static_field_int : -5);
        Sum += (func_sb_false() ^ ab_false[index] ? static_field_int : local_int);
        Sum += (func_sb_false() ^ ab_false[index] ? static_field_int : static_field_int);
        Sum += (func_sb_false() ^ ab_false[index] ? static_field_int : t1_i.mfi);
        Sum += (func_sb_false() ^ ab_false[index] ? static_field_int : simple_func_int());
        Sum += (func_sb_false() ^ ab_false[index] ? static_field_int : ab[index]);
        Sum += (func_sb_false() ^ ab_false[index] ? static_field_int : ab[index - 1]);
        Sum += (func_sb_false() ^ ab_false[index] ? t1_i.mfi : 3);
        Sum += (func_sb_false() ^ ab_false[index] ? t1_i.mfi : -5);
        Sum += (func_sb_false() ^ ab_false[index] ? t1_i.mfi : local_int);
        Sum += (func_sb_false() ^ ab_false[index] ? t1_i.mfi : static_field_int);
        Sum += (func_sb_false() ^ ab_false[index] ? t1_i.mfi : t1_i.mfi);
        Sum += (func_sb_false() ^ ab_false[index] ? t1_i.mfi : simple_func_int());
        Sum += (func_sb_false() ^ ab_false[index] ? t1_i.mfi : ab[index]);
        Sum += (func_sb_false() ^ ab_false[index] ? t1_i.mfi : ab[index - 1]);
        Sum += (func_sb_false() ^ ab_false[index] ? simple_func_int() : 3);
        Sum += (func_sb_false() ^ ab_false[index] ? simple_func_int() : -5);
        Sum += (func_sb_false() ^ ab_false[index] ? simple_func_int() : local_int);
        Sum += (func_sb_false() ^ ab_false[index] ? simple_func_int() : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_383()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ^ ab_false[index] ? simple_func_int() : t1_i.mfi);
        Sum += (func_sb_false() ^ ab_false[index] ? simple_func_int() : simple_func_int());
        Sum += (func_sb_false() ^ ab_false[index] ? simple_func_int() : ab[index]);
        Sum += (func_sb_false() ^ ab_false[index] ? simple_func_int() : ab[index - 1]);
        Sum += (func_sb_false() ^ ab_false[index] ? ab[index] : 3);
        Sum += (func_sb_false() ^ ab_false[index] ? ab[index] : -5);
        Sum += (func_sb_false() ^ ab_false[index] ? ab[index] : local_int);
        Sum += (func_sb_false() ^ ab_false[index] ? ab[index] : static_field_int);
        Sum += (func_sb_false() ^ ab_false[index] ? ab[index] : t1_i.mfi);
        Sum += (func_sb_false() ^ ab_false[index] ? ab[index] : simple_func_int());
        Sum += (func_sb_false() ^ ab_false[index] ? ab[index] : ab[index]);
        Sum += (func_sb_false() ^ ab_false[index] ? ab[index] : ab[index - 1]);
        Sum += (func_sb_false() ^ ab_false[index] ? ab[index - 1] : 3);
        Sum += (func_sb_false() ^ ab_false[index] ? ab[index - 1] : -5);
        Sum += (func_sb_false() ^ ab_false[index] ? ab[index - 1] : local_int);
        Sum += (func_sb_false() ^ ab_false[index] ? ab[index - 1] : static_field_int);
        Sum += (func_sb_false() ^ ab_false[index] ? ab[index - 1] : t1_i.mfi);
        Sum += (func_sb_false() ^ ab_false[index] ? ab[index - 1] : simple_func_int());
        Sum += (func_sb_false() ^ ab_false[index] ? ab[index - 1] : ab[index]);
        Sum += (func_sb_false() ^ ab_false[index] ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_384()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ true ? 3 : 3);
        Sum += (ab_true[index] ^ true ? 3 : -5);
        Sum += (ab_true[index] ^ true ? 3 : local_int);
        Sum += (ab_true[index] ^ true ? 3 : static_field_int);
        Sum += (ab_true[index] ^ true ? 3 : t1_i.mfi);
        Sum += (ab_true[index] ^ true ? 3 : simple_func_int());
        Sum += (ab_true[index] ^ true ? 3 : ab[index]);
        Sum += (ab_true[index] ^ true ? 3 : ab[index - 1]);
        Sum += (ab_true[index] ^ true ? -5 : 3);
        Sum += (ab_true[index] ^ true ? -5 : -5);
        Sum += (ab_true[index] ^ true ? -5 : local_int);
        Sum += (ab_true[index] ^ true ? -5 : static_field_int);
        Sum += (ab_true[index] ^ true ? -5 : t1_i.mfi);
        Sum += (ab_true[index] ^ true ? -5 : simple_func_int());
        Sum += (ab_true[index] ^ true ? -5 : ab[index]);
        Sum += (ab_true[index] ^ true ? -5 : ab[index - 1]);
        Sum += (ab_true[index] ^ true ? local_int : 3);
        Sum += (ab_true[index] ^ true ? local_int : -5);
        Sum += (ab_true[index] ^ true ? local_int : local_int);
        Sum += (ab_true[index] ^ true ? local_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_385()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ true ? local_int : t1_i.mfi);
        Sum += (ab_true[index] ^ true ? local_int : simple_func_int());
        Sum += (ab_true[index] ^ true ? local_int : ab[index]);
        Sum += (ab_true[index] ^ true ? local_int : ab[index - 1]);
        Sum += (ab_true[index] ^ true ? static_field_int : 3);
        Sum += (ab_true[index] ^ true ? static_field_int : -5);
        Sum += (ab_true[index] ^ true ? static_field_int : local_int);
        Sum += (ab_true[index] ^ true ? static_field_int : static_field_int);
        Sum += (ab_true[index] ^ true ? static_field_int : t1_i.mfi);
        Sum += (ab_true[index] ^ true ? static_field_int : simple_func_int());
        Sum += (ab_true[index] ^ true ? static_field_int : ab[index]);
        Sum += (ab_true[index] ^ true ? static_field_int : ab[index - 1]);
        Sum += (ab_true[index] ^ true ? t1_i.mfi : 3);
        Sum += (ab_true[index] ^ true ? t1_i.mfi : -5);
        Sum += (ab_true[index] ^ true ? t1_i.mfi : local_int);
        Sum += (ab_true[index] ^ true ? t1_i.mfi : static_field_int);
        Sum += (ab_true[index] ^ true ? t1_i.mfi : t1_i.mfi);
        Sum += (ab_true[index] ^ true ? t1_i.mfi : simple_func_int());
        Sum += (ab_true[index] ^ true ? t1_i.mfi : ab[index]);
        Sum += (ab_true[index] ^ true ? t1_i.mfi : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_386()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ true ? simple_func_int() : 3);
        Sum += (ab_true[index] ^ true ? simple_func_int() : -5);
        Sum += (ab_true[index] ^ true ? simple_func_int() : local_int);
        Sum += (ab_true[index] ^ true ? simple_func_int() : static_field_int);
        Sum += (ab_true[index] ^ true ? simple_func_int() : t1_i.mfi);
        Sum += (ab_true[index] ^ true ? simple_func_int() : simple_func_int());
        Sum += (ab_true[index] ^ true ? simple_func_int() : ab[index]);
        Sum += (ab_true[index] ^ true ? simple_func_int() : ab[index - 1]);
        Sum += (ab_true[index] ^ true ? ab[index] : 3);
        Sum += (ab_true[index] ^ true ? ab[index] : -5);
        Sum += (ab_true[index] ^ true ? ab[index] : local_int);
        Sum += (ab_true[index] ^ true ? ab[index] : static_field_int);
        Sum += (ab_true[index] ^ true ? ab[index] : t1_i.mfi);
        Sum += (ab_true[index] ^ true ? ab[index] : simple_func_int());
        Sum += (ab_true[index] ^ true ? ab[index] : ab[index]);
        Sum += (ab_true[index] ^ true ? ab[index] : ab[index - 1]);
        Sum += (ab_true[index] ^ true ? ab[index - 1] : 3);
        Sum += (ab_true[index] ^ true ? ab[index - 1] : -5);
        Sum += (ab_true[index] ^ true ? ab[index - 1] : local_int);
        Sum += (ab_true[index] ^ true ? ab[index - 1] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_387()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ true ? ab[index - 1] : t1_i.mfi);
        Sum += (ab_true[index] ^ true ? ab[index - 1] : simple_func_int());
        Sum += (ab_true[index] ^ true ? ab[index - 1] : ab[index]);
        Sum += (ab_true[index] ^ true ? ab[index - 1] : ab[index - 1]);
        Sum += (ab_true[index] ^ false ? 3 : 3);
        Sum += (ab_true[index] ^ false ? 3 : -5);
        Sum += (ab_true[index] ^ false ? 3 : local_int);
        Sum += (ab_true[index] ^ false ? 3 : static_field_int);
        Sum += (ab_true[index] ^ false ? 3 : t1_i.mfi);
        Sum += (ab_true[index] ^ false ? 3 : simple_func_int());
        Sum += (ab_true[index] ^ false ? 3 : ab[index]);
        Sum += (ab_true[index] ^ false ? 3 : ab[index - 1]);
        Sum += (ab_true[index] ^ false ? -5 : 3);
        Sum += (ab_true[index] ^ false ? -5 : -5);
        Sum += (ab_true[index] ^ false ? -5 : local_int);
        Sum += (ab_true[index] ^ false ? -5 : static_field_int);
        Sum += (ab_true[index] ^ false ? -5 : t1_i.mfi);
        Sum += (ab_true[index] ^ false ? -5 : simple_func_int());
        Sum += (ab_true[index] ^ false ? -5 : ab[index]);
        Sum += (ab_true[index] ^ false ? -5 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_388()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ false ? local_int : 3);
        Sum += (ab_true[index] ^ false ? local_int : -5);
        Sum += (ab_true[index] ^ false ? local_int : local_int);
        Sum += (ab_true[index] ^ false ? local_int : static_field_int);
        Sum += (ab_true[index] ^ false ? local_int : t1_i.mfi);
        Sum += (ab_true[index] ^ false ? local_int : simple_func_int());
        Sum += (ab_true[index] ^ false ? local_int : ab[index]);
        Sum += (ab_true[index] ^ false ? local_int : ab[index - 1]);
        Sum += (ab_true[index] ^ false ? static_field_int : 3);
        Sum += (ab_true[index] ^ false ? static_field_int : -5);
        Sum += (ab_true[index] ^ false ? static_field_int : local_int);
        Sum += (ab_true[index] ^ false ? static_field_int : static_field_int);
        Sum += (ab_true[index] ^ false ? static_field_int : t1_i.mfi);
        Sum += (ab_true[index] ^ false ? static_field_int : simple_func_int());
        Sum += (ab_true[index] ^ false ? static_field_int : ab[index]);
        Sum += (ab_true[index] ^ false ? static_field_int : ab[index - 1]);
        Sum += (ab_true[index] ^ false ? t1_i.mfi : 3);
        Sum += (ab_true[index] ^ false ? t1_i.mfi : -5);
        Sum += (ab_true[index] ^ false ? t1_i.mfi : local_int);
        Sum += (ab_true[index] ^ false ? t1_i.mfi : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_389()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ false ? t1_i.mfi : t1_i.mfi);
        Sum += (ab_true[index] ^ false ? t1_i.mfi : simple_func_int());
        Sum += (ab_true[index] ^ false ? t1_i.mfi : ab[index]);
        Sum += (ab_true[index] ^ false ? t1_i.mfi : ab[index - 1]);
        Sum += (ab_true[index] ^ false ? simple_func_int() : 3);
        Sum += (ab_true[index] ^ false ? simple_func_int() : -5);
        Sum += (ab_true[index] ^ false ? simple_func_int() : local_int);
        Sum += (ab_true[index] ^ false ? simple_func_int() : static_field_int);
        Sum += (ab_true[index] ^ false ? simple_func_int() : t1_i.mfi);
        Sum += (ab_true[index] ^ false ? simple_func_int() : simple_func_int());
        Sum += (ab_true[index] ^ false ? simple_func_int() : ab[index]);
        Sum += (ab_true[index] ^ false ? simple_func_int() : ab[index - 1]);
        Sum += (ab_true[index] ^ false ? ab[index] : 3);
        Sum += (ab_true[index] ^ false ? ab[index] : -5);
        Sum += (ab_true[index] ^ false ? ab[index] : local_int);
        Sum += (ab_true[index] ^ false ? ab[index] : static_field_int);
        Sum += (ab_true[index] ^ false ? ab[index] : t1_i.mfi);
        Sum += (ab_true[index] ^ false ? ab[index] : simple_func_int());
        Sum += (ab_true[index] ^ false ? ab[index] : ab[index]);
        Sum += (ab_true[index] ^ false ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_390()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ false ? ab[index - 1] : 3);
        Sum += (ab_true[index] ^ false ? ab[index - 1] : -5);
        Sum += (ab_true[index] ^ false ? ab[index - 1] : local_int);
        Sum += (ab_true[index] ^ false ? ab[index - 1] : static_field_int);
        Sum += (ab_true[index] ^ false ? ab[index - 1] : t1_i.mfi);
        Sum += (ab_true[index] ^ false ? ab[index - 1] : simple_func_int());
        Sum += (ab_true[index] ^ false ? ab[index - 1] : ab[index]);
        Sum += (ab_true[index] ^ false ? ab[index - 1] : ab[index - 1]);
        Sum += (ab_true[index] ^ lb_true ? 3 : 3);
        Sum += (ab_true[index] ^ lb_true ? 3 : -5);
        Sum += (ab_true[index] ^ lb_true ? 3 : local_int);
        Sum += (ab_true[index] ^ lb_true ? 3 : static_field_int);
        Sum += (ab_true[index] ^ lb_true ? 3 : t1_i.mfi);
        Sum += (ab_true[index] ^ lb_true ? 3 : simple_func_int());
        Sum += (ab_true[index] ^ lb_true ? 3 : ab[index]);
        Sum += (ab_true[index] ^ lb_true ? 3 : ab[index - 1]);
        Sum += (ab_true[index] ^ lb_true ? -5 : 3);
        Sum += (ab_true[index] ^ lb_true ? -5 : -5);
        Sum += (ab_true[index] ^ lb_true ? -5 : local_int);
        Sum += (ab_true[index] ^ lb_true ? -5 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_391()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ lb_true ? -5 : t1_i.mfi);
        Sum += (ab_true[index] ^ lb_true ? -5 : simple_func_int());
        Sum += (ab_true[index] ^ lb_true ? -5 : ab[index]);
        Sum += (ab_true[index] ^ lb_true ? -5 : ab[index - 1]);
        Sum += (ab_true[index] ^ lb_true ? local_int : 3);
        Sum += (ab_true[index] ^ lb_true ? local_int : -5);
        Sum += (ab_true[index] ^ lb_true ? local_int : local_int);
        Sum += (ab_true[index] ^ lb_true ? local_int : static_field_int);
        Sum += (ab_true[index] ^ lb_true ? local_int : t1_i.mfi);
        Sum += (ab_true[index] ^ lb_true ? local_int : simple_func_int());
        Sum += (ab_true[index] ^ lb_true ? local_int : ab[index]);
        Sum += (ab_true[index] ^ lb_true ? local_int : ab[index - 1]);
        Sum += (ab_true[index] ^ lb_true ? static_field_int : 3);
        Sum += (ab_true[index] ^ lb_true ? static_field_int : -5);
        Sum += (ab_true[index] ^ lb_true ? static_field_int : local_int);
        Sum += (ab_true[index] ^ lb_true ? static_field_int : static_field_int);
        Sum += (ab_true[index] ^ lb_true ? static_field_int : t1_i.mfi);
        Sum += (ab_true[index] ^ lb_true ? static_field_int : simple_func_int());
        Sum += (ab_true[index] ^ lb_true ? static_field_int : ab[index]);
        Sum += (ab_true[index] ^ lb_true ? static_field_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_392()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ lb_true ? t1_i.mfi : 3);
        Sum += (ab_true[index] ^ lb_true ? t1_i.mfi : -5);
        Sum += (ab_true[index] ^ lb_true ? t1_i.mfi : local_int);
        Sum += (ab_true[index] ^ lb_true ? t1_i.mfi : static_field_int);
        Sum += (ab_true[index] ^ lb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (ab_true[index] ^ lb_true ? t1_i.mfi : simple_func_int());
        Sum += (ab_true[index] ^ lb_true ? t1_i.mfi : ab[index]);
        Sum += (ab_true[index] ^ lb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (ab_true[index] ^ lb_true ? simple_func_int() : 3);
        Sum += (ab_true[index] ^ lb_true ? simple_func_int() : -5);
        Sum += (ab_true[index] ^ lb_true ? simple_func_int() : local_int);
        Sum += (ab_true[index] ^ lb_true ? simple_func_int() : static_field_int);
        Sum += (ab_true[index] ^ lb_true ? simple_func_int() : t1_i.mfi);
        Sum += (ab_true[index] ^ lb_true ? simple_func_int() : simple_func_int());
        Sum += (ab_true[index] ^ lb_true ? simple_func_int() : ab[index]);
        Sum += (ab_true[index] ^ lb_true ? simple_func_int() : ab[index - 1]);
        Sum += (ab_true[index] ^ lb_true ? ab[index] : 3);
        Sum += (ab_true[index] ^ lb_true ? ab[index] : -5);
        Sum += (ab_true[index] ^ lb_true ? ab[index] : local_int);
        Sum += (ab_true[index] ^ lb_true ? ab[index] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_393()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ lb_true ? ab[index] : t1_i.mfi);
        Sum += (ab_true[index] ^ lb_true ? ab[index] : simple_func_int());
        Sum += (ab_true[index] ^ lb_true ? ab[index] : ab[index]);
        Sum += (ab_true[index] ^ lb_true ? ab[index] : ab[index - 1]);
        Sum += (ab_true[index] ^ lb_true ? ab[index - 1] : 3);
        Sum += (ab_true[index] ^ lb_true ? ab[index - 1] : -5);
        Sum += (ab_true[index] ^ lb_true ? ab[index - 1] : local_int);
        Sum += (ab_true[index] ^ lb_true ? ab[index - 1] : static_field_int);
        Sum += (ab_true[index] ^ lb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (ab_true[index] ^ lb_true ? ab[index - 1] : simple_func_int());
        Sum += (ab_true[index] ^ lb_true ? ab[index - 1] : ab[index]);
        Sum += (ab_true[index] ^ lb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (ab_true[index] ^ lb_false ? 3 : 3);
        Sum += (ab_true[index] ^ lb_false ? 3 : -5);
        Sum += (ab_true[index] ^ lb_false ? 3 : local_int);
        Sum += (ab_true[index] ^ lb_false ? 3 : static_field_int);
        Sum += (ab_true[index] ^ lb_false ? 3 : t1_i.mfi);
        Sum += (ab_true[index] ^ lb_false ? 3 : simple_func_int());
        Sum += (ab_true[index] ^ lb_false ? 3 : ab[index]);
        Sum += (ab_true[index] ^ lb_false ? 3 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_394()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ lb_false ? -5 : 3);
        Sum += (ab_true[index] ^ lb_false ? -5 : -5);
        Sum += (ab_true[index] ^ lb_false ? -5 : local_int);
        Sum += (ab_true[index] ^ lb_false ? -5 : static_field_int);
        Sum += (ab_true[index] ^ lb_false ? -5 : t1_i.mfi);
        Sum += (ab_true[index] ^ lb_false ? -5 : simple_func_int());
        Sum += (ab_true[index] ^ lb_false ? -5 : ab[index]);
        Sum += (ab_true[index] ^ lb_false ? -5 : ab[index - 1]);
        Sum += (ab_true[index] ^ lb_false ? local_int : 3);
        Sum += (ab_true[index] ^ lb_false ? local_int : -5);
        Sum += (ab_true[index] ^ lb_false ? local_int : local_int);
        Sum += (ab_true[index] ^ lb_false ? local_int : static_field_int);
        Sum += (ab_true[index] ^ lb_false ? local_int : t1_i.mfi);
        Sum += (ab_true[index] ^ lb_false ? local_int : simple_func_int());
        Sum += (ab_true[index] ^ lb_false ? local_int : ab[index]);
        Sum += (ab_true[index] ^ lb_false ? local_int : ab[index - 1]);
        Sum += (ab_true[index] ^ lb_false ? static_field_int : 3);
        Sum += (ab_true[index] ^ lb_false ? static_field_int : -5);
        Sum += (ab_true[index] ^ lb_false ? static_field_int : local_int);
        Sum += (ab_true[index] ^ lb_false ? static_field_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_395()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ lb_false ? static_field_int : t1_i.mfi);
        Sum += (ab_true[index] ^ lb_false ? static_field_int : simple_func_int());
        Sum += (ab_true[index] ^ lb_false ? static_field_int : ab[index]);
        Sum += (ab_true[index] ^ lb_false ? static_field_int : ab[index - 1]);
        Sum += (ab_true[index] ^ lb_false ? t1_i.mfi : 3);
        Sum += (ab_true[index] ^ lb_false ? t1_i.mfi : -5);
        Sum += (ab_true[index] ^ lb_false ? t1_i.mfi : local_int);
        Sum += (ab_true[index] ^ lb_false ? t1_i.mfi : static_field_int);
        Sum += (ab_true[index] ^ lb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (ab_true[index] ^ lb_false ? t1_i.mfi : simple_func_int());
        Sum += (ab_true[index] ^ lb_false ? t1_i.mfi : ab[index]);
        Sum += (ab_true[index] ^ lb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (ab_true[index] ^ lb_false ? simple_func_int() : 3);
        Sum += (ab_true[index] ^ lb_false ? simple_func_int() : -5);
        Sum += (ab_true[index] ^ lb_false ? simple_func_int() : local_int);
        Sum += (ab_true[index] ^ lb_false ? simple_func_int() : static_field_int);
        Sum += (ab_true[index] ^ lb_false ? simple_func_int() : t1_i.mfi);
        Sum += (ab_true[index] ^ lb_false ? simple_func_int() : simple_func_int());
        Sum += (ab_true[index] ^ lb_false ? simple_func_int() : ab[index]);
        Sum += (ab_true[index] ^ lb_false ? simple_func_int() : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_396()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ lb_false ? ab[index] : 3);
        Sum += (ab_true[index] ^ lb_false ? ab[index] : -5);
        Sum += (ab_true[index] ^ lb_false ? ab[index] : local_int);
        Sum += (ab_true[index] ^ lb_false ? ab[index] : static_field_int);
        Sum += (ab_true[index] ^ lb_false ? ab[index] : t1_i.mfi);
        Sum += (ab_true[index] ^ lb_false ? ab[index] : simple_func_int());
        Sum += (ab_true[index] ^ lb_false ? ab[index] : ab[index]);
        Sum += (ab_true[index] ^ lb_false ? ab[index] : ab[index - 1]);
        Sum += (ab_true[index] ^ lb_false ? ab[index - 1] : 3);
        Sum += (ab_true[index] ^ lb_false ? ab[index - 1] : -5);
        Sum += (ab_true[index] ^ lb_false ? ab[index - 1] : local_int);
        Sum += (ab_true[index] ^ lb_false ? ab[index - 1] : static_field_int);
        Sum += (ab_true[index] ^ lb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (ab_true[index] ^ lb_false ? ab[index - 1] : simple_func_int());
        Sum += (ab_true[index] ^ lb_false ? ab[index - 1] : ab[index]);
        Sum += (ab_true[index] ^ lb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (ab_true[index] ^ sfb_true ? 3 : 3);
        Sum += (ab_true[index] ^ sfb_true ? 3 : -5);
        Sum += (ab_true[index] ^ sfb_true ? 3 : local_int);
        Sum += (ab_true[index] ^ sfb_true ? 3 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_397()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ sfb_true ? 3 : t1_i.mfi);
        Sum += (ab_true[index] ^ sfb_true ? 3 : simple_func_int());
        Sum += (ab_true[index] ^ sfb_true ? 3 : ab[index]);
        Sum += (ab_true[index] ^ sfb_true ? 3 : ab[index - 1]);
        Sum += (ab_true[index] ^ sfb_true ? -5 : 3);
        Sum += (ab_true[index] ^ sfb_true ? -5 : -5);
        Sum += (ab_true[index] ^ sfb_true ? -5 : local_int);
        Sum += (ab_true[index] ^ sfb_true ? -5 : static_field_int);
        Sum += (ab_true[index] ^ sfb_true ? -5 : t1_i.mfi);
        Sum += (ab_true[index] ^ sfb_true ? -5 : simple_func_int());
        Sum += (ab_true[index] ^ sfb_true ? -5 : ab[index]);
        Sum += (ab_true[index] ^ sfb_true ? -5 : ab[index - 1]);
        Sum += (ab_true[index] ^ sfb_true ? local_int : 3);
        Sum += (ab_true[index] ^ sfb_true ? local_int : -5);
        Sum += (ab_true[index] ^ sfb_true ? local_int : local_int);
        Sum += (ab_true[index] ^ sfb_true ? local_int : static_field_int);
        Sum += (ab_true[index] ^ sfb_true ? local_int : t1_i.mfi);
        Sum += (ab_true[index] ^ sfb_true ? local_int : simple_func_int());
        Sum += (ab_true[index] ^ sfb_true ? local_int : ab[index]);
        Sum += (ab_true[index] ^ sfb_true ? local_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_398()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ sfb_true ? static_field_int : 3);
        Sum += (ab_true[index] ^ sfb_true ? static_field_int : -5);
        Sum += (ab_true[index] ^ sfb_true ? static_field_int : local_int);
        Sum += (ab_true[index] ^ sfb_true ? static_field_int : static_field_int);
        Sum += (ab_true[index] ^ sfb_true ? static_field_int : t1_i.mfi);
        Sum += (ab_true[index] ^ sfb_true ? static_field_int : simple_func_int());
        Sum += (ab_true[index] ^ sfb_true ? static_field_int : ab[index]);
        Sum += (ab_true[index] ^ sfb_true ? static_field_int : ab[index - 1]);
        Sum += (ab_true[index] ^ sfb_true ? t1_i.mfi : 3);
        Sum += (ab_true[index] ^ sfb_true ? t1_i.mfi : -5);
        Sum += (ab_true[index] ^ sfb_true ? t1_i.mfi : local_int);
        Sum += (ab_true[index] ^ sfb_true ? t1_i.mfi : static_field_int);
        Sum += (ab_true[index] ^ sfb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (ab_true[index] ^ sfb_true ? t1_i.mfi : simple_func_int());
        Sum += (ab_true[index] ^ sfb_true ? t1_i.mfi : ab[index]);
        Sum += (ab_true[index] ^ sfb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (ab_true[index] ^ sfb_true ? simple_func_int() : 3);
        Sum += (ab_true[index] ^ sfb_true ? simple_func_int() : -5);
        Sum += (ab_true[index] ^ sfb_true ? simple_func_int() : local_int);
        Sum += (ab_true[index] ^ sfb_true ? simple_func_int() : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_399()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ sfb_true ? simple_func_int() : t1_i.mfi);
        Sum += (ab_true[index] ^ sfb_true ? simple_func_int() : simple_func_int());
        Sum += (ab_true[index] ^ sfb_true ? simple_func_int() : ab[index]);
        Sum += (ab_true[index] ^ sfb_true ? simple_func_int() : ab[index - 1]);
        Sum += (ab_true[index] ^ sfb_true ? ab[index] : 3);
        Sum += (ab_true[index] ^ sfb_true ? ab[index] : -5);
        Sum += (ab_true[index] ^ sfb_true ? ab[index] : local_int);
        Sum += (ab_true[index] ^ sfb_true ? ab[index] : static_field_int);
        Sum += (ab_true[index] ^ sfb_true ? ab[index] : t1_i.mfi);
        Sum += (ab_true[index] ^ sfb_true ? ab[index] : simple_func_int());
        Sum += (ab_true[index] ^ sfb_true ? ab[index] : ab[index]);
        Sum += (ab_true[index] ^ sfb_true ? ab[index] : ab[index - 1]);
        Sum += (ab_true[index] ^ sfb_true ? ab[index - 1] : 3);
        Sum += (ab_true[index] ^ sfb_true ? ab[index - 1] : -5);
        Sum += (ab_true[index] ^ sfb_true ? ab[index - 1] : local_int);
        Sum += (ab_true[index] ^ sfb_true ? ab[index - 1] : static_field_int);
        Sum += (ab_true[index] ^ sfb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (ab_true[index] ^ sfb_true ? ab[index - 1] : simple_func_int());
        Sum += (ab_true[index] ^ sfb_true ? ab[index - 1] : ab[index]);
        Sum += (ab_true[index] ^ sfb_true ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_400()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ sfb_false ? 3 : 3);
        Sum += (ab_true[index] ^ sfb_false ? 3 : -5);
        Sum += (ab_true[index] ^ sfb_false ? 3 : local_int);
        Sum += (ab_true[index] ^ sfb_false ? 3 : static_field_int);
        Sum += (ab_true[index] ^ sfb_false ? 3 : t1_i.mfi);
        Sum += (ab_true[index] ^ sfb_false ? 3 : simple_func_int());
        Sum += (ab_true[index] ^ sfb_false ? 3 : ab[index]);
        Sum += (ab_true[index] ^ sfb_false ? 3 : ab[index - 1]);
        Sum += (ab_true[index] ^ sfb_false ? -5 : 3);
        Sum += (ab_true[index] ^ sfb_false ? -5 : -5);
        Sum += (ab_true[index] ^ sfb_false ? -5 : local_int);
        Sum += (ab_true[index] ^ sfb_false ? -5 : static_field_int);
        Sum += (ab_true[index] ^ sfb_false ? -5 : t1_i.mfi);
        Sum += (ab_true[index] ^ sfb_false ? -5 : simple_func_int());
        Sum += (ab_true[index] ^ sfb_false ? -5 : ab[index]);
        Sum += (ab_true[index] ^ sfb_false ? -5 : ab[index - 1]);
        Sum += (ab_true[index] ^ sfb_false ? local_int : 3);
        Sum += (ab_true[index] ^ sfb_false ? local_int : -5);
        Sum += (ab_true[index] ^ sfb_false ? local_int : local_int);
        Sum += (ab_true[index] ^ sfb_false ? local_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_401()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ sfb_false ? local_int : t1_i.mfi);
        Sum += (ab_true[index] ^ sfb_false ? local_int : simple_func_int());
        Sum += (ab_true[index] ^ sfb_false ? local_int : ab[index]);
        Sum += (ab_true[index] ^ sfb_false ? local_int : ab[index - 1]);
        Sum += (ab_true[index] ^ sfb_false ? static_field_int : 3);
        Sum += (ab_true[index] ^ sfb_false ? static_field_int : -5);
        Sum += (ab_true[index] ^ sfb_false ? static_field_int : local_int);
        Sum += (ab_true[index] ^ sfb_false ? static_field_int : static_field_int);
        Sum += (ab_true[index] ^ sfb_false ? static_field_int : t1_i.mfi);
        Sum += (ab_true[index] ^ sfb_false ? static_field_int : simple_func_int());
        Sum += (ab_true[index] ^ sfb_false ? static_field_int : ab[index]);
        Sum += (ab_true[index] ^ sfb_false ? static_field_int : ab[index - 1]);
        Sum += (ab_true[index] ^ sfb_false ? t1_i.mfi : 3);
        Sum += (ab_true[index] ^ sfb_false ? t1_i.mfi : -5);
        Sum += (ab_true[index] ^ sfb_false ? t1_i.mfi : local_int);
        Sum += (ab_true[index] ^ sfb_false ? t1_i.mfi : static_field_int);
        Sum += (ab_true[index] ^ sfb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (ab_true[index] ^ sfb_false ? t1_i.mfi : simple_func_int());
        Sum += (ab_true[index] ^ sfb_false ? t1_i.mfi : ab[index]);
        Sum += (ab_true[index] ^ sfb_false ? t1_i.mfi : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_402()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ sfb_false ? simple_func_int() : 3);
        Sum += (ab_true[index] ^ sfb_false ? simple_func_int() : -5);
        Sum += (ab_true[index] ^ sfb_false ? simple_func_int() : local_int);
        Sum += (ab_true[index] ^ sfb_false ? simple_func_int() : static_field_int);
        Sum += (ab_true[index] ^ sfb_false ? simple_func_int() : t1_i.mfi);
        Sum += (ab_true[index] ^ sfb_false ? simple_func_int() : simple_func_int());
        Sum += (ab_true[index] ^ sfb_false ? simple_func_int() : ab[index]);
        Sum += (ab_true[index] ^ sfb_false ? simple_func_int() : ab[index - 1]);
        Sum += (ab_true[index] ^ sfb_false ? ab[index] : 3);
        Sum += (ab_true[index] ^ sfb_false ? ab[index] : -5);
        Sum += (ab_true[index] ^ sfb_false ? ab[index] : local_int);
        Sum += (ab_true[index] ^ sfb_false ? ab[index] : static_field_int);
        Sum += (ab_true[index] ^ sfb_false ? ab[index] : t1_i.mfi);
        Sum += (ab_true[index] ^ sfb_false ? ab[index] : simple_func_int());
        Sum += (ab_true[index] ^ sfb_false ? ab[index] : ab[index]);
        Sum += (ab_true[index] ^ sfb_false ? ab[index] : ab[index - 1]);
        Sum += (ab_true[index] ^ sfb_false ? ab[index - 1] : 3);
        Sum += (ab_true[index] ^ sfb_false ? ab[index - 1] : -5);
        Sum += (ab_true[index] ^ sfb_false ? ab[index - 1] : local_int);
        Sum += (ab_true[index] ^ sfb_false ? ab[index - 1] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_403()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ sfb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (ab_true[index] ^ sfb_false ? ab[index - 1] : simple_func_int());
        Sum += (ab_true[index] ^ sfb_false ? ab[index - 1] : ab[index]);
        Sum += (ab_true[index] ^ sfb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? 3 : 3);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? 3 : -5);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? 3 : local_int);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? 3 : static_field_int);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? 3 : t1_i.mfi);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? 3 : simple_func_int());
        Sum += (ab_true[index] ^ t1_i.mfb_true ? 3 : ab[index]);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? 3 : ab[index - 1]);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? -5 : 3);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? -5 : -5);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? -5 : local_int);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? -5 : static_field_int);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? -5 : t1_i.mfi);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? -5 : simple_func_int());
        Sum += (ab_true[index] ^ t1_i.mfb_true ? -5 : ab[index]);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? -5 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_404()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ t1_i.mfb_true ? local_int : 3);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? local_int : -5);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? local_int : local_int);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? local_int : static_field_int);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? local_int : t1_i.mfi);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? local_int : simple_func_int());
        Sum += (ab_true[index] ^ t1_i.mfb_true ? local_int : ab[index]);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? local_int : ab[index - 1]);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? static_field_int : 3);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? static_field_int : -5);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? static_field_int : local_int);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? static_field_int : static_field_int);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? static_field_int : t1_i.mfi);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? static_field_int : simple_func_int());
        Sum += (ab_true[index] ^ t1_i.mfb_true ? static_field_int : ab[index]);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? static_field_int : ab[index - 1]);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? t1_i.mfi : 3);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? t1_i.mfi : -5);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? t1_i.mfi : local_int);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? t1_i.mfi : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_405()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ t1_i.mfb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? t1_i.mfi : simple_func_int());
        Sum += (ab_true[index] ^ t1_i.mfb_true ? t1_i.mfi : ab[index]);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? simple_func_int() : 3);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? simple_func_int() : -5);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? simple_func_int() : local_int);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? simple_func_int() : static_field_int);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? simple_func_int() : t1_i.mfi);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? simple_func_int() : simple_func_int());
        Sum += (ab_true[index] ^ t1_i.mfb_true ? simple_func_int() : ab[index]);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? simple_func_int() : ab[index - 1]);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? ab[index] : 3);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? ab[index] : -5);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? ab[index] : local_int);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? ab[index] : static_field_int);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? ab[index] : t1_i.mfi);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? ab[index] : simple_func_int());
        Sum += (ab_true[index] ^ t1_i.mfb_true ? ab[index] : ab[index]);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_406()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ t1_i.mfb_true ? ab[index - 1] : 3);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? ab[index - 1] : -5);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? ab[index - 1] : local_int);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? ab[index - 1] : static_field_int);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? ab[index - 1] : simple_func_int());
        Sum += (ab_true[index] ^ t1_i.mfb_true ? ab[index - 1] : ab[index]);
        Sum += (ab_true[index] ^ t1_i.mfb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? 3 : 3);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? 3 : -5);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? 3 : local_int);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? 3 : static_field_int);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? 3 : t1_i.mfi);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? 3 : simple_func_int());
        Sum += (ab_true[index] ^ t1_i.mfb_false ? 3 : ab[index]);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? 3 : ab[index - 1]);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? -5 : 3);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? -5 : -5);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? -5 : local_int);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? -5 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_407()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ t1_i.mfb_false ? -5 : t1_i.mfi);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? -5 : simple_func_int());
        Sum += (ab_true[index] ^ t1_i.mfb_false ? -5 : ab[index]);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? -5 : ab[index - 1]);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? local_int : 3);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? local_int : -5);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? local_int : local_int);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? local_int : static_field_int);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? local_int : t1_i.mfi);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? local_int : simple_func_int());
        Sum += (ab_true[index] ^ t1_i.mfb_false ? local_int : ab[index]);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? local_int : ab[index - 1]);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? static_field_int : 3);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? static_field_int : -5);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? static_field_int : local_int);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? static_field_int : static_field_int);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? static_field_int : t1_i.mfi);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? static_field_int : simple_func_int());
        Sum += (ab_true[index] ^ t1_i.mfb_false ? static_field_int : ab[index]);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? static_field_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_408()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ t1_i.mfb_false ? t1_i.mfi : 3);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? t1_i.mfi : -5);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? t1_i.mfi : local_int);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? t1_i.mfi : static_field_int);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? t1_i.mfi : simple_func_int());
        Sum += (ab_true[index] ^ t1_i.mfb_false ? t1_i.mfi : ab[index]);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? simple_func_int() : 3);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? simple_func_int() : -5);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? simple_func_int() : local_int);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? simple_func_int() : static_field_int);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? simple_func_int() : t1_i.mfi);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? simple_func_int() : simple_func_int());
        Sum += (ab_true[index] ^ t1_i.mfb_false ? simple_func_int() : ab[index]);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? simple_func_int() : ab[index - 1]);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? ab[index] : 3);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? ab[index] : -5);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? ab[index] : local_int);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? ab[index] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_409()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ t1_i.mfb_false ? ab[index] : t1_i.mfi);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? ab[index] : simple_func_int());
        Sum += (ab_true[index] ^ t1_i.mfb_false ? ab[index] : ab[index]);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? ab[index] : ab[index - 1]);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? ab[index - 1] : 3);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? ab[index - 1] : -5);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? ab[index - 1] : local_int);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? ab[index - 1] : static_field_int);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? ab[index - 1] : simple_func_int());
        Sum += (ab_true[index] ^ t1_i.mfb_false ? ab[index - 1] : ab[index]);
        Sum += (ab_true[index] ^ t1_i.mfb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (ab_true[index] ^ func_sb_true() ? 3 : 3);
        Sum += (ab_true[index] ^ func_sb_true() ? 3 : -5);
        Sum += (ab_true[index] ^ func_sb_true() ? 3 : local_int);
        Sum += (ab_true[index] ^ func_sb_true() ? 3 : static_field_int);
        Sum += (ab_true[index] ^ func_sb_true() ? 3 : t1_i.mfi);
        Sum += (ab_true[index] ^ func_sb_true() ? 3 : simple_func_int());
        Sum += (ab_true[index] ^ func_sb_true() ? 3 : ab[index]);
        Sum += (ab_true[index] ^ func_sb_true() ? 3 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_410()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ func_sb_true() ? -5 : 3);
        Sum += (ab_true[index] ^ func_sb_true() ? -5 : -5);
        Sum += (ab_true[index] ^ func_sb_true() ? -5 : local_int);
        Sum += (ab_true[index] ^ func_sb_true() ? -5 : static_field_int);
        Sum += (ab_true[index] ^ func_sb_true() ? -5 : t1_i.mfi);
        Sum += (ab_true[index] ^ func_sb_true() ? -5 : simple_func_int());
        Sum += (ab_true[index] ^ func_sb_true() ? -5 : ab[index]);
        Sum += (ab_true[index] ^ func_sb_true() ? -5 : ab[index - 1]);
        Sum += (ab_true[index] ^ func_sb_true() ? local_int : 3);
        Sum += (ab_true[index] ^ func_sb_true() ? local_int : -5);
        Sum += (ab_true[index] ^ func_sb_true() ? local_int : local_int);
        Sum += (ab_true[index] ^ func_sb_true() ? local_int : static_field_int);
        Sum += (ab_true[index] ^ func_sb_true() ? local_int : t1_i.mfi);
        Sum += (ab_true[index] ^ func_sb_true() ? local_int : simple_func_int());
        Sum += (ab_true[index] ^ func_sb_true() ? local_int : ab[index]);
        Sum += (ab_true[index] ^ func_sb_true() ? local_int : ab[index - 1]);
        Sum += (ab_true[index] ^ func_sb_true() ? static_field_int : 3);
        Sum += (ab_true[index] ^ func_sb_true() ? static_field_int : -5);
        Sum += (ab_true[index] ^ func_sb_true() ? static_field_int : local_int);
        Sum += (ab_true[index] ^ func_sb_true() ? static_field_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_411()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ func_sb_true() ? static_field_int : t1_i.mfi);
        Sum += (ab_true[index] ^ func_sb_true() ? static_field_int : simple_func_int());
        Sum += (ab_true[index] ^ func_sb_true() ? static_field_int : ab[index]);
        Sum += (ab_true[index] ^ func_sb_true() ? static_field_int : ab[index - 1]);
        Sum += (ab_true[index] ^ func_sb_true() ? t1_i.mfi : 3);
        Sum += (ab_true[index] ^ func_sb_true() ? t1_i.mfi : -5);
        Sum += (ab_true[index] ^ func_sb_true() ? t1_i.mfi : local_int);
        Sum += (ab_true[index] ^ func_sb_true() ? t1_i.mfi : static_field_int);
        Sum += (ab_true[index] ^ func_sb_true() ? t1_i.mfi : t1_i.mfi);
        Sum += (ab_true[index] ^ func_sb_true() ? t1_i.mfi : simple_func_int());
        Sum += (ab_true[index] ^ func_sb_true() ? t1_i.mfi : ab[index]);
        Sum += (ab_true[index] ^ func_sb_true() ? t1_i.mfi : ab[index - 1]);
        Sum += (ab_true[index] ^ func_sb_true() ? simple_func_int() : 3);
        Sum += (ab_true[index] ^ func_sb_true() ? simple_func_int() : -5);
        Sum += (ab_true[index] ^ func_sb_true() ? simple_func_int() : local_int);
        Sum += (ab_true[index] ^ func_sb_true() ? simple_func_int() : static_field_int);
        Sum += (ab_true[index] ^ func_sb_true() ? simple_func_int() : t1_i.mfi);
        Sum += (ab_true[index] ^ func_sb_true() ? simple_func_int() : simple_func_int());
        Sum += (ab_true[index] ^ func_sb_true() ? simple_func_int() : ab[index]);
        Sum += (ab_true[index] ^ func_sb_true() ? simple_func_int() : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_412()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ func_sb_true() ? ab[index] : 3);
        Sum += (ab_true[index] ^ func_sb_true() ? ab[index] : -5);
        Sum += (ab_true[index] ^ func_sb_true() ? ab[index] : local_int);
        Sum += (ab_true[index] ^ func_sb_true() ? ab[index] : static_field_int);
        Sum += (ab_true[index] ^ func_sb_true() ? ab[index] : t1_i.mfi);
        Sum += (ab_true[index] ^ func_sb_true() ? ab[index] : simple_func_int());
        Sum += (ab_true[index] ^ func_sb_true() ? ab[index] : ab[index]);
        Sum += (ab_true[index] ^ func_sb_true() ? ab[index] : ab[index - 1]);
        Sum += (ab_true[index] ^ func_sb_true() ? ab[index - 1] : 3);
        Sum += (ab_true[index] ^ func_sb_true() ? ab[index - 1] : -5);
        Sum += (ab_true[index] ^ func_sb_true() ? ab[index - 1] : local_int);
        Sum += (ab_true[index] ^ func_sb_true() ? ab[index - 1] : static_field_int);
        Sum += (ab_true[index] ^ func_sb_true() ? ab[index - 1] : t1_i.mfi);
        Sum += (ab_true[index] ^ func_sb_true() ? ab[index - 1] : simple_func_int());
        Sum += (ab_true[index] ^ func_sb_true() ? ab[index - 1] : ab[index]);
        Sum += (ab_true[index] ^ func_sb_true() ? ab[index - 1] : ab[index - 1]);
        Sum += (ab_true[index] ^ func_sb_false() ? 3 : 3);
        Sum += (ab_true[index] ^ func_sb_false() ? 3 : -5);
        Sum += (ab_true[index] ^ func_sb_false() ? 3 : local_int);
        Sum += (ab_true[index] ^ func_sb_false() ? 3 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_413()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ func_sb_false() ? 3 : t1_i.mfi);
        Sum += (ab_true[index] ^ func_sb_false() ? 3 : simple_func_int());
        Sum += (ab_true[index] ^ func_sb_false() ? 3 : ab[index]);
        Sum += (ab_true[index] ^ func_sb_false() ? 3 : ab[index - 1]);
        Sum += (ab_true[index] ^ func_sb_false() ? -5 : 3);
        Sum += (ab_true[index] ^ func_sb_false() ? -5 : -5);
        Sum += (ab_true[index] ^ func_sb_false() ? -5 : local_int);
        Sum += (ab_true[index] ^ func_sb_false() ? -5 : static_field_int);
        Sum += (ab_true[index] ^ func_sb_false() ? -5 : t1_i.mfi);
        Sum += (ab_true[index] ^ func_sb_false() ? -5 : simple_func_int());
        Sum += (ab_true[index] ^ func_sb_false() ? -5 : ab[index]);
        Sum += (ab_true[index] ^ func_sb_false() ? -5 : ab[index - 1]);
        Sum += (ab_true[index] ^ func_sb_false() ? local_int : 3);
        Sum += (ab_true[index] ^ func_sb_false() ? local_int : -5);
        Sum += (ab_true[index] ^ func_sb_false() ? local_int : local_int);
        Sum += (ab_true[index] ^ func_sb_false() ? local_int : static_field_int);
        Sum += (ab_true[index] ^ func_sb_false() ? local_int : t1_i.mfi);
        Sum += (ab_true[index] ^ func_sb_false() ? local_int : simple_func_int());
        Sum += (ab_true[index] ^ func_sb_false() ? local_int : ab[index]);
        Sum += (ab_true[index] ^ func_sb_false() ? local_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_414()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ func_sb_false() ? static_field_int : 3);
        Sum += (ab_true[index] ^ func_sb_false() ? static_field_int : -5);
        Sum += (ab_true[index] ^ func_sb_false() ? static_field_int : local_int);
        Sum += (ab_true[index] ^ func_sb_false() ? static_field_int : static_field_int);
        Sum += (ab_true[index] ^ func_sb_false() ? static_field_int : t1_i.mfi);
        Sum += (ab_true[index] ^ func_sb_false() ? static_field_int : simple_func_int());
        Sum += (ab_true[index] ^ func_sb_false() ? static_field_int : ab[index]);
        Sum += (ab_true[index] ^ func_sb_false() ? static_field_int : ab[index - 1]);
        Sum += (ab_true[index] ^ func_sb_false() ? t1_i.mfi : 3);
        Sum += (ab_true[index] ^ func_sb_false() ? t1_i.mfi : -5);
        Sum += (ab_true[index] ^ func_sb_false() ? t1_i.mfi : local_int);
        Sum += (ab_true[index] ^ func_sb_false() ? t1_i.mfi : static_field_int);
        Sum += (ab_true[index] ^ func_sb_false() ? t1_i.mfi : t1_i.mfi);
        Sum += (ab_true[index] ^ func_sb_false() ? t1_i.mfi : simple_func_int());
        Sum += (ab_true[index] ^ func_sb_false() ? t1_i.mfi : ab[index]);
        Sum += (ab_true[index] ^ func_sb_false() ? t1_i.mfi : ab[index - 1]);
        Sum += (ab_true[index] ^ func_sb_false() ? simple_func_int() : 3);
        Sum += (ab_true[index] ^ func_sb_false() ? simple_func_int() : -5);
        Sum += (ab_true[index] ^ func_sb_false() ? simple_func_int() : local_int);
        Sum += (ab_true[index] ^ func_sb_false() ? simple_func_int() : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_415()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ func_sb_false() ? simple_func_int() : t1_i.mfi);
        Sum += (ab_true[index] ^ func_sb_false() ? simple_func_int() : simple_func_int());
        Sum += (ab_true[index] ^ func_sb_false() ? simple_func_int() : ab[index]);
        Sum += (ab_true[index] ^ func_sb_false() ? simple_func_int() : ab[index - 1]);
        Sum += (ab_true[index] ^ func_sb_false() ? ab[index] : 3);
        Sum += (ab_true[index] ^ func_sb_false() ? ab[index] : -5);
        Sum += (ab_true[index] ^ func_sb_false() ? ab[index] : local_int);
        Sum += (ab_true[index] ^ func_sb_false() ? ab[index] : static_field_int);
        Sum += (ab_true[index] ^ func_sb_false() ? ab[index] : t1_i.mfi);
        Sum += (ab_true[index] ^ func_sb_false() ? ab[index] : simple_func_int());
        Sum += (ab_true[index] ^ func_sb_false() ? ab[index] : ab[index]);
        Sum += (ab_true[index] ^ func_sb_false() ? ab[index] : ab[index - 1]);
        Sum += (ab_true[index] ^ func_sb_false() ? ab[index - 1] : 3);
        Sum += (ab_true[index] ^ func_sb_false() ? ab[index - 1] : -5);
        Sum += (ab_true[index] ^ func_sb_false() ? ab[index - 1] : local_int);
        Sum += (ab_true[index] ^ func_sb_false() ? ab[index - 1] : static_field_int);
        Sum += (ab_true[index] ^ func_sb_false() ? ab[index - 1] : t1_i.mfi);
        Sum += (ab_true[index] ^ func_sb_false() ? ab[index - 1] : simple_func_int());
        Sum += (ab_true[index] ^ func_sb_false() ? ab[index - 1] : ab[index]);
        Sum += (ab_true[index] ^ func_sb_false() ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_416()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ ab_true[index] ? 3 : 3);
        Sum += (ab_true[index] ^ ab_true[index] ? 3 : -5);
        Sum += (ab_true[index] ^ ab_true[index] ? 3 : local_int);
        Sum += (ab_true[index] ^ ab_true[index] ? 3 : static_field_int);
        Sum += (ab_true[index] ^ ab_true[index] ? 3 : t1_i.mfi);
        Sum += (ab_true[index] ^ ab_true[index] ? 3 : simple_func_int());
        Sum += (ab_true[index] ^ ab_true[index] ? 3 : ab[index]);
        Sum += (ab_true[index] ^ ab_true[index] ? 3 : ab[index - 1]);
        Sum += (ab_true[index] ^ ab_true[index] ? -5 : 3);
        Sum += (ab_true[index] ^ ab_true[index] ? -5 : -5);
        Sum += (ab_true[index] ^ ab_true[index] ? -5 : local_int);
        Sum += (ab_true[index] ^ ab_true[index] ? -5 : static_field_int);
        Sum += (ab_true[index] ^ ab_true[index] ? -5 : t1_i.mfi);
        Sum += (ab_true[index] ^ ab_true[index] ? -5 : simple_func_int());
        Sum += (ab_true[index] ^ ab_true[index] ? -5 : ab[index]);
        Sum += (ab_true[index] ^ ab_true[index] ? -5 : ab[index - 1]);
        Sum += (ab_true[index] ^ ab_true[index] ? local_int : 3);
        Sum += (ab_true[index] ^ ab_true[index] ? local_int : -5);
        Sum += (ab_true[index] ^ ab_true[index] ? local_int : local_int);
        Sum += (ab_true[index] ^ ab_true[index] ? local_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_417()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ ab_true[index] ? local_int : t1_i.mfi);
        Sum += (ab_true[index] ^ ab_true[index] ? local_int : simple_func_int());
        Sum += (ab_true[index] ^ ab_true[index] ? local_int : ab[index]);
        Sum += (ab_true[index] ^ ab_true[index] ? local_int : ab[index - 1]);
        Sum += (ab_true[index] ^ ab_true[index] ? static_field_int : 3);
        Sum += (ab_true[index] ^ ab_true[index] ? static_field_int : -5);
        Sum += (ab_true[index] ^ ab_true[index] ? static_field_int : local_int);
        Sum += (ab_true[index] ^ ab_true[index] ? static_field_int : static_field_int);
        Sum += (ab_true[index] ^ ab_true[index] ? static_field_int : t1_i.mfi);
        Sum += (ab_true[index] ^ ab_true[index] ? static_field_int : simple_func_int());
        Sum += (ab_true[index] ^ ab_true[index] ? static_field_int : ab[index]);
        Sum += (ab_true[index] ^ ab_true[index] ? static_field_int : ab[index - 1]);
        Sum += (ab_true[index] ^ ab_true[index] ? t1_i.mfi : 3);
        Sum += (ab_true[index] ^ ab_true[index] ? t1_i.mfi : -5);
        Sum += (ab_true[index] ^ ab_true[index] ? t1_i.mfi : local_int);
        Sum += (ab_true[index] ^ ab_true[index] ? t1_i.mfi : static_field_int);
        Sum += (ab_true[index] ^ ab_true[index] ? t1_i.mfi : t1_i.mfi);
        Sum += (ab_true[index] ^ ab_true[index] ? t1_i.mfi : simple_func_int());
        Sum += (ab_true[index] ^ ab_true[index] ? t1_i.mfi : ab[index]);
        Sum += (ab_true[index] ^ ab_true[index] ? t1_i.mfi : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_418()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ ab_true[index] ? simple_func_int() : 3);
        Sum += (ab_true[index] ^ ab_true[index] ? simple_func_int() : -5);
        Sum += (ab_true[index] ^ ab_true[index] ? simple_func_int() : local_int);
        Sum += (ab_true[index] ^ ab_true[index] ? simple_func_int() : static_field_int);
        Sum += (ab_true[index] ^ ab_true[index] ? simple_func_int() : t1_i.mfi);
        Sum += (ab_true[index] ^ ab_true[index] ? simple_func_int() : simple_func_int());
        Sum += (ab_true[index] ^ ab_true[index] ? simple_func_int() : ab[index]);
        Sum += (ab_true[index] ^ ab_true[index] ? simple_func_int() : ab[index - 1]);
        Sum += (ab_true[index] ^ ab_true[index] ? ab[index] : 3);
        Sum += (ab_true[index] ^ ab_true[index] ? ab[index] : -5);
        Sum += (ab_true[index] ^ ab_true[index] ? ab[index] : local_int);
        Sum += (ab_true[index] ^ ab_true[index] ? ab[index] : static_field_int);
        Sum += (ab_true[index] ^ ab_true[index] ? ab[index] : t1_i.mfi);
        Sum += (ab_true[index] ^ ab_true[index] ? ab[index] : simple_func_int());
        Sum += (ab_true[index] ^ ab_true[index] ? ab[index] : ab[index]);
        Sum += (ab_true[index] ^ ab_true[index] ? ab[index] : ab[index - 1]);
        Sum += (ab_true[index] ^ ab_true[index] ? ab[index - 1] : 3);
        Sum += (ab_true[index] ^ ab_true[index] ? ab[index - 1] : -5);
        Sum += (ab_true[index] ^ ab_true[index] ? ab[index - 1] : local_int);
        Sum += (ab_true[index] ^ ab_true[index] ? ab[index - 1] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_419()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ ab_true[index] ? ab[index - 1] : t1_i.mfi);
        Sum += (ab_true[index] ^ ab_true[index] ? ab[index - 1] : simple_func_int());
        Sum += (ab_true[index] ^ ab_true[index] ? ab[index - 1] : ab[index]);
        Sum += (ab_true[index] ^ ab_true[index] ? ab[index - 1] : ab[index - 1]);
        Sum += (ab_true[index] ^ ab_false[index] ? 3 : 3);
        Sum += (ab_true[index] ^ ab_false[index] ? 3 : -5);
        Sum += (ab_true[index] ^ ab_false[index] ? 3 : local_int);
        Sum += (ab_true[index] ^ ab_false[index] ? 3 : static_field_int);
        Sum += (ab_true[index] ^ ab_false[index] ? 3 : t1_i.mfi);
        Sum += (ab_true[index] ^ ab_false[index] ? 3 : simple_func_int());
        Sum += (ab_true[index] ^ ab_false[index] ? 3 : ab[index]);
        Sum += (ab_true[index] ^ ab_false[index] ? 3 : ab[index - 1]);
        Sum += (ab_true[index] ^ ab_false[index] ? -5 : 3);
        Sum += (ab_true[index] ^ ab_false[index] ? -5 : -5);
        Sum += (ab_true[index] ^ ab_false[index] ? -5 : local_int);
        Sum += (ab_true[index] ^ ab_false[index] ? -5 : static_field_int);
        Sum += (ab_true[index] ^ ab_false[index] ? -5 : t1_i.mfi);
        Sum += (ab_true[index] ^ ab_false[index] ? -5 : simple_func_int());
        Sum += (ab_true[index] ^ ab_false[index] ? -5 : ab[index]);
        Sum += (ab_true[index] ^ ab_false[index] ? -5 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_420()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ ab_false[index] ? local_int : 3);
        Sum += (ab_true[index] ^ ab_false[index] ? local_int : -5);
        Sum += (ab_true[index] ^ ab_false[index] ? local_int : local_int);
        Sum += (ab_true[index] ^ ab_false[index] ? local_int : static_field_int);
        Sum += (ab_true[index] ^ ab_false[index] ? local_int : t1_i.mfi);
        Sum += (ab_true[index] ^ ab_false[index] ? local_int : simple_func_int());
        Sum += (ab_true[index] ^ ab_false[index] ? local_int : ab[index]);
        Sum += (ab_true[index] ^ ab_false[index] ? local_int : ab[index - 1]);
        Sum += (ab_true[index] ^ ab_false[index] ? static_field_int : 3);
        Sum += (ab_true[index] ^ ab_false[index] ? static_field_int : -5);
        Sum += (ab_true[index] ^ ab_false[index] ? static_field_int : local_int);
        Sum += (ab_true[index] ^ ab_false[index] ? static_field_int : static_field_int);
        Sum += (ab_true[index] ^ ab_false[index] ? static_field_int : t1_i.mfi);
        Sum += (ab_true[index] ^ ab_false[index] ? static_field_int : simple_func_int());
        Sum += (ab_true[index] ^ ab_false[index] ? static_field_int : ab[index]);
        Sum += (ab_true[index] ^ ab_false[index] ? static_field_int : ab[index - 1]);
        Sum += (ab_true[index] ^ ab_false[index] ? t1_i.mfi : 3);
        Sum += (ab_true[index] ^ ab_false[index] ? t1_i.mfi : -5);
        Sum += (ab_true[index] ^ ab_false[index] ? t1_i.mfi : local_int);
        Sum += (ab_true[index] ^ ab_false[index] ? t1_i.mfi : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_421()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ ab_false[index] ? t1_i.mfi : t1_i.mfi);
        Sum += (ab_true[index] ^ ab_false[index] ? t1_i.mfi : simple_func_int());
        Sum += (ab_true[index] ^ ab_false[index] ? t1_i.mfi : ab[index]);
        Sum += (ab_true[index] ^ ab_false[index] ? t1_i.mfi : ab[index - 1]);
        Sum += (ab_true[index] ^ ab_false[index] ? simple_func_int() : 3);
        Sum += (ab_true[index] ^ ab_false[index] ? simple_func_int() : -5);
        Sum += (ab_true[index] ^ ab_false[index] ? simple_func_int() : local_int);
        Sum += (ab_true[index] ^ ab_false[index] ? simple_func_int() : static_field_int);
        Sum += (ab_true[index] ^ ab_false[index] ? simple_func_int() : t1_i.mfi);
        Sum += (ab_true[index] ^ ab_false[index] ? simple_func_int() : simple_func_int());
        Sum += (ab_true[index] ^ ab_false[index] ? simple_func_int() : ab[index]);
        Sum += (ab_true[index] ^ ab_false[index] ? simple_func_int() : ab[index - 1]);
        Sum += (ab_true[index] ^ ab_false[index] ? ab[index] : 3);
        Sum += (ab_true[index] ^ ab_false[index] ? ab[index] : -5);
        Sum += (ab_true[index] ^ ab_false[index] ? ab[index] : local_int);
        Sum += (ab_true[index] ^ ab_false[index] ? ab[index] : static_field_int);
        Sum += (ab_true[index] ^ ab_false[index] ? ab[index] : t1_i.mfi);
        Sum += (ab_true[index] ^ ab_false[index] ? ab[index] : simple_func_int());
        Sum += (ab_true[index] ^ ab_false[index] ? ab[index] : ab[index]);
        Sum += (ab_true[index] ^ ab_false[index] ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_422()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ^ ab_false[index] ? ab[index - 1] : 3);
        Sum += (ab_true[index] ^ ab_false[index] ? ab[index - 1] : -5);
        Sum += (ab_true[index] ^ ab_false[index] ? ab[index - 1] : local_int);
        Sum += (ab_true[index] ^ ab_false[index] ? ab[index - 1] : static_field_int);
        Sum += (ab_true[index] ^ ab_false[index] ? ab[index - 1] : t1_i.mfi);
        Sum += (ab_true[index] ^ ab_false[index] ? ab[index - 1] : simple_func_int());
        Sum += (ab_true[index] ^ ab_false[index] ? ab[index - 1] : ab[index]);
        Sum += (ab_true[index] ^ ab_false[index] ? ab[index - 1] : ab[index - 1]);
        Sum += (ab_false[index] ^ true ? 3 : 3);
        Sum += (ab_false[index] ^ true ? 3 : -5);
        Sum += (ab_false[index] ^ true ? 3 : local_int);
        Sum += (ab_false[index] ^ true ? 3 : static_field_int);
        Sum += (ab_false[index] ^ true ? 3 : t1_i.mfi);
        Sum += (ab_false[index] ^ true ? 3 : simple_func_int());
        Sum += (ab_false[index] ^ true ? 3 : ab[index]);
        Sum += (ab_false[index] ^ true ? 3 : ab[index - 1]);
        Sum += (ab_false[index] ^ true ? -5 : 3);
        Sum += (ab_false[index] ^ true ? -5 : -5);
        Sum += (ab_false[index] ^ true ? -5 : local_int);
        Sum += (ab_false[index] ^ true ? -5 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_423()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ true ? -5 : t1_i.mfi);
        Sum += (ab_false[index] ^ true ? -5 : simple_func_int());
        Sum += (ab_false[index] ^ true ? -5 : ab[index]);
        Sum += (ab_false[index] ^ true ? -5 : ab[index - 1]);
        Sum += (ab_false[index] ^ true ? local_int : 3);
        Sum += (ab_false[index] ^ true ? local_int : -5);
        Sum += (ab_false[index] ^ true ? local_int : local_int);
        Sum += (ab_false[index] ^ true ? local_int : static_field_int);
        Sum += (ab_false[index] ^ true ? local_int : t1_i.mfi);
        Sum += (ab_false[index] ^ true ? local_int : simple_func_int());
        Sum += (ab_false[index] ^ true ? local_int : ab[index]);
        Sum += (ab_false[index] ^ true ? local_int : ab[index - 1]);
        Sum += (ab_false[index] ^ true ? static_field_int : 3);
        Sum += (ab_false[index] ^ true ? static_field_int : -5);
        Sum += (ab_false[index] ^ true ? static_field_int : local_int);
        Sum += (ab_false[index] ^ true ? static_field_int : static_field_int);
        Sum += (ab_false[index] ^ true ? static_field_int : t1_i.mfi);
        Sum += (ab_false[index] ^ true ? static_field_int : simple_func_int());
        Sum += (ab_false[index] ^ true ? static_field_int : ab[index]);
        Sum += (ab_false[index] ^ true ? static_field_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_424()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ true ? t1_i.mfi : 3);
        Sum += (ab_false[index] ^ true ? t1_i.mfi : -5);
        Sum += (ab_false[index] ^ true ? t1_i.mfi : local_int);
        Sum += (ab_false[index] ^ true ? t1_i.mfi : static_field_int);
        Sum += (ab_false[index] ^ true ? t1_i.mfi : t1_i.mfi);
        Sum += (ab_false[index] ^ true ? t1_i.mfi : simple_func_int());
        Sum += (ab_false[index] ^ true ? t1_i.mfi : ab[index]);
        Sum += (ab_false[index] ^ true ? t1_i.mfi : ab[index - 1]);
        Sum += (ab_false[index] ^ true ? simple_func_int() : 3);
        Sum += (ab_false[index] ^ true ? simple_func_int() : -5);
        Sum += (ab_false[index] ^ true ? simple_func_int() : local_int);
        Sum += (ab_false[index] ^ true ? simple_func_int() : static_field_int);
        Sum += (ab_false[index] ^ true ? simple_func_int() : t1_i.mfi);
        Sum += (ab_false[index] ^ true ? simple_func_int() : simple_func_int());
        Sum += (ab_false[index] ^ true ? simple_func_int() : ab[index]);
        Sum += (ab_false[index] ^ true ? simple_func_int() : ab[index - 1]);
        Sum += (ab_false[index] ^ true ? ab[index] : 3);
        Sum += (ab_false[index] ^ true ? ab[index] : -5);
        Sum += (ab_false[index] ^ true ? ab[index] : local_int);
        Sum += (ab_false[index] ^ true ? ab[index] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_425()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ true ? ab[index] : t1_i.mfi);
        Sum += (ab_false[index] ^ true ? ab[index] : simple_func_int());
        Sum += (ab_false[index] ^ true ? ab[index] : ab[index]);
        Sum += (ab_false[index] ^ true ? ab[index] : ab[index - 1]);
        Sum += (ab_false[index] ^ true ? ab[index - 1] : 3);
        Sum += (ab_false[index] ^ true ? ab[index - 1] : -5);
        Sum += (ab_false[index] ^ true ? ab[index - 1] : local_int);
        Sum += (ab_false[index] ^ true ? ab[index - 1] : static_field_int);
        Sum += (ab_false[index] ^ true ? ab[index - 1] : t1_i.mfi);
        Sum += (ab_false[index] ^ true ? ab[index - 1] : simple_func_int());
        Sum += (ab_false[index] ^ true ? ab[index - 1] : ab[index]);
        Sum += (ab_false[index] ^ true ? ab[index - 1] : ab[index - 1]);
        Sum += (ab_false[index] ^ false ? 3 : 3);
        Sum += (ab_false[index] ^ false ? 3 : -5);
        Sum += (ab_false[index] ^ false ? 3 : local_int);
        Sum += (ab_false[index] ^ false ? 3 : static_field_int);
        Sum += (ab_false[index] ^ false ? 3 : t1_i.mfi);
        Sum += (ab_false[index] ^ false ? 3 : simple_func_int());
        Sum += (ab_false[index] ^ false ? 3 : ab[index]);
        Sum += (ab_false[index] ^ false ? 3 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_426()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ false ? -5 : 3);
        Sum += (ab_false[index] ^ false ? -5 : -5);
        Sum += (ab_false[index] ^ false ? -5 : local_int);
        Sum += (ab_false[index] ^ false ? -5 : static_field_int);
        Sum += (ab_false[index] ^ false ? -5 : t1_i.mfi);
        Sum += (ab_false[index] ^ false ? -5 : simple_func_int());
        Sum += (ab_false[index] ^ false ? -5 : ab[index]);
        Sum += (ab_false[index] ^ false ? -5 : ab[index - 1]);
        Sum += (ab_false[index] ^ false ? local_int : 3);
        Sum += (ab_false[index] ^ false ? local_int : -5);
        Sum += (ab_false[index] ^ false ? local_int : local_int);
        Sum += (ab_false[index] ^ false ? local_int : static_field_int);
        Sum += (ab_false[index] ^ false ? local_int : t1_i.mfi);
        Sum += (ab_false[index] ^ false ? local_int : simple_func_int());
        Sum += (ab_false[index] ^ false ? local_int : ab[index]);
        Sum += (ab_false[index] ^ false ? local_int : ab[index - 1]);
        Sum += (ab_false[index] ^ false ? static_field_int : 3);
        Sum += (ab_false[index] ^ false ? static_field_int : -5);
        Sum += (ab_false[index] ^ false ? static_field_int : local_int);
        Sum += (ab_false[index] ^ false ? static_field_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_427()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ false ? static_field_int : t1_i.mfi);
        Sum += (ab_false[index] ^ false ? static_field_int : simple_func_int());
        Sum += (ab_false[index] ^ false ? static_field_int : ab[index]);
        Sum += (ab_false[index] ^ false ? static_field_int : ab[index - 1]);
        Sum += (ab_false[index] ^ false ? t1_i.mfi : 3);
        Sum += (ab_false[index] ^ false ? t1_i.mfi : -5);
        Sum += (ab_false[index] ^ false ? t1_i.mfi : local_int);
        Sum += (ab_false[index] ^ false ? t1_i.mfi : static_field_int);
        Sum += (ab_false[index] ^ false ? t1_i.mfi : t1_i.mfi);
        Sum += (ab_false[index] ^ false ? t1_i.mfi : simple_func_int());
        Sum += (ab_false[index] ^ false ? t1_i.mfi : ab[index]);
        Sum += (ab_false[index] ^ false ? t1_i.mfi : ab[index - 1]);
        Sum += (ab_false[index] ^ false ? simple_func_int() : 3);
        Sum += (ab_false[index] ^ false ? simple_func_int() : -5);
        Sum += (ab_false[index] ^ false ? simple_func_int() : local_int);
        Sum += (ab_false[index] ^ false ? simple_func_int() : static_field_int);
        Sum += (ab_false[index] ^ false ? simple_func_int() : t1_i.mfi);
        Sum += (ab_false[index] ^ false ? simple_func_int() : simple_func_int());
        Sum += (ab_false[index] ^ false ? simple_func_int() : ab[index]);
        Sum += (ab_false[index] ^ false ? simple_func_int() : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_428()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ false ? ab[index] : 3);
        Sum += (ab_false[index] ^ false ? ab[index] : -5);
        Sum += (ab_false[index] ^ false ? ab[index] : local_int);
        Sum += (ab_false[index] ^ false ? ab[index] : static_field_int);
        Sum += (ab_false[index] ^ false ? ab[index] : t1_i.mfi);
        Sum += (ab_false[index] ^ false ? ab[index] : simple_func_int());
        Sum += (ab_false[index] ^ false ? ab[index] : ab[index]);
        Sum += (ab_false[index] ^ false ? ab[index] : ab[index - 1]);
        Sum += (ab_false[index] ^ false ? ab[index - 1] : 3);
        Sum += (ab_false[index] ^ false ? ab[index - 1] : -5);
        Sum += (ab_false[index] ^ false ? ab[index - 1] : local_int);
        Sum += (ab_false[index] ^ false ? ab[index - 1] : static_field_int);
        Sum += (ab_false[index] ^ false ? ab[index - 1] : t1_i.mfi);
        Sum += (ab_false[index] ^ false ? ab[index - 1] : simple_func_int());
        Sum += (ab_false[index] ^ false ? ab[index - 1] : ab[index]);
        Sum += (ab_false[index] ^ false ? ab[index - 1] : ab[index - 1]);
        Sum += (ab_false[index] ^ lb_true ? 3 : 3);
        Sum += (ab_false[index] ^ lb_true ? 3 : -5);
        Sum += (ab_false[index] ^ lb_true ? 3 : local_int);
        Sum += (ab_false[index] ^ lb_true ? 3 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_429()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ lb_true ? 3 : t1_i.mfi);
        Sum += (ab_false[index] ^ lb_true ? 3 : simple_func_int());
        Sum += (ab_false[index] ^ lb_true ? 3 : ab[index]);
        Sum += (ab_false[index] ^ lb_true ? 3 : ab[index - 1]);
        Sum += (ab_false[index] ^ lb_true ? -5 : 3);
        Sum += (ab_false[index] ^ lb_true ? -5 : -5);
        Sum += (ab_false[index] ^ lb_true ? -5 : local_int);
        Sum += (ab_false[index] ^ lb_true ? -5 : static_field_int);
        Sum += (ab_false[index] ^ lb_true ? -5 : t1_i.mfi);
        Sum += (ab_false[index] ^ lb_true ? -5 : simple_func_int());
        Sum += (ab_false[index] ^ lb_true ? -5 : ab[index]);
        Sum += (ab_false[index] ^ lb_true ? -5 : ab[index - 1]);
        Sum += (ab_false[index] ^ lb_true ? local_int : 3);
        Sum += (ab_false[index] ^ lb_true ? local_int : -5);
        Sum += (ab_false[index] ^ lb_true ? local_int : local_int);
        Sum += (ab_false[index] ^ lb_true ? local_int : static_field_int);
        Sum += (ab_false[index] ^ lb_true ? local_int : t1_i.mfi);
        Sum += (ab_false[index] ^ lb_true ? local_int : simple_func_int());
        Sum += (ab_false[index] ^ lb_true ? local_int : ab[index]);
        Sum += (ab_false[index] ^ lb_true ? local_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_430()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ lb_true ? static_field_int : 3);
        Sum += (ab_false[index] ^ lb_true ? static_field_int : -5);
        Sum += (ab_false[index] ^ lb_true ? static_field_int : local_int);
        Sum += (ab_false[index] ^ lb_true ? static_field_int : static_field_int);
        Sum += (ab_false[index] ^ lb_true ? static_field_int : t1_i.mfi);
        Sum += (ab_false[index] ^ lb_true ? static_field_int : simple_func_int());
        Sum += (ab_false[index] ^ lb_true ? static_field_int : ab[index]);
        Sum += (ab_false[index] ^ lb_true ? static_field_int : ab[index - 1]);
        Sum += (ab_false[index] ^ lb_true ? t1_i.mfi : 3);
        Sum += (ab_false[index] ^ lb_true ? t1_i.mfi : -5);
        Sum += (ab_false[index] ^ lb_true ? t1_i.mfi : local_int);
        Sum += (ab_false[index] ^ lb_true ? t1_i.mfi : static_field_int);
        Sum += (ab_false[index] ^ lb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (ab_false[index] ^ lb_true ? t1_i.mfi : simple_func_int());
        Sum += (ab_false[index] ^ lb_true ? t1_i.mfi : ab[index]);
        Sum += (ab_false[index] ^ lb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (ab_false[index] ^ lb_true ? simple_func_int() : 3);
        Sum += (ab_false[index] ^ lb_true ? simple_func_int() : -5);
        Sum += (ab_false[index] ^ lb_true ? simple_func_int() : local_int);
        Sum += (ab_false[index] ^ lb_true ? simple_func_int() : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_431()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ lb_true ? simple_func_int() : t1_i.mfi);
        Sum += (ab_false[index] ^ lb_true ? simple_func_int() : simple_func_int());
        Sum += (ab_false[index] ^ lb_true ? simple_func_int() : ab[index]);
        Sum += (ab_false[index] ^ lb_true ? simple_func_int() : ab[index - 1]);
        Sum += (ab_false[index] ^ lb_true ? ab[index] : 3);
        Sum += (ab_false[index] ^ lb_true ? ab[index] : -5);
        Sum += (ab_false[index] ^ lb_true ? ab[index] : local_int);
        Sum += (ab_false[index] ^ lb_true ? ab[index] : static_field_int);
        Sum += (ab_false[index] ^ lb_true ? ab[index] : t1_i.mfi);
        Sum += (ab_false[index] ^ lb_true ? ab[index] : simple_func_int());
        Sum += (ab_false[index] ^ lb_true ? ab[index] : ab[index]);
        Sum += (ab_false[index] ^ lb_true ? ab[index] : ab[index - 1]);
        Sum += (ab_false[index] ^ lb_true ? ab[index - 1] : 3);
        Sum += (ab_false[index] ^ lb_true ? ab[index - 1] : -5);
        Sum += (ab_false[index] ^ lb_true ? ab[index - 1] : local_int);
        Sum += (ab_false[index] ^ lb_true ? ab[index - 1] : static_field_int);
        Sum += (ab_false[index] ^ lb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (ab_false[index] ^ lb_true ? ab[index - 1] : simple_func_int());
        Sum += (ab_false[index] ^ lb_true ? ab[index - 1] : ab[index]);
        Sum += (ab_false[index] ^ lb_true ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_432()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ lb_false ? 3 : 3);
        Sum += (ab_false[index] ^ lb_false ? 3 : -5);
        Sum += (ab_false[index] ^ lb_false ? 3 : local_int);
        Sum += (ab_false[index] ^ lb_false ? 3 : static_field_int);
        Sum += (ab_false[index] ^ lb_false ? 3 : t1_i.mfi);
        Sum += (ab_false[index] ^ lb_false ? 3 : simple_func_int());
        Sum += (ab_false[index] ^ lb_false ? 3 : ab[index]);
        Sum += (ab_false[index] ^ lb_false ? 3 : ab[index - 1]);
        Sum += (ab_false[index] ^ lb_false ? -5 : 3);
        Sum += (ab_false[index] ^ lb_false ? -5 : -5);
        Sum += (ab_false[index] ^ lb_false ? -5 : local_int);
        Sum += (ab_false[index] ^ lb_false ? -5 : static_field_int);
        Sum += (ab_false[index] ^ lb_false ? -5 : t1_i.mfi);
        Sum += (ab_false[index] ^ lb_false ? -5 : simple_func_int());
        Sum += (ab_false[index] ^ lb_false ? -5 : ab[index]);
        Sum += (ab_false[index] ^ lb_false ? -5 : ab[index - 1]);
        Sum += (ab_false[index] ^ lb_false ? local_int : 3);
        Sum += (ab_false[index] ^ lb_false ? local_int : -5);
        Sum += (ab_false[index] ^ lb_false ? local_int : local_int);
        Sum += (ab_false[index] ^ lb_false ? local_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_433()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ lb_false ? local_int : t1_i.mfi);
        Sum += (ab_false[index] ^ lb_false ? local_int : simple_func_int());
        Sum += (ab_false[index] ^ lb_false ? local_int : ab[index]);
        Sum += (ab_false[index] ^ lb_false ? local_int : ab[index - 1]);
        Sum += (ab_false[index] ^ lb_false ? static_field_int : 3);
        Sum += (ab_false[index] ^ lb_false ? static_field_int : -5);
        Sum += (ab_false[index] ^ lb_false ? static_field_int : local_int);
        Sum += (ab_false[index] ^ lb_false ? static_field_int : static_field_int);
        Sum += (ab_false[index] ^ lb_false ? static_field_int : t1_i.mfi);
        Sum += (ab_false[index] ^ lb_false ? static_field_int : simple_func_int());
        Sum += (ab_false[index] ^ lb_false ? static_field_int : ab[index]);
        Sum += (ab_false[index] ^ lb_false ? static_field_int : ab[index - 1]);
        Sum += (ab_false[index] ^ lb_false ? t1_i.mfi : 3);
        Sum += (ab_false[index] ^ lb_false ? t1_i.mfi : -5);
        Sum += (ab_false[index] ^ lb_false ? t1_i.mfi : local_int);
        Sum += (ab_false[index] ^ lb_false ? t1_i.mfi : static_field_int);
        Sum += (ab_false[index] ^ lb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (ab_false[index] ^ lb_false ? t1_i.mfi : simple_func_int());
        Sum += (ab_false[index] ^ lb_false ? t1_i.mfi : ab[index]);
        Sum += (ab_false[index] ^ lb_false ? t1_i.mfi : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_434()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ lb_false ? simple_func_int() : 3);
        Sum += (ab_false[index] ^ lb_false ? simple_func_int() : -5);
        Sum += (ab_false[index] ^ lb_false ? simple_func_int() : local_int);
        Sum += (ab_false[index] ^ lb_false ? simple_func_int() : static_field_int);
        Sum += (ab_false[index] ^ lb_false ? simple_func_int() : t1_i.mfi);
        Sum += (ab_false[index] ^ lb_false ? simple_func_int() : simple_func_int());
        Sum += (ab_false[index] ^ lb_false ? simple_func_int() : ab[index]);
        Sum += (ab_false[index] ^ lb_false ? simple_func_int() : ab[index - 1]);
        Sum += (ab_false[index] ^ lb_false ? ab[index] : 3);
        Sum += (ab_false[index] ^ lb_false ? ab[index] : -5);
        Sum += (ab_false[index] ^ lb_false ? ab[index] : local_int);
        Sum += (ab_false[index] ^ lb_false ? ab[index] : static_field_int);
        Sum += (ab_false[index] ^ lb_false ? ab[index] : t1_i.mfi);
        Sum += (ab_false[index] ^ lb_false ? ab[index] : simple_func_int());
        Sum += (ab_false[index] ^ lb_false ? ab[index] : ab[index]);
        Sum += (ab_false[index] ^ lb_false ? ab[index] : ab[index - 1]);
        Sum += (ab_false[index] ^ lb_false ? ab[index - 1] : 3);
        Sum += (ab_false[index] ^ lb_false ? ab[index - 1] : -5);
        Sum += (ab_false[index] ^ lb_false ? ab[index - 1] : local_int);
        Sum += (ab_false[index] ^ lb_false ? ab[index - 1] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_435()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ lb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (ab_false[index] ^ lb_false ? ab[index - 1] : simple_func_int());
        Sum += (ab_false[index] ^ lb_false ? ab[index - 1] : ab[index]);
        Sum += (ab_false[index] ^ lb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (ab_false[index] ^ sfb_true ? 3 : 3);
        Sum += (ab_false[index] ^ sfb_true ? 3 : -5);
        Sum += (ab_false[index] ^ sfb_true ? 3 : local_int);
        Sum += (ab_false[index] ^ sfb_true ? 3 : static_field_int);
        Sum += (ab_false[index] ^ sfb_true ? 3 : t1_i.mfi);
        Sum += (ab_false[index] ^ sfb_true ? 3 : simple_func_int());
        Sum += (ab_false[index] ^ sfb_true ? 3 : ab[index]);
        Sum += (ab_false[index] ^ sfb_true ? 3 : ab[index - 1]);
        Sum += (ab_false[index] ^ sfb_true ? -5 : 3);
        Sum += (ab_false[index] ^ sfb_true ? -5 : -5);
        Sum += (ab_false[index] ^ sfb_true ? -5 : local_int);
        Sum += (ab_false[index] ^ sfb_true ? -5 : static_field_int);
        Sum += (ab_false[index] ^ sfb_true ? -5 : t1_i.mfi);
        Sum += (ab_false[index] ^ sfb_true ? -5 : simple_func_int());
        Sum += (ab_false[index] ^ sfb_true ? -5 : ab[index]);
        Sum += (ab_false[index] ^ sfb_true ? -5 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_436()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ sfb_true ? local_int : 3);
        Sum += (ab_false[index] ^ sfb_true ? local_int : -5);
        Sum += (ab_false[index] ^ sfb_true ? local_int : local_int);
        Sum += (ab_false[index] ^ sfb_true ? local_int : static_field_int);
        Sum += (ab_false[index] ^ sfb_true ? local_int : t1_i.mfi);
        Sum += (ab_false[index] ^ sfb_true ? local_int : simple_func_int());
        Sum += (ab_false[index] ^ sfb_true ? local_int : ab[index]);
        Sum += (ab_false[index] ^ sfb_true ? local_int : ab[index - 1]);
        Sum += (ab_false[index] ^ sfb_true ? static_field_int : 3);
        Sum += (ab_false[index] ^ sfb_true ? static_field_int : -5);
        Sum += (ab_false[index] ^ sfb_true ? static_field_int : local_int);
        Sum += (ab_false[index] ^ sfb_true ? static_field_int : static_field_int);
        Sum += (ab_false[index] ^ sfb_true ? static_field_int : t1_i.mfi);
        Sum += (ab_false[index] ^ sfb_true ? static_field_int : simple_func_int());
        Sum += (ab_false[index] ^ sfb_true ? static_field_int : ab[index]);
        Sum += (ab_false[index] ^ sfb_true ? static_field_int : ab[index - 1]);
        Sum += (ab_false[index] ^ sfb_true ? t1_i.mfi : 3);
        Sum += (ab_false[index] ^ sfb_true ? t1_i.mfi : -5);
        Sum += (ab_false[index] ^ sfb_true ? t1_i.mfi : local_int);
        Sum += (ab_false[index] ^ sfb_true ? t1_i.mfi : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_437()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ sfb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (ab_false[index] ^ sfb_true ? t1_i.mfi : simple_func_int());
        Sum += (ab_false[index] ^ sfb_true ? t1_i.mfi : ab[index]);
        Sum += (ab_false[index] ^ sfb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (ab_false[index] ^ sfb_true ? simple_func_int() : 3);
        Sum += (ab_false[index] ^ sfb_true ? simple_func_int() : -5);
        Sum += (ab_false[index] ^ sfb_true ? simple_func_int() : local_int);
        Sum += (ab_false[index] ^ sfb_true ? simple_func_int() : static_field_int);
        Sum += (ab_false[index] ^ sfb_true ? simple_func_int() : t1_i.mfi);
        Sum += (ab_false[index] ^ sfb_true ? simple_func_int() : simple_func_int());
        Sum += (ab_false[index] ^ sfb_true ? simple_func_int() : ab[index]);
        Sum += (ab_false[index] ^ sfb_true ? simple_func_int() : ab[index - 1]);
        Sum += (ab_false[index] ^ sfb_true ? ab[index] : 3);
        Sum += (ab_false[index] ^ sfb_true ? ab[index] : -5);
        Sum += (ab_false[index] ^ sfb_true ? ab[index] : local_int);
        Sum += (ab_false[index] ^ sfb_true ? ab[index] : static_field_int);
        Sum += (ab_false[index] ^ sfb_true ? ab[index] : t1_i.mfi);
        Sum += (ab_false[index] ^ sfb_true ? ab[index] : simple_func_int());
        Sum += (ab_false[index] ^ sfb_true ? ab[index] : ab[index]);
        Sum += (ab_false[index] ^ sfb_true ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_438()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ sfb_true ? ab[index - 1] : 3);
        Sum += (ab_false[index] ^ sfb_true ? ab[index - 1] : -5);
        Sum += (ab_false[index] ^ sfb_true ? ab[index - 1] : local_int);
        Sum += (ab_false[index] ^ sfb_true ? ab[index - 1] : static_field_int);
        Sum += (ab_false[index] ^ sfb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (ab_false[index] ^ sfb_true ? ab[index - 1] : simple_func_int());
        Sum += (ab_false[index] ^ sfb_true ? ab[index - 1] : ab[index]);
        Sum += (ab_false[index] ^ sfb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (ab_false[index] ^ sfb_false ? 3 : 3);
        Sum += (ab_false[index] ^ sfb_false ? 3 : -5);
        Sum += (ab_false[index] ^ sfb_false ? 3 : local_int);
        Sum += (ab_false[index] ^ sfb_false ? 3 : static_field_int);
        Sum += (ab_false[index] ^ sfb_false ? 3 : t1_i.mfi);
        Sum += (ab_false[index] ^ sfb_false ? 3 : simple_func_int());
        Sum += (ab_false[index] ^ sfb_false ? 3 : ab[index]);
        Sum += (ab_false[index] ^ sfb_false ? 3 : ab[index - 1]);
        Sum += (ab_false[index] ^ sfb_false ? -5 : 3);
        Sum += (ab_false[index] ^ sfb_false ? -5 : -5);
        Sum += (ab_false[index] ^ sfb_false ? -5 : local_int);
        Sum += (ab_false[index] ^ sfb_false ? -5 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_439()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ sfb_false ? -5 : t1_i.mfi);
        Sum += (ab_false[index] ^ sfb_false ? -5 : simple_func_int());
        Sum += (ab_false[index] ^ sfb_false ? -5 : ab[index]);
        Sum += (ab_false[index] ^ sfb_false ? -5 : ab[index - 1]);
        Sum += (ab_false[index] ^ sfb_false ? local_int : 3);
        Sum += (ab_false[index] ^ sfb_false ? local_int : -5);
        Sum += (ab_false[index] ^ sfb_false ? local_int : local_int);
        Sum += (ab_false[index] ^ sfb_false ? local_int : static_field_int);
        Sum += (ab_false[index] ^ sfb_false ? local_int : t1_i.mfi);
        Sum += (ab_false[index] ^ sfb_false ? local_int : simple_func_int());
        Sum += (ab_false[index] ^ sfb_false ? local_int : ab[index]);
        Sum += (ab_false[index] ^ sfb_false ? local_int : ab[index - 1]);
        Sum += (ab_false[index] ^ sfb_false ? static_field_int : 3);
        Sum += (ab_false[index] ^ sfb_false ? static_field_int : -5);
        Sum += (ab_false[index] ^ sfb_false ? static_field_int : local_int);
        Sum += (ab_false[index] ^ sfb_false ? static_field_int : static_field_int);
        Sum += (ab_false[index] ^ sfb_false ? static_field_int : t1_i.mfi);
        Sum += (ab_false[index] ^ sfb_false ? static_field_int : simple_func_int());
        Sum += (ab_false[index] ^ sfb_false ? static_field_int : ab[index]);
        Sum += (ab_false[index] ^ sfb_false ? static_field_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_440()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ sfb_false ? t1_i.mfi : 3);
        Sum += (ab_false[index] ^ sfb_false ? t1_i.mfi : -5);
        Sum += (ab_false[index] ^ sfb_false ? t1_i.mfi : local_int);
        Sum += (ab_false[index] ^ sfb_false ? t1_i.mfi : static_field_int);
        Sum += (ab_false[index] ^ sfb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (ab_false[index] ^ sfb_false ? t1_i.mfi : simple_func_int());
        Sum += (ab_false[index] ^ sfb_false ? t1_i.mfi : ab[index]);
        Sum += (ab_false[index] ^ sfb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (ab_false[index] ^ sfb_false ? simple_func_int() : 3);
        Sum += (ab_false[index] ^ sfb_false ? simple_func_int() : -5);
        Sum += (ab_false[index] ^ sfb_false ? simple_func_int() : local_int);
        Sum += (ab_false[index] ^ sfb_false ? simple_func_int() : static_field_int);
        Sum += (ab_false[index] ^ sfb_false ? simple_func_int() : t1_i.mfi);
        Sum += (ab_false[index] ^ sfb_false ? simple_func_int() : simple_func_int());
        Sum += (ab_false[index] ^ sfb_false ? simple_func_int() : ab[index]);
        Sum += (ab_false[index] ^ sfb_false ? simple_func_int() : ab[index - 1]);
        Sum += (ab_false[index] ^ sfb_false ? ab[index] : 3);
        Sum += (ab_false[index] ^ sfb_false ? ab[index] : -5);
        Sum += (ab_false[index] ^ sfb_false ? ab[index] : local_int);
        Sum += (ab_false[index] ^ sfb_false ? ab[index] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_441()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ sfb_false ? ab[index] : t1_i.mfi);
        Sum += (ab_false[index] ^ sfb_false ? ab[index] : simple_func_int());
        Sum += (ab_false[index] ^ sfb_false ? ab[index] : ab[index]);
        Sum += (ab_false[index] ^ sfb_false ? ab[index] : ab[index - 1]);
        Sum += (ab_false[index] ^ sfb_false ? ab[index - 1] : 3);
        Sum += (ab_false[index] ^ sfb_false ? ab[index - 1] : -5);
        Sum += (ab_false[index] ^ sfb_false ? ab[index - 1] : local_int);
        Sum += (ab_false[index] ^ sfb_false ? ab[index - 1] : static_field_int);
        Sum += (ab_false[index] ^ sfb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (ab_false[index] ^ sfb_false ? ab[index - 1] : simple_func_int());
        Sum += (ab_false[index] ^ sfb_false ? ab[index - 1] : ab[index]);
        Sum += (ab_false[index] ^ sfb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? 3 : 3);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? 3 : -5);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? 3 : local_int);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? 3 : static_field_int);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? 3 : t1_i.mfi);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? 3 : simple_func_int());
        Sum += (ab_false[index] ^ t1_i.mfb_true ? 3 : ab[index]);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? 3 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_442()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ t1_i.mfb_true ? -5 : 3);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? -5 : -5);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? -5 : local_int);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? -5 : static_field_int);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? -5 : t1_i.mfi);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? -5 : simple_func_int());
        Sum += (ab_false[index] ^ t1_i.mfb_true ? -5 : ab[index]);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? -5 : ab[index - 1]);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? local_int : 3);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? local_int : -5);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? local_int : local_int);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? local_int : static_field_int);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? local_int : t1_i.mfi);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? local_int : simple_func_int());
        Sum += (ab_false[index] ^ t1_i.mfb_true ? local_int : ab[index]);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? local_int : ab[index - 1]);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? static_field_int : 3);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? static_field_int : -5);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? static_field_int : local_int);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? static_field_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_443()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ t1_i.mfb_true ? static_field_int : t1_i.mfi);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? static_field_int : simple_func_int());
        Sum += (ab_false[index] ^ t1_i.mfb_true ? static_field_int : ab[index]);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? static_field_int : ab[index - 1]);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? t1_i.mfi : 3);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? t1_i.mfi : -5);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? t1_i.mfi : local_int);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? t1_i.mfi : static_field_int);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? t1_i.mfi : simple_func_int());
        Sum += (ab_false[index] ^ t1_i.mfb_true ? t1_i.mfi : ab[index]);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? simple_func_int() : 3);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? simple_func_int() : -5);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? simple_func_int() : local_int);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? simple_func_int() : static_field_int);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? simple_func_int() : t1_i.mfi);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? simple_func_int() : simple_func_int());
        Sum += (ab_false[index] ^ t1_i.mfb_true ? simple_func_int() : ab[index]);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? simple_func_int() : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_444()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ t1_i.mfb_true ? ab[index] : 3);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? ab[index] : -5);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? ab[index] : local_int);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? ab[index] : static_field_int);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? ab[index] : t1_i.mfi);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? ab[index] : simple_func_int());
        Sum += (ab_false[index] ^ t1_i.mfb_true ? ab[index] : ab[index]);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? ab[index] : ab[index - 1]);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? ab[index - 1] : 3);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? ab[index - 1] : -5);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? ab[index - 1] : local_int);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? ab[index - 1] : static_field_int);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? ab[index - 1] : simple_func_int());
        Sum += (ab_false[index] ^ t1_i.mfb_true ? ab[index - 1] : ab[index]);
        Sum += (ab_false[index] ^ t1_i.mfb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? 3 : 3);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? 3 : -5);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? 3 : local_int);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? 3 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_445()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ t1_i.mfb_false ? 3 : t1_i.mfi);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? 3 : simple_func_int());
        Sum += (ab_false[index] ^ t1_i.mfb_false ? 3 : ab[index]);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? 3 : ab[index - 1]);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? -5 : 3);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? -5 : -5);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? -5 : local_int);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? -5 : static_field_int);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? -5 : t1_i.mfi);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? -5 : simple_func_int());
        Sum += (ab_false[index] ^ t1_i.mfb_false ? -5 : ab[index]);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? -5 : ab[index - 1]);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? local_int : 3);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? local_int : -5);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? local_int : local_int);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? local_int : static_field_int);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? local_int : t1_i.mfi);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? local_int : simple_func_int());
        Sum += (ab_false[index] ^ t1_i.mfb_false ? local_int : ab[index]);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? local_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_446()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ t1_i.mfb_false ? static_field_int : 3);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? static_field_int : -5);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? static_field_int : local_int);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? static_field_int : static_field_int);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? static_field_int : t1_i.mfi);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? static_field_int : simple_func_int());
        Sum += (ab_false[index] ^ t1_i.mfb_false ? static_field_int : ab[index]);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? static_field_int : ab[index - 1]);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? t1_i.mfi : 3);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? t1_i.mfi : -5);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? t1_i.mfi : local_int);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? t1_i.mfi : static_field_int);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? t1_i.mfi : simple_func_int());
        Sum += (ab_false[index] ^ t1_i.mfb_false ? t1_i.mfi : ab[index]);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? simple_func_int() : 3);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? simple_func_int() : -5);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? simple_func_int() : local_int);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? simple_func_int() : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_447()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ t1_i.mfb_false ? simple_func_int() : t1_i.mfi);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? simple_func_int() : simple_func_int());
        Sum += (ab_false[index] ^ t1_i.mfb_false ? simple_func_int() : ab[index]);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? simple_func_int() : ab[index - 1]);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? ab[index] : 3);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? ab[index] : -5);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? ab[index] : local_int);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? ab[index] : static_field_int);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? ab[index] : t1_i.mfi);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? ab[index] : simple_func_int());
        Sum += (ab_false[index] ^ t1_i.mfb_false ? ab[index] : ab[index]);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? ab[index] : ab[index - 1]);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? ab[index - 1] : 3);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? ab[index - 1] : -5);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? ab[index - 1] : local_int);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? ab[index - 1] : static_field_int);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? ab[index - 1] : simple_func_int());
        Sum += (ab_false[index] ^ t1_i.mfb_false ? ab[index - 1] : ab[index]);
        Sum += (ab_false[index] ^ t1_i.mfb_false ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_448()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ func_sb_true() ? 3 : 3);
        Sum += (ab_false[index] ^ func_sb_true() ? 3 : -5);
        Sum += (ab_false[index] ^ func_sb_true() ? 3 : local_int);
        Sum += (ab_false[index] ^ func_sb_true() ? 3 : static_field_int);
        Sum += (ab_false[index] ^ func_sb_true() ? 3 : t1_i.mfi);
        Sum += (ab_false[index] ^ func_sb_true() ? 3 : simple_func_int());
        Sum += (ab_false[index] ^ func_sb_true() ? 3 : ab[index]);
        Sum += (ab_false[index] ^ func_sb_true() ? 3 : ab[index - 1]);
        Sum += (ab_false[index] ^ func_sb_true() ? -5 : 3);
        Sum += (ab_false[index] ^ func_sb_true() ? -5 : -5);
        Sum += (ab_false[index] ^ func_sb_true() ? -5 : local_int);
        Sum += (ab_false[index] ^ func_sb_true() ? -5 : static_field_int);
        Sum += (ab_false[index] ^ func_sb_true() ? -5 : t1_i.mfi);
        Sum += (ab_false[index] ^ func_sb_true() ? -5 : simple_func_int());
        Sum += (ab_false[index] ^ func_sb_true() ? -5 : ab[index]);
        Sum += (ab_false[index] ^ func_sb_true() ? -5 : ab[index - 1]);
        Sum += (ab_false[index] ^ func_sb_true() ? local_int : 3);
        Sum += (ab_false[index] ^ func_sb_true() ? local_int : -5);
        Sum += (ab_false[index] ^ func_sb_true() ? local_int : local_int);
        Sum += (ab_false[index] ^ func_sb_true() ? local_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_449()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ func_sb_true() ? local_int : t1_i.mfi);
        Sum += (ab_false[index] ^ func_sb_true() ? local_int : simple_func_int());
        Sum += (ab_false[index] ^ func_sb_true() ? local_int : ab[index]);
        Sum += (ab_false[index] ^ func_sb_true() ? local_int : ab[index - 1]);
        Sum += (ab_false[index] ^ func_sb_true() ? static_field_int : 3);
        Sum += (ab_false[index] ^ func_sb_true() ? static_field_int : -5);
        Sum += (ab_false[index] ^ func_sb_true() ? static_field_int : local_int);
        Sum += (ab_false[index] ^ func_sb_true() ? static_field_int : static_field_int);
        Sum += (ab_false[index] ^ func_sb_true() ? static_field_int : t1_i.mfi);
        Sum += (ab_false[index] ^ func_sb_true() ? static_field_int : simple_func_int());
        Sum += (ab_false[index] ^ func_sb_true() ? static_field_int : ab[index]);
        Sum += (ab_false[index] ^ func_sb_true() ? static_field_int : ab[index - 1]);
        Sum += (ab_false[index] ^ func_sb_true() ? t1_i.mfi : 3);
        Sum += (ab_false[index] ^ func_sb_true() ? t1_i.mfi : -5);
        Sum += (ab_false[index] ^ func_sb_true() ? t1_i.mfi : local_int);
        Sum += (ab_false[index] ^ func_sb_true() ? t1_i.mfi : static_field_int);
        Sum += (ab_false[index] ^ func_sb_true() ? t1_i.mfi : t1_i.mfi);
        Sum += (ab_false[index] ^ func_sb_true() ? t1_i.mfi : simple_func_int());
        Sum += (ab_false[index] ^ func_sb_true() ? t1_i.mfi : ab[index]);
        Sum += (ab_false[index] ^ func_sb_true() ? t1_i.mfi : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_450()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ func_sb_true() ? simple_func_int() : 3);
        Sum += (ab_false[index] ^ func_sb_true() ? simple_func_int() : -5);
        Sum += (ab_false[index] ^ func_sb_true() ? simple_func_int() : local_int);
        Sum += (ab_false[index] ^ func_sb_true() ? simple_func_int() : static_field_int);
        Sum += (ab_false[index] ^ func_sb_true() ? simple_func_int() : t1_i.mfi);
        Sum += (ab_false[index] ^ func_sb_true() ? simple_func_int() : simple_func_int());
        Sum += (ab_false[index] ^ func_sb_true() ? simple_func_int() : ab[index]);
        Sum += (ab_false[index] ^ func_sb_true() ? simple_func_int() : ab[index - 1]);
        Sum += (ab_false[index] ^ func_sb_true() ? ab[index] : 3);
        Sum += (ab_false[index] ^ func_sb_true() ? ab[index] : -5);
        Sum += (ab_false[index] ^ func_sb_true() ? ab[index] : local_int);
        Sum += (ab_false[index] ^ func_sb_true() ? ab[index] : static_field_int);
        Sum += (ab_false[index] ^ func_sb_true() ? ab[index] : t1_i.mfi);
        Sum += (ab_false[index] ^ func_sb_true() ? ab[index] : simple_func_int());
        Sum += (ab_false[index] ^ func_sb_true() ? ab[index] : ab[index]);
        Sum += (ab_false[index] ^ func_sb_true() ? ab[index] : ab[index - 1]);
        Sum += (ab_false[index] ^ func_sb_true() ? ab[index - 1] : 3);
        Sum += (ab_false[index] ^ func_sb_true() ? ab[index - 1] : -5);
        Sum += (ab_false[index] ^ func_sb_true() ? ab[index - 1] : local_int);
        Sum += (ab_false[index] ^ func_sb_true() ? ab[index - 1] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_451()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ func_sb_true() ? ab[index - 1] : t1_i.mfi);
        Sum += (ab_false[index] ^ func_sb_true() ? ab[index - 1] : simple_func_int());
        Sum += (ab_false[index] ^ func_sb_true() ? ab[index - 1] : ab[index]);
        Sum += (ab_false[index] ^ func_sb_true() ? ab[index - 1] : ab[index - 1]);
        Sum += (ab_false[index] ^ func_sb_false() ? 3 : 3);
        Sum += (ab_false[index] ^ func_sb_false() ? 3 : -5);
        Sum += (ab_false[index] ^ func_sb_false() ? 3 : local_int);
        Sum += (ab_false[index] ^ func_sb_false() ? 3 : static_field_int);
        Sum += (ab_false[index] ^ func_sb_false() ? 3 : t1_i.mfi);
        Sum += (ab_false[index] ^ func_sb_false() ? 3 : simple_func_int());
        Sum += (ab_false[index] ^ func_sb_false() ? 3 : ab[index]);
        Sum += (ab_false[index] ^ func_sb_false() ? 3 : ab[index - 1]);
        Sum += (ab_false[index] ^ func_sb_false() ? -5 : 3);
        Sum += (ab_false[index] ^ func_sb_false() ? -5 : -5);
        Sum += (ab_false[index] ^ func_sb_false() ? -5 : local_int);
        Sum += (ab_false[index] ^ func_sb_false() ? -5 : static_field_int);
        Sum += (ab_false[index] ^ func_sb_false() ? -5 : t1_i.mfi);
        Sum += (ab_false[index] ^ func_sb_false() ? -5 : simple_func_int());
        Sum += (ab_false[index] ^ func_sb_false() ? -5 : ab[index]);
        Sum += (ab_false[index] ^ func_sb_false() ? -5 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_452()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ func_sb_false() ? local_int : 3);
        Sum += (ab_false[index] ^ func_sb_false() ? local_int : -5);
        Sum += (ab_false[index] ^ func_sb_false() ? local_int : local_int);
        Sum += (ab_false[index] ^ func_sb_false() ? local_int : static_field_int);
        Sum += (ab_false[index] ^ func_sb_false() ? local_int : t1_i.mfi);
        Sum += (ab_false[index] ^ func_sb_false() ? local_int : simple_func_int());
        Sum += (ab_false[index] ^ func_sb_false() ? local_int : ab[index]);
        Sum += (ab_false[index] ^ func_sb_false() ? local_int : ab[index - 1]);
        Sum += (ab_false[index] ^ func_sb_false() ? static_field_int : 3);
        Sum += (ab_false[index] ^ func_sb_false() ? static_field_int : -5);
        Sum += (ab_false[index] ^ func_sb_false() ? static_field_int : local_int);
        Sum += (ab_false[index] ^ func_sb_false() ? static_field_int : static_field_int);
        Sum += (ab_false[index] ^ func_sb_false() ? static_field_int : t1_i.mfi);
        Sum += (ab_false[index] ^ func_sb_false() ? static_field_int : simple_func_int());
        Sum += (ab_false[index] ^ func_sb_false() ? static_field_int : ab[index]);
        Sum += (ab_false[index] ^ func_sb_false() ? static_field_int : ab[index - 1]);
        Sum += (ab_false[index] ^ func_sb_false() ? t1_i.mfi : 3);
        Sum += (ab_false[index] ^ func_sb_false() ? t1_i.mfi : -5);
        Sum += (ab_false[index] ^ func_sb_false() ? t1_i.mfi : local_int);
        Sum += (ab_false[index] ^ func_sb_false() ? t1_i.mfi : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_453()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ func_sb_false() ? t1_i.mfi : t1_i.mfi);
        Sum += (ab_false[index] ^ func_sb_false() ? t1_i.mfi : simple_func_int());
        Sum += (ab_false[index] ^ func_sb_false() ? t1_i.mfi : ab[index]);
        Sum += (ab_false[index] ^ func_sb_false() ? t1_i.mfi : ab[index - 1]);
        Sum += (ab_false[index] ^ func_sb_false() ? simple_func_int() : 3);
        Sum += (ab_false[index] ^ func_sb_false() ? simple_func_int() : -5);
        Sum += (ab_false[index] ^ func_sb_false() ? simple_func_int() : local_int);
        Sum += (ab_false[index] ^ func_sb_false() ? simple_func_int() : static_field_int);
        Sum += (ab_false[index] ^ func_sb_false() ? simple_func_int() : t1_i.mfi);
        Sum += (ab_false[index] ^ func_sb_false() ? simple_func_int() : simple_func_int());
        Sum += (ab_false[index] ^ func_sb_false() ? simple_func_int() : ab[index]);
        Sum += (ab_false[index] ^ func_sb_false() ? simple_func_int() : ab[index - 1]);
        Sum += (ab_false[index] ^ func_sb_false() ? ab[index] : 3);
        Sum += (ab_false[index] ^ func_sb_false() ? ab[index] : -5);
        Sum += (ab_false[index] ^ func_sb_false() ? ab[index] : local_int);
        Sum += (ab_false[index] ^ func_sb_false() ? ab[index] : static_field_int);
        Sum += (ab_false[index] ^ func_sb_false() ? ab[index] : t1_i.mfi);
        Sum += (ab_false[index] ^ func_sb_false() ? ab[index] : simple_func_int());
        Sum += (ab_false[index] ^ func_sb_false() ? ab[index] : ab[index]);
        Sum += (ab_false[index] ^ func_sb_false() ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_454()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ func_sb_false() ? ab[index - 1] : 3);
        Sum += (ab_false[index] ^ func_sb_false() ? ab[index - 1] : -5);
        Sum += (ab_false[index] ^ func_sb_false() ? ab[index - 1] : local_int);
        Sum += (ab_false[index] ^ func_sb_false() ? ab[index - 1] : static_field_int);
        Sum += (ab_false[index] ^ func_sb_false() ? ab[index - 1] : t1_i.mfi);
        Sum += (ab_false[index] ^ func_sb_false() ? ab[index - 1] : simple_func_int());
        Sum += (ab_false[index] ^ func_sb_false() ? ab[index - 1] : ab[index]);
        Sum += (ab_false[index] ^ func_sb_false() ? ab[index - 1] : ab[index - 1]);
        Sum += (ab_false[index] ^ ab_true[index] ? 3 : 3);
        Sum += (ab_false[index] ^ ab_true[index] ? 3 : -5);
        Sum += (ab_false[index] ^ ab_true[index] ? 3 : local_int);
        Sum += (ab_false[index] ^ ab_true[index] ? 3 : static_field_int);
        Sum += (ab_false[index] ^ ab_true[index] ? 3 : t1_i.mfi);
        Sum += (ab_false[index] ^ ab_true[index] ? 3 : simple_func_int());
        Sum += (ab_false[index] ^ ab_true[index] ? 3 : ab[index]);
        Sum += (ab_false[index] ^ ab_true[index] ? 3 : ab[index - 1]);
        Sum += (ab_false[index] ^ ab_true[index] ? -5 : 3);
        Sum += (ab_false[index] ^ ab_true[index] ? -5 : -5);
        Sum += (ab_false[index] ^ ab_true[index] ? -5 : local_int);
        Sum += (ab_false[index] ^ ab_true[index] ? -5 : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_455()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ ab_true[index] ? -5 : t1_i.mfi);
        Sum += (ab_false[index] ^ ab_true[index] ? -5 : simple_func_int());
        Sum += (ab_false[index] ^ ab_true[index] ? -5 : ab[index]);
        Sum += (ab_false[index] ^ ab_true[index] ? -5 : ab[index - 1]);
        Sum += (ab_false[index] ^ ab_true[index] ? local_int : 3);
        Sum += (ab_false[index] ^ ab_true[index] ? local_int : -5);
        Sum += (ab_false[index] ^ ab_true[index] ? local_int : local_int);
        Sum += (ab_false[index] ^ ab_true[index] ? local_int : static_field_int);
        Sum += (ab_false[index] ^ ab_true[index] ? local_int : t1_i.mfi);
        Sum += (ab_false[index] ^ ab_true[index] ? local_int : simple_func_int());
        Sum += (ab_false[index] ^ ab_true[index] ? local_int : ab[index]);
        Sum += (ab_false[index] ^ ab_true[index] ? local_int : ab[index - 1]);
        Sum += (ab_false[index] ^ ab_true[index] ? static_field_int : 3);
        Sum += (ab_false[index] ^ ab_true[index] ? static_field_int : -5);
        Sum += (ab_false[index] ^ ab_true[index] ? static_field_int : local_int);
        Sum += (ab_false[index] ^ ab_true[index] ? static_field_int : static_field_int);
        Sum += (ab_false[index] ^ ab_true[index] ? static_field_int : t1_i.mfi);
        Sum += (ab_false[index] ^ ab_true[index] ? static_field_int : simple_func_int());
        Sum += (ab_false[index] ^ ab_true[index] ? static_field_int : ab[index]);
        Sum += (ab_false[index] ^ ab_true[index] ? static_field_int : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_456()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ ab_true[index] ? t1_i.mfi : 3);
        Sum += (ab_false[index] ^ ab_true[index] ? t1_i.mfi : -5);
        Sum += (ab_false[index] ^ ab_true[index] ? t1_i.mfi : local_int);
        Sum += (ab_false[index] ^ ab_true[index] ? t1_i.mfi : static_field_int);
        Sum += (ab_false[index] ^ ab_true[index] ? t1_i.mfi : t1_i.mfi);
        Sum += (ab_false[index] ^ ab_true[index] ? t1_i.mfi : simple_func_int());
        Sum += (ab_false[index] ^ ab_true[index] ? t1_i.mfi : ab[index]);
        Sum += (ab_false[index] ^ ab_true[index] ? t1_i.mfi : ab[index - 1]);
        Sum += (ab_false[index] ^ ab_true[index] ? simple_func_int() : 3);
        Sum += (ab_false[index] ^ ab_true[index] ? simple_func_int() : -5);
        Sum += (ab_false[index] ^ ab_true[index] ? simple_func_int() : local_int);
        Sum += (ab_false[index] ^ ab_true[index] ? simple_func_int() : static_field_int);
        Sum += (ab_false[index] ^ ab_true[index] ? simple_func_int() : t1_i.mfi);
        Sum += (ab_false[index] ^ ab_true[index] ? simple_func_int() : simple_func_int());
        Sum += (ab_false[index] ^ ab_true[index] ? simple_func_int() : ab[index]);
        Sum += (ab_false[index] ^ ab_true[index] ? simple_func_int() : ab[index - 1]);
        Sum += (ab_false[index] ^ ab_true[index] ? ab[index] : 3);
        Sum += (ab_false[index] ^ ab_true[index] ? ab[index] : -5);
        Sum += (ab_false[index] ^ ab_true[index] ? ab[index] : local_int);
        Sum += (ab_false[index] ^ ab_true[index] ? ab[index] : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_457()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ ab_true[index] ? ab[index] : t1_i.mfi);
        Sum += (ab_false[index] ^ ab_true[index] ? ab[index] : simple_func_int());
        Sum += (ab_false[index] ^ ab_true[index] ? ab[index] : ab[index]);
        Sum += (ab_false[index] ^ ab_true[index] ? ab[index] : ab[index - 1]);
        Sum += (ab_false[index] ^ ab_true[index] ? ab[index - 1] : 3);
        Sum += (ab_false[index] ^ ab_true[index] ? ab[index - 1] : -5);
        Sum += (ab_false[index] ^ ab_true[index] ? ab[index - 1] : local_int);
        Sum += (ab_false[index] ^ ab_true[index] ? ab[index - 1] : static_field_int);
        Sum += (ab_false[index] ^ ab_true[index] ? ab[index - 1] : t1_i.mfi);
        Sum += (ab_false[index] ^ ab_true[index] ? ab[index - 1] : simple_func_int());
        Sum += (ab_false[index] ^ ab_true[index] ? ab[index - 1] : ab[index]);
        Sum += (ab_false[index] ^ ab_true[index] ? ab[index - 1] : ab[index - 1]);
        Sum += (ab_false[index] ^ ab_false[index] ? 3 : 3);
        Sum += (ab_false[index] ^ ab_false[index] ? 3 : -5);
        Sum += (ab_false[index] ^ ab_false[index] ? 3 : local_int);
        Sum += (ab_false[index] ^ ab_false[index] ? 3 : static_field_int);
        Sum += (ab_false[index] ^ ab_false[index] ? 3 : t1_i.mfi);
        Sum += (ab_false[index] ^ ab_false[index] ? 3 : simple_func_int());
        Sum += (ab_false[index] ^ ab_false[index] ? 3 : ab[index]);
        Sum += (ab_false[index] ^ ab_false[index] ? 3 : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_458()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ ab_false[index] ? -5 : 3);
        Sum += (ab_false[index] ^ ab_false[index] ? -5 : -5);
        Sum += (ab_false[index] ^ ab_false[index] ? -5 : local_int);
        Sum += (ab_false[index] ^ ab_false[index] ? -5 : static_field_int);
        Sum += (ab_false[index] ^ ab_false[index] ? -5 : t1_i.mfi);
        Sum += (ab_false[index] ^ ab_false[index] ? -5 : simple_func_int());
        Sum += (ab_false[index] ^ ab_false[index] ? -5 : ab[index]);
        Sum += (ab_false[index] ^ ab_false[index] ? -5 : ab[index - 1]);
        Sum += (ab_false[index] ^ ab_false[index] ? local_int : 3);
        Sum += (ab_false[index] ^ ab_false[index] ? local_int : -5);
        Sum += (ab_false[index] ^ ab_false[index] ? local_int : local_int);
        Sum += (ab_false[index] ^ ab_false[index] ? local_int : static_field_int);
        Sum += (ab_false[index] ^ ab_false[index] ? local_int : t1_i.mfi);
        Sum += (ab_false[index] ^ ab_false[index] ? local_int : simple_func_int());
        Sum += (ab_false[index] ^ ab_false[index] ? local_int : ab[index]);
        Sum += (ab_false[index] ^ ab_false[index] ? local_int : ab[index - 1]);
        Sum += (ab_false[index] ^ ab_false[index] ? static_field_int : 3);
        Sum += (ab_false[index] ^ ab_false[index] ? static_field_int : -5);
        Sum += (ab_false[index] ^ ab_false[index] ? static_field_int : local_int);
        Sum += (ab_false[index] ^ ab_false[index] ? static_field_int : static_field_int);
        return Sum;
    }
    static int Sub_Funclet_459()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ ab_false[index] ? static_field_int : t1_i.mfi);
        Sum += (ab_false[index] ^ ab_false[index] ? static_field_int : simple_func_int());
        Sum += (ab_false[index] ^ ab_false[index] ? static_field_int : ab[index]);
        Sum += (ab_false[index] ^ ab_false[index] ? static_field_int : ab[index - 1]);
        Sum += (ab_false[index] ^ ab_false[index] ? t1_i.mfi : 3);
        Sum += (ab_false[index] ^ ab_false[index] ? t1_i.mfi : -5);
        Sum += (ab_false[index] ^ ab_false[index] ? t1_i.mfi : local_int);
        Sum += (ab_false[index] ^ ab_false[index] ? t1_i.mfi : static_field_int);
        Sum += (ab_false[index] ^ ab_false[index] ? t1_i.mfi : t1_i.mfi);
        Sum += (ab_false[index] ^ ab_false[index] ? t1_i.mfi : simple_func_int());
        Sum += (ab_false[index] ^ ab_false[index] ? t1_i.mfi : ab[index]);
        Sum += (ab_false[index] ^ ab_false[index] ? t1_i.mfi : ab[index - 1]);
        Sum += (ab_false[index] ^ ab_false[index] ? simple_func_int() : 3);
        Sum += (ab_false[index] ^ ab_false[index] ? simple_func_int() : -5);
        Sum += (ab_false[index] ^ ab_false[index] ? simple_func_int() : local_int);
        Sum += (ab_false[index] ^ ab_false[index] ? simple_func_int() : static_field_int);
        Sum += (ab_false[index] ^ ab_false[index] ? simple_func_int() : t1_i.mfi);
        Sum += (ab_false[index] ^ ab_false[index] ? simple_func_int() : simple_func_int());
        Sum += (ab_false[index] ^ ab_false[index] ? simple_func_int() : ab[index]);
        Sum += (ab_false[index] ^ ab_false[index] ? simple_func_int() : ab[index - 1]);
        return Sum;
    }
    static int Sub_Funclet_460()
    {
        int Sum = 0;
        int index = 1;
        int local_int = -5;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        int[] ab = new int[3];
        ab[0] = 21;
        ab[1] = -27;
        ab[2] = -31;

        static_field_int = 7;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfi = -13;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ^ ab_false[index] ? ab[index] : 3);
        Sum += (ab_false[index] ^ ab_false[index] ? ab[index] : -5);
        Sum += (ab_false[index] ^ ab_false[index] ? ab[index] : local_int);
        Sum += (ab_false[index] ^ ab_false[index] ? ab[index] : static_field_int);
        Sum += (ab_false[index] ^ ab_false[index] ? ab[index] : t1_i.mfi);
        Sum += (ab_false[index] ^ ab_false[index] ? ab[index] : simple_func_int());
        Sum += (ab_false[index] ^ ab_false[index] ? ab[index] : ab[index]);
        Sum += (ab_false[index] ^ ab_false[index] ? ab[index] : ab[index - 1]);
        Sum += (ab_false[index] ^ ab_false[index] ? ab[index - 1] : 3);
        Sum += (ab_false[index] ^ ab_false[index] ? ab[index - 1] : -5);
        Sum += (ab_false[index] ^ ab_false[index] ? ab[index - 1] : local_int);
        Sum += (ab_false[index] ^ ab_false[index] ? ab[index - 1] : static_field_int);
        Sum += (ab_false[index] ^ ab_false[index] ? ab[index - 1] : t1_i.mfi);
        Sum += (ab_false[index] ^ ab_false[index] ? ab[index - 1] : simple_func_int());
        Sum += (ab_false[index] ^ ab_false[index] ? ab[index - 1] : ab[index]);
        Sum += (ab_false[index] ^ ab_false[index] ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    [Fact]
    public static int TestEntryPoint()
    {
        int Sum = 0;
        Sum += Sub_Funclet_0();
        Sum += Sub_Funclet_1();
        Sum += Sub_Funclet_2();
        Sum += Sub_Funclet_3();
        Sum += Sub_Funclet_4();
        Sum += Sub_Funclet_5();
        Sum += Sub_Funclet_6();
        Sum += Sub_Funclet_7();
        Sum += Sub_Funclet_8();
        Sum += Sub_Funclet_9();
        Sum += Sub_Funclet_10();
        Sum += Sub_Funclet_11();
        Sum += Sub_Funclet_12();
        Sum += Sub_Funclet_13();
        Sum += Sub_Funclet_14();
        Sum += Sub_Funclet_15();
        Sum += Sub_Funclet_16();
        Sum += Sub_Funclet_17();
        Sum += Sub_Funclet_18();
        Sum += Sub_Funclet_19();
        Sum += Sub_Funclet_20();
        Sum += Sub_Funclet_21();
        Sum += Sub_Funclet_22();
        Sum += Sub_Funclet_23();
        Sum += Sub_Funclet_24();
        Sum += Sub_Funclet_25();
        Sum += Sub_Funclet_26();
        Sum += Sub_Funclet_27();
        Sum += Sub_Funclet_28();
        Sum += Sub_Funclet_29();
        Sum += Sub_Funclet_30();
        Sum += Sub_Funclet_31();
        Sum += Sub_Funclet_32();
        Sum += Sub_Funclet_33();
        Sum += Sub_Funclet_34();
        Sum += Sub_Funclet_35();
        Sum += Sub_Funclet_36();
        Sum += Sub_Funclet_37();
        Sum += Sub_Funclet_38();
        Sum += Sub_Funclet_39();
        Sum += Sub_Funclet_40();
        Sum += Sub_Funclet_41();
        Sum += Sub_Funclet_42();
        Sum += Sub_Funclet_43();
        Sum += Sub_Funclet_44();
        Sum += Sub_Funclet_45();
        Sum += Sub_Funclet_46();
        Sum += Sub_Funclet_47();
        Sum += Sub_Funclet_48();
        Sum += Sub_Funclet_49();
        Sum += Sub_Funclet_50();
        Sum += Sub_Funclet_51();
        Sum += Sub_Funclet_52();
        Sum += Sub_Funclet_53();
        Sum += Sub_Funclet_54();
        Sum += Sub_Funclet_55();
        Sum += Sub_Funclet_56();
        Sum += Sub_Funclet_57();
        Sum += Sub_Funclet_58();
        Sum += Sub_Funclet_59();
        Sum += Sub_Funclet_60();
        Sum += Sub_Funclet_61();
        Sum += Sub_Funclet_62();
        Sum += Sub_Funclet_63();
        Sum += Sub_Funclet_64();
        Sum += Sub_Funclet_65();
        Sum += Sub_Funclet_66();
        Sum += Sub_Funclet_67();
        Sum += Sub_Funclet_68();
        Sum += Sub_Funclet_69();
        Sum += Sub_Funclet_70();
        Sum += Sub_Funclet_71();
        Sum += Sub_Funclet_72();
        Sum += Sub_Funclet_73();
        Sum += Sub_Funclet_74();
        Sum += Sub_Funclet_75();
        Sum += Sub_Funclet_76();
        Sum += Sub_Funclet_77();
        Sum += Sub_Funclet_78();
        Sum += Sub_Funclet_79();
        Sum += Sub_Funclet_80();
        Sum += Sub_Funclet_81();
        Sum += Sub_Funclet_82();
        Sum += Sub_Funclet_83();
        Sum += Sub_Funclet_84();
        Sum += Sub_Funclet_85();
        Sum += Sub_Funclet_86();
        Sum += Sub_Funclet_87();
        Sum += Sub_Funclet_88();
        Sum += Sub_Funclet_89();
        Sum += Sub_Funclet_90();
        Sum += Sub_Funclet_91();
        Sum += Sub_Funclet_92();
        Sum += Sub_Funclet_93();
        Sum += Sub_Funclet_94();
        Sum += Sub_Funclet_95();
        Sum += Sub_Funclet_96();
        Sum += Sub_Funclet_97();
        Sum += Sub_Funclet_98();
        Sum += Sub_Funclet_99();
        Sum += Sub_Funclet_100();
        Sum += Sub_Funclet_101();
        Sum += Sub_Funclet_102();
        Sum += Sub_Funclet_103();
        Sum += Sub_Funclet_104();
        Sum += Sub_Funclet_105();
        Sum += Sub_Funclet_106();
        Sum += Sub_Funclet_107();
        Sum += Sub_Funclet_108();
        Sum += Sub_Funclet_109();
        Sum += Sub_Funclet_110();
        Sum += Sub_Funclet_111();
        Sum += Sub_Funclet_112();
        Sum += Sub_Funclet_113();
        Sum += Sub_Funclet_114();
        Sum += Sub_Funclet_115();
        Sum += Sub_Funclet_116();
        Sum += Sub_Funclet_117();
        Sum += Sub_Funclet_118();
        Sum += Sub_Funclet_119();
        Sum += Sub_Funclet_120();
        Sum += Sub_Funclet_121();
        Sum += Sub_Funclet_122();
        Sum += Sub_Funclet_123();
        Sum += Sub_Funclet_124();
        Sum += Sub_Funclet_125();
        Sum += Sub_Funclet_126();
        Sum += Sub_Funclet_127();
        Sum += Sub_Funclet_128();
        Sum += Sub_Funclet_129();
        Sum += Sub_Funclet_130();
        Sum += Sub_Funclet_131();
        Sum += Sub_Funclet_132();
        Sum += Sub_Funclet_133();
        Sum += Sub_Funclet_134();
        Sum += Sub_Funclet_135();
        Sum += Sub_Funclet_136();
        Sum += Sub_Funclet_137();
        Sum += Sub_Funclet_138();
        Sum += Sub_Funclet_139();
        Sum += Sub_Funclet_140();
        Sum += Sub_Funclet_141();
        Sum += Sub_Funclet_142();
        Sum += Sub_Funclet_143();
        Sum += Sub_Funclet_144();
        Sum += Sub_Funclet_145();
        Sum += Sub_Funclet_146();
        Sum += Sub_Funclet_147();
        Sum += Sub_Funclet_148();
        Sum += Sub_Funclet_149();
        Sum += Sub_Funclet_150();
        Sum += Sub_Funclet_151();
        Sum += Sub_Funclet_152();
        Sum += Sub_Funclet_153();
        Sum += Sub_Funclet_154();
        Sum += Sub_Funclet_155();
        Sum += Sub_Funclet_156();
        Sum += Sub_Funclet_157();
        Sum += Sub_Funclet_158();
        Sum += Sub_Funclet_159();
        Sum += Sub_Funclet_160();
        Sum += Sub_Funclet_161();
        Sum += Sub_Funclet_162();
        Sum += Sub_Funclet_163();
        Sum += Sub_Funclet_164();
        Sum += Sub_Funclet_165();
        Sum += Sub_Funclet_166();
        Sum += Sub_Funclet_167();
        Sum += Sub_Funclet_168();
        Sum += Sub_Funclet_169();
        Sum += Sub_Funclet_170();
        Sum += Sub_Funclet_171();
        Sum += Sub_Funclet_172();
        Sum += Sub_Funclet_173();
        Sum += Sub_Funclet_174();
        Sum += Sub_Funclet_175();
        Sum += Sub_Funclet_176();
        Sum += Sub_Funclet_177();
        Sum += Sub_Funclet_178();
        Sum += Sub_Funclet_179();
        Sum += Sub_Funclet_180();
        Sum += Sub_Funclet_181();
        Sum += Sub_Funclet_182();
        Sum += Sub_Funclet_183();
        Sum += Sub_Funclet_184();
        Sum += Sub_Funclet_185();
        Sum += Sub_Funclet_186();
        Sum += Sub_Funclet_187();
        Sum += Sub_Funclet_188();
        Sum += Sub_Funclet_189();
        Sum += Sub_Funclet_190();
        Sum += Sub_Funclet_191();
        Sum += Sub_Funclet_192();
        Sum += Sub_Funclet_193();
        Sum += Sub_Funclet_194();
        Sum += Sub_Funclet_195();
        Sum += Sub_Funclet_196();
        Sum += Sub_Funclet_197();
        Sum += Sub_Funclet_198();
        Sum += Sub_Funclet_199();
        Sum += Sub_Funclet_200();
        Sum += Sub_Funclet_201();
        Sum += Sub_Funclet_202();
        Sum += Sub_Funclet_203();
        Sum += Sub_Funclet_204();
        Sum += Sub_Funclet_205();
        Sum += Sub_Funclet_206();
        Sum += Sub_Funclet_207();
        Sum += Sub_Funclet_208();
        Sum += Sub_Funclet_209();
        Sum += Sub_Funclet_210();
        Sum += Sub_Funclet_211();
        Sum += Sub_Funclet_212();
        Sum += Sub_Funclet_213();
        Sum += Sub_Funclet_214();
        Sum += Sub_Funclet_215();
        Sum += Sub_Funclet_216();
        Sum += Sub_Funclet_217();
        Sum += Sub_Funclet_218();
        Sum += Sub_Funclet_219();
        Sum += Sub_Funclet_220();
        Sum += Sub_Funclet_221();
        Sum += Sub_Funclet_222();
        Sum += Sub_Funclet_223();
        Sum += Sub_Funclet_224();
        Sum += Sub_Funclet_225();
        Sum += Sub_Funclet_226();
        Sum += Sub_Funclet_227();
        Sum += Sub_Funclet_228();
        Sum += Sub_Funclet_229();
        Sum += Sub_Funclet_230();
        Sum += Sub_Funclet_231();
        Sum += Sub_Funclet_232();
        Sum += Sub_Funclet_233();
        Sum += Sub_Funclet_234();
        Sum += Sub_Funclet_235();
        Sum += Sub_Funclet_236();
        Sum += Sub_Funclet_237();
        Sum += Sub_Funclet_238();
        Sum += Sub_Funclet_239();
        Sum += Sub_Funclet_240();
        Sum += Sub_Funclet_241();
        Sum += Sub_Funclet_242();
        Sum += Sub_Funclet_243();
        Sum += Sub_Funclet_244();
        Sum += Sub_Funclet_245();
        Sum += Sub_Funclet_246();
        Sum += Sub_Funclet_247();
        Sum += Sub_Funclet_248();
        Sum += Sub_Funclet_249();
        Sum += Sub_Funclet_250();
        Sum += Sub_Funclet_251();
        Sum += Sub_Funclet_252();
        Sum += Sub_Funclet_253();
        Sum += Sub_Funclet_254();
        Sum += Sub_Funclet_255();
        Sum += Sub_Funclet_256();
        Sum += Sub_Funclet_257();
        Sum += Sub_Funclet_258();
        Sum += Sub_Funclet_259();
        Sum += Sub_Funclet_260();
        Sum += Sub_Funclet_261();
        Sum += Sub_Funclet_262();
        Sum += Sub_Funclet_263();
        Sum += Sub_Funclet_264();
        Sum += Sub_Funclet_265();
        Sum += Sub_Funclet_266();
        Sum += Sub_Funclet_267();
        Sum += Sub_Funclet_268();
        Sum += Sub_Funclet_269();
        Sum += Sub_Funclet_270();
        Sum += Sub_Funclet_271();
        Sum += Sub_Funclet_272();
        Sum += Sub_Funclet_273();
        Sum += Sub_Funclet_274();
        Sum += Sub_Funclet_275();
        Sum += Sub_Funclet_276();
        Sum += Sub_Funclet_277();
        Sum += Sub_Funclet_278();
        Sum += Sub_Funclet_279();
        Sum += Sub_Funclet_280();
        Sum += Sub_Funclet_281();
        Sum += Sub_Funclet_282();
        Sum += Sub_Funclet_283();
        Sum += Sub_Funclet_284();
        Sum += Sub_Funclet_285();
        Sum += Sub_Funclet_286();
        Sum += Sub_Funclet_287();
        Sum += Sub_Funclet_288();
        Sum += Sub_Funclet_289();
        Sum += Sub_Funclet_290();
        Sum += Sub_Funclet_291();
        Sum += Sub_Funclet_292();
        Sum += Sub_Funclet_293();
        Sum += Sub_Funclet_294();
        Sum += Sub_Funclet_295();
        Sum += Sub_Funclet_296();
        Sum += Sub_Funclet_297();
        Sum += Sub_Funclet_298();
        Sum += Sub_Funclet_299();
        Sum += Sub_Funclet_300();
        Sum += Sub_Funclet_301();
        Sum += Sub_Funclet_302();
        Sum += Sub_Funclet_303();
        Sum += Sub_Funclet_304();
        Sum += Sub_Funclet_305();
        Sum += Sub_Funclet_306();
        Sum += Sub_Funclet_307();
        Sum += Sub_Funclet_308();
        Sum += Sub_Funclet_309();
        Sum += Sub_Funclet_310();
        Sum += Sub_Funclet_311();
        Sum += Sub_Funclet_312();
        Sum += Sub_Funclet_313();
        Sum += Sub_Funclet_314();
        Sum += Sub_Funclet_315();
        Sum += Sub_Funclet_316();
        Sum += Sub_Funclet_317();
        Sum += Sub_Funclet_318();
        Sum += Sub_Funclet_319();
        Sum += Sub_Funclet_320();
        Sum += Sub_Funclet_321();
        Sum += Sub_Funclet_322();
        Sum += Sub_Funclet_323();
        Sum += Sub_Funclet_324();
        Sum += Sub_Funclet_325();
        Sum += Sub_Funclet_326();
        Sum += Sub_Funclet_327();
        Sum += Sub_Funclet_328();
        Sum += Sub_Funclet_329();
        Sum += Sub_Funclet_330();
        Sum += Sub_Funclet_331();
        Sum += Sub_Funclet_332();
        Sum += Sub_Funclet_333();
        Sum += Sub_Funclet_334();
        Sum += Sub_Funclet_335();
        Sum += Sub_Funclet_336();
        Sum += Sub_Funclet_337();
        Sum += Sub_Funclet_338();
        Sum += Sub_Funclet_339();
        Sum += Sub_Funclet_340();
        Sum += Sub_Funclet_341();
        Sum += Sub_Funclet_342();
        Sum += Sub_Funclet_343();
        Sum += Sub_Funclet_344();
        Sum += Sub_Funclet_345();
        Sum += Sub_Funclet_346();
        Sum += Sub_Funclet_347();
        Sum += Sub_Funclet_348();
        Sum += Sub_Funclet_349();
        Sum += Sub_Funclet_350();
        Sum += Sub_Funclet_351();
        Sum += Sub_Funclet_352();
        Sum += Sub_Funclet_353();
        Sum += Sub_Funclet_354();
        Sum += Sub_Funclet_355();
        Sum += Sub_Funclet_356();
        Sum += Sub_Funclet_357();
        Sum += Sub_Funclet_358();
        Sum += Sub_Funclet_359();
        Sum += Sub_Funclet_360();
        Sum += Sub_Funclet_361();
        Sum += Sub_Funclet_362();
        Sum += Sub_Funclet_363();
        Sum += Sub_Funclet_364();
        Sum += Sub_Funclet_365();
        Sum += Sub_Funclet_366();
        Sum += Sub_Funclet_367();
        Sum += Sub_Funclet_368();
        Sum += Sub_Funclet_369();
        Sum += Sub_Funclet_370();
        Sum += Sub_Funclet_371();
        Sum += Sub_Funclet_372();
        Sum += Sub_Funclet_373();
        Sum += Sub_Funclet_374();
        Sum += Sub_Funclet_375();
        Sum += Sub_Funclet_376();
        Sum += Sub_Funclet_377();
        Sum += Sub_Funclet_378();
        Sum += Sub_Funclet_379();
        Sum += Sub_Funclet_380();
        Sum += Sub_Funclet_381();
        Sum += Sub_Funclet_382();
        Sum += Sub_Funclet_383();
        Sum += Sub_Funclet_384();
        Sum += Sub_Funclet_385();
        Sum += Sub_Funclet_386();
        Sum += Sub_Funclet_387();
        Sum += Sub_Funclet_388();
        Sum += Sub_Funclet_389();
        Sum += Sub_Funclet_390();
        Sum += Sub_Funclet_391();
        Sum += Sub_Funclet_392();
        Sum += Sub_Funclet_393();
        Sum += Sub_Funclet_394();
        Sum += Sub_Funclet_395();
        Sum += Sub_Funclet_396();
        Sum += Sub_Funclet_397();
        Sum += Sub_Funclet_398();
        Sum += Sub_Funclet_399();
        Sum += Sub_Funclet_400();
        Sum += Sub_Funclet_401();
        Sum += Sub_Funclet_402();
        Sum += Sub_Funclet_403();
        Sum += Sub_Funclet_404();
        Sum += Sub_Funclet_405();
        Sum += Sub_Funclet_406();
        Sum += Sub_Funclet_407();
        Sum += Sub_Funclet_408();
        Sum += Sub_Funclet_409();
        Sum += Sub_Funclet_410();
        Sum += Sub_Funclet_411();
        Sum += Sub_Funclet_412();
        Sum += Sub_Funclet_413();
        Sum += Sub_Funclet_414();
        Sum += Sub_Funclet_415();
        Sum += Sub_Funclet_416();
        Sum += Sub_Funclet_417();
        Sum += Sub_Funclet_418();
        Sum += Sub_Funclet_419();
        Sum += Sub_Funclet_420();
        Sum += Sub_Funclet_421();
        Sum += Sub_Funclet_422();
        Sum += Sub_Funclet_423();
        Sum += Sub_Funclet_424();
        Sum += Sub_Funclet_425();
        Sum += Sub_Funclet_426();
        Sum += Sub_Funclet_427();
        Sum += Sub_Funclet_428();
        Sum += Sub_Funclet_429();
        Sum += Sub_Funclet_430();
        Sum += Sub_Funclet_431();
        Sum += Sub_Funclet_432();
        Sum += Sub_Funclet_433();
        Sum += Sub_Funclet_434();
        Sum += Sub_Funclet_435();
        Sum += Sub_Funclet_436();
        Sum += Sub_Funclet_437();
        Sum += Sub_Funclet_438();
        Sum += Sub_Funclet_439();
        Sum += Sub_Funclet_440();
        Sum += Sub_Funclet_441();
        Sum += Sub_Funclet_442();
        Sum += Sub_Funclet_443();
        Sum += Sub_Funclet_444();
        Sum += Sub_Funclet_445();
        Sum += Sub_Funclet_446();
        Sum += Sub_Funclet_447();
        Sum += Sub_Funclet_448();
        Sum += Sub_Funclet_449();
        Sum += Sub_Funclet_450();
        Sum += Sub_Funclet_451();
        Sum += Sub_Funclet_452();
        Sum += Sub_Funclet_453();
        Sum += Sub_Funclet_454();
        Sum += Sub_Funclet_455();
        Sum += Sub_Funclet_456();
        Sum += Sub_Funclet_457();
        Sum += Sub_Funclet_458();
        Sum += Sub_Funclet_459();
        Sum += Sub_Funclet_460();

        if (Sum == -2304)
        {
            Console.WriteLine("PASSED");
            return 100;
        }
        else
        {
            Console.WriteLine("FAILED");
            return 1;
        }
    }
}
