// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#pragma warning disable

using System;
using Xunit;
public class testout1
{
    static double static_field_double;
    static bool sfb_false;
    static bool sfb_true;
    double mfd;
    bool mfb_false;
    bool mfb_true;
    static double simple_func_double()
    {
        return 17.2222;
    }
    static bool func_sb_true()
    {
        return true;
    }
    static bool func_sb_false()
    {
        return false;
    }

    static double Sub_Funclet_0()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ? 3.1 : 3.1);
        Sum += (true ? 3.1 : -5.31);
        Sum += (true ? 3.1 : local_double);
        Sum += (true ? 3.1 : static_field_double);
        Sum += (true ? 3.1 : t1_i.mfd);
        Sum += (true ? 3.1 : simple_func_double());
        Sum += (true ? 3.1 : ab[index]);
        Sum += (true ? 3.1 : ab[index - 1]);
        Sum += (true ? -5.31 : 3.1);
        Sum += (true ? -5.31 : -5.31);
        Sum += (true ? -5.31 : local_double);
        Sum += (true ? -5.31 : static_field_double);
        Sum += (true ? -5.31 : t1_i.mfd);
        Sum += (true ? -5.31 : simple_func_double());
        Sum += (true ? -5.31 : ab[index]);
        Sum += (true ? -5.31 : ab[index - 1]);
        Sum += (true ? local_double : 3.1);
        Sum += (true ? local_double : -5.31);
        Sum += (true ? local_double : local_double);
        Sum += (true ? local_double : static_field_double);
        return Sum;
    }
    static double Sub_Funclet_1()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ? local_double : t1_i.mfd);
        Sum += (true ? local_double : simple_func_double());
        Sum += (true ? local_double : ab[index]);
        Sum += (true ? local_double : ab[index - 1]);
        Sum += (true ? static_field_double : 3.1);
        Sum += (true ? static_field_double : -5.31);
        Sum += (true ? static_field_double : local_double);
        Sum += (true ? static_field_double : static_field_double);
        Sum += (true ? static_field_double : t1_i.mfd);
        Sum += (true ? static_field_double : simple_func_double());
        Sum += (true ? static_field_double : ab[index]);
        Sum += (true ? static_field_double : ab[index - 1]);
        Sum += (true ? t1_i.mfd : 3.1);
        Sum += (true ? t1_i.mfd : -5.31);
        Sum += (true ? t1_i.mfd : local_double);
        Sum += (true ? t1_i.mfd : static_field_double);
        Sum += (true ? t1_i.mfd : t1_i.mfd);
        Sum += (true ? t1_i.mfd : simple_func_double());
        Sum += (true ? t1_i.mfd : ab[index]);
        Sum += (true ? t1_i.mfd : ab[index - 1]);
        return Sum;
    }
    static double Sub_Funclet_2()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ? simple_func_double() : 3.1);
        Sum += (true ? simple_func_double() : -5.31);
        Sum += (true ? simple_func_double() : local_double);
        Sum += (true ? simple_func_double() : static_field_double);
        Sum += (true ? simple_func_double() : t1_i.mfd);
        Sum += (true ? simple_func_double() : simple_func_double());
        Sum += (true ? simple_func_double() : ab[index]);
        Sum += (true ? simple_func_double() : ab[index - 1]);
        Sum += (true ? ab[index] : 3.1);
        Sum += (true ? ab[index] : -5.31);
        Sum += (true ? ab[index] : local_double);
        Sum += (true ? ab[index] : static_field_double);
        Sum += (true ? ab[index] : t1_i.mfd);
        Sum += (true ? ab[index] : simple_func_double());
        Sum += (true ? ab[index] : ab[index]);
        Sum += (true ? ab[index] : ab[index - 1]);
        Sum += (true ? ab[index - 1] : 3.1);
        Sum += (true ? ab[index - 1] : -5.31);
        Sum += (true ? ab[index - 1] : local_double);
        Sum += (true ? ab[index - 1] : static_field_double);
        return Sum;
    }
    static double Sub_Funclet_3()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ? ab[index - 1] : t1_i.mfd);
        Sum += (true ? ab[index - 1] : simple_func_double());
        Sum += (true ? ab[index - 1] : ab[index]);
        Sum += (true ? ab[index - 1] : ab[index - 1]);
        Sum += (false ? 3.1 : 3.1);
        Sum += (false ? 3.1 : -5.31);
        Sum += (false ? 3.1 : local_double);
        Sum += (false ? 3.1 : static_field_double);
        Sum += (false ? 3.1 : t1_i.mfd);
        Sum += (false ? 3.1 : simple_func_double());
        Sum += (false ? 3.1 : ab[index]);
        Sum += (false ? 3.1 : ab[index - 1]);
        Sum += (false ? -5.31 : 3.1);
        Sum += (false ? -5.31 : -5.31);
        Sum += (false ? -5.31 : local_double);
        Sum += (false ? -5.31 : static_field_double);
        Sum += (false ? -5.31 : t1_i.mfd);
        Sum += (false ? -5.31 : simple_func_double());
        Sum += (false ? -5.31 : ab[index]);
        Sum += (false ? -5.31 : ab[index - 1]);
        return Sum;
    }
    static double Sub_Funclet_4()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ? local_double : 3.1);
        Sum += (false ? local_double : -5.31);
        Sum += (false ? local_double : local_double);
        Sum += (false ? local_double : static_field_double);
        Sum += (false ? local_double : t1_i.mfd);
        Sum += (false ? local_double : simple_func_double());
        Sum += (false ? local_double : ab[index]);
        Sum += (false ? local_double : ab[index - 1]);
        Sum += (false ? static_field_double : 3.1);
        Sum += (false ? static_field_double : -5.31);
        Sum += (false ? static_field_double : local_double);
        Sum += (false ? static_field_double : static_field_double);
        Sum += (false ? static_field_double : t1_i.mfd);
        Sum += (false ? static_field_double : simple_func_double());
        Sum += (false ? static_field_double : ab[index]);
        Sum += (false ? static_field_double : ab[index - 1]);
        Sum += (false ? t1_i.mfd : 3.1);
        Sum += (false ? t1_i.mfd : -5.31);
        Sum += (false ? t1_i.mfd : local_double);
        Sum += (false ? t1_i.mfd : static_field_double);
        return Sum;
    }
    static double Sub_Funclet_5()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ? t1_i.mfd : t1_i.mfd);
        Sum += (false ? t1_i.mfd : simple_func_double());
        Sum += (false ? t1_i.mfd : ab[index]);
        Sum += (false ? t1_i.mfd : ab[index - 1]);
        Sum += (false ? simple_func_double() : 3.1);
        Sum += (false ? simple_func_double() : -5.31);
        Sum += (false ? simple_func_double() : local_double);
        Sum += (false ? simple_func_double() : static_field_double);
        Sum += (false ? simple_func_double() : t1_i.mfd);
        Sum += (false ? simple_func_double() : simple_func_double());
        Sum += (false ? simple_func_double() : ab[index]);
        Sum += (false ? simple_func_double() : ab[index - 1]);
        Sum += (false ? ab[index] : 3.1);
        Sum += (false ? ab[index] : -5.31);
        Sum += (false ? ab[index] : local_double);
        Sum += (false ? ab[index] : static_field_double);
        Sum += (false ? ab[index] : t1_i.mfd);
        Sum += (false ? ab[index] : simple_func_double());
        Sum += (false ? ab[index] : ab[index]);
        Sum += (false ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static double Sub_Funclet_6()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ? ab[index - 1] : 3.1);
        Sum += (false ? ab[index - 1] : -5.31);
        Sum += (false ? ab[index - 1] : local_double);
        Sum += (false ? ab[index - 1] : static_field_double);
        Sum += (false ? ab[index - 1] : t1_i.mfd);
        Sum += (false ? ab[index - 1] : simple_func_double());
        Sum += (false ? ab[index - 1] : ab[index]);
        Sum += (false ? ab[index - 1] : ab[index - 1]);
        Sum += (lb_true ? 3.1 : 3.1);
        Sum += (lb_true ? 3.1 : -5.31);
        Sum += (lb_true ? 3.1 : local_double);
        Sum += (lb_true ? 3.1 : static_field_double);
        Sum += (lb_true ? 3.1 : t1_i.mfd);
        Sum += (lb_true ? 3.1 : simple_func_double());
        Sum += (lb_true ? 3.1 : ab[index]);
        Sum += (lb_true ? 3.1 : ab[index - 1]);
        Sum += (lb_true ? -5.31 : 3.1);
        Sum += (lb_true ? -5.31 : -5.31);
        Sum += (lb_true ? -5.31 : local_double);
        Sum += (lb_true ? -5.31 : static_field_double);
        return Sum;
    }
    static double Sub_Funclet_7()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ? -5.31 : t1_i.mfd);
        Sum += (lb_true ? -5.31 : simple_func_double());
        Sum += (lb_true ? -5.31 : ab[index]);
        Sum += (lb_true ? -5.31 : ab[index - 1]);
        Sum += (lb_true ? local_double : 3.1);
        Sum += (lb_true ? local_double : -5.31);
        Sum += (lb_true ? local_double : local_double);
        Sum += (lb_true ? local_double : static_field_double);
        Sum += (lb_true ? local_double : t1_i.mfd);
        Sum += (lb_true ? local_double : simple_func_double());
        Sum += (lb_true ? local_double : ab[index]);
        Sum += (lb_true ? local_double : ab[index - 1]);
        Sum += (lb_true ? static_field_double : 3.1);
        Sum += (lb_true ? static_field_double : -5.31);
        Sum += (lb_true ? static_field_double : local_double);
        Sum += (lb_true ? static_field_double : static_field_double);
        Sum += (lb_true ? static_field_double : t1_i.mfd);
        Sum += (lb_true ? static_field_double : simple_func_double());
        Sum += (lb_true ? static_field_double : ab[index]);
        Sum += (lb_true ? static_field_double : ab[index - 1]);
        return Sum;
    }
    static double Sub_Funclet_8()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ? t1_i.mfd : 3.1);
        Sum += (lb_true ? t1_i.mfd : -5.31);
        Sum += (lb_true ? t1_i.mfd : local_double);
        Sum += (lb_true ? t1_i.mfd : static_field_double);
        Sum += (lb_true ? t1_i.mfd : t1_i.mfd);
        Sum += (lb_true ? t1_i.mfd : simple_func_double());
        Sum += (lb_true ? t1_i.mfd : ab[index]);
        Sum += (lb_true ? t1_i.mfd : ab[index - 1]);
        Sum += (lb_true ? simple_func_double() : 3.1);
        Sum += (lb_true ? simple_func_double() : -5.31);
        Sum += (lb_true ? simple_func_double() : local_double);
        Sum += (lb_true ? simple_func_double() : static_field_double);
        Sum += (lb_true ? simple_func_double() : t1_i.mfd);
        Sum += (lb_true ? simple_func_double() : simple_func_double());
        Sum += (lb_true ? simple_func_double() : ab[index]);
        Sum += (lb_true ? simple_func_double() : ab[index - 1]);
        Sum += (lb_true ? ab[index] : 3.1);
        Sum += (lb_true ? ab[index] : -5.31);
        Sum += (lb_true ? ab[index] : local_double);
        Sum += (lb_true ? ab[index] : static_field_double);
        return Sum;
    }
    static double Sub_Funclet_9()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ? ab[index] : t1_i.mfd);
        Sum += (lb_true ? ab[index] : simple_func_double());
        Sum += (lb_true ? ab[index] : ab[index]);
        Sum += (lb_true ? ab[index] : ab[index - 1]);
        Sum += (lb_true ? ab[index - 1] : 3.1);
        Sum += (lb_true ? ab[index - 1] : -5.31);
        Sum += (lb_true ? ab[index - 1] : local_double);
        Sum += (lb_true ? ab[index - 1] : static_field_double);
        Sum += (lb_true ? ab[index - 1] : t1_i.mfd);
        Sum += (lb_true ? ab[index - 1] : simple_func_double());
        Sum += (lb_true ? ab[index - 1] : ab[index]);
        Sum += (lb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (lb_false ? 3.1 : 3.1);
        Sum += (lb_false ? 3.1 : -5.31);
        Sum += (lb_false ? 3.1 : local_double);
        Sum += (lb_false ? 3.1 : static_field_double);
        Sum += (lb_false ? 3.1 : t1_i.mfd);
        Sum += (lb_false ? 3.1 : simple_func_double());
        Sum += (lb_false ? 3.1 : ab[index]);
        Sum += (lb_false ? 3.1 : ab[index - 1]);
        return Sum;
    }
    static double Sub_Funclet_10()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ? -5.31 : 3.1);
        Sum += (lb_false ? -5.31 : -5.31);
        Sum += (lb_false ? -5.31 : local_double);
        Sum += (lb_false ? -5.31 : static_field_double);
        Sum += (lb_false ? -5.31 : t1_i.mfd);
        Sum += (lb_false ? -5.31 : simple_func_double());
        Sum += (lb_false ? -5.31 : ab[index]);
        Sum += (lb_false ? -5.31 : ab[index - 1]);
        Sum += (lb_false ? local_double : 3.1);
        Sum += (lb_false ? local_double : -5.31);
        Sum += (lb_false ? local_double : local_double);
        Sum += (lb_false ? local_double : static_field_double);
        Sum += (lb_false ? local_double : t1_i.mfd);
        Sum += (lb_false ? local_double : simple_func_double());
        Sum += (lb_false ? local_double : ab[index]);
        Sum += (lb_false ? local_double : ab[index - 1]);
        Sum += (lb_false ? static_field_double : 3.1);
        Sum += (lb_false ? static_field_double : -5.31);
        Sum += (lb_false ? static_field_double : local_double);
        Sum += (lb_false ? static_field_double : static_field_double);
        return Sum;
    }
    static double Sub_Funclet_11()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ? static_field_double : t1_i.mfd);
        Sum += (lb_false ? static_field_double : simple_func_double());
        Sum += (lb_false ? static_field_double : ab[index]);
        Sum += (lb_false ? static_field_double : ab[index - 1]);
        Sum += (lb_false ? t1_i.mfd : 3.1);
        Sum += (lb_false ? t1_i.mfd : -5.31);
        Sum += (lb_false ? t1_i.mfd : local_double);
        Sum += (lb_false ? t1_i.mfd : static_field_double);
        Sum += (lb_false ? t1_i.mfd : t1_i.mfd);
        Sum += (lb_false ? t1_i.mfd : simple_func_double());
        Sum += (lb_false ? t1_i.mfd : ab[index]);
        Sum += (lb_false ? t1_i.mfd : ab[index - 1]);
        Sum += (lb_false ? simple_func_double() : 3.1);
        Sum += (lb_false ? simple_func_double() : -5.31);
        Sum += (lb_false ? simple_func_double() : local_double);
        Sum += (lb_false ? simple_func_double() : static_field_double);
        Sum += (lb_false ? simple_func_double() : t1_i.mfd);
        Sum += (lb_false ? simple_func_double() : simple_func_double());
        Sum += (lb_false ? simple_func_double() : ab[index]);
        Sum += (lb_false ? simple_func_double() : ab[index - 1]);
        return Sum;
    }
    static double Sub_Funclet_12()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ? ab[index] : 3.1);
        Sum += (lb_false ? ab[index] : -5.31);
        Sum += (lb_false ? ab[index] : local_double);
        Sum += (lb_false ? ab[index] : static_field_double);
        Sum += (lb_false ? ab[index] : t1_i.mfd);
        Sum += (lb_false ? ab[index] : simple_func_double());
        Sum += (lb_false ? ab[index] : ab[index]);
        Sum += (lb_false ? ab[index] : ab[index - 1]);
        Sum += (lb_false ? ab[index - 1] : 3.1);
        Sum += (lb_false ? ab[index - 1] : -5.31);
        Sum += (lb_false ? ab[index - 1] : local_double);
        Sum += (lb_false ? ab[index - 1] : static_field_double);
        Sum += (lb_false ? ab[index - 1] : t1_i.mfd);
        Sum += (lb_false ? ab[index - 1] : simple_func_double());
        Sum += (lb_false ? ab[index - 1] : ab[index]);
        Sum += (lb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (sfb_true ? 3.1 : 3.1);
        Sum += (sfb_true ? 3.1 : -5.31);
        Sum += (sfb_true ? 3.1 : local_double);
        Sum += (sfb_true ? 3.1 : static_field_double);
        return Sum;
    }
    static double Sub_Funclet_13()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ? 3.1 : t1_i.mfd);
        Sum += (sfb_true ? 3.1 : simple_func_double());
        Sum += (sfb_true ? 3.1 : ab[index]);
        Sum += (sfb_true ? 3.1 : ab[index - 1]);
        Sum += (sfb_true ? -5.31 : 3.1);
        Sum += (sfb_true ? -5.31 : -5.31);
        Sum += (sfb_true ? -5.31 : local_double);
        Sum += (sfb_true ? -5.31 : static_field_double);
        Sum += (sfb_true ? -5.31 : t1_i.mfd);
        Sum += (sfb_true ? -5.31 : simple_func_double());
        Sum += (sfb_true ? -5.31 : ab[index]);
        Sum += (sfb_true ? -5.31 : ab[index - 1]);
        Sum += (sfb_true ? local_double : 3.1);
        Sum += (sfb_true ? local_double : -5.31);
        Sum += (sfb_true ? local_double : local_double);
        Sum += (sfb_true ? local_double : static_field_double);
        Sum += (sfb_true ? local_double : t1_i.mfd);
        Sum += (sfb_true ? local_double : simple_func_double());
        Sum += (sfb_true ? local_double : ab[index]);
        Sum += (sfb_true ? local_double : ab[index - 1]);
        return Sum;
    }
    static double Sub_Funclet_14()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ? static_field_double : 3.1);
        Sum += (sfb_true ? static_field_double : -5.31);
        Sum += (sfb_true ? static_field_double : local_double);
        Sum += (sfb_true ? static_field_double : static_field_double);
        Sum += (sfb_true ? static_field_double : t1_i.mfd);
        Sum += (sfb_true ? static_field_double : simple_func_double());
        Sum += (sfb_true ? static_field_double : ab[index]);
        Sum += (sfb_true ? static_field_double : ab[index - 1]);
        Sum += (sfb_true ? t1_i.mfd : 3.1);
        Sum += (sfb_true ? t1_i.mfd : -5.31);
        Sum += (sfb_true ? t1_i.mfd : local_double);
        Sum += (sfb_true ? t1_i.mfd : static_field_double);
        Sum += (sfb_true ? t1_i.mfd : t1_i.mfd);
        Sum += (sfb_true ? t1_i.mfd : simple_func_double());
        Sum += (sfb_true ? t1_i.mfd : ab[index]);
        Sum += (sfb_true ? t1_i.mfd : ab[index - 1]);
        Sum += (sfb_true ? simple_func_double() : 3.1);
        Sum += (sfb_true ? simple_func_double() : -5.31);
        Sum += (sfb_true ? simple_func_double() : local_double);
        Sum += (sfb_true ? simple_func_double() : static_field_double);
        return Sum;
    }
    static double Sub_Funclet_15()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ? simple_func_double() : t1_i.mfd);
        Sum += (sfb_true ? simple_func_double() : simple_func_double());
        Sum += (sfb_true ? simple_func_double() : ab[index]);
        Sum += (sfb_true ? simple_func_double() : ab[index - 1]);
        Sum += (sfb_true ? ab[index] : 3.1);
        Sum += (sfb_true ? ab[index] : -5.31);
        Sum += (sfb_true ? ab[index] : local_double);
        Sum += (sfb_true ? ab[index] : static_field_double);
        Sum += (sfb_true ? ab[index] : t1_i.mfd);
        Sum += (sfb_true ? ab[index] : simple_func_double());
        Sum += (sfb_true ? ab[index] : ab[index]);
        Sum += (sfb_true ? ab[index] : ab[index - 1]);
        Sum += (sfb_true ? ab[index - 1] : 3.1);
        Sum += (sfb_true ? ab[index - 1] : -5.31);
        Sum += (sfb_true ? ab[index - 1] : local_double);
        Sum += (sfb_true ? ab[index - 1] : static_field_double);
        Sum += (sfb_true ? ab[index - 1] : t1_i.mfd);
        Sum += (sfb_true ? ab[index - 1] : simple_func_double());
        Sum += (sfb_true ? ab[index - 1] : ab[index]);
        Sum += (sfb_true ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static double Sub_Funclet_16()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ? 3.1 : 3.1);
        Sum += (sfb_false ? 3.1 : -5.31);
        Sum += (sfb_false ? 3.1 : local_double);
        Sum += (sfb_false ? 3.1 : static_field_double);
        Sum += (sfb_false ? 3.1 : t1_i.mfd);
        Sum += (sfb_false ? 3.1 : simple_func_double());
        Sum += (sfb_false ? 3.1 : ab[index]);
        Sum += (sfb_false ? 3.1 : ab[index - 1]);
        Sum += (sfb_false ? -5.31 : 3.1);
        Sum += (sfb_false ? -5.31 : -5.31);
        Sum += (sfb_false ? -5.31 : local_double);
        Sum += (sfb_false ? -5.31 : static_field_double);
        Sum += (sfb_false ? -5.31 : t1_i.mfd);
        Sum += (sfb_false ? -5.31 : simple_func_double());
        Sum += (sfb_false ? -5.31 : ab[index]);
        Sum += (sfb_false ? -5.31 : ab[index - 1]);
        Sum += (sfb_false ? local_double : 3.1);
        Sum += (sfb_false ? local_double : -5.31);
        Sum += (sfb_false ? local_double : local_double);
        Sum += (sfb_false ? local_double : static_field_double);
        return Sum;
    }
    static double Sub_Funclet_17()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ? local_double : t1_i.mfd);
        Sum += (sfb_false ? local_double : simple_func_double());
        Sum += (sfb_false ? local_double : ab[index]);
        Sum += (sfb_false ? local_double : ab[index - 1]);
        Sum += (sfb_false ? static_field_double : 3.1);
        Sum += (sfb_false ? static_field_double : -5.31);
        Sum += (sfb_false ? static_field_double : local_double);
        Sum += (sfb_false ? static_field_double : static_field_double);
        Sum += (sfb_false ? static_field_double : t1_i.mfd);
        Sum += (sfb_false ? static_field_double : simple_func_double());
        Sum += (sfb_false ? static_field_double : ab[index]);
        Sum += (sfb_false ? static_field_double : ab[index - 1]);
        Sum += (sfb_false ? t1_i.mfd : 3.1);
        Sum += (sfb_false ? t1_i.mfd : -5.31);
        Sum += (sfb_false ? t1_i.mfd : local_double);
        Sum += (sfb_false ? t1_i.mfd : static_field_double);
        Sum += (sfb_false ? t1_i.mfd : t1_i.mfd);
        Sum += (sfb_false ? t1_i.mfd : simple_func_double());
        Sum += (sfb_false ? t1_i.mfd : ab[index]);
        Sum += (sfb_false ? t1_i.mfd : ab[index - 1]);
        return Sum;
    }
    static double Sub_Funclet_18()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ? simple_func_double() : 3.1);
        Sum += (sfb_false ? simple_func_double() : -5.31);
        Sum += (sfb_false ? simple_func_double() : local_double);
        Sum += (sfb_false ? simple_func_double() : static_field_double);
        Sum += (sfb_false ? simple_func_double() : t1_i.mfd);
        Sum += (sfb_false ? simple_func_double() : simple_func_double());
        Sum += (sfb_false ? simple_func_double() : ab[index]);
        Sum += (sfb_false ? simple_func_double() : ab[index - 1]);
        Sum += (sfb_false ? ab[index] : 3.1);
        Sum += (sfb_false ? ab[index] : -5.31);
        Sum += (sfb_false ? ab[index] : local_double);
        Sum += (sfb_false ? ab[index] : static_field_double);
        Sum += (sfb_false ? ab[index] : t1_i.mfd);
        Sum += (sfb_false ? ab[index] : simple_func_double());
        Sum += (sfb_false ? ab[index] : ab[index]);
        Sum += (sfb_false ? ab[index] : ab[index - 1]);
        Sum += (sfb_false ? ab[index - 1] : 3.1);
        Sum += (sfb_false ? ab[index - 1] : -5.31);
        Sum += (sfb_false ? ab[index - 1] : local_double);
        Sum += (sfb_false ? ab[index - 1] : static_field_double);
        return Sum;
    }
    static double Sub_Funclet_19()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ? ab[index - 1] : t1_i.mfd);
        Sum += (sfb_false ? ab[index - 1] : simple_func_double());
        Sum += (sfb_false ? ab[index - 1] : ab[index]);
        Sum += (sfb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (t1_i.mfb_true ? 3.1 : 3.1);
        Sum += (t1_i.mfb_true ? 3.1 : -5.31);
        Sum += (t1_i.mfb_true ? 3.1 : local_double);
        Sum += (t1_i.mfb_true ? 3.1 : static_field_double);
        Sum += (t1_i.mfb_true ? 3.1 : t1_i.mfd);
        Sum += (t1_i.mfb_true ? 3.1 : simple_func_double());
        Sum += (t1_i.mfb_true ? 3.1 : ab[index]);
        Sum += (t1_i.mfb_true ? 3.1 : ab[index - 1]);
        Sum += (t1_i.mfb_true ? -5.31 : 3.1);
        Sum += (t1_i.mfb_true ? -5.31 : -5.31);
        Sum += (t1_i.mfb_true ? -5.31 : local_double);
        Sum += (t1_i.mfb_true ? -5.31 : static_field_double);
        Sum += (t1_i.mfb_true ? -5.31 : t1_i.mfd);
        Sum += (t1_i.mfb_true ? -5.31 : simple_func_double());
        Sum += (t1_i.mfb_true ? -5.31 : ab[index]);
        Sum += (t1_i.mfb_true ? -5.31 : ab[index - 1]);
        return Sum;
    }
    static double Sub_Funclet_20()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ? local_double : 3.1);
        Sum += (t1_i.mfb_true ? local_double : -5.31);
        Sum += (t1_i.mfb_true ? local_double : local_double);
        Sum += (t1_i.mfb_true ? local_double : static_field_double);
        Sum += (t1_i.mfb_true ? local_double : t1_i.mfd);
        Sum += (t1_i.mfb_true ? local_double : simple_func_double());
        Sum += (t1_i.mfb_true ? local_double : ab[index]);
        Sum += (t1_i.mfb_true ? local_double : ab[index - 1]);
        Sum += (t1_i.mfb_true ? static_field_double : 3.1);
        Sum += (t1_i.mfb_true ? static_field_double : -5.31);
        Sum += (t1_i.mfb_true ? static_field_double : local_double);
        Sum += (t1_i.mfb_true ? static_field_double : static_field_double);
        Sum += (t1_i.mfb_true ? static_field_double : t1_i.mfd);
        Sum += (t1_i.mfb_true ? static_field_double : simple_func_double());
        Sum += (t1_i.mfb_true ? static_field_double : ab[index]);
        Sum += (t1_i.mfb_true ? static_field_double : ab[index - 1]);
        Sum += (t1_i.mfb_true ? t1_i.mfd : 3.1);
        Sum += (t1_i.mfb_true ? t1_i.mfd : -5.31);
        Sum += (t1_i.mfb_true ? t1_i.mfd : local_double);
        Sum += (t1_i.mfb_true ? t1_i.mfd : static_field_double);
        return Sum;
    }
    static double Sub_Funclet_21()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ? t1_i.mfd : t1_i.mfd);
        Sum += (t1_i.mfb_true ? t1_i.mfd : simple_func_double());
        Sum += (t1_i.mfb_true ? t1_i.mfd : ab[index]);
        Sum += (t1_i.mfb_true ? t1_i.mfd : ab[index - 1]);
        Sum += (t1_i.mfb_true ? simple_func_double() : 3.1);
        Sum += (t1_i.mfb_true ? simple_func_double() : -5.31);
        Sum += (t1_i.mfb_true ? simple_func_double() : local_double);
        Sum += (t1_i.mfb_true ? simple_func_double() : static_field_double);
        Sum += (t1_i.mfb_true ? simple_func_double() : t1_i.mfd);
        Sum += (t1_i.mfb_true ? simple_func_double() : simple_func_double());
        Sum += (t1_i.mfb_true ? simple_func_double() : ab[index]);
        Sum += (t1_i.mfb_true ? simple_func_double() : ab[index - 1]);
        Sum += (t1_i.mfb_true ? ab[index] : 3.1);
        Sum += (t1_i.mfb_true ? ab[index] : -5.31);
        Sum += (t1_i.mfb_true ? ab[index] : local_double);
        Sum += (t1_i.mfb_true ? ab[index] : static_field_double);
        Sum += (t1_i.mfb_true ? ab[index] : t1_i.mfd);
        Sum += (t1_i.mfb_true ? ab[index] : simple_func_double());
        Sum += (t1_i.mfb_true ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_true ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static double Sub_Funclet_22()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ? ab[index - 1] : 3.1);
        Sum += (t1_i.mfb_true ? ab[index - 1] : -5.31);
        Sum += (t1_i.mfb_true ? ab[index - 1] : local_double);
        Sum += (t1_i.mfb_true ? ab[index - 1] : static_field_double);
        Sum += (t1_i.mfb_true ? ab[index - 1] : t1_i.mfd);
        Sum += (t1_i.mfb_true ? ab[index - 1] : simple_func_double());
        Sum += (t1_i.mfb_true ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (t1_i.mfb_false ? 3.1 : 3.1);
        Sum += (t1_i.mfb_false ? 3.1 : -5.31);
        Sum += (t1_i.mfb_false ? 3.1 : local_double);
        Sum += (t1_i.mfb_false ? 3.1 : static_field_double);
        Sum += (t1_i.mfb_false ? 3.1 : t1_i.mfd);
        Sum += (t1_i.mfb_false ? 3.1 : simple_func_double());
        Sum += (t1_i.mfb_false ? 3.1 : ab[index]);
        Sum += (t1_i.mfb_false ? 3.1 : ab[index - 1]);
        Sum += (t1_i.mfb_false ? -5.31 : 3.1);
        Sum += (t1_i.mfb_false ? -5.31 : -5.31);
        Sum += (t1_i.mfb_false ? -5.31 : local_double);
        Sum += (t1_i.mfb_false ? -5.31 : static_field_double);
        return Sum;
    }
    static double Sub_Funclet_23()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ? -5.31 : t1_i.mfd);
        Sum += (t1_i.mfb_false ? -5.31 : simple_func_double());
        Sum += (t1_i.mfb_false ? -5.31 : ab[index]);
        Sum += (t1_i.mfb_false ? -5.31 : ab[index - 1]);
        Sum += (t1_i.mfb_false ? local_double : 3.1);
        Sum += (t1_i.mfb_false ? local_double : -5.31);
        Sum += (t1_i.mfb_false ? local_double : local_double);
        Sum += (t1_i.mfb_false ? local_double : static_field_double);
        Sum += (t1_i.mfb_false ? local_double : t1_i.mfd);
        Sum += (t1_i.mfb_false ? local_double : simple_func_double());
        Sum += (t1_i.mfb_false ? local_double : ab[index]);
        Sum += (t1_i.mfb_false ? local_double : ab[index - 1]);
        Sum += (t1_i.mfb_false ? static_field_double : 3.1);
        Sum += (t1_i.mfb_false ? static_field_double : -5.31);
        Sum += (t1_i.mfb_false ? static_field_double : local_double);
        Sum += (t1_i.mfb_false ? static_field_double : static_field_double);
        Sum += (t1_i.mfb_false ? static_field_double : t1_i.mfd);
        Sum += (t1_i.mfb_false ? static_field_double : simple_func_double());
        Sum += (t1_i.mfb_false ? static_field_double : ab[index]);
        Sum += (t1_i.mfb_false ? static_field_double : ab[index - 1]);
        return Sum;
    }
    static double Sub_Funclet_24()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ? t1_i.mfd : 3.1);
        Sum += (t1_i.mfb_false ? t1_i.mfd : -5.31);
        Sum += (t1_i.mfb_false ? t1_i.mfd : local_double);
        Sum += (t1_i.mfb_false ? t1_i.mfd : static_field_double);
        Sum += (t1_i.mfb_false ? t1_i.mfd : t1_i.mfd);
        Sum += (t1_i.mfb_false ? t1_i.mfd : simple_func_double());
        Sum += (t1_i.mfb_false ? t1_i.mfd : ab[index]);
        Sum += (t1_i.mfb_false ? t1_i.mfd : ab[index - 1]);
        Sum += (t1_i.mfb_false ? simple_func_double() : 3.1);
        Sum += (t1_i.mfb_false ? simple_func_double() : -5.31);
        Sum += (t1_i.mfb_false ? simple_func_double() : local_double);
        Sum += (t1_i.mfb_false ? simple_func_double() : static_field_double);
        Sum += (t1_i.mfb_false ? simple_func_double() : t1_i.mfd);
        Sum += (t1_i.mfb_false ? simple_func_double() : simple_func_double());
        Sum += (t1_i.mfb_false ? simple_func_double() : ab[index]);
        Sum += (t1_i.mfb_false ? simple_func_double() : ab[index - 1]);
        Sum += (t1_i.mfb_false ? ab[index] : 3.1);
        Sum += (t1_i.mfb_false ? ab[index] : -5.31);
        Sum += (t1_i.mfb_false ? ab[index] : local_double);
        Sum += (t1_i.mfb_false ? ab[index] : static_field_double);
        return Sum;
    }
    static double Sub_Funclet_25()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ? ab[index] : t1_i.mfd);
        Sum += (t1_i.mfb_false ? ab[index] : simple_func_double());
        Sum += (t1_i.mfb_false ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_false ? ab[index] : ab[index - 1]);
        Sum += (t1_i.mfb_false ? ab[index - 1] : 3.1);
        Sum += (t1_i.mfb_false ? ab[index - 1] : -5.31);
        Sum += (t1_i.mfb_false ? ab[index - 1] : local_double);
        Sum += (t1_i.mfb_false ? ab[index - 1] : static_field_double);
        Sum += (t1_i.mfb_false ? ab[index - 1] : t1_i.mfd);
        Sum += (t1_i.mfb_false ? ab[index - 1] : simple_func_double());
        Sum += (t1_i.mfb_false ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (func_sb_true() ? 3.1 : 3.1);
        Sum += (func_sb_true() ? 3.1 : -5.31);
        Sum += (func_sb_true() ? 3.1 : local_double);
        Sum += (func_sb_true() ? 3.1 : static_field_double);
        Sum += (func_sb_true() ? 3.1 : t1_i.mfd);
        Sum += (func_sb_true() ? 3.1 : simple_func_double());
        Sum += (func_sb_true() ? 3.1 : ab[index]);
        Sum += (func_sb_true() ? 3.1 : ab[index - 1]);
        return Sum;
    }
    static double Sub_Funclet_26()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ? -5.31 : 3.1);
        Sum += (func_sb_true() ? -5.31 : -5.31);
        Sum += (func_sb_true() ? -5.31 : local_double);
        Sum += (func_sb_true() ? -5.31 : static_field_double);
        Sum += (func_sb_true() ? -5.31 : t1_i.mfd);
        Sum += (func_sb_true() ? -5.31 : simple_func_double());
        Sum += (func_sb_true() ? -5.31 : ab[index]);
        Sum += (func_sb_true() ? -5.31 : ab[index - 1]);
        Sum += (func_sb_true() ? local_double : 3.1);
        Sum += (func_sb_true() ? local_double : -5.31);
        Sum += (func_sb_true() ? local_double : local_double);
        Sum += (func_sb_true() ? local_double : static_field_double);
        Sum += (func_sb_true() ? local_double : t1_i.mfd);
        Sum += (func_sb_true() ? local_double : simple_func_double());
        Sum += (func_sb_true() ? local_double : ab[index]);
        Sum += (func_sb_true() ? local_double : ab[index - 1]);
        Sum += (func_sb_true() ? static_field_double : 3.1);
        Sum += (func_sb_true() ? static_field_double : -5.31);
        Sum += (func_sb_true() ? static_field_double : local_double);
        Sum += (func_sb_true() ? static_field_double : static_field_double);
        return Sum;
    }
    static double Sub_Funclet_27()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ? static_field_double : t1_i.mfd);
        Sum += (func_sb_true() ? static_field_double : simple_func_double());
        Sum += (func_sb_true() ? static_field_double : ab[index]);
        Sum += (func_sb_true() ? static_field_double : ab[index - 1]);
        Sum += (func_sb_true() ? t1_i.mfd : 3.1);
        Sum += (func_sb_true() ? t1_i.mfd : -5.31);
        Sum += (func_sb_true() ? t1_i.mfd : local_double);
        Sum += (func_sb_true() ? t1_i.mfd : static_field_double);
        Sum += (func_sb_true() ? t1_i.mfd : t1_i.mfd);
        Sum += (func_sb_true() ? t1_i.mfd : simple_func_double());
        Sum += (func_sb_true() ? t1_i.mfd : ab[index]);
        Sum += (func_sb_true() ? t1_i.mfd : ab[index - 1]);
        Sum += (func_sb_true() ? simple_func_double() : 3.1);
        Sum += (func_sb_true() ? simple_func_double() : -5.31);
        Sum += (func_sb_true() ? simple_func_double() : local_double);
        Sum += (func_sb_true() ? simple_func_double() : static_field_double);
        Sum += (func_sb_true() ? simple_func_double() : t1_i.mfd);
        Sum += (func_sb_true() ? simple_func_double() : simple_func_double());
        Sum += (func_sb_true() ? simple_func_double() : ab[index]);
        Sum += (func_sb_true() ? simple_func_double() : ab[index - 1]);
        return Sum;
    }
    static double Sub_Funclet_28()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ? ab[index] : 3.1);
        Sum += (func_sb_true() ? ab[index] : -5.31);
        Sum += (func_sb_true() ? ab[index] : local_double);
        Sum += (func_sb_true() ? ab[index] : static_field_double);
        Sum += (func_sb_true() ? ab[index] : t1_i.mfd);
        Sum += (func_sb_true() ? ab[index] : simple_func_double());
        Sum += (func_sb_true() ? ab[index] : ab[index]);
        Sum += (func_sb_true() ? ab[index] : ab[index - 1]);
        Sum += (func_sb_true() ? ab[index - 1] : 3.1);
        Sum += (func_sb_true() ? ab[index - 1] : -5.31);
        Sum += (func_sb_true() ? ab[index - 1] : local_double);
        Sum += (func_sb_true() ? ab[index - 1] : static_field_double);
        Sum += (func_sb_true() ? ab[index - 1] : t1_i.mfd);
        Sum += (func_sb_true() ? ab[index - 1] : simple_func_double());
        Sum += (func_sb_true() ? ab[index - 1] : ab[index]);
        Sum += (func_sb_true() ? ab[index - 1] : ab[index - 1]);
        Sum += (func_sb_false() ? 3.1 : 3.1);
        Sum += (func_sb_false() ? 3.1 : -5.31);
        Sum += (func_sb_false() ? 3.1 : local_double);
        Sum += (func_sb_false() ? 3.1 : static_field_double);
        return Sum;
    }
    static double Sub_Funclet_29()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ? 3.1 : t1_i.mfd);
        Sum += (func_sb_false() ? 3.1 : simple_func_double());
        Sum += (func_sb_false() ? 3.1 : ab[index]);
        Sum += (func_sb_false() ? 3.1 : ab[index - 1]);
        Sum += (func_sb_false() ? -5.31 : 3.1);
        Sum += (func_sb_false() ? -5.31 : -5.31);
        Sum += (func_sb_false() ? -5.31 : local_double);
        Sum += (func_sb_false() ? -5.31 : static_field_double);
        Sum += (func_sb_false() ? -5.31 : t1_i.mfd);
        Sum += (func_sb_false() ? -5.31 : simple_func_double());
        Sum += (func_sb_false() ? -5.31 : ab[index]);
        Sum += (func_sb_false() ? -5.31 : ab[index - 1]);
        Sum += (func_sb_false() ? local_double : 3.1);
        Sum += (func_sb_false() ? local_double : -5.31);
        Sum += (func_sb_false() ? local_double : local_double);
        Sum += (func_sb_false() ? local_double : static_field_double);
        Sum += (func_sb_false() ? local_double : t1_i.mfd);
        Sum += (func_sb_false() ? local_double : simple_func_double());
        Sum += (func_sb_false() ? local_double : ab[index]);
        Sum += (func_sb_false() ? local_double : ab[index - 1]);
        return Sum;
    }
    static double Sub_Funclet_30()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ? static_field_double : 3.1);
        Sum += (func_sb_false() ? static_field_double : -5.31);
        Sum += (func_sb_false() ? static_field_double : local_double);
        Sum += (func_sb_false() ? static_field_double : static_field_double);
        Sum += (func_sb_false() ? static_field_double : t1_i.mfd);
        Sum += (func_sb_false() ? static_field_double : simple_func_double());
        Sum += (func_sb_false() ? static_field_double : ab[index]);
        Sum += (func_sb_false() ? static_field_double : ab[index - 1]);
        Sum += (func_sb_false() ? t1_i.mfd : 3.1);
        Sum += (func_sb_false() ? t1_i.mfd : -5.31);
        Sum += (func_sb_false() ? t1_i.mfd : local_double);
        Sum += (func_sb_false() ? t1_i.mfd : static_field_double);
        Sum += (func_sb_false() ? t1_i.mfd : t1_i.mfd);
        Sum += (func_sb_false() ? t1_i.mfd : simple_func_double());
        Sum += (func_sb_false() ? t1_i.mfd : ab[index]);
        Sum += (func_sb_false() ? t1_i.mfd : ab[index - 1]);
        Sum += (func_sb_false() ? simple_func_double() : 3.1);
        Sum += (func_sb_false() ? simple_func_double() : -5.31);
        Sum += (func_sb_false() ? simple_func_double() : local_double);
        Sum += (func_sb_false() ? simple_func_double() : static_field_double);
        return Sum;
    }
    static double Sub_Funclet_31()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ? simple_func_double() : t1_i.mfd);
        Sum += (func_sb_false() ? simple_func_double() : simple_func_double());
        Sum += (func_sb_false() ? simple_func_double() : ab[index]);
        Sum += (func_sb_false() ? simple_func_double() : ab[index - 1]);
        Sum += (func_sb_false() ? ab[index] : 3.1);
        Sum += (func_sb_false() ? ab[index] : -5.31);
        Sum += (func_sb_false() ? ab[index] : local_double);
        Sum += (func_sb_false() ? ab[index] : static_field_double);
        Sum += (func_sb_false() ? ab[index] : t1_i.mfd);
        Sum += (func_sb_false() ? ab[index] : simple_func_double());
        Sum += (func_sb_false() ? ab[index] : ab[index]);
        Sum += (func_sb_false() ? ab[index] : ab[index - 1]);
        Sum += (func_sb_false() ? ab[index - 1] : 3.1);
        Sum += (func_sb_false() ? ab[index - 1] : -5.31);
        Sum += (func_sb_false() ? ab[index - 1] : local_double);
        Sum += (func_sb_false() ? ab[index - 1] : static_field_double);
        Sum += (func_sb_false() ? ab[index - 1] : t1_i.mfd);
        Sum += (func_sb_false() ? ab[index - 1] : simple_func_double());
        Sum += (func_sb_false() ? ab[index - 1] : ab[index]);
        Sum += (func_sb_false() ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static double Sub_Funclet_32()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ? 3.1 : 3.1);
        Sum += (ab_true[index] ? 3.1 : -5.31);
        Sum += (ab_true[index] ? 3.1 : local_double);
        Sum += (ab_true[index] ? 3.1 : static_field_double);
        Sum += (ab_true[index] ? 3.1 : t1_i.mfd);
        Sum += (ab_true[index] ? 3.1 : simple_func_double());
        Sum += (ab_true[index] ? 3.1 : ab[index]);
        Sum += (ab_true[index] ? 3.1 : ab[index - 1]);
        Sum += (ab_true[index] ? -5.31 : 3.1);
        Sum += (ab_true[index] ? -5.31 : -5.31);
        Sum += (ab_true[index] ? -5.31 : local_double);
        Sum += (ab_true[index] ? -5.31 : static_field_double);
        Sum += (ab_true[index] ? -5.31 : t1_i.mfd);
        Sum += (ab_true[index] ? -5.31 : simple_func_double());
        Sum += (ab_true[index] ? -5.31 : ab[index]);
        Sum += (ab_true[index] ? -5.31 : ab[index - 1]);
        Sum += (ab_true[index] ? local_double : 3.1);
        Sum += (ab_true[index] ? local_double : -5.31);
        Sum += (ab_true[index] ? local_double : local_double);
        Sum += (ab_true[index] ? local_double : static_field_double);
        return Sum;
    }
    static double Sub_Funclet_33()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ? local_double : t1_i.mfd);
        Sum += (ab_true[index] ? local_double : simple_func_double());
        Sum += (ab_true[index] ? local_double : ab[index]);
        Sum += (ab_true[index] ? local_double : ab[index - 1]);
        Sum += (ab_true[index] ? static_field_double : 3.1);
        Sum += (ab_true[index] ? static_field_double : -5.31);
        Sum += (ab_true[index] ? static_field_double : local_double);
        Sum += (ab_true[index] ? static_field_double : static_field_double);
        Sum += (ab_true[index] ? static_field_double : t1_i.mfd);
        Sum += (ab_true[index] ? static_field_double : simple_func_double());
        Sum += (ab_true[index] ? static_field_double : ab[index]);
        Sum += (ab_true[index] ? static_field_double : ab[index - 1]);
        Sum += (ab_true[index] ? t1_i.mfd : 3.1);
        Sum += (ab_true[index] ? t1_i.mfd : -5.31);
        Sum += (ab_true[index] ? t1_i.mfd : local_double);
        Sum += (ab_true[index] ? t1_i.mfd : static_field_double);
        Sum += (ab_true[index] ? t1_i.mfd : t1_i.mfd);
        Sum += (ab_true[index] ? t1_i.mfd : simple_func_double());
        Sum += (ab_true[index] ? t1_i.mfd : ab[index]);
        Sum += (ab_true[index] ? t1_i.mfd : ab[index - 1]);
        return Sum;
    }
    static double Sub_Funclet_34()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ? simple_func_double() : 3.1);
        Sum += (ab_true[index] ? simple_func_double() : -5.31);
        Sum += (ab_true[index] ? simple_func_double() : local_double);
        Sum += (ab_true[index] ? simple_func_double() : static_field_double);
        Sum += (ab_true[index] ? simple_func_double() : t1_i.mfd);
        Sum += (ab_true[index] ? simple_func_double() : simple_func_double());
        Sum += (ab_true[index] ? simple_func_double() : ab[index]);
        Sum += (ab_true[index] ? simple_func_double() : ab[index - 1]);
        Sum += (ab_true[index] ? ab[index] : 3.1);
        Sum += (ab_true[index] ? ab[index] : -5.31);
        Sum += (ab_true[index] ? ab[index] : local_double);
        Sum += (ab_true[index] ? ab[index] : static_field_double);
        Sum += (ab_true[index] ? ab[index] : t1_i.mfd);
        Sum += (ab_true[index] ? ab[index] : simple_func_double());
        Sum += (ab_true[index] ? ab[index] : ab[index]);
        Sum += (ab_true[index] ? ab[index] : ab[index - 1]);
        Sum += (ab_true[index] ? ab[index - 1] : 3.1);
        Sum += (ab_true[index] ? ab[index - 1] : -5.31);
        Sum += (ab_true[index] ? ab[index - 1] : local_double);
        Sum += (ab_true[index] ? ab[index - 1] : static_field_double);
        return Sum;
    }
    static double Sub_Funclet_35()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ? ab[index - 1] : t1_i.mfd);
        Sum += (ab_true[index] ? ab[index - 1] : simple_func_double());
        Sum += (ab_true[index] ? ab[index - 1] : ab[index]);
        Sum += (ab_true[index] ? ab[index - 1] : ab[index - 1]);
        Sum += (ab_false[index] ? 3.1 : 3.1);
        Sum += (ab_false[index] ? 3.1 : -5.31);
        Sum += (ab_false[index] ? 3.1 : local_double);
        Sum += (ab_false[index] ? 3.1 : static_field_double);
        Sum += (ab_false[index] ? 3.1 : t1_i.mfd);
        Sum += (ab_false[index] ? 3.1 : simple_func_double());
        Sum += (ab_false[index] ? 3.1 : ab[index]);
        Sum += (ab_false[index] ? 3.1 : ab[index - 1]);
        Sum += (ab_false[index] ? -5.31 : 3.1);
        Sum += (ab_false[index] ? -5.31 : -5.31);
        Sum += (ab_false[index] ? -5.31 : local_double);
        Sum += (ab_false[index] ? -5.31 : static_field_double);
        Sum += (ab_false[index] ? -5.31 : t1_i.mfd);
        Sum += (ab_false[index] ? -5.31 : simple_func_double());
        Sum += (ab_false[index] ? -5.31 : ab[index]);
        Sum += (ab_false[index] ? -5.31 : ab[index - 1]);
        return Sum;
    }
    static double Sub_Funclet_36()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ? local_double : 3.1);
        Sum += (ab_false[index] ? local_double : -5.31);
        Sum += (ab_false[index] ? local_double : local_double);
        Sum += (ab_false[index] ? local_double : static_field_double);
        Sum += (ab_false[index] ? local_double : t1_i.mfd);
        Sum += (ab_false[index] ? local_double : simple_func_double());
        Sum += (ab_false[index] ? local_double : ab[index]);
        Sum += (ab_false[index] ? local_double : ab[index - 1]);
        Sum += (ab_false[index] ? static_field_double : 3.1);
        Sum += (ab_false[index] ? static_field_double : -5.31);
        Sum += (ab_false[index] ? static_field_double : local_double);
        Sum += (ab_false[index] ? static_field_double : static_field_double);
        Sum += (ab_false[index] ? static_field_double : t1_i.mfd);
        Sum += (ab_false[index] ? static_field_double : simple_func_double());
        Sum += (ab_false[index] ? static_field_double : ab[index]);
        Sum += (ab_false[index] ? static_field_double : ab[index - 1]);
        Sum += (ab_false[index] ? t1_i.mfd : 3.1);
        Sum += (ab_false[index] ? t1_i.mfd : -5.31);
        Sum += (ab_false[index] ? t1_i.mfd : local_double);
        Sum += (ab_false[index] ? t1_i.mfd : static_field_double);
        return Sum;
    }
    static double Sub_Funclet_37()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ? t1_i.mfd : t1_i.mfd);
        Sum += (ab_false[index] ? t1_i.mfd : simple_func_double());
        Sum += (ab_false[index] ? t1_i.mfd : ab[index]);
        Sum += (ab_false[index] ? t1_i.mfd : ab[index - 1]);
        Sum += (ab_false[index] ? simple_func_double() : 3.1);
        Sum += (ab_false[index] ? simple_func_double() : -5.31);
        Sum += (ab_false[index] ? simple_func_double() : local_double);
        Sum += (ab_false[index] ? simple_func_double() : static_field_double);
        Sum += (ab_false[index] ? simple_func_double() : t1_i.mfd);
        Sum += (ab_false[index] ? simple_func_double() : simple_func_double());
        Sum += (ab_false[index] ? simple_func_double() : ab[index]);
        Sum += (ab_false[index] ? simple_func_double() : ab[index - 1]);
        Sum += (ab_false[index] ? ab[index] : 3.1);
        Sum += (ab_false[index] ? ab[index] : -5.31);
        Sum += (ab_false[index] ? ab[index] : local_double);
        Sum += (ab_false[index] ? ab[index] : static_field_double);
        Sum += (ab_false[index] ? ab[index] : t1_i.mfd);
        Sum += (ab_false[index] ? ab[index] : simple_func_double());
        Sum += (ab_false[index] ? ab[index] : ab[index]);
        Sum += (ab_false[index] ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static double Sub_Funclet_38()
    {
        double Sum = 0.0;
        int index = 1;
        double local_double = -5.2;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        double[] ab = new double[3];
        ab[0] = 21.2;
        ab[1] = -27.645;
        ab[2] = -31.987;

        static_field_double = 7.7777;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ? ab[index - 1] : 3.1);
        Sum += (ab_false[index] ? ab[index - 1] : -5.31);
        Sum += (ab_false[index] ? ab[index - 1] : local_double);
        Sum += (ab_false[index] ? ab[index - 1] : static_field_double);
        Sum += (ab_false[index] ? ab[index - 1] : t1_i.mfd);
        Sum += (ab_false[index] ? ab[index - 1] : simple_func_double());
        Sum += (ab_false[index] ? ab[index - 1] : ab[index]);
        Sum += (ab_false[index] ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    [Fact]
    public static int TestEntryPoint()
    {
        double Sum = 0;
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

        if ((Sum > -253) && (Sum < -252))
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
