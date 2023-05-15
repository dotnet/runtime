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
        Sum += (true ? 3 : 3);
        Sum += (true ? 3 : -5);
        Sum += (true ? 3 : local_int);
        Sum += (true ? 3 : static_field_int);
        Sum += (true ? 3 : t1_i.mfi);
        Sum += (true ? 3 : simple_func_int());
        Sum += (true ? 3 : ab[index]);
        Sum += (true ? 3 : ab[index - 1]);
        Sum += (true ? -5 : 3);
        Sum += (true ? -5 : -5);
        Sum += (true ? -5 : local_int);
        Sum += (true ? -5 : static_field_int);
        Sum += (true ? -5 : t1_i.mfi);
        Sum += (true ? -5 : simple_func_int());
        Sum += (true ? -5 : ab[index]);
        Sum += (true ? -5 : ab[index - 1]);
        Sum += (true ? local_int : 3);
        Sum += (true ? local_int : -5);
        Sum += (true ? local_int : local_int);
        Sum += (true ? local_int : static_field_int);
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
        Sum += (true ? local_int : t1_i.mfi);
        Sum += (true ? local_int : simple_func_int());
        Sum += (true ? local_int : ab[index]);
        Sum += (true ? local_int : ab[index - 1]);
        Sum += (true ? static_field_int : 3);
        Sum += (true ? static_field_int : -5);
        Sum += (true ? static_field_int : local_int);
        Sum += (true ? static_field_int : static_field_int);
        Sum += (true ? static_field_int : t1_i.mfi);
        Sum += (true ? static_field_int : simple_func_int());
        Sum += (true ? static_field_int : ab[index]);
        Sum += (true ? static_field_int : ab[index - 1]);
        Sum += (true ? t1_i.mfi : 3);
        Sum += (true ? t1_i.mfi : -5);
        Sum += (true ? t1_i.mfi : local_int);
        Sum += (true ? t1_i.mfi : static_field_int);
        Sum += (true ? t1_i.mfi : t1_i.mfi);
        Sum += (true ? t1_i.mfi : simple_func_int());
        Sum += (true ? t1_i.mfi : ab[index]);
        Sum += (true ? t1_i.mfi : ab[index - 1]);
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
        Sum += (true ? simple_func_int() : 3);
        Sum += (true ? simple_func_int() : -5);
        Sum += (true ? simple_func_int() : local_int);
        Sum += (true ? simple_func_int() : static_field_int);
        Sum += (true ? simple_func_int() : t1_i.mfi);
        Sum += (true ? simple_func_int() : simple_func_int());
        Sum += (true ? simple_func_int() : ab[index]);
        Sum += (true ? simple_func_int() : ab[index - 1]);
        Sum += (true ? ab[index] : 3);
        Sum += (true ? ab[index] : -5);
        Sum += (true ? ab[index] : local_int);
        Sum += (true ? ab[index] : static_field_int);
        Sum += (true ? ab[index] : t1_i.mfi);
        Sum += (true ? ab[index] : simple_func_int());
        Sum += (true ? ab[index] : ab[index]);
        Sum += (true ? ab[index] : ab[index - 1]);
        Sum += (true ? ab[index - 1] : 3);
        Sum += (true ? ab[index - 1] : -5);
        Sum += (true ? ab[index - 1] : local_int);
        Sum += (true ? ab[index - 1] : static_field_int);
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
        Sum += (true ? ab[index - 1] : t1_i.mfi);
        Sum += (true ? ab[index - 1] : simple_func_int());
        Sum += (true ? ab[index - 1] : ab[index]);
        Sum += (true ? ab[index - 1] : ab[index - 1]);
        Sum += (false ? 3 : 3);
        Sum += (false ? 3 : -5);
        Sum += (false ? 3 : local_int);
        Sum += (false ? 3 : static_field_int);
        Sum += (false ? 3 : t1_i.mfi);
        Sum += (false ? 3 : simple_func_int());
        Sum += (false ? 3 : ab[index]);
        Sum += (false ? 3 : ab[index - 1]);
        Sum += (false ? -5 : 3);
        Sum += (false ? -5 : -5);
        Sum += (false ? -5 : local_int);
        Sum += (false ? -5 : static_field_int);
        Sum += (false ? -5 : t1_i.mfi);
        Sum += (false ? -5 : simple_func_int());
        Sum += (false ? -5 : ab[index]);
        Sum += (false ? -5 : ab[index - 1]);
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
        Sum += (false ? local_int : 3);
        Sum += (false ? local_int : -5);
        Sum += (false ? local_int : local_int);
        Sum += (false ? local_int : static_field_int);
        Sum += (false ? local_int : t1_i.mfi);
        Sum += (false ? local_int : simple_func_int());
        Sum += (false ? local_int : ab[index]);
        Sum += (false ? local_int : ab[index - 1]);
        Sum += (false ? static_field_int : 3);
        Sum += (false ? static_field_int : -5);
        Sum += (false ? static_field_int : local_int);
        Sum += (false ? static_field_int : static_field_int);
        Sum += (false ? static_field_int : t1_i.mfi);
        Sum += (false ? static_field_int : simple_func_int());
        Sum += (false ? static_field_int : ab[index]);
        Sum += (false ? static_field_int : ab[index - 1]);
        Sum += (false ? t1_i.mfi : 3);
        Sum += (false ? t1_i.mfi : -5);
        Sum += (false ? t1_i.mfi : local_int);
        Sum += (false ? t1_i.mfi : static_field_int);
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
        Sum += (false ? t1_i.mfi : t1_i.mfi);
        Sum += (false ? t1_i.mfi : simple_func_int());
        Sum += (false ? t1_i.mfi : ab[index]);
        Sum += (false ? t1_i.mfi : ab[index - 1]);
        Sum += (false ? simple_func_int() : 3);
        Sum += (false ? simple_func_int() : -5);
        Sum += (false ? simple_func_int() : local_int);
        Sum += (false ? simple_func_int() : static_field_int);
        Sum += (false ? simple_func_int() : t1_i.mfi);
        Sum += (false ? simple_func_int() : simple_func_int());
        Sum += (false ? simple_func_int() : ab[index]);
        Sum += (false ? simple_func_int() : ab[index - 1]);
        Sum += (false ? ab[index] : 3);
        Sum += (false ? ab[index] : -5);
        Sum += (false ? ab[index] : local_int);
        Sum += (false ? ab[index] : static_field_int);
        Sum += (false ? ab[index] : t1_i.mfi);
        Sum += (false ? ab[index] : simple_func_int());
        Sum += (false ? ab[index] : ab[index]);
        Sum += (false ? ab[index] : ab[index - 1]);
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
        Sum += (false ? ab[index - 1] : 3);
        Sum += (false ? ab[index - 1] : -5);
        Sum += (false ? ab[index - 1] : local_int);
        Sum += (false ? ab[index - 1] : static_field_int);
        Sum += (false ? ab[index - 1] : t1_i.mfi);
        Sum += (false ? ab[index - 1] : simple_func_int());
        Sum += (false ? ab[index - 1] : ab[index]);
        Sum += (false ? ab[index - 1] : ab[index - 1]);
        Sum += (lb_true ? 3 : 3);
        Sum += (lb_true ? 3 : -5);
        Sum += (lb_true ? 3 : local_int);
        Sum += (lb_true ? 3 : static_field_int);
        Sum += (lb_true ? 3 : t1_i.mfi);
        Sum += (lb_true ? 3 : simple_func_int());
        Sum += (lb_true ? 3 : ab[index]);
        Sum += (lb_true ? 3 : ab[index - 1]);
        Sum += (lb_true ? -5 : 3);
        Sum += (lb_true ? -5 : -5);
        Sum += (lb_true ? -5 : local_int);
        Sum += (lb_true ? -5 : static_field_int);
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
        Sum += (lb_true ? -5 : t1_i.mfi);
        Sum += (lb_true ? -5 : simple_func_int());
        Sum += (lb_true ? -5 : ab[index]);
        Sum += (lb_true ? -5 : ab[index - 1]);
        Sum += (lb_true ? local_int : 3);
        Sum += (lb_true ? local_int : -5);
        Sum += (lb_true ? local_int : local_int);
        Sum += (lb_true ? local_int : static_field_int);
        Sum += (lb_true ? local_int : t1_i.mfi);
        Sum += (lb_true ? local_int : simple_func_int());
        Sum += (lb_true ? local_int : ab[index]);
        Sum += (lb_true ? local_int : ab[index - 1]);
        Sum += (lb_true ? static_field_int : 3);
        Sum += (lb_true ? static_field_int : -5);
        Sum += (lb_true ? static_field_int : local_int);
        Sum += (lb_true ? static_field_int : static_field_int);
        Sum += (lb_true ? static_field_int : t1_i.mfi);
        Sum += (lb_true ? static_field_int : simple_func_int());
        Sum += (lb_true ? static_field_int : ab[index]);
        Sum += (lb_true ? static_field_int : ab[index - 1]);
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
        Sum += (lb_true ? t1_i.mfi : 3);
        Sum += (lb_true ? t1_i.mfi : -5);
        Sum += (lb_true ? t1_i.mfi : local_int);
        Sum += (lb_true ? t1_i.mfi : static_field_int);
        Sum += (lb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (lb_true ? t1_i.mfi : simple_func_int());
        Sum += (lb_true ? t1_i.mfi : ab[index]);
        Sum += (lb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (lb_true ? simple_func_int() : 3);
        Sum += (lb_true ? simple_func_int() : -5);
        Sum += (lb_true ? simple_func_int() : local_int);
        Sum += (lb_true ? simple_func_int() : static_field_int);
        Sum += (lb_true ? simple_func_int() : t1_i.mfi);
        Sum += (lb_true ? simple_func_int() : simple_func_int());
        Sum += (lb_true ? simple_func_int() : ab[index]);
        Sum += (lb_true ? simple_func_int() : ab[index - 1]);
        Sum += (lb_true ? ab[index] : 3);
        Sum += (lb_true ? ab[index] : -5);
        Sum += (lb_true ? ab[index] : local_int);
        Sum += (lb_true ? ab[index] : static_field_int);
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
        Sum += (lb_true ? ab[index] : t1_i.mfi);
        Sum += (lb_true ? ab[index] : simple_func_int());
        Sum += (lb_true ? ab[index] : ab[index]);
        Sum += (lb_true ? ab[index] : ab[index - 1]);
        Sum += (lb_true ? ab[index - 1] : 3);
        Sum += (lb_true ? ab[index - 1] : -5);
        Sum += (lb_true ? ab[index - 1] : local_int);
        Sum += (lb_true ? ab[index - 1] : static_field_int);
        Sum += (lb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (lb_true ? ab[index - 1] : simple_func_int());
        Sum += (lb_true ? ab[index - 1] : ab[index]);
        Sum += (lb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (lb_false ? 3 : 3);
        Sum += (lb_false ? 3 : -5);
        Sum += (lb_false ? 3 : local_int);
        Sum += (lb_false ? 3 : static_field_int);
        Sum += (lb_false ? 3 : t1_i.mfi);
        Sum += (lb_false ? 3 : simple_func_int());
        Sum += (lb_false ? 3 : ab[index]);
        Sum += (lb_false ? 3 : ab[index - 1]);
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
        Sum += (lb_false ? -5 : 3);
        Sum += (lb_false ? -5 : -5);
        Sum += (lb_false ? -5 : local_int);
        Sum += (lb_false ? -5 : static_field_int);
        Sum += (lb_false ? -5 : t1_i.mfi);
        Sum += (lb_false ? -5 : simple_func_int());
        Sum += (lb_false ? -5 : ab[index]);
        Sum += (lb_false ? -5 : ab[index - 1]);
        Sum += (lb_false ? local_int : 3);
        Sum += (lb_false ? local_int : -5);
        Sum += (lb_false ? local_int : local_int);
        Sum += (lb_false ? local_int : static_field_int);
        Sum += (lb_false ? local_int : t1_i.mfi);
        Sum += (lb_false ? local_int : simple_func_int());
        Sum += (lb_false ? local_int : ab[index]);
        Sum += (lb_false ? local_int : ab[index - 1]);
        Sum += (lb_false ? static_field_int : 3);
        Sum += (lb_false ? static_field_int : -5);
        Sum += (lb_false ? static_field_int : local_int);
        Sum += (lb_false ? static_field_int : static_field_int);
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
        Sum += (lb_false ? static_field_int : t1_i.mfi);
        Sum += (lb_false ? static_field_int : simple_func_int());
        Sum += (lb_false ? static_field_int : ab[index]);
        Sum += (lb_false ? static_field_int : ab[index - 1]);
        Sum += (lb_false ? t1_i.mfi : 3);
        Sum += (lb_false ? t1_i.mfi : -5);
        Sum += (lb_false ? t1_i.mfi : local_int);
        Sum += (lb_false ? t1_i.mfi : static_field_int);
        Sum += (lb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (lb_false ? t1_i.mfi : simple_func_int());
        Sum += (lb_false ? t1_i.mfi : ab[index]);
        Sum += (lb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (lb_false ? simple_func_int() : 3);
        Sum += (lb_false ? simple_func_int() : -5);
        Sum += (lb_false ? simple_func_int() : local_int);
        Sum += (lb_false ? simple_func_int() : static_field_int);
        Sum += (lb_false ? simple_func_int() : t1_i.mfi);
        Sum += (lb_false ? simple_func_int() : simple_func_int());
        Sum += (lb_false ? simple_func_int() : ab[index]);
        Sum += (lb_false ? simple_func_int() : ab[index - 1]);
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
        Sum += (lb_false ? ab[index] : 3);
        Sum += (lb_false ? ab[index] : -5);
        Sum += (lb_false ? ab[index] : local_int);
        Sum += (lb_false ? ab[index] : static_field_int);
        Sum += (lb_false ? ab[index] : t1_i.mfi);
        Sum += (lb_false ? ab[index] : simple_func_int());
        Sum += (lb_false ? ab[index] : ab[index]);
        Sum += (lb_false ? ab[index] : ab[index - 1]);
        Sum += (lb_false ? ab[index - 1] : 3);
        Sum += (lb_false ? ab[index - 1] : -5);
        Sum += (lb_false ? ab[index - 1] : local_int);
        Sum += (lb_false ? ab[index - 1] : static_field_int);
        Sum += (lb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (lb_false ? ab[index - 1] : simple_func_int());
        Sum += (lb_false ? ab[index - 1] : ab[index]);
        Sum += (lb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (sfb_true ? 3 : 3);
        Sum += (sfb_true ? 3 : -5);
        Sum += (sfb_true ? 3 : local_int);
        Sum += (sfb_true ? 3 : static_field_int);
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
        Sum += (sfb_true ? 3 : t1_i.mfi);
        Sum += (sfb_true ? 3 : simple_func_int());
        Sum += (sfb_true ? 3 : ab[index]);
        Sum += (sfb_true ? 3 : ab[index - 1]);
        Sum += (sfb_true ? -5 : 3);
        Sum += (sfb_true ? -5 : -5);
        Sum += (sfb_true ? -5 : local_int);
        Sum += (sfb_true ? -5 : static_field_int);
        Sum += (sfb_true ? -5 : t1_i.mfi);
        Sum += (sfb_true ? -5 : simple_func_int());
        Sum += (sfb_true ? -5 : ab[index]);
        Sum += (sfb_true ? -5 : ab[index - 1]);
        Sum += (sfb_true ? local_int : 3);
        Sum += (sfb_true ? local_int : -5);
        Sum += (sfb_true ? local_int : local_int);
        Sum += (sfb_true ? local_int : static_field_int);
        Sum += (sfb_true ? local_int : t1_i.mfi);
        Sum += (sfb_true ? local_int : simple_func_int());
        Sum += (sfb_true ? local_int : ab[index]);
        Sum += (sfb_true ? local_int : ab[index - 1]);
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
        Sum += (sfb_true ? static_field_int : 3);
        Sum += (sfb_true ? static_field_int : -5);
        Sum += (sfb_true ? static_field_int : local_int);
        Sum += (sfb_true ? static_field_int : static_field_int);
        Sum += (sfb_true ? static_field_int : t1_i.mfi);
        Sum += (sfb_true ? static_field_int : simple_func_int());
        Sum += (sfb_true ? static_field_int : ab[index]);
        Sum += (sfb_true ? static_field_int : ab[index - 1]);
        Sum += (sfb_true ? t1_i.mfi : 3);
        Sum += (sfb_true ? t1_i.mfi : -5);
        Sum += (sfb_true ? t1_i.mfi : local_int);
        Sum += (sfb_true ? t1_i.mfi : static_field_int);
        Sum += (sfb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (sfb_true ? t1_i.mfi : simple_func_int());
        Sum += (sfb_true ? t1_i.mfi : ab[index]);
        Sum += (sfb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (sfb_true ? simple_func_int() : 3);
        Sum += (sfb_true ? simple_func_int() : -5);
        Sum += (sfb_true ? simple_func_int() : local_int);
        Sum += (sfb_true ? simple_func_int() : static_field_int);
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
        Sum += (sfb_true ? simple_func_int() : t1_i.mfi);
        Sum += (sfb_true ? simple_func_int() : simple_func_int());
        Sum += (sfb_true ? simple_func_int() : ab[index]);
        Sum += (sfb_true ? simple_func_int() : ab[index - 1]);
        Sum += (sfb_true ? ab[index] : 3);
        Sum += (sfb_true ? ab[index] : -5);
        Sum += (sfb_true ? ab[index] : local_int);
        Sum += (sfb_true ? ab[index] : static_field_int);
        Sum += (sfb_true ? ab[index] : t1_i.mfi);
        Sum += (sfb_true ? ab[index] : simple_func_int());
        Sum += (sfb_true ? ab[index] : ab[index]);
        Sum += (sfb_true ? ab[index] : ab[index - 1]);
        Sum += (sfb_true ? ab[index - 1] : 3);
        Sum += (sfb_true ? ab[index - 1] : -5);
        Sum += (sfb_true ? ab[index - 1] : local_int);
        Sum += (sfb_true ? ab[index - 1] : static_field_int);
        Sum += (sfb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (sfb_true ? ab[index - 1] : simple_func_int());
        Sum += (sfb_true ? ab[index - 1] : ab[index]);
        Sum += (sfb_true ? ab[index - 1] : ab[index - 1]);
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
        Sum += (sfb_false ? 3 : 3);
        Sum += (sfb_false ? 3 : -5);
        Sum += (sfb_false ? 3 : local_int);
        Sum += (sfb_false ? 3 : static_field_int);
        Sum += (sfb_false ? 3 : t1_i.mfi);
        Sum += (sfb_false ? 3 : simple_func_int());
        Sum += (sfb_false ? 3 : ab[index]);
        Sum += (sfb_false ? 3 : ab[index - 1]);
        Sum += (sfb_false ? -5 : 3);
        Sum += (sfb_false ? -5 : -5);
        Sum += (sfb_false ? -5 : local_int);
        Sum += (sfb_false ? -5 : static_field_int);
        Sum += (sfb_false ? -5 : t1_i.mfi);
        Sum += (sfb_false ? -5 : simple_func_int());
        Sum += (sfb_false ? -5 : ab[index]);
        Sum += (sfb_false ? -5 : ab[index - 1]);
        Sum += (sfb_false ? local_int : 3);
        Sum += (sfb_false ? local_int : -5);
        Sum += (sfb_false ? local_int : local_int);
        Sum += (sfb_false ? local_int : static_field_int);
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
        Sum += (sfb_false ? local_int : t1_i.mfi);
        Sum += (sfb_false ? local_int : simple_func_int());
        Sum += (sfb_false ? local_int : ab[index]);
        Sum += (sfb_false ? local_int : ab[index - 1]);
        Sum += (sfb_false ? static_field_int : 3);
        Sum += (sfb_false ? static_field_int : -5);
        Sum += (sfb_false ? static_field_int : local_int);
        Sum += (sfb_false ? static_field_int : static_field_int);
        Sum += (sfb_false ? static_field_int : t1_i.mfi);
        Sum += (sfb_false ? static_field_int : simple_func_int());
        Sum += (sfb_false ? static_field_int : ab[index]);
        Sum += (sfb_false ? static_field_int : ab[index - 1]);
        Sum += (sfb_false ? t1_i.mfi : 3);
        Sum += (sfb_false ? t1_i.mfi : -5);
        Sum += (sfb_false ? t1_i.mfi : local_int);
        Sum += (sfb_false ? t1_i.mfi : static_field_int);
        Sum += (sfb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (sfb_false ? t1_i.mfi : simple_func_int());
        Sum += (sfb_false ? t1_i.mfi : ab[index]);
        Sum += (sfb_false ? t1_i.mfi : ab[index - 1]);
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
        Sum += (sfb_false ? simple_func_int() : 3);
        Sum += (sfb_false ? simple_func_int() : -5);
        Sum += (sfb_false ? simple_func_int() : local_int);
        Sum += (sfb_false ? simple_func_int() : static_field_int);
        Sum += (sfb_false ? simple_func_int() : t1_i.mfi);
        Sum += (sfb_false ? simple_func_int() : simple_func_int());
        Sum += (sfb_false ? simple_func_int() : ab[index]);
        Sum += (sfb_false ? simple_func_int() : ab[index - 1]);
        Sum += (sfb_false ? ab[index] : 3);
        Sum += (sfb_false ? ab[index] : -5);
        Sum += (sfb_false ? ab[index] : local_int);
        Sum += (sfb_false ? ab[index] : static_field_int);
        Sum += (sfb_false ? ab[index] : t1_i.mfi);
        Sum += (sfb_false ? ab[index] : simple_func_int());
        Sum += (sfb_false ? ab[index] : ab[index]);
        Sum += (sfb_false ? ab[index] : ab[index - 1]);
        Sum += (sfb_false ? ab[index - 1] : 3);
        Sum += (sfb_false ? ab[index - 1] : -5);
        Sum += (sfb_false ? ab[index - 1] : local_int);
        Sum += (sfb_false ? ab[index - 1] : static_field_int);
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
        Sum += (sfb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (sfb_false ? ab[index - 1] : simple_func_int());
        Sum += (sfb_false ? ab[index - 1] : ab[index]);
        Sum += (sfb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (t1_i.mfb_true ? 3 : 3);
        Sum += (t1_i.mfb_true ? 3 : -5);
        Sum += (t1_i.mfb_true ? 3 : local_int);
        Sum += (t1_i.mfb_true ? 3 : static_field_int);
        Sum += (t1_i.mfb_true ? 3 : t1_i.mfi);
        Sum += (t1_i.mfb_true ? 3 : simple_func_int());
        Sum += (t1_i.mfb_true ? 3 : ab[index]);
        Sum += (t1_i.mfb_true ? 3 : ab[index - 1]);
        Sum += (t1_i.mfb_true ? -5 : 3);
        Sum += (t1_i.mfb_true ? -5 : -5);
        Sum += (t1_i.mfb_true ? -5 : local_int);
        Sum += (t1_i.mfb_true ? -5 : static_field_int);
        Sum += (t1_i.mfb_true ? -5 : t1_i.mfi);
        Sum += (t1_i.mfb_true ? -5 : simple_func_int());
        Sum += (t1_i.mfb_true ? -5 : ab[index]);
        Sum += (t1_i.mfb_true ? -5 : ab[index - 1]);
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
        Sum += (t1_i.mfb_true ? local_int : 3);
        Sum += (t1_i.mfb_true ? local_int : -5);
        Sum += (t1_i.mfb_true ? local_int : local_int);
        Sum += (t1_i.mfb_true ? local_int : static_field_int);
        Sum += (t1_i.mfb_true ? local_int : t1_i.mfi);
        Sum += (t1_i.mfb_true ? local_int : simple_func_int());
        Sum += (t1_i.mfb_true ? local_int : ab[index]);
        Sum += (t1_i.mfb_true ? local_int : ab[index - 1]);
        Sum += (t1_i.mfb_true ? static_field_int : 3);
        Sum += (t1_i.mfb_true ? static_field_int : -5);
        Sum += (t1_i.mfb_true ? static_field_int : local_int);
        Sum += (t1_i.mfb_true ? static_field_int : static_field_int);
        Sum += (t1_i.mfb_true ? static_field_int : t1_i.mfi);
        Sum += (t1_i.mfb_true ? static_field_int : simple_func_int());
        Sum += (t1_i.mfb_true ? static_field_int : ab[index]);
        Sum += (t1_i.mfb_true ? static_field_int : ab[index - 1]);
        Sum += (t1_i.mfb_true ? t1_i.mfi : 3);
        Sum += (t1_i.mfb_true ? t1_i.mfi : -5);
        Sum += (t1_i.mfb_true ? t1_i.mfi : local_int);
        Sum += (t1_i.mfb_true ? t1_i.mfi : static_field_int);
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
        Sum += (t1_i.mfb_true ? t1_i.mfi : t1_i.mfi);
        Sum += (t1_i.mfb_true ? t1_i.mfi : simple_func_int());
        Sum += (t1_i.mfb_true ? t1_i.mfi : ab[index]);
        Sum += (t1_i.mfb_true ? t1_i.mfi : ab[index - 1]);
        Sum += (t1_i.mfb_true ? simple_func_int() : 3);
        Sum += (t1_i.mfb_true ? simple_func_int() : -5);
        Sum += (t1_i.mfb_true ? simple_func_int() : local_int);
        Sum += (t1_i.mfb_true ? simple_func_int() : static_field_int);
        Sum += (t1_i.mfb_true ? simple_func_int() : t1_i.mfi);
        Sum += (t1_i.mfb_true ? simple_func_int() : simple_func_int());
        Sum += (t1_i.mfb_true ? simple_func_int() : ab[index]);
        Sum += (t1_i.mfb_true ? simple_func_int() : ab[index - 1]);
        Sum += (t1_i.mfb_true ? ab[index] : 3);
        Sum += (t1_i.mfb_true ? ab[index] : -5);
        Sum += (t1_i.mfb_true ? ab[index] : local_int);
        Sum += (t1_i.mfb_true ? ab[index] : static_field_int);
        Sum += (t1_i.mfb_true ? ab[index] : t1_i.mfi);
        Sum += (t1_i.mfb_true ? ab[index] : simple_func_int());
        Sum += (t1_i.mfb_true ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_true ? ab[index] : ab[index - 1]);
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
        Sum += (t1_i.mfb_true ? ab[index - 1] : 3);
        Sum += (t1_i.mfb_true ? ab[index - 1] : -5);
        Sum += (t1_i.mfb_true ? ab[index - 1] : local_int);
        Sum += (t1_i.mfb_true ? ab[index - 1] : static_field_int);
        Sum += (t1_i.mfb_true ? ab[index - 1] : t1_i.mfi);
        Sum += (t1_i.mfb_true ? ab[index - 1] : simple_func_int());
        Sum += (t1_i.mfb_true ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (t1_i.mfb_false ? 3 : 3);
        Sum += (t1_i.mfb_false ? 3 : -5);
        Sum += (t1_i.mfb_false ? 3 : local_int);
        Sum += (t1_i.mfb_false ? 3 : static_field_int);
        Sum += (t1_i.mfb_false ? 3 : t1_i.mfi);
        Sum += (t1_i.mfb_false ? 3 : simple_func_int());
        Sum += (t1_i.mfb_false ? 3 : ab[index]);
        Sum += (t1_i.mfb_false ? 3 : ab[index - 1]);
        Sum += (t1_i.mfb_false ? -5 : 3);
        Sum += (t1_i.mfb_false ? -5 : -5);
        Sum += (t1_i.mfb_false ? -5 : local_int);
        Sum += (t1_i.mfb_false ? -5 : static_field_int);
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
        Sum += (t1_i.mfb_false ? -5 : t1_i.mfi);
        Sum += (t1_i.mfb_false ? -5 : simple_func_int());
        Sum += (t1_i.mfb_false ? -5 : ab[index]);
        Sum += (t1_i.mfb_false ? -5 : ab[index - 1]);
        Sum += (t1_i.mfb_false ? local_int : 3);
        Sum += (t1_i.mfb_false ? local_int : -5);
        Sum += (t1_i.mfb_false ? local_int : local_int);
        Sum += (t1_i.mfb_false ? local_int : static_field_int);
        Sum += (t1_i.mfb_false ? local_int : t1_i.mfi);
        Sum += (t1_i.mfb_false ? local_int : simple_func_int());
        Sum += (t1_i.mfb_false ? local_int : ab[index]);
        Sum += (t1_i.mfb_false ? local_int : ab[index - 1]);
        Sum += (t1_i.mfb_false ? static_field_int : 3);
        Sum += (t1_i.mfb_false ? static_field_int : -5);
        Sum += (t1_i.mfb_false ? static_field_int : local_int);
        Sum += (t1_i.mfb_false ? static_field_int : static_field_int);
        Sum += (t1_i.mfb_false ? static_field_int : t1_i.mfi);
        Sum += (t1_i.mfb_false ? static_field_int : simple_func_int());
        Sum += (t1_i.mfb_false ? static_field_int : ab[index]);
        Sum += (t1_i.mfb_false ? static_field_int : ab[index - 1]);
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
        Sum += (t1_i.mfb_false ? t1_i.mfi : 3);
        Sum += (t1_i.mfb_false ? t1_i.mfi : -5);
        Sum += (t1_i.mfb_false ? t1_i.mfi : local_int);
        Sum += (t1_i.mfb_false ? t1_i.mfi : static_field_int);
        Sum += (t1_i.mfb_false ? t1_i.mfi : t1_i.mfi);
        Sum += (t1_i.mfb_false ? t1_i.mfi : simple_func_int());
        Sum += (t1_i.mfb_false ? t1_i.mfi : ab[index]);
        Sum += (t1_i.mfb_false ? t1_i.mfi : ab[index - 1]);
        Sum += (t1_i.mfb_false ? simple_func_int() : 3);
        Sum += (t1_i.mfb_false ? simple_func_int() : -5);
        Sum += (t1_i.mfb_false ? simple_func_int() : local_int);
        Sum += (t1_i.mfb_false ? simple_func_int() : static_field_int);
        Sum += (t1_i.mfb_false ? simple_func_int() : t1_i.mfi);
        Sum += (t1_i.mfb_false ? simple_func_int() : simple_func_int());
        Sum += (t1_i.mfb_false ? simple_func_int() : ab[index]);
        Sum += (t1_i.mfb_false ? simple_func_int() : ab[index - 1]);
        Sum += (t1_i.mfb_false ? ab[index] : 3);
        Sum += (t1_i.mfb_false ? ab[index] : -5);
        Sum += (t1_i.mfb_false ? ab[index] : local_int);
        Sum += (t1_i.mfb_false ? ab[index] : static_field_int);
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
        Sum += (t1_i.mfb_false ? ab[index] : t1_i.mfi);
        Sum += (t1_i.mfb_false ? ab[index] : simple_func_int());
        Sum += (t1_i.mfb_false ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_false ? ab[index] : ab[index - 1]);
        Sum += (t1_i.mfb_false ? ab[index - 1] : 3);
        Sum += (t1_i.mfb_false ? ab[index - 1] : -5);
        Sum += (t1_i.mfb_false ? ab[index - 1] : local_int);
        Sum += (t1_i.mfb_false ? ab[index - 1] : static_field_int);
        Sum += (t1_i.mfb_false ? ab[index - 1] : t1_i.mfi);
        Sum += (t1_i.mfb_false ? ab[index - 1] : simple_func_int());
        Sum += (t1_i.mfb_false ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (func_sb_true() ? 3 : 3);
        Sum += (func_sb_true() ? 3 : -5);
        Sum += (func_sb_true() ? 3 : local_int);
        Sum += (func_sb_true() ? 3 : static_field_int);
        Sum += (func_sb_true() ? 3 : t1_i.mfi);
        Sum += (func_sb_true() ? 3 : simple_func_int());
        Sum += (func_sb_true() ? 3 : ab[index]);
        Sum += (func_sb_true() ? 3 : ab[index - 1]);
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
        Sum += (func_sb_true() ? -5 : 3);
        Sum += (func_sb_true() ? -5 : -5);
        Sum += (func_sb_true() ? -5 : local_int);
        Sum += (func_sb_true() ? -5 : static_field_int);
        Sum += (func_sb_true() ? -5 : t1_i.mfi);
        Sum += (func_sb_true() ? -5 : simple_func_int());
        Sum += (func_sb_true() ? -5 : ab[index]);
        Sum += (func_sb_true() ? -5 : ab[index - 1]);
        Sum += (func_sb_true() ? local_int : 3);
        Sum += (func_sb_true() ? local_int : -5);
        Sum += (func_sb_true() ? local_int : local_int);
        Sum += (func_sb_true() ? local_int : static_field_int);
        Sum += (func_sb_true() ? local_int : t1_i.mfi);
        Sum += (func_sb_true() ? local_int : simple_func_int());
        Sum += (func_sb_true() ? local_int : ab[index]);
        Sum += (func_sb_true() ? local_int : ab[index - 1]);
        Sum += (func_sb_true() ? static_field_int : 3);
        Sum += (func_sb_true() ? static_field_int : -5);
        Sum += (func_sb_true() ? static_field_int : local_int);
        Sum += (func_sb_true() ? static_field_int : static_field_int);
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
        Sum += (func_sb_true() ? static_field_int : t1_i.mfi);
        Sum += (func_sb_true() ? static_field_int : simple_func_int());
        Sum += (func_sb_true() ? static_field_int : ab[index]);
        Sum += (func_sb_true() ? static_field_int : ab[index - 1]);
        Sum += (func_sb_true() ? t1_i.mfi : 3);
        Sum += (func_sb_true() ? t1_i.mfi : -5);
        Sum += (func_sb_true() ? t1_i.mfi : local_int);
        Sum += (func_sb_true() ? t1_i.mfi : static_field_int);
        Sum += (func_sb_true() ? t1_i.mfi : t1_i.mfi);
        Sum += (func_sb_true() ? t1_i.mfi : simple_func_int());
        Sum += (func_sb_true() ? t1_i.mfi : ab[index]);
        Sum += (func_sb_true() ? t1_i.mfi : ab[index - 1]);
        Sum += (func_sb_true() ? simple_func_int() : 3);
        Sum += (func_sb_true() ? simple_func_int() : -5);
        Sum += (func_sb_true() ? simple_func_int() : local_int);
        Sum += (func_sb_true() ? simple_func_int() : static_field_int);
        Sum += (func_sb_true() ? simple_func_int() : t1_i.mfi);
        Sum += (func_sb_true() ? simple_func_int() : simple_func_int());
        Sum += (func_sb_true() ? simple_func_int() : ab[index]);
        Sum += (func_sb_true() ? simple_func_int() : ab[index - 1]);
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
        Sum += (func_sb_true() ? ab[index] : 3);
        Sum += (func_sb_true() ? ab[index] : -5);
        Sum += (func_sb_true() ? ab[index] : local_int);
        Sum += (func_sb_true() ? ab[index] : static_field_int);
        Sum += (func_sb_true() ? ab[index] : t1_i.mfi);
        Sum += (func_sb_true() ? ab[index] : simple_func_int());
        Sum += (func_sb_true() ? ab[index] : ab[index]);
        Sum += (func_sb_true() ? ab[index] : ab[index - 1]);
        Sum += (func_sb_true() ? ab[index - 1] : 3);
        Sum += (func_sb_true() ? ab[index - 1] : -5);
        Sum += (func_sb_true() ? ab[index - 1] : local_int);
        Sum += (func_sb_true() ? ab[index - 1] : static_field_int);
        Sum += (func_sb_true() ? ab[index - 1] : t1_i.mfi);
        Sum += (func_sb_true() ? ab[index - 1] : simple_func_int());
        Sum += (func_sb_true() ? ab[index - 1] : ab[index]);
        Sum += (func_sb_true() ? ab[index - 1] : ab[index - 1]);
        Sum += (func_sb_false() ? 3 : 3);
        Sum += (func_sb_false() ? 3 : -5);
        Sum += (func_sb_false() ? 3 : local_int);
        Sum += (func_sb_false() ? 3 : static_field_int);
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
        Sum += (func_sb_false() ? 3 : t1_i.mfi);
        Sum += (func_sb_false() ? 3 : simple_func_int());
        Sum += (func_sb_false() ? 3 : ab[index]);
        Sum += (func_sb_false() ? 3 : ab[index - 1]);
        Sum += (func_sb_false() ? -5 : 3);
        Sum += (func_sb_false() ? -5 : -5);
        Sum += (func_sb_false() ? -5 : local_int);
        Sum += (func_sb_false() ? -5 : static_field_int);
        Sum += (func_sb_false() ? -5 : t1_i.mfi);
        Sum += (func_sb_false() ? -5 : simple_func_int());
        Sum += (func_sb_false() ? -5 : ab[index]);
        Sum += (func_sb_false() ? -5 : ab[index - 1]);
        Sum += (func_sb_false() ? local_int : 3);
        Sum += (func_sb_false() ? local_int : -5);
        Sum += (func_sb_false() ? local_int : local_int);
        Sum += (func_sb_false() ? local_int : static_field_int);
        Sum += (func_sb_false() ? local_int : t1_i.mfi);
        Sum += (func_sb_false() ? local_int : simple_func_int());
        Sum += (func_sb_false() ? local_int : ab[index]);
        Sum += (func_sb_false() ? local_int : ab[index - 1]);
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
        Sum += (func_sb_false() ? static_field_int : 3);
        Sum += (func_sb_false() ? static_field_int : -5);
        Sum += (func_sb_false() ? static_field_int : local_int);
        Sum += (func_sb_false() ? static_field_int : static_field_int);
        Sum += (func_sb_false() ? static_field_int : t1_i.mfi);
        Sum += (func_sb_false() ? static_field_int : simple_func_int());
        Sum += (func_sb_false() ? static_field_int : ab[index]);
        Sum += (func_sb_false() ? static_field_int : ab[index - 1]);
        Sum += (func_sb_false() ? t1_i.mfi : 3);
        Sum += (func_sb_false() ? t1_i.mfi : -5);
        Sum += (func_sb_false() ? t1_i.mfi : local_int);
        Sum += (func_sb_false() ? t1_i.mfi : static_field_int);
        Sum += (func_sb_false() ? t1_i.mfi : t1_i.mfi);
        Sum += (func_sb_false() ? t1_i.mfi : simple_func_int());
        Sum += (func_sb_false() ? t1_i.mfi : ab[index]);
        Sum += (func_sb_false() ? t1_i.mfi : ab[index - 1]);
        Sum += (func_sb_false() ? simple_func_int() : 3);
        Sum += (func_sb_false() ? simple_func_int() : -5);
        Sum += (func_sb_false() ? simple_func_int() : local_int);
        Sum += (func_sb_false() ? simple_func_int() : static_field_int);
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
        Sum += (func_sb_false() ? simple_func_int() : t1_i.mfi);
        Sum += (func_sb_false() ? simple_func_int() : simple_func_int());
        Sum += (func_sb_false() ? simple_func_int() : ab[index]);
        Sum += (func_sb_false() ? simple_func_int() : ab[index - 1]);
        Sum += (func_sb_false() ? ab[index] : 3);
        Sum += (func_sb_false() ? ab[index] : -5);
        Sum += (func_sb_false() ? ab[index] : local_int);
        Sum += (func_sb_false() ? ab[index] : static_field_int);
        Sum += (func_sb_false() ? ab[index] : t1_i.mfi);
        Sum += (func_sb_false() ? ab[index] : simple_func_int());
        Sum += (func_sb_false() ? ab[index] : ab[index]);
        Sum += (func_sb_false() ? ab[index] : ab[index - 1]);
        Sum += (func_sb_false() ? ab[index - 1] : 3);
        Sum += (func_sb_false() ? ab[index - 1] : -5);
        Sum += (func_sb_false() ? ab[index - 1] : local_int);
        Sum += (func_sb_false() ? ab[index - 1] : static_field_int);
        Sum += (func_sb_false() ? ab[index - 1] : t1_i.mfi);
        Sum += (func_sb_false() ? ab[index - 1] : simple_func_int());
        Sum += (func_sb_false() ? ab[index - 1] : ab[index]);
        Sum += (func_sb_false() ? ab[index - 1] : ab[index - 1]);
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
        Sum += (ab_true[index] ? 3 : 3);
        Sum += (ab_true[index] ? 3 : -5);
        Sum += (ab_true[index] ? 3 : local_int);
        Sum += (ab_true[index] ? 3 : static_field_int);
        Sum += (ab_true[index] ? 3 : t1_i.mfi);
        Sum += (ab_true[index] ? 3 : simple_func_int());
        Sum += (ab_true[index] ? 3 : ab[index]);
        Sum += (ab_true[index] ? 3 : ab[index - 1]);
        Sum += (ab_true[index] ? -5 : 3);
        Sum += (ab_true[index] ? -5 : -5);
        Sum += (ab_true[index] ? -5 : local_int);
        Sum += (ab_true[index] ? -5 : static_field_int);
        Sum += (ab_true[index] ? -5 : t1_i.mfi);
        Sum += (ab_true[index] ? -5 : simple_func_int());
        Sum += (ab_true[index] ? -5 : ab[index]);
        Sum += (ab_true[index] ? -5 : ab[index - 1]);
        Sum += (ab_true[index] ? local_int : 3);
        Sum += (ab_true[index] ? local_int : -5);
        Sum += (ab_true[index] ? local_int : local_int);
        Sum += (ab_true[index] ? local_int : static_field_int);
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
        Sum += (ab_true[index] ? local_int : t1_i.mfi);
        Sum += (ab_true[index] ? local_int : simple_func_int());
        Sum += (ab_true[index] ? local_int : ab[index]);
        Sum += (ab_true[index] ? local_int : ab[index - 1]);
        Sum += (ab_true[index] ? static_field_int : 3);
        Sum += (ab_true[index] ? static_field_int : -5);
        Sum += (ab_true[index] ? static_field_int : local_int);
        Sum += (ab_true[index] ? static_field_int : static_field_int);
        Sum += (ab_true[index] ? static_field_int : t1_i.mfi);
        Sum += (ab_true[index] ? static_field_int : simple_func_int());
        Sum += (ab_true[index] ? static_field_int : ab[index]);
        Sum += (ab_true[index] ? static_field_int : ab[index - 1]);
        Sum += (ab_true[index] ? t1_i.mfi : 3);
        Sum += (ab_true[index] ? t1_i.mfi : -5);
        Sum += (ab_true[index] ? t1_i.mfi : local_int);
        Sum += (ab_true[index] ? t1_i.mfi : static_field_int);
        Sum += (ab_true[index] ? t1_i.mfi : t1_i.mfi);
        Sum += (ab_true[index] ? t1_i.mfi : simple_func_int());
        Sum += (ab_true[index] ? t1_i.mfi : ab[index]);
        Sum += (ab_true[index] ? t1_i.mfi : ab[index - 1]);
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
        Sum += (ab_true[index] ? simple_func_int() : 3);
        Sum += (ab_true[index] ? simple_func_int() : -5);
        Sum += (ab_true[index] ? simple_func_int() : local_int);
        Sum += (ab_true[index] ? simple_func_int() : static_field_int);
        Sum += (ab_true[index] ? simple_func_int() : t1_i.mfi);
        Sum += (ab_true[index] ? simple_func_int() : simple_func_int());
        Sum += (ab_true[index] ? simple_func_int() : ab[index]);
        Sum += (ab_true[index] ? simple_func_int() : ab[index - 1]);
        Sum += (ab_true[index] ? ab[index] : 3);
        Sum += (ab_true[index] ? ab[index] : -5);
        Sum += (ab_true[index] ? ab[index] : local_int);
        Sum += (ab_true[index] ? ab[index] : static_field_int);
        Sum += (ab_true[index] ? ab[index] : t1_i.mfi);
        Sum += (ab_true[index] ? ab[index] : simple_func_int());
        Sum += (ab_true[index] ? ab[index] : ab[index]);
        Sum += (ab_true[index] ? ab[index] : ab[index - 1]);
        Sum += (ab_true[index] ? ab[index - 1] : 3);
        Sum += (ab_true[index] ? ab[index - 1] : -5);
        Sum += (ab_true[index] ? ab[index - 1] : local_int);
        Sum += (ab_true[index] ? ab[index - 1] : static_field_int);
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
        Sum += (ab_true[index] ? ab[index - 1] : t1_i.mfi);
        Sum += (ab_true[index] ? ab[index - 1] : simple_func_int());
        Sum += (ab_true[index] ? ab[index - 1] : ab[index]);
        Sum += (ab_true[index] ? ab[index - 1] : ab[index - 1]);
        Sum += (ab_false[index] ? 3 : 3);
        Sum += (ab_false[index] ? 3 : -5);
        Sum += (ab_false[index] ? 3 : local_int);
        Sum += (ab_false[index] ? 3 : static_field_int);
        Sum += (ab_false[index] ? 3 : t1_i.mfi);
        Sum += (ab_false[index] ? 3 : simple_func_int());
        Sum += (ab_false[index] ? 3 : ab[index]);
        Sum += (ab_false[index] ? 3 : ab[index - 1]);
        Sum += (ab_false[index] ? -5 : 3);
        Sum += (ab_false[index] ? -5 : -5);
        Sum += (ab_false[index] ? -5 : local_int);
        Sum += (ab_false[index] ? -5 : static_field_int);
        Sum += (ab_false[index] ? -5 : t1_i.mfi);
        Sum += (ab_false[index] ? -5 : simple_func_int());
        Sum += (ab_false[index] ? -5 : ab[index]);
        Sum += (ab_false[index] ? -5 : ab[index - 1]);
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
        Sum += (ab_false[index] ? local_int : 3);
        Sum += (ab_false[index] ? local_int : -5);
        Sum += (ab_false[index] ? local_int : local_int);
        Sum += (ab_false[index] ? local_int : static_field_int);
        Sum += (ab_false[index] ? local_int : t1_i.mfi);
        Sum += (ab_false[index] ? local_int : simple_func_int());
        Sum += (ab_false[index] ? local_int : ab[index]);
        Sum += (ab_false[index] ? local_int : ab[index - 1]);
        Sum += (ab_false[index] ? static_field_int : 3);
        Sum += (ab_false[index] ? static_field_int : -5);
        Sum += (ab_false[index] ? static_field_int : local_int);
        Sum += (ab_false[index] ? static_field_int : static_field_int);
        Sum += (ab_false[index] ? static_field_int : t1_i.mfi);
        Sum += (ab_false[index] ? static_field_int : simple_func_int());
        Sum += (ab_false[index] ? static_field_int : ab[index]);
        Sum += (ab_false[index] ? static_field_int : ab[index - 1]);
        Sum += (ab_false[index] ? t1_i.mfi : 3);
        Sum += (ab_false[index] ? t1_i.mfi : -5);
        Sum += (ab_false[index] ? t1_i.mfi : local_int);
        Sum += (ab_false[index] ? t1_i.mfi : static_field_int);
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
        Sum += (ab_false[index] ? t1_i.mfi : t1_i.mfi);
        Sum += (ab_false[index] ? t1_i.mfi : simple_func_int());
        Sum += (ab_false[index] ? t1_i.mfi : ab[index]);
        Sum += (ab_false[index] ? t1_i.mfi : ab[index - 1]);
        Sum += (ab_false[index] ? simple_func_int() : 3);
        Sum += (ab_false[index] ? simple_func_int() : -5);
        Sum += (ab_false[index] ? simple_func_int() : local_int);
        Sum += (ab_false[index] ? simple_func_int() : static_field_int);
        Sum += (ab_false[index] ? simple_func_int() : t1_i.mfi);
        Sum += (ab_false[index] ? simple_func_int() : simple_func_int());
        Sum += (ab_false[index] ? simple_func_int() : ab[index]);
        Sum += (ab_false[index] ? simple_func_int() : ab[index - 1]);
        Sum += (ab_false[index] ? ab[index] : 3);
        Sum += (ab_false[index] ? ab[index] : -5);
        Sum += (ab_false[index] ? ab[index] : local_int);
        Sum += (ab_false[index] ? ab[index] : static_field_int);
        Sum += (ab_false[index] ? ab[index] : t1_i.mfi);
        Sum += (ab_false[index] ? ab[index] : simple_func_int());
        Sum += (ab_false[index] ? ab[index] : ab[index]);
        Sum += (ab_false[index] ? ab[index] : ab[index - 1]);
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
        Sum += (ab_false[index] ? ab[index - 1] : 3);
        Sum += (ab_false[index] ? ab[index - 1] : -5);
        Sum += (ab_false[index] ? ab[index - 1] : local_int);
        Sum += (ab_false[index] ? ab[index - 1] : static_field_int);
        Sum += (ab_false[index] ? ab[index - 1] : t1_i.mfi);
        Sum += (ab_false[index] ? ab[index - 1] : simple_func_int());
        Sum += (ab_false[index] ? ab[index - 1] : ab[index]);
        Sum += (ab_false[index] ? ab[index - 1] : ab[index - 1]);
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

        if (Sum == -192)
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
