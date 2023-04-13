// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#pragma warning disable

using System;
using Xunit;
public class testout1
{
    static float static_field_float;
    static bool sfb_false;
    static bool sfb_true;
    float mfd;
    bool mfb_false;
    bool mfb_true;
    static float simple_func_float()
    {
        return 17.2222F;
    }
    static bool func_sb_true()
    {
        return true;
    }
    static bool func_sb_false()
    {
        return false;
    }

    static float Sub_Funclet_0()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ? 3.1F : 3.1F);
        Sum += (true ? 3.1F : -5.31F);
        Sum += (true ? 3.1F : local_float);
        Sum += (true ? 3.1F : static_field_float);
        Sum += (true ? 3.1F : t1_i.mfd);
        Sum += (true ? 3.1F : simple_func_float());
        Sum += (true ? 3.1F : ab[index]);
        Sum += (true ? 3.1F : ab[index - 1]);
        Sum += (true ? -5.31F : 3.1F);
        Sum += (true ? -5.31F : -5.31F);
        Sum += (true ? -5.31F : local_float);
        Sum += (true ? -5.31F : static_field_float);
        Sum += (true ? -5.31F : t1_i.mfd);
        Sum += (true ? -5.31F : simple_func_float());
        Sum += (true ? -5.31F : ab[index]);
        Sum += (true ? -5.31F : ab[index - 1]);
        Sum += (true ? local_float : 3.1F);
        Sum += (true ? local_float : -5.31F);
        Sum += (true ? local_float : local_float);
        Sum += (true ? local_float : static_field_float);
        return Sum;
    }
    static float Sub_Funclet_1()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ? local_float : t1_i.mfd);
        Sum += (true ? local_float : simple_func_float());
        Sum += (true ? local_float : ab[index]);
        Sum += (true ? local_float : ab[index - 1]);
        Sum += (true ? static_field_float : 3.1F);
        Sum += (true ? static_field_float : -5.31F);
        Sum += (true ? static_field_float : local_float);
        Sum += (true ? static_field_float : static_field_float);
        Sum += (true ? static_field_float : t1_i.mfd);
        Sum += (true ? static_field_float : simple_func_float());
        Sum += (true ? static_field_float : ab[index]);
        Sum += (true ? static_field_float : ab[index - 1]);
        Sum += (true ? t1_i.mfd : 3.1F);
        Sum += (true ? t1_i.mfd : -5.31F);
        Sum += (true ? t1_i.mfd : local_float);
        Sum += (true ? t1_i.mfd : static_field_float);
        Sum += (true ? t1_i.mfd : t1_i.mfd);
        Sum += (true ? t1_i.mfd : simple_func_float());
        Sum += (true ? t1_i.mfd : ab[index]);
        Sum += (true ? t1_i.mfd : ab[index - 1]);
        return Sum;
    }
    static float Sub_Funclet_2()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ? simple_func_float() : 3.1F);
        Sum += (true ? simple_func_float() : -5.31F);
        Sum += (true ? simple_func_float() : local_float);
        Sum += (true ? simple_func_float() : static_field_float);
        Sum += (true ? simple_func_float() : t1_i.mfd);
        Sum += (true ? simple_func_float() : simple_func_float());
        Sum += (true ? simple_func_float() : ab[index]);
        Sum += (true ? simple_func_float() : ab[index - 1]);
        Sum += (true ? ab[index] : 3.1F);
        Sum += (true ? ab[index] : -5.31F);
        Sum += (true ? ab[index] : local_float);
        Sum += (true ? ab[index] : static_field_float);
        Sum += (true ? ab[index] : t1_i.mfd);
        Sum += (true ? ab[index] : simple_func_float());
        Sum += (true ? ab[index] : ab[index]);
        Sum += (true ? ab[index] : ab[index - 1]);
        Sum += (true ? ab[index - 1] : 3.1F);
        Sum += (true ? ab[index - 1] : -5.31F);
        Sum += (true ? ab[index - 1] : local_float);
        Sum += (true ? ab[index - 1] : static_field_float);
        return Sum;
    }
    static float Sub_Funclet_3()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (true ? ab[index - 1] : t1_i.mfd);
        Sum += (true ? ab[index - 1] : simple_func_float());
        Sum += (true ? ab[index - 1] : ab[index]);
        Sum += (true ? ab[index - 1] : ab[index - 1]);
        Sum += (false ? 3.1F : 3.1F);
        Sum += (false ? 3.1F : -5.31F);
        Sum += (false ? 3.1F : local_float);
        Sum += (false ? 3.1F : static_field_float);
        Sum += (false ? 3.1F : t1_i.mfd);
        Sum += (false ? 3.1F : simple_func_float());
        Sum += (false ? 3.1F : ab[index]);
        Sum += (false ? 3.1F : ab[index - 1]);
        Sum += (false ? -5.31F : 3.1F);
        Sum += (false ? -5.31F : -5.31F);
        Sum += (false ? -5.31F : local_float);
        Sum += (false ? -5.31F : static_field_float);
        Sum += (false ? -5.31F : t1_i.mfd);
        Sum += (false ? -5.31F : simple_func_float());
        Sum += (false ? -5.31F : ab[index]);
        Sum += (false ? -5.31F : ab[index - 1]);
        return Sum;
    }
    static float Sub_Funclet_4()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ? local_float : 3.1F);
        Sum += (false ? local_float : -5.31F);
        Sum += (false ? local_float : local_float);
        Sum += (false ? local_float : static_field_float);
        Sum += (false ? local_float : t1_i.mfd);
        Sum += (false ? local_float : simple_func_float());
        Sum += (false ? local_float : ab[index]);
        Sum += (false ? local_float : ab[index - 1]);
        Sum += (false ? static_field_float : 3.1F);
        Sum += (false ? static_field_float : -5.31F);
        Sum += (false ? static_field_float : local_float);
        Sum += (false ? static_field_float : static_field_float);
        Sum += (false ? static_field_float : t1_i.mfd);
        Sum += (false ? static_field_float : simple_func_float());
        Sum += (false ? static_field_float : ab[index]);
        Sum += (false ? static_field_float : ab[index - 1]);
        Sum += (false ? t1_i.mfd : 3.1F);
        Sum += (false ? t1_i.mfd : -5.31F);
        Sum += (false ? t1_i.mfd : local_float);
        Sum += (false ? t1_i.mfd : static_field_float);
        return Sum;
    }
    static float Sub_Funclet_5()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ? t1_i.mfd : t1_i.mfd);
        Sum += (false ? t1_i.mfd : simple_func_float());
        Sum += (false ? t1_i.mfd : ab[index]);
        Sum += (false ? t1_i.mfd : ab[index - 1]);
        Sum += (false ? simple_func_float() : 3.1F);
        Sum += (false ? simple_func_float() : -5.31F);
        Sum += (false ? simple_func_float() : local_float);
        Sum += (false ? simple_func_float() : static_field_float);
        Sum += (false ? simple_func_float() : t1_i.mfd);
        Sum += (false ? simple_func_float() : simple_func_float());
        Sum += (false ? simple_func_float() : ab[index]);
        Sum += (false ? simple_func_float() : ab[index - 1]);
        Sum += (false ? ab[index] : 3.1F);
        Sum += (false ? ab[index] : -5.31F);
        Sum += (false ? ab[index] : local_float);
        Sum += (false ? ab[index] : static_field_float);
        Sum += (false ? ab[index] : t1_i.mfd);
        Sum += (false ? ab[index] : simple_func_float());
        Sum += (false ? ab[index] : ab[index]);
        Sum += (false ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static float Sub_Funclet_6()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (false ? ab[index - 1] : 3.1F);
        Sum += (false ? ab[index - 1] : -5.31F);
        Sum += (false ? ab[index - 1] : local_float);
        Sum += (false ? ab[index - 1] : static_field_float);
        Sum += (false ? ab[index - 1] : t1_i.mfd);
        Sum += (false ? ab[index - 1] : simple_func_float());
        Sum += (false ? ab[index - 1] : ab[index]);
        Sum += (false ? ab[index - 1] : ab[index - 1]);
        Sum += (lb_true ? 3.1F : 3.1F);
        Sum += (lb_true ? 3.1F : -5.31F);
        Sum += (lb_true ? 3.1F : local_float);
        Sum += (lb_true ? 3.1F : static_field_float);
        Sum += (lb_true ? 3.1F : t1_i.mfd);
        Sum += (lb_true ? 3.1F : simple_func_float());
        Sum += (lb_true ? 3.1F : ab[index]);
        Sum += (lb_true ? 3.1F : ab[index - 1]);
        Sum += (lb_true ? -5.31F : 3.1F);
        Sum += (lb_true ? -5.31F : -5.31F);
        Sum += (lb_true ? -5.31F : local_float);
        Sum += (lb_true ? -5.31F : static_field_float);
        return Sum;
    }
    static float Sub_Funclet_7()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ? -5.31F : t1_i.mfd);
        Sum += (lb_true ? -5.31F : simple_func_float());
        Sum += (lb_true ? -5.31F : ab[index]);
        Sum += (lb_true ? -5.31F : ab[index - 1]);
        Sum += (lb_true ? local_float : 3.1F);
        Sum += (lb_true ? local_float : -5.31F);
        Sum += (lb_true ? local_float : local_float);
        Sum += (lb_true ? local_float : static_field_float);
        Sum += (lb_true ? local_float : t1_i.mfd);
        Sum += (lb_true ? local_float : simple_func_float());
        Sum += (lb_true ? local_float : ab[index]);
        Sum += (lb_true ? local_float : ab[index - 1]);
        Sum += (lb_true ? static_field_float : 3.1F);
        Sum += (lb_true ? static_field_float : -5.31F);
        Sum += (lb_true ? static_field_float : local_float);
        Sum += (lb_true ? static_field_float : static_field_float);
        Sum += (lb_true ? static_field_float : t1_i.mfd);
        Sum += (lb_true ? static_field_float : simple_func_float());
        Sum += (lb_true ? static_field_float : ab[index]);
        Sum += (lb_true ? static_field_float : ab[index - 1]);
        return Sum;
    }
    static float Sub_Funclet_8()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ? t1_i.mfd : 3.1F);
        Sum += (lb_true ? t1_i.mfd : -5.31F);
        Sum += (lb_true ? t1_i.mfd : local_float);
        Sum += (lb_true ? t1_i.mfd : static_field_float);
        Sum += (lb_true ? t1_i.mfd : t1_i.mfd);
        Sum += (lb_true ? t1_i.mfd : simple_func_float());
        Sum += (lb_true ? t1_i.mfd : ab[index]);
        Sum += (lb_true ? t1_i.mfd : ab[index - 1]);
        Sum += (lb_true ? simple_func_float() : 3.1F);
        Sum += (lb_true ? simple_func_float() : -5.31F);
        Sum += (lb_true ? simple_func_float() : local_float);
        Sum += (lb_true ? simple_func_float() : static_field_float);
        Sum += (lb_true ? simple_func_float() : t1_i.mfd);
        Sum += (lb_true ? simple_func_float() : simple_func_float());
        Sum += (lb_true ? simple_func_float() : ab[index]);
        Sum += (lb_true ? simple_func_float() : ab[index - 1]);
        Sum += (lb_true ? ab[index] : 3.1F);
        Sum += (lb_true ? ab[index] : -5.31F);
        Sum += (lb_true ? ab[index] : local_float);
        Sum += (lb_true ? ab[index] : static_field_float);
        return Sum;
    }
    static float Sub_Funclet_9()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_true ? ab[index] : t1_i.mfd);
        Sum += (lb_true ? ab[index] : simple_func_float());
        Sum += (lb_true ? ab[index] : ab[index]);
        Sum += (lb_true ? ab[index] : ab[index - 1]);
        Sum += (lb_true ? ab[index - 1] : 3.1F);
        Sum += (lb_true ? ab[index - 1] : -5.31F);
        Sum += (lb_true ? ab[index - 1] : local_float);
        Sum += (lb_true ? ab[index - 1] : static_field_float);
        Sum += (lb_true ? ab[index - 1] : t1_i.mfd);
        Sum += (lb_true ? ab[index - 1] : simple_func_float());
        Sum += (lb_true ? ab[index - 1] : ab[index]);
        Sum += (lb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (lb_false ? 3.1F : 3.1F);
        Sum += (lb_false ? 3.1F : -5.31F);
        Sum += (lb_false ? 3.1F : local_float);
        Sum += (lb_false ? 3.1F : static_field_float);
        Sum += (lb_false ? 3.1F : t1_i.mfd);
        Sum += (lb_false ? 3.1F : simple_func_float());
        Sum += (lb_false ? 3.1F : ab[index]);
        Sum += (lb_false ? 3.1F : ab[index - 1]);
        return Sum;
    }
    static float Sub_Funclet_10()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ? -5.31F : 3.1F);
        Sum += (lb_false ? -5.31F : -5.31F);
        Sum += (lb_false ? -5.31F : local_float);
        Sum += (lb_false ? -5.31F : static_field_float);
        Sum += (lb_false ? -5.31F : t1_i.mfd);
        Sum += (lb_false ? -5.31F : simple_func_float());
        Sum += (lb_false ? -5.31F : ab[index]);
        Sum += (lb_false ? -5.31F : ab[index - 1]);
        Sum += (lb_false ? local_float : 3.1F);
        Sum += (lb_false ? local_float : -5.31F);
        Sum += (lb_false ? local_float : local_float);
        Sum += (lb_false ? local_float : static_field_float);
        Sum += (lb_false ? local_float : t1_i.mfd);
        Sum += (lb_false ? local_float : simple_func_float());
        Sum += (lb_false ? local_float : ab[index]);
        Sum += (lb_false ? local_float : ab[index - 1]);
        Sum += (lb_false ? static_field_float : 3.1F);
        Sum += (lb_false ? static_field_float : -5.31F);
        Sum += (lb_false ? static_field_float : local_float);
        Sum += (lb_false ? static_field_float : static_field_float);
        return Sum;
    }
    static float Sub_Funclet_11()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ? static_field_float : t1_i.mfd);
        Sum += (lb_false ? static_field_float : simple_func_float());
        Sum += (lb_false ? static_field_float : ab[index]);
        Sum += (lb_false ? static_field_float : ab[index - 1]);
        Sum += (lb_false ? t1_i.mfd : 3.1F);
        Sum += (lb_false ? t1_i.mfd : -5.31F);
        Sum += (lb_false ? t1_i.mfd : local_float);
        Sum += (lb_false ? t1_i.mfd : static_field_float);
        Sum += (lb_false ? t1_i.mfd : t1_i.mfd);
        Sum += (lb_false ? t1_i.mfd : simple_func_float());
        Sum += (lb_false ? t1_i.mfd : ab[index]);
        Sum += (lb_false ? t1_i.mfd : ab[index - 1]);
        Sum += (lb_false ? simple_func_float() : 3.1F);
        Sum += (lb_false ? simple_func_float() : -5.31F);
        Sum += (lb_false ? simple_func_float() : local_float);
        Sum += (lb_false ? simple_func_float() : static_field_float);
        Sum += (lb_false ? simple_func_float() : t1_i.mfd);
        Sum += (lb_false ? simple_func_float() : simple_func_float());
        Sum += (lb_false ? simple_func_float() : ab[index]);
        Sum += (lb_false ? simple_func_float() : ab[index - 1]);
        return Sum;
    }
    static float Sub_Funclet_12()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (lb_false ? ab[index] : 3.1F);
        Sum += (lb_false ? ab[index] : -5.31F);
        Sum += (lb_false ? ab[index] : local_float);
        Sum += (lb_false ? ab[index] : static_field_float);
        Sum += (lb_false ? ab[index] : t1_i.mfd);
        Sum += (lb_false ? ab[index] : simple_func_float());
        Sum += (lb_false ? ab[index] : ab[index]);
        Sum += (lb_false ? ab[index] : ab[index - 1]);
        Sum += (lb_false ? ab[index - 1] : 3.1F);
        Sum += (lb_false ? ab[index - 1] : -5.31F);
        Sum += (lb_false ? ab[index - 1] : local_float);
        Sum += (lb_false ? ab[index - 1] : static_field_float);
        Sum += (lb_false ? ab[index - 1] : t1_i.mfd);
        Sum += (lb_false ? ab[index - 1] : simple_func_float());
        Sum += (lb_false ? ab[index - 1] : ab[index]);
        Sum += (lb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (sfb_true ? 3.1F : 3.1F);
        Sum += (sfb_true ? 3.1F : -5.31F);
        Sum += (sfb_true ? 3.1F : local_float);
        Sum += (sfb_true ? 3.1F : static_field_float);
        return Sum;
    }
    static float Sub_Funclet_13()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ? 3.1F : t1_i.mfd);
        Sum += (sfb_true ? 3.1F : simple_func_float());
        Sum += (sfb_true ? 3.1F : ab[index]);
        Sum += (sfb_true ? 3.1F : ab[index - 1]);
        Sum += (sfb_true ? -5.31F : 3.1F);
        Sum += (sfb_true ? -5.31F : -5.31F);
        Sum += (sfb_true ? -5.31F : local_float);
        Sum += (sfb_true ? -5.31F : static_field_float);
        Sum += (sfb_true ? -5.31F : t1_i.mfd);
        Sum += (sfb_true ? -5.31F : simple_func_float());
        Sum += (sfb_true ? -5.31F : ab[index]);
        Sum += (sfb_true ? -5.31F : ab[index - 1]);
        Sum += (sfb_true ? local_float : 3.1F);
        Sum += (sfb_true ? local_float : -5.31F);
        Sum += (sfb_true ? local_float : local_float);
        Sum += (sfb_true ? local_float : static_field_float);
        Sum += (sfb_true ? local_float : t1_i.mfd);
        Sum += (sfb_true ? local_float : simple_func_float());
        Sum += (sfb_true ? local_float : ab[index]);
        Sum += (sfb_true ? local_float : ab[index - 1]);
        return Sum;
    }
    static float Sub_Funclet_14()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ? static_field_float : 3.1F);
        Sum += (sfb_true ? static_field_float : -5.31F);
        Sum += (sfb_true ? static_field_float : local_float);
        Sum += (sfb_true ? static_field_float : static_field_float);
        Sum += (sfb_true ? static_field_float : t1_i.mfd);
        Sum += (sfb_true ? static_field_float : simple_func_float());
        Sum += (sfb_true ? static_field_float : ab[index]);
        Sum += (sfb_true ? static_field_float : ab[index - 1]);
        Sum += (sfb_true ? t1_i.mfd : 3.1F);
        Sum += (sfb_true ? t1_i.mfd : -5.31F);
        Sum += (sfb_true ? t1_i.mfd : local_float);
        Sum += (sfb_true ? t1_i.mfd : static_field_float);
        Sum += (sfb_true ? t1_i.mfd : t1_i.mfd);
        Sum += (sfb_true ? t1_i.mfd : simple_func_float());
        Sum += (sfb_true ? t1_i.mfd : ab[index]);
        Sum += (sfb_true ? t1_i.mfd : ab[index - 1]);
        Sum += (sfb_true ? simple_func_float() : 3.1F);
        Sum += (sfb_true ? simple_func_float() : -5.31F);
        Sum += (sfb_true ? simple_func_float() : local_float);
        Sum += (sfb_true ? simple_func_float() : static_field_float);
        return Sum;
    }
    static float Sub_Funclet_15()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_true ? simple_func_float() : t1_i.mfd);
        Sum += (sfb_true ? simple_func_float() : simple_func_float());
        Sum += (sfb_true ? simple_func_float() : ab[index]);
        Sum += (sfb_true ? simple_func_float() : ab[index - 1]);
        Sum += (sfb_true ? ab[index] : 3.1F);
        Sum += (sfb_true ? ab[index] : -5.31F);
        Sum += (sfb_true ? ab[index] : local_float);
        Sum += (sfb_true ? ab[index] : static_field_float);
        Sum += (sfb_true ? ab[index] : t1_i.mfd);
        Sum += (sfb_true ? ab[index] : simple_func_float());
        Sum += (sfb_true ? ab[index] : ab[index]);
        Sum += (sfb_true ? ab[index] : ab[index - 1]);
        Sum += (sfb_true ? ab[index - 1] : 3.1F);
        Sum += (sfb_true ? ab[index - 1] : -5.31F);
        Sum += (sfb_true ? ab[index - 1] : local_float);
        Sum += (sfb_true ? ab[index - 1] : static_field_float);
        Sum += (sfb_true ? ab[index - 1] : t1_i.mfd);
        Sum += (sfb_true ? ab[index - 1] : simple_func_float());
        Sum += (sfb_true ? ab[index - 1] : ab[index]);
        Sum += (sfb_true ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static float Sub_Funclet_16()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ? 3.1F : 3.1F);
        Sum += (sfb_false ? 3.1F : -5.31F);
        Sum += (sfb_false ? 3.1F : local_float);
        Sum += (sfb_false ? 3.1F : static_field_float);
        Sum += (sfb_false ? 3.1F : t1_i.mfd);
        Sum += (sfb_false ? 3.1F : simple_func_float());
        Sum += (sfb_false ? 3.1F : ab[index]);
        Sum += (sfb_false ? 3.1F : ab[index - 1]);
        Sum += (sfb_false ? -5.31F : 3.1F);
        Sum += (sfb_false ? -5.31F : -5.31F);
        Sum += (sfb_false ? -5.31F : local_float);
        Sum += (sfb_false ? -5.31F : static_field_float);
        Sum += (sfb_false ? -5.31F : t1_i.mfd);
        Sum += (sfb_false ? -5.31F : simple_func_float());
        Sum += (sfb_false ? -5.31F : ab[index]);
        Sum += (sfb_false ? -5.31F : ab[index - 1]);
        Sum += (sfb_false ? local_float : 3.1F);
        Sum += (sfb_false ? local_float : -5.31F);
        Sum += (sfb_false ? local_float : local_float);
        Sum += (sfb_false ? local_float : static_field_float);
        return Sum;
    }
    static float Sub_Funclet_17()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ? local_float : t1_i.mfd);
        Sum += (sfb_false ? local_float : simple_func_float());
        Sum += (sfb_false ? local_float : ab[index]);
        Sum += (sfb_false ? local_float : ab[index - 1]);
        Sum += (sfb_false ? static_field_float : 3.1F);
        Sum += (sfb_false ? static_field_float : -5.31F);
        Sum += (sfb_false ? static_field_float : local_float);
        Sum += (sfb_false ? static_field_float : static_field_float);
        Sum += (sfb_false ? static_field_float : t1_i.mfd);
        Sum += (sfb_false ? static_field_float : simple_func_float());
        Sum += (sfb_false ? static_field_float : ab[index]);
        Sum += (sfb_false ? static_field_float : ab[index - 1]);
        Sum += (sfb_false ? t1_i.mfd : 3.1F);
        Sum += (sfb_false ? t1_i.mfd : -5.31F);
        Sum += (sfb_false ? t1_i.mfd : local_float);
        Sum += (sfb_false ? t1_i.mfd : static_field_float);
        Sum += (sfb_false ? t1_i.mfd : t1_i.mfd);
        Sum += (sfb_false ? t1_i.mfd : simple_func_float());
        Sum += (sfb_false ? t1_i.mfd : ab[index]);
        Sum += (sfb_false ? t1_i.mfd : ab[index - 1]);
        return Sum;
    }
    static float Sub_Funclet_18()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ? simple_func_float() : 3.1F);
        Sum += (sfb_false ? simple_func_float() : -5.31F);
        Sum += (sfb_false ? simple_func_float() : local_float);
        Sum += (sfb_false ? simple_func_float() : static_field_float);
        Sum += (sfb_false ? simple_func_float() : t1_i.mfd);
        Sum += (sfb_false ? simple_func_float() : simple_func_float());
        Sum += (sfb_false ? simple_func_float() : ab[index]);
        Sum += (sfb_false ? simple_func_float() : ab[index - 1]);
        Sum += (sfb_false ? ab[index] : 3.1F);
        Sum += (sfb_false ? ab[index] : -5.31F);
        Sum += (sfb_false ? ab[index] : local_float);
        Sum += (sfb_false ? ab[index] : static_field_float);
        Sum += (sfb_false ? ab[index] : t1_i.mfd);
        Sum += (sfb_false ? ab[index] : simple_func_float());
        Sum += (sfb_false ? ab[index] : ab[index]);
        Sum += (sfb_false ? ab[index] : ab[index - 1]);
        Sum += (sfb_false ? ab[index - 1] : 3.1F);
        Sum += (sfb_false ? ab[index - 1] : -5.31F);
        Sum += (sfb_false ? ab[index - 1] : local_float);
        Sum += (sfb_false ? ab[index - 1] : static_field_float);
        return Sum;
    }
    static float Sub_Funclet_19()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (sfb_false ? ab[index - 1] : t1_i.mfd);
        Sum += (sfb_false ? ab[index - 1] : simple_func_float());
        Sum += (sfb_false ? ab[index - 1] : ab[index]);
        Sum += (sfb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (t1_i.mfb_true ? 3.1F : 3.1F);
        Sum += (t1_i.mfb_true ? 3.1F : -5.31F);
        Sum += (t1_i.mfb_true ? 3.1F : local_float);
        Sum += (t1_i.mfb_true ? 3.1F : static_field_float);
        Sum += (t1_i.mfb_true ? 3.1F : t1_i.mfd);
        Sum += (t1_i.mfb_true ? 3.1F : simple_func_float());
        Sum += (t1_i.mfb_true ? 3.1F : ab[index]);
        Sum += (t1_i.mfb_true ? 3.1F : ab[index - 1]);
        Sum += (t1_i.mfb_true ? -5.31F : 3.1F);
        Sum += (t1_i.mfb_true ? -5.31F : -5.31F);
        Sum += (t1_i.mfb_true ? -5.31F : local_float);
        Sum += (t1_i.mfb_true ? -5.31F : static_field_float);
        Sum += (t1_i.mfb_true ? -5.31F : t1_i.mfd);
        Sum += (t1_i.mfb_true ? -5.31F : simple_func_float());
        Sum += (t1_i.mfb_true ? -5.31F : ab[index]);
        Sum += (t1_i.mfb_true ? -5.31F : ab[index - 1]);
        return Sum;
    }
    static float Sub_Funclet_20()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ? local_float : 3.1F);
        Sum += (t1_i.mfb_true ? local_float : -5.31F);
        Sum += (t1_i.mfb_true ? local_float : local_float);
        Sum += (t1_i.mfb_true ? local_float : static_field_float);
        Sum += (t1_i.mfb_true ? local_float : t1_i.mfd);
        Sum += (t1_i.mfb_true ? local_float : simple_func_float());
        Sum += (t1_i.mfb_true ? local_float : ab[index]);
        Sum += (t1_i.mfb_true ? local_float : ab[index - 1]);
        Sum += (t1_i.mfb_true ? static_field_float : 3.1F);
        Sum += (t1_i.mfb_true ? static_field_float : -5.31F);
        Sum += (t1_i.mfb_true ? static_field_float : local_float);
        Sum += (t1_i.mfb_true ? static_field_float : static_field_float);
        Sum += (t1_i.mfb_true ? static_field_float : t1_i.mfd);
        Sum += (t1_i.mfb_true ? static_field_float : simple_func_float());
        Sum += (t1_i.mfb_true ? static_field_float : ab[index]);
        Sum += (t1_i.mfb_true ? static_field_float : ab[index - 1]);
        Sum += (t1_i.mfb_true ? t1_i.mfd : 3.1F);
        Sum += (t1_i.mfb_true ? t1_i.mfd : -5.31F);
        Sum += (t1_i.mfb_true ? t1_i.mfd : local_float);
        Sum += (t1_i.mfb_true ? t1_i.mfd : static_field_float);
        return Sum;
    }
    static float Sub_Funclet_21()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ? t1_i.mfd : t1_i.mfd);
        Sum += (t1_i.mfb_true ? t1_i.mfd : simple_func_float());
        Sum += (t1_i.mfb_true ? t1_i.mfd : ab[index]);
        Sum += (t1_i.mfb_true ? t1_i.mfd : ab[index - 1]);
        Sum += (t1_i.mfb_true ? simple_func_float() : 3.1F);
        Sum += (t1_i.mfb_true ? simple_func_float() : -5.31F);
        Sum += (t1_i.mfb_true ? simple_func_float() : local_float);
        Sum += (t1_i.mfb_true ? simple_func_float() : static_field_float);
        Sum += (t1_i.mfb_true ? simple_func_float() : t1_i.mfd);
        Sum += (t1_i.mfb_true ? simple_func_float() : simple_func_float());
        Sum += (t1_i.mfb_true ? simple_func_float() : ab[index]);
        Sum += (t1_i.mfb_true ? simple_func_float() : ab[index - 1]);
        Sum += (t1_i.mfb_true ? ab[index] : 3.1F);
        Sum += (t1_i.mfb_true ? ab[index] : -5.31F);
        Sum += (t1_i.mfb_true ? ab[index] : local_float);
        Sum += (t1_i.mfb_true ? ab[index] : static_field_float);
        Sum += (t1_i.mfb_true ? ab[index] : t1_i.mfd);
        Sum += (t1_i.mfb_true ? ab[index] : simple_func_float());
        Sum += (t1_i.mfb_true ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_true ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static float Sub_Funclet_22()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_true ? ab[index - 1] : 3.1F);
        Sum += (t1_i.mfb_true ? ab[index - 1] : -5.31F);
        Sum += (t1_i.mfb_true ? ab[index - 1] : local_float);
        Sum += (t1_i.mfb_true ? ab[index - 1] : static_field_float);
        Sum += (t1_i.mfb_true ? ab[index - 1] : t1_i.mfd);
        Sum += (t1_i.mfb_true ? ab[index - 1] : simple_func_float());
        Sum += (t1_i.mfb_true ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_true ? ab[index - 1] : ab[index - 1]);
        Sum += (t1_i.mfb_false ? 3.1F : 3.1F);
        Sum += (t1_i.mfb_false ? 3.1F : -5.31F);
        Sum += (t1_i.mfb_false ? 3.1F : local_float);
        Sum += (t1_i.mfb_false ? 3.1F : static_field_float);
        Sum += (t1_i.mfb_false ? 3.1F : t1_i.mfd);
        Sum += (t1_i.mfb_false ? 3.1F : simple_func_float());
        Sum += (t1_i.mfb_false ? 3.1F : ab[index]);
        Sum += (t1_i.mfb_false ? 3.1F : ab[index - 1]);
        Sum += (t1_i.mfb_false ? -5.31F : 3.1F);
        Sum += (t1_i.mfb_false ? -5.31F : -5.31F);
        Sum += (t1_i.mfb_false ? -5.31F : local_float);
        Sum += (t1_i.mfb_false ? -5.31F : static_field_float);
        return Sum;
    }
    static float Sub_Funclet_23()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ? -5.31F : t1_i.mfd);
        Sum += (t1_i.mfb_false ? -5.31F : simple_func_float());
        Sum += (t1_i.mfb_false ? -5.31F : ab[index]);
        Sum += (t1_i.mfb_false ? -5.31F : ab[index - 1]);
        Sum += (t1_i.mfb_false ? local_float : 3.1F);
        Sum += (t1_i.mfb_false ? local_float : -5.31F);
        Sum += (t1_i.mfb_false ? local_float : local_float);
        Sum += (t1_i.mfb_false ? local_float : static_field_float);
        Sum += (t1_i.mfb_false ? local_float : t1_i.mfd);
        Sum += (t1_i.mfb_false ? local_float : simple_func_float());
        Sum += (t1_i.mfb_false ? local_float : ab[index]);
        Sum += (t1_i.mfb_false ? local_float : ab[index - 1]);
        Sum += (t1_i.mfb_false ? static_field_float : 3.1F);
        Sum += (t1_i.mfb_false ? static_field_float : -5.31F);
        Sum += (t1_i.mfb_false ? static_field_float : local_float);
        Sum += (t1_i.mfb_false ? static_field_float : static_field_float);
        Sum += (t1_i.mfb_false ? static_field_float : t1_i.mfd);
        Sum += (t1_i.mfb_false ? static_field_float : simple_func_float());
        Sum += (t1_i.mfb_false ? static_field_float : ab[index]);
        Sum += (t1_i.mfb_false ? static_field_float : ab[index - 1]);
        return Sum;
    }
    static float Sub_Funclet_24()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ? t1_i.mfd : 3.1F);
        Sum += (t1_i.mfb_false ? t1_i.mfd : -5.31F);
        Sum += (t1_i.mfb_false ? t1_i.mfd : local_float);
        Sum += (t1_i.mfb_false ? t1_i.mfd : static_field_float);
        Sum += (t1_i.mfb_false ? t1_i.mfd : t1_i.mfd);
        Sum += (t1_i.mfb_false ? t1_i.mfd : simple_func_float());
        Sum += (t1_i.mfb_false ? t1_i.mfd : ab[index]);
        Sum += (t1_i.mfb_false ? t1_i.mfd : ab[index - 1]);
        Sum += (t1_i.mfb_false ? simple_func_float() : 3.1F);
        Sum += (t1_i.mfb_false ? simple_func_float() : -5.31F);
        Sum += (t1_i.mfb_false ? simple_func_float() : local_float);
        Sum += (t1_i.mfb_false ? simple_func_float() : static_field_float);
        Sum += (t1_i.mfb_false ? simple_func_float() : t1_i.mfd);
        Sum += (t1_i.mfb_false ? simple_func_float() : simple_func_float());
        Sum += (t1_i.mfb_false ? simple_func_float() : ab[index]);
        Sum += (t1_i.mfb_false ? simple_func_float() : ab[index - 1]);
        Sum += (t1_i.mfb_false ? ab[index] : 3.1F);
        Sum += (t1_i.mfb_false ? ab[index] : -5.31F);
        Sum += (t1_i.mfb_false ? ab[index] : local_float);
        Sum += (t1_i.mfb_false ? ab[index] : static_field_float);
        return Sum;
    }
    static float Sub_Funclet_25()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (t1_i.mfb_false ? ab[index] : t1_i.mfd);
        Sum += (t1_i.mfb_false ? ab[index] : simple_func_float());
        Sum += (t1_i.mfb_false ? ab[index] : ab[index]);
        Sum += (t1_i.mfb_false ? ab[index] : ab[index - 1]);
        Sum += (t1_i.mfb_false ? ab[index - 1] : 3.1F);
        Sum += (t1_i.mfb_false ? ab[index - 1] : -5.31F);
        Sum += (t1_i.mfb_false ? ab[index - 1] : local_float);
        Sum += (t1_i.mfb_false ? ab[index - 1] : static_field_float);
        Sum += (t1_i.mfb_false ? ab[index - 1] : t1_i.mfd);
        Sum += (t1_i.mfb_false ? ab[index - 1] : simple_func_float());
        Sum += (t1_i.mfb_false ? ab[index - 1] : ab[index]);
        Sum += (t1_i.mfb_false ? ab[index - 1] : ab[index - 1]);
        Sum += (func_sb_true() ? 3.1F : 3.1F);
        Sum += (func_sb_true() ? 3.1F : -5.31F);
        Sum += (func_sb_true() ? 3.1F : local_float);
        Sum += (func_sb_true() ? 3.1F : static_field_float);
        Sum += (func_sb_true() ? 3.1F : t1_i.mfd);
        Sum += (func_sb_true() ? 3.1F : simple_func_float());
        Sum += (func_sb_true() ? 3.1F : ab[index]);
        Sum += (func_sb_true() ? 3.1F : ab[index - 1]);
        return Sum;
    }
    static float Sub_Funclet_26()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ? -5.31F : 3.1F);
        Sum += (func_sb_true() ? -5.31F : -5.31F);
        Sum += (func_sb_true() ? -5.31F : local_float);
        Sum += (func_sb_true() ? -5.31F : static_field_float);
        Sum += (func_sb_true() ? -5.31F : t1_i.mfd);
        Sum += (func_sb_true() ? -5.31F : simple_func_float());
        Sum += (func_sb_true() ? -5.31F : ab[index]);
        Sum += (func_sb_true() ? -5.31F : ab[index - 1]);
        Sum += (func_sb_true() ? local_float : 3.1F);
        Sum += (func_sb_true() ? local_float : -5.31F);
        Sum += (func_sb_true() ? local_float : local_float);
        Sum += (func_sb_true() ? local_float : static_field_float);
        Sum += (func_sb_true() ? local_float : t1_i.mfd);
        Sum += (func_sb_true() ? local_float : simple_func_float());
        Sum += (func_sb_true() ? local_float : ab[index]);
        Sum += (func_sb_true() ? local_float : ab[index - 1]);
        Sum += (func_sb_true() ? static_field_float : 3.1F);
        Sum += (func_sb_true() ? static_field_float : -5.31F);
        Sum += (func_sb_true() ? static_field_float : local_float);
        Sum += (func_sb_true() ? static_field_float : static_field_float);
        return Sum;
    }
    static float Sub_Funclet_27()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ? static_field_float : t1_i.mfd);
        Sum += (func_sb_true() ? static_field_float : simple_func_float());
        Sum += (func_sb_true() ? static_field_float : ab[index]);
        Sum += (func_sb_true() ? static_field_float : ab[index - 1]);
        Sum += (func_sb_true() ? t1_i.mfd : 3.1F);
        Sum += (func_sb_true() ? t1_i.mfd : -5.31F);
        Sum += (func_sb_true() ? t1_i.mfd : local_float);
        Sum += (func_sb_true() ? t1_i.mfd : static_field_float);
        Sum += (func_sb_true() ? t1_i.mfd : t1_i.mfd);
        Sum += (func_sb_true() ? t1_i.mfd : simple_func_float());
        Sum += (func_sb_true() ? t1_i.mfd : ab[index]);
        Sum += (func_sb_true() ? t1_i.mfd : ab[index - 1]);
        Sum += (func_sb_true() ? simple_func_float() : 3.1F);
        Sum += (func_sb_true() ? simple_func_float() : -5.31F);
        Sum += (func_sb_true() ? simple_func_float() : local_float);
        Sum += (func_sb_true() ? simple_func_float() : static_field_float);
        Sum += (func_sb_true() ? simple_func_float() : t1_i.mfd);
        Sum += (func_sb_true() ? simple_func_float() : simple_func_float());
        Sum += (func_sb_true() ? simple_func_float() : ab[index]);
        Sum += (func_sb_true() ? simple_func_float() : ab[index - 1]);
        return Sum;
    }
    static float Sub_Funclet_28()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_true() ? ab[index] : 3.1F);
        Sum += (func_sb_true() ? ab[index] : -5.31F);
        Sum += (func_sb_true() ? ab[index] : local_float);
        Sum += (func_sb_true() ? ab[index] : static_field_float);
        Sum += (func_sb_true() ? ab[index] : t1_i.mfd);
        Sum += (func_sb_true() ? ab[index] : simple_func_float());
        Sum += (func_sb_true() ? ab[index] : ab[index]);
        Sum += (func_sb_true() ? ab[index] : ab[index - 1]);
        Sum += (func_sb_true() ? ab[index - 1] : 3.1F);
        Sum += (func_sb_true() ? ab[index - 1] : -5.31F);
        Sum += (func_sb_true() ? ab[index - 1] : local_float);
        Sum += (func_sb_true() ? ab[index - 1] : static_field_float);
        Sum += (func_sb_true() ? ab[index - 1] : t1_i.mfd);
        Sum += (func_sb_true() ? ab[index - 1] : simple_func_float());
        Sum += (func_sb_true() ? ab[index - 1] : ab[index]);
        Sum += (func_sb_true() ? ab[index - 1] : ab[index - 1]);
        Sum += (func_sb_false() ? 3.1F : 3.1F);
        Sum += (func_sb_false() ? 3.1F : -5.31F);
        Sum += (func_sb_false() ? 3.1F : local_float);
        Sum += (func_sb_false() ? 3.1F : static_field_float);
        return Sum;
    }
    static float Sub_Funclet_29()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ? 3.1F : t1_i.mfd);
        Sum += (func_sb_false() ? 3.1F : simple_func_float());
        Sum += (func_sb_false() ? 3.1F : ab[index]);
        Sum += (func_sb_false() ? 3.1F : ab[index - 1]);
        Sum += (func_sb_false() ? -5.31F : 3.1F);
        Sum += (func_sb_false() ? -5.31F : -5.31F);
        Sum += (func_sb_false() ? -5.31F : local_float);
        Sum += (func_sb_false() ? -5.31F : static_field_float);
        Sum += (func_sb_false() ? -5.31F : t1_i.mfd);
        Sum += (func_sb_false() ? -5.31F : simple_func_float());
        Sum += (func_sb_false() ? -5.31F : ab[index]);
        Sum += (func_sb_false() ? -5.31F : ab[index - 1]);
        Sum += (func_sb_false() ? local_float : 3.1F);
        Sum += (func_sb_false() ? local_float : -5.31F);
        Sum += (func_sb_false() ? local_float : local_float);
        Sum += (func_sb_false() ? local_float : static_field_float);
        Sum += (func_sb_false() ? local_float : t1_i.mfd);
        Sum += (func_sb_false() ? local_float : simple_func_float());
        Sum += (func_sb_false() ? local_float : ab[index]);
        Sum += (func_sb_false() ? local_float : ab[index - 1]);
        return Sum;
    }
    static float Sub_Funclet_30()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ? static_field_float : 3.1F);
        Sum += (func_sb_false() ? static_field_float : -5.31F);
        Sum += (func_sb_false() ? static_field_float : local_float);
        Sum += (func_sb_false() ? static_field_float : static_field_float);
        Sum += (func_sb_false() ? static_field_float : t1_i.mfd);
        Sum += (func_sb_false() ? static_field_float : simple_func_float());
        Sum += (func_sb_false() ? static_field_float : ab[index]);
        Sum += (func_sb_false() ? static_field_float : ab[index - 1]);
        Sum += (func_sb_false() ? t1_i.mfd : 3.1F);
        Sum += (func_sb_false() ? t1_i.mfd : -5.31F);
        Sum += (func_sb_false() ? t1_i.mfd : local_float);
        Sum += (func_sb_false() ? t1_i.mfd : static_field_float);
        Sum += (func_sb_false() ? t1_i.mfd : t1_i.mfd);
        Sum += (func_sb_false() ? t1_i.mfd : simple_func_float());
        Sum += (func_sb_false() ? t1_i.mfd : ab[index]);
        Sum += (func_sb_false() ? t1_i.mfd : ab[index - 1]);
        Sum += (func_sb_false() ? simple_func_float() : 3.1F);
        Sum += (func_sb_false() ? simple_func_float() : -5.31F);
        Sum += (func_sb_false() ? simple_func_float() : local_float);
        Sum += (func_sb_false() ? simple_func_float() : static_field_float);
        return Sum;
    }
    static float Sub_Funclet_31()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (func_sb_false() ? simple_func_float() : t1_i.mfd);
        Sum += (func_sb_false() ? simple_func_float() : simple_func_float());
        Sum += (func_sb_false() ? simple_func_float() : ab[index]);
        Sum += (func_sb_false() ? simple_func_float() : ab[index - 1]);
        Sum += (func_sb_false() ? ab[index] : 3.1F);
        Sum += (func_sb_false() ? ab[index] : -5.31F);
        Sum += (func_sb_false() ? ab[index] : local_float);
        Sum += (func_sb_false() ? ab[index] : static_field_float);
        Sum += (func_sb_false() ? ab[index] : t1_i.mfd);
        Sum += (func_sb_false() ? ab[index] : simple_func_float());
        Sum += (func_sb_false() ? ab[index] : ab[index]);
        Sum += (func_sb_false() ? ab[index] : ab[index - 1]);
        Sum += (func_sb_false() ? ab[index - 1] : 3.1F);
        Sum += (func_sb_false() ? ab[index - 1] : -5.31F);
        Sum += (func_sb_false() ? ab[index - 1] : local_float);
        Sum += (func_sb_false() ? ab[index - 1] : static_field_float);
        Sum += (func_sb_false() ? ab[index - 1] : t1_i.mfd);
        Sum += (func_sb_false() ? ab[index - 1] : simple_func_float());
        Sum += (func_sb_false() ? ab[index - 1] : ab[index]);
        Sum += (func_sb_false() ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    static float Sub_Funclet_32()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ? 3.1F : 3.1F);
        Sum += (ab_true[index] ? 3.1F : -5.31F);
        Sum += (ab_true[index] ? 3.1F : local_float);
        Sum += (ab_true[index] ? 3.1F : static_field_float);
        Sum += (ab_true[index] ? 3.1F : t1_i.mfd);
        Sum += (ab_true[index] ? 3.1F : simple_func_float());
        Sum += (ab_true[index] ? 3.1F : ab[index]);
        Sum += (ab_true[index] ? 3.1F : ab[index - 1]);
        Sum += (ab_true[index] ? -5.31F : 3.1F);
        Sum += (ab_true[index] ? -5.31F : -5.31F);
        Sum += (ab_true[index] ? -5.31F : local_float);
        Sum += (ab_true[index] ? -5.31F : static_field_float);
        Sum += (ab_true[index] ? -5.31F : t1_i.mfd);
        Sum += (ab_true[index] ? -5.31F : simple_func_float());
        Sum += (ab_true[index] ? -5.31F : ab[index]);
        Sum += (ab_true[index] ? -5.31F : ab[index - 1]);
        Sum += (ab_true[index] ? local_float : 3.1F);
        Sum += (ab_true[index] ? local_float : -5.31F);
        Sum += (ab_true[index] ? local_float : local_float);
        Sum += (ab_true[index] ? local_float : static_field_float);
        return Sum;
    }
    static float Sub_Funclet_33()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ? local_float : t1_i.mfd);
        Sum += (ab_true[index] ? local_float : simple_func_float());
        Sum += (ab_true[index] ? local_float : ab[index]);
        Sum += (ab_true[index] ? local_float : ab[index - 1]);
        Sum += (ab_true[index] ? static_field_float : 3.1F);
        Sum += (ab_true[index] ? static_field_float : -5.31F);
        Sum += (ab_true[index] ? static_field_float : local_float);
        Sum += (ab_true[index] ? static_field_float : static_field_float);
        Sum += (ab_true[index] ? static_field_float : t1_i.mfd);
        Sum += (ab_true[index] ? static_field_float : simple_func_float());
        Sum += (ab_true[index] ? static_field_float : ab[index]);
        Sum += (ab_true[index] ? static_field_float : ab[index - 1]);
        Sum += (ab_true[index] ? t1_i.mfd : 3.1F);
        Sum += (ab_true[index] ? t1_i.mfd : -5.31F);
        Sum += (ab_true[index] ? t1_i.mfd : local_float);
        Sum += (ab_true[index] ? t1_i.mfd : static_field_float);
        Sum += (ab_true[index] ? t1_i.mfd : t1_i.mfd);
        Sum += (ab_true[index] ? t1_i.mfd : simple_func_float());
        Sum += (ab_true[index] ? t1_i.mfd : ab[index]);
        Sum += (ab_true[index] ? t1_i.mfd : ab[index - 1]);
        return Sum;
    }
    static float Sub_Funclet_34()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ? simple_func_float() : 3.1F);
        Sum += (ab_true[index] ? simple_func_float() : -5.31F);
        Sum += (ab_true[index] ? simple_func_float() : local_float);
        Sum += (ab_true[index] ? simple_func_float() : static_field_float);
        Sum += (ab_true[index] ? simple_func_float() : t1_i.mfd);
        Sum += (ab_true[index] ? simple_func_float() : simple_func_float());
        Sum += (ab_true[index] ? simple_func_float() : ab[index]);
        Sum += (ab_true[index] ? simple_func_float() : ab[index - 1]);
        Sum += (ab_true[index] ? ab[index] : 3.1F);
        Sum += (ab_true[index] ? ab[index] : -5.31F);
        Sum += (ab_true[index] ? ab[index] : local_float);
        Sum += (ab_true[index] ? ab[index] : static_field_float);
        Sum += (ab_true[index] ? ab[index] : t1_i.mfd);
        Sum += (ab_true[index] ? ab[index] : simple_func_float());
        Sum += (ab_true[index] ? ab[index] : ab[index]);
        Sum += (ab_true[index] ? ab[index] : ab[index - 1]);
        Sum += (ab_true[index] ? ab[index - 1] : 3.1F);
        Sum += (ab_true[index] ? ab[index - 1] : -5.31F);
        Sum += (ab_true[index] ? ab[index - 1] : local_float);
        Sum += (ab_true[index] ? ab[index - 1] : static_field_float);
        return Sum;
    }
    static float Sub_Funclet_35()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_true[index] ? ab[index - 1] : t1_i.mfd);
        Sum += (ab_true[index] ? ab[index - 1] : simple_func_float());
        Sum += (ab_true[index] ? ab[index - 1] : ab[index]);
        Sum += (ab_true[index] ? ab[index - 1] : ab[index - 1]);
        Sum += (ab_false[index] ? 3.1F : 3.1F);
        Sum += (ab_false[index] ? 3.1F : -5.31F);
        Sum += (ab_false[index] ? 3.1F : local_float);
        Sum += (ab_false[index] ? 3.1F : static_field_float);
        Sum += (ab_false[index] ? 3.1F : t1_i.mfd);
        Sum += (ab_false[index] ? 3.1F : simple_func_float());
        Sum += (ab_false[index] ? 3.1F : ab[index]);
        Sum += (ab_false[index] ? 3.1F : ab[index - 1]);
        Sum += (ab_false[index] ? -5.31F : 3.1F);
        Sum += (ab_false[index] ? -5.31F : -5.31F);
        Sum += (ab_false[index] ? -5.31F : local_float);
        Sum += (ab_false[index] ? -5.31F : static_field_float);
        Sum += (ab_false[index] ? -5.31F : t1_i.mfd);
        Sum += (ab_false[index] ? -5.31F : simple_func_float());
        Sum += (ab_false[index] ? -5.31F : ab[index]);
        Sum += (ab_false[index] ? -5.31F : ab[index - 1]);
        return Sum;
    }
    static float Sub_Funclet_36()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ? local_float : 3.1F);
        Sum += (ab_false[index] ? local_float : -5.31F);
        Sum += (ab_false[index] ? local_float : local_float);
        Sum += (ab_false[index] ? local_float : static_field_float);
        Sum += (ab_false[index] ? local_float : t1_i.mfd);
        Sum += (ab_false[index] ? local_float : simple_func_float());
        Sum += (ab_false[index] ? local_float : ab[index]);
        Sum += (ab_false[index] ? local_float : ab[index - 1]);
        Sum += (ab_false[index] ? static_field_float : 3.1F);
        Sum += (ab_false[index] ? static_field_float : -5.31F);
        Sum += (ab_false[index] ? static_field_float : local_float);
        Sum += (ab_false[index] ? static_field_float : static_field_float);
        Sum += (ab_false[index] ? static_field_float : t1_i.mfd);
        Sum += (ab_false[index] ? static_field_float : simple_func_float());
        Sum += (ab_false[index] ? static_field_float : ab[index]);
        Sum += (ab_false[index] ? static_field_float : ab[index - 1]);
        Sum += (ab_false[index] ? t1_i.mfd : 3.1F);
        Sum += (ab_false[index] ? t1_i.mfd : -5.31F);
        Sum += (ab_false[index] ? t1_i.mfd : local_float);
        Sum += (ab_false[index] ? t1_i.mfd : static_field_float);
        return Sum;
    }
    static float Sub_Funclet_37()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ? t1_i.mfd : t1_i.mfd);
        Sum += (ab_false[index] ? t1_i.mfd : simple_func_float());
        Sum += (ab_false[index] ? t1_i.mfd : ab[index]);
        Sum += (ab_false[index] ? t1_i.mfd : ab[index - 1]);
        Sum += (ab_false[index] ? simple_func_float() : 3.1F);
        Sum += (ab_false[index] ? simple_func_float() : -5.31F);
        Sum += (ab_false[index] ? simple_func_float() : local_float);
        Sum += (ab_false[index] ? simple_func_float() : static_field_float);
        Sum += (ab_false[index] ? simple_func_float() : t1_i.mfd);
        Sum += (ab_false[index] ? simple_func_float() : simple_func_float());
        Sum += (ab_false[index] ? simple_func_float() : ab[index]);
        Sum += (ab_false[index] ? simple_func_float() : ab[index - 1]);
        Sum += (ab_false[index] ? ab[index] : 3.1F);
        Sum += (ab_false[index] ? ab[index] : -5.31F);
        Sum += (ab_false[index] ? ab[index] : local_float);
        Sum += (ab_false[index] ? ab[index] : static_field_float);
        Sum += (ab_false[index] ? ab[index] : t1_i.mfd);
        Sum += (ab_false[index] ? ab[index] : simple_func_float());
        Sum += (ab_false[index] ? ab[index] : ab[index]);
        Sum += (ab_false[index] ? ab[index] : ab[index - 1]);
        return Sum;
    }
    static float Sub_Funclet_38()
    {
        float Sum = 0.0F;
        int index = 1;
        float local_float = -5.2F;
        bool lb_false = false;
        bool lb_true = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;
        float[] ab = new float[3];
        ab[0] = 21.2F;
        ab[1] = -27.645F;
        ab[2] = -31.987F;

        static_field_float = 7.7777F;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfd = -13.777F;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        Sum += (ab_false[index] ? ab[index - 1] : 3.1F);
        Sum += (ab_false[index] ? ab[index - 1] : -5.31F);
        Sum += (ab_false[index] ? ab[index - 1] : local_float);
        Sum += (ab_false[index] ? ab[index - 1] : static_field_float);
        Sum += (ab_false[index] ? ab[index - 1] : t1_i.mfd);
        Sum += (ab_false[index] ? ab[index - 1] : simple_func_float());
        Sum += (ab_false[index] ? ab[index - 1] : ab[index]);
        Sum += (ab_false[index] ? ab[index - 1] : ab[index - 1]);
        return Sum;
    }
    [Fact]
    public static int TestEntryPoint()
    {
        float Sum = 0.0F;
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
