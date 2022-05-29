// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/****************************************************************************
op1/op2, op1 is of type i4, op2 can be i4, u4, i8, u8, r4, r8, decimal
op1 and op2 can be static, local, class/struct member, function retval, 1D/2D/3D array
*****************************************************************************/

using System;
using Xunit;
public class i4div
{
    private static int s_i_s_op1 = 6;
    private static int s_i_s_op2 = 3;
    private static uint s_ui_s_op2 = 3;
    private static long s_l_s_op2 = 3;
    private static ulong s_ul_s_op2 = 3;
    private static float s_f_s_op2 = 3;
    private static double s_d_s_op2 = 3;
    private static decimal s_m_s_op2 = 3;

    public static int i_f(String s)
    {
        if (s == "op1")
            return 6;
        else
            return 3;
    }
    public static uint ui_f(String s)
    {
        if (s == "op1")
            return 6;
        else
            return 3;
    }
    public static long l_f(String s)
    {
        if (s == "op1")
            return 6;
        else
            return 3;
    }
    public static ulong ul_f(String s)
    {
        if (s == "op1")
            return 6;
        else
            return 3;
    }
    public static float f_f(String s)
    {
        if (s == "op1")
            return 6;
        else
            return 3;
    }
    public static double d_f(String s)
    {
        if (s == "op1")
            return 6;
        else
            return 3;
    }
    public static decimal m_f(String s)
    {
        if (s == "op1")
            return 6;
        else
            return 3;
    }
    private class CL
    {
        public int i_cl_op1 = 6;
        public int i_cl_op2 = 3;
        public uint ui_cl_op2 = 3;
        public long l_cl_op2 = 3;
        public ulong ul_cl_op2 = 3;
        public float f_cl_op2 = 3;
        public double d_cl_op2 = 3;
        public decimal m_cl_op2 = 3;
    }

    private struct VT
    {
        public int i_vt_op1;
        public int i_vt_op2;
        public uint ui_vt_op2;
        public long l_vt_op2;
        public ulong ul_vt_op2;
        public float f_vt_op2;
        public double d_vt_op2;
        public decimal m_vt_op2;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool passed = true;
        //initialize class
        CL cl1 = new CL();
        //initialize struct
        VT vt1;
        vt1.i_vt_op1 = 6;
        vt1.i_vt_op2 = 3;
        vt1.ui_vt_op2 = 3;
        vt1.l_vt_op2 = 3;
        vt1.ul_vt_op2 = 3;
        vt1.f_vt_op2 = 3;
        vt1.d_vt_op2 = 3;
        vt1.m_vt_op2 = 3;

        int[] i_arr1d_op1 = { 0, 6 };
        int[,] i_arr2d_op1 = { { 0, 6 }, { 1, 1 } };
        int[,,] i_arr3d_op1 = { { { 0, 6 }, { 1, 1 } } };

        int[] i_arr1d_op2 = { 3, 0, 1 };
        int[,] i_arr2d_op2 = { { 0, 3 }, { 1, 1 } };
        int[,,] i_arr3d_op2 = { { { 0, 3 }, { 1, 1 } } };
        uint[] ui_arr1d_op2 = { 3, 0, 1 };
        uint[,] ui_arr2d_op2 = { { 0, 3 }, { 1, 1 } };
        uint[,,] ui_arr3d_op2 = { { { 0, 3 }, { 1, 1 } } };
        long[] l_arr1d_op2 = { 3, 0, 1 };
        long[,] l_arr2d_op2 = { { 0, 3 }, { 1, 1 } };
        long[,,] l_arr3d_op2 = { { { 0, 3 }, { 1, 1 } } };
        ulong[] ul_arr1d_op2 = { 3, 0, 1 };
        ulong[,] ul_arr2d_op2 = { { 0, 3 }, { 1, 1 } };
        ulong[,,] ul_arr3d_op2 = { { { 0, 3 }, { 1, 1 } } };
        float[] f_arr1d_op2 = { 3, 0, 1 };
        float[,] f_arr2d_op2 = { { 0, 3 }, { 1, 1 } };
        float[,,] f_arr3d_op2 = { { { 0, 3 }, { 1, 1 } } };
        double[] d_arr1d_op2 = { 3, 0, 1 };
        double[,] d_arr2d_op2 = { { 0, 3 }, { 1, 1 } };
        double[,,] d_arr3d_op2 = { { { 0, 3 }, { 1, 1 } } };
        decimal[] m_arr1d_op2 = { 3, 0, 1 };
        decimal[,] m_arr2d_op2 = { { 0, 3 }, { 1, 1 } };
        decimal[,,] m_arr3d_op2 = { { { 0, 3 }, { 1, 1 } } };

        int[,] index = { { 0, 0 }, { 1, 1 } };

        {
            int i_l_op1 = 6;
            int i_l_op2 = 3;
            uint ui_l_op2 = 3;
            long l_l_op2 = 3;
            ulong ul_l_op2 = 3;
            float f_l_op2 = 3;
            double d_l_op2 = 3;
            decimal m_l_op2 = 3;
            if ((i_l_op1 / i_l_op2 != i_l_op1 / ui_l_op2) || (i_l_op1 / ui_l_op2 != i_l_op1 / l_l_op2) || (i_l_op1 / l_l_op2 != i_l_op1 / (int)ul_l_op2) || (i_l_op1 / (int)ul_l_op2 != i_l_op1 / f_l_op2) || (i_l_op1 / f_l_op2 != i_l_op1 / d_l_op2) || ((decimal)(i_l_op1 / d_l_op2) != i_l_op1 / m_l_op2) || (i_l_op1 / m_l_op2 != i_l_op1 / i_l_op2) || (i_l_op1 / i_l_op2 != 2))
            {
                Console.WriteLine("testcase 1 failed");
                passed = false;
            }
            if ((i_l_op1 / s_i_s_op2 != i_l_op1 / s_ui_s_op2) || (i_l_op1 / s_ui_s_op2 != i_l_op1 / s_l_s_op2) || (i_l_op1 / s_l_s_op2 != i_l_op1 / (int)s_ul_s_op2) || (i_l_op1 / (int)s_ul_s_op2 != i_l_op1 / s_f_s_op2) || (i_l_op1 / s_f_s_op2 != i_l_op1 / s_d_s_op2) || ((decimal)(i_l_op1 / s_d_s_op2) != i_l_op1 / s_m_s_op2) || (i_l_op1 / s_m_s_op2 != i_l_op1 / s_i_s_op2) || (i_l_op1 / s_i_s_op2 != 2))
            {
                Console.WriteLine("testcase 2 failed");
                passed = false;
            }
            if ((i_l_op1 / i_f("op2") != i_l_op1 / i_f("op2")) || (i_l_op1 / i_f("op2") != i_l_op1 / i_f("op2")) || (i_l_op1 / i_f("op2") != i_l_op1 / (int)i_f("op2")) || (i_l_op1 / (int)i_f("op2") != i_l_op1 / i_f("op2")) || (i_l_op1 / i_f("op2") != i_l_op1 / i_f("op2")) || ((decimal)(i_l_op1 / i_f("op2")) != i_l_op1 / i_f("op2")) || (i_l_op1 / i_f("op2") != i_l_op1 / i_f("op2")) || (i_l_op1 / i_f("op2") != 2))
            {
                Console.WriteLine("testcase 3 failed");
                passed = false;
            }
            if ((i_l_op1 / cl1.i_cl_op2 != i_l_op1 / cl1.ui_cl_op2) || (i_l_op1 / cl1.ui_cl_op2 != i_l_op1 / cl1.l_cl_op2) || (i_l_op1 / cl1.l_cl_op2 != i_l_op1 / (int)cl1.ul_cl_op2) || (i_l_op1 / (int)cl1.ul_cl_op2 != i_l_op1 / cl1.f_cl_op2) || (i_l_op1 / cl1.f_cl_op2 != i_l_op1 / cl1.d_cl_op2) || ((decimal)(i_l_op1 / cl1.d_cl_op2) != i_l_op1 / cl1.m_cl_op2) || (i_l_op1 / cl1.m_cl_op2 != i_l_op1 / cl1.i_cl_op2) || (i_l_op1 / cl1.i_cl_op2 != 2))
            {
                Console.WriteLine("testcase 4 failed");
                passed = false;
            }
            if ((i_l_op1 / vt1.i_vt_op2 != i_l_op1 / vt1.ui_vt_op2) || (i_l_op1 / vt1.ui_vt_op2 != i_l_op1 / vt1.l_vt_op2) || (i_l_op1 / vt1.l_vt_op2 != i_l_op1 / (int)vt1.ul_vt_op2) || (i_l_op1 / (int)vt1.ul_vt_op2 != i_l_op1 / vt1.f_vt_op2) || (i_l_op1 / vt1.f_vt_op2 != i_l_op1 / vt1.d_vt_op2) || ((decimal)(i_l_op1 / vt1.d_vt_op2) != i_l_op1 / vt1.m_vt_op2) || (i_l_op1 / vt1.m_vt_op2 != i_l_op1 / vt1.i_vt_op2) || (i_l_op1 / vt1.i_vt_op2 != 2))
            {
                Console.WriteLine("testcase 5 failed");
                passed = false;
            }
            if ((i_l_op1 / i_arr1d_op2[0] != i_l_op1 / ui_arr1d_op2[0]) || (i_l_op1 / ui_arr1d_op2[0] != i_l_op1 / l_arr1d_op2[0]) || (i_l_op1 / l_arr1d_op2[0] != i_l_op1 / (int)ul_arr1d_op2[0]) || (i_l_op1 / (int)ul_arr1d_op2[0] != i_l_op1 / f_arr1d_op2[0]) || (i_l_op1 / f_arr1d_op2[0] != i_l_op1 / d_arr1d_op2[0]) || ((decimal)(i_l_op1 / d_arr1d_op2[0]) != i_l_op1 / m_arr1d_op2[0]) || (i_l_op1 / m_arr1d_op2[0] != i_l_op1 / i_arr1d_op2[0]) || (i_l_op1 / i_arr1d_op2[0] != 2))
            {
                Console.WriteLine("testcase 6 failed");
                passed = false;
            }
            if ((i_l_op1 / i_arr2d_op2[index[0, 1], index[1, 0]] != i_l_op1 / ui_arr2d_op2[index[0, 1], index[1, 0]]) || (i_l_op1 / ui_arr2d_op2[index[0, 1], index[1, 0]] != i_l_op1 / l_arr2d_op2[index[0, 1], index[1, 0]]) || (i_l_op1 / l_arr2d_op2[index[0, 1], index[1, 0]] != i_l_op1 / (int)ul_arr2d_op2[index[0, 1], index[1, 0]]) || (i_l_op1 / (int)ul_arr2d_op2[index[0, 1], index[1, 0]] != i_l_op1 / f_arr2d_op2[index[0, 1], index[1, 0]]) || (i_l_op1 / f_arr2d_op2[index[0, 1], index[1, 0]] != i_l_op1 / d_arr2d_op2[index[0, 1], index[1, 0]]) || ((decimal)(i_l_op1 / d_arr2d_op2[index[0, 1], index[1, 0]]) != i_l_op1 / m_arr2d_op2[index[0, 1], index[1, 0]]) || (i_l_op1 / m_arr2d_op2[index[0, 1], index[1, 0]] != i_l_op1 / i_arr2d_op2[index[0, 1], index[1, 0]]) || (i_l_op1 / i_arr2d_op2[index[0, 1], index[1, 0]] != 2))
            {
                Console.WriteLine("testcase 7 failed");
                passed = false;
            }
            if ((i_l_op1 / i_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_l_op1 / ui_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_l_op1 / ui_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_l_op1 / l_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_l_op1 / l_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_l_op1 / (int)ul_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_l_op1 / (int)ul_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_l_op1 / f_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_l_op1 / f_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_l_op1 / d_arr3d_op2[index[0, 0], 0, index[1, 1]]) || ((decimal)(i_l_op1 / d_arr3d_op2[index[0, 0], 0, index[1, 1]]) != i_l_op1 / m_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_l_op1 / m_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_l_op1 / i_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_l_op1 / i_arr3d_op2[index[0, 0], 0, index[1, 1]] != 2))
            {
                Console.WriteLine("testcase 8 failed");
                passed = false;
            }
            if ((s_i_s_op1 / i_l_op2 != s_i_s_op1 / ui_l_op2) || (s_i_s_op1 / ui_l_op2 != s_i_s_op1 / l_l_op2) || (s_i_s_op1 / l_l_op2 != s_i_s_op1 / (int)ul_l_op2) || (s_i_s_op1 / (int)ul_l_op2 != s_i_s_op1 / f_l_op2) || (s_i_s_op1 / f_l_op2 != s_i_s_op1 / d_l_op2) || ((decimal)(s_i_s_op1 / d_l_op2) != s_i_s_op1 / m_l_op2) || (s_i_s_op1 / m_l_op2 != s_i_s_op1 / i_l_op2) || (s_i_s_op1 / i_l_op2 != 2))
            {
                Console.WriteLine("testcase 9 failed");
                passed = false;
            }
            if ((s_i_s_op1 / s_i_s_op2 != s_i_s_op1 / s_ui_s_op2) || (s_i_s_op1 / s_ui_s_op2 != s_i_s_op1 / s_l_s_op2) || (s_i_s_op1 / s_l_s_op2 != s_i_s_op1 / (int)s_ul_s_op2) || (s_i_s_op1 / (int)s_ul_s_op2 != s_i_s_op1 / s_f_s_op2) || (s_i_s_op1 / s_f_s_op2 != s_i_s_op1 / s_d_s_op2) || ((decimal)(s_i_s_op1 / s_d_s_op2) != s_i_s_op1 / s_m_s_op2) || (s_i_s_op1 / s_m_s_op2 != s_i_s_op1 / s_i_s_op2) || (s_i_s_op1 / s_i_s_op2 != 2))
            {
                Console.WriteLine("testcase 10 failed");
                passed = false;
            }
            if ((s_i_s_op1 / i_f("op2") != s_i_s_op1 / i_f("op2")) || (s_i_s_op1 / i_f("op2") != s_i_s_op1 / i_f("op2")) || (s_i_s_op1 / i_f("op2") != s_i_s_op1 / (int)i_f("op2")) || (s_i_s_op1 / (int)i_f("op2") != s_i_s_op1 / i_f("op2")) || (s_i_s_op1 / i_f("op2") != s_i_s_op1 / i_f("op2")) || ((decimal)(s_i_s_op1 / i_f("op2")) != s_i_s_op1 / i_f("op2")) || (s_i_s_op1 / i_f("op2") != s_i_s_op1 / i_f("op2")) || (s_i_s_op1 / i_f("op2") != 2))
            {
                Console.WriteLine("testcase 11 failed");
                passed = false;
            }
            if ((s_i_s_op1 / cl1.i_cl_op2 != s_i_s_op1 / cl1.ui_cl_op2) || (s_i_s_op1 / cl1.ui_cl_op2 != s_i_s_op1 / cl1.l_cl_op2) || (s_i_s_op1 / cl1.l_cl_op2 != s_i_s_op1 / (int)cl1.ul_cl_op2) || (s_i_s_op1 / (int)cl1.ul_cl_op2 != s_i_s_op1 / cl1.f_cl_op2) || (s_i_s_op1 / cl1.f_cl_op2 != s_i_s_op1 / cl1.d_cl_op2) || ((decimal)(s_i_s_op1 / cl1.d_cl_op2) != s_i_s_op1 / cl1.m_cl_op2) || (s_i_s_op1 / cl1.m_cl_op2 != s_i_s_op1 / cl1.i_cl_op2) || (s_i_s_op1 / cl1.i_cl_op2 != 2))
            {
                Console.WriteLine("testcase 12 failed");
                passed = false;
            }
            if ((s_i_s_op1 / vt1.i_vt_op2 != s_i_s_op1 / vt1.ui_vt_op2) || (s_i_s_op1 / vt1.ui_vt_op2 != s_i_s_op1 / vt1.l_vt_op2) || (s_i_s_op1 / vt1.l_vt_op2 != s_i_s_op1 / (int)vt1.ul_vt_op2) || (s_i_s_op1 / (int)vt1.ul_vt_op2 != s_i_s_op1 / vt1.f_vt_op2) || (s_i_s_op1 / vt1.f_vt_op2 != s_i_s_op1 / vt1.d_vt_op2) || ((decimal)(s_i_s_op1 / vt1.d_vt_op2) != s_i_s_op1 / vt1.m_vt_op2) || (s_i_s_op1 / vt1.m_vt_op2 != s_i_s_op1 / vt1.i_vt_op2) || (s_i_s_op1 / vt1.i_vt_op2 != 2))
            {
                Console.WriteLine("testcase 13 failed");
                passed = false;
            }
            if ((s_i_s_op1 / i_arr1d_op2[0] != s_i_s_op1 / ui_arr1d_op2[0]) || (s_i_s_op1 / ui_arr1d_op2[0] != s_i_s_op1 / l_arr1d_op2[0]) || (s_i_s_op1 / l_arr1d_op2[0] != s_i_s_op1 / (int)ul_arr1d_op2[0]) || (s_i_s_op1 / (int)ul_arr1d_op2[0] != s_i_s_op1 / f_arr1d_op2[0]) || (s_i_s_op1 / f_arr1d_op2[0] != s_i_s_op1 / d_arr1d_op2[0]) || ((decimal)(s_i_s_op1 / d_arr1d_op2[0]) != s_i_s_op1 / m_arr1d_op2[0]) || (s_i_s_op1 / m_arr1d_op2[0] != s_i_s_op1 / i_arr1d_op2[0]) || (s_i_s_op1 / i_arr1d_op2[0] != 2))
            {
                Console.WriteLine("testcase 14 failed");
                passed = false;
            }
            if ((s_i_s_op1 / i_arr2d_op2[index[0, 1], index[1, 0]] != s_i_s_op1 / ui_arr2d_op2[index[0, 1], index[1, 0]]) || (s_i_s_op1 / ui_arr2d_op2[index[0, 1], index[1, 0]] != s_i_s_op1 / l_arr2d_op2[index[0, 1], index[1, 0]]) || (s_i_s_op1 / l_arr2d_op2[index[0, 1], index[1, 0]] != s_i_s_op1 / (int)ul_arr2d_op2[index[0, 1], index[1, 0]]) || (s_i_s_op1 / (int)ul_arr2d_op2[index[0, 1], index[1, 0]] != s_i_s_op1 / f_arr2d_op2[index[0, 1], index[1, 0]]) || (s_i_s_op1 / f_arr2d_op2[index[0, 1], index[1, 0]] != s_i_s_op1 / d_arr2d_op2[index[0, 1], index[1, 0]]) || ((decimal)(s_i_s_op1 / d_arr2d_op2[index[0, 1], index[1, 0]]) != s_i_s_op1 / m_arr2d_op2[index[0, 1], index[1, 0]]) || (s_i_s_op1 / m_arr2d_op2[index[0, 1], index[1, 0]] != s_i_s_op1 / i_arr2d_op2[index[0, 1], index[1, 0]]) || (s_i_s_op1 / i_arr2d_op2[index[0, 1], index[1, 0]] != 2))
            {
                Console.WriteLine("testcase 15 failed");
                passed = false;
            }
            if ((s_i_s_op1 / i_arr3d_op2[index[0, 0], 0, index[1, 1]] != s_i_s_op1 / ui_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (s_i_s_op1 / ui_arr3d_op2[index[0, 0], 0, index[1, 1]] != s_i_s_op1 / l_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (s_i_s_op1 / l_arr3d_op2[index[0, 0], 0, index[1, 1]] != s_i_s_op1 / (int)ul_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (s_i_s_op1 / (int)ul_arr3d_op2[index[0, 0], 0, index[1, 1]] != s_i_s_op1 / f_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (s_i_s_op1 / f_arr3d_op2[index[0, 0], 0, index[1, 1]] != s_i_s_op1 / d_arr3d_op2[index[0, 0], 0, index[1, 1]]) || ((decimal)(s_i_s_op1 / d_arr3d_op2[index[0, 0], 0, index[1, 1]]) != s_i_s_op1 / m_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (s_i_s_op1 / m_arr3d_op2[index[0, 0], 0, index[1, 1]] != s_i_s_op1 / i_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (s_i_s_op1 / i_arr3d_op2[index[0, 0], 0, index[1, 1]] != 2))
            {
                Console.WriteLine("testcase 16 failed");
                passed = false;
            }
            if ((i_f("op1") / i_l_op2 != i_f("op1") / ui_l_op2) || (i_f("op1") / ui_l_op2 != i_f("op1") / l_l_op2) || (i_f("op1") / l_l_op2 != i_f("op1") / (int)ul_l_op2) || (i_f("op1") / (int)ul_l_op2 != i_f("op1") / f_l_op2) || (i_f("op1") / f_l_op2 != i_f("op1") / d_l_op2) || ((decimal)(i_f("op1") / d_l_op2) != i_f("op1") / m_l_op2) || (i_f("op1") / m_l_op2 != i_f("op1") / i_l_op2) || (i_f("op1") / i_l_op2 != 2))
            {
                Console.WriteLine("testcase 17 failed");
                passed = false;
            }
            if ((i_f("op1") / s_i_s_op2 != i_f("op1") / s_ui_s_op2) || (i_f("op1") / s_ui_s_op2 != i_f("op1") / s_l_s_op2) || (i_f("op1") / s_l_s_op2 != i_f("op1") / (int)s_ul_s_op2) || (i_f("op1") / (int)s_ul_s_op2 != i_f("op1") / s_f_s_op2) || (i_f("op1") / s_f_s_op2 != i_f("op1") / s_d_s_op2) || ((decimal)(i_f("op1") / s_d_s_op2) != i_f("op1") / s_m_s_op2) || (i_f("op1") / s_m_s_op2 != i_f("op1") / s_i_s_op2) || (i_f("op1") / s_i_s_op2 != 2))
            {
                Console.WriteLine("testcase 18 failed");
                passed = false;
            }
            if ((i_f("op1") / i_f("op2") != i_f("op1") / i_f("op2")) || (i_f("op1") / i_f("op2") != i_f("op1") / i_f("op2")) || (i_f("op1") / i_f("op2") != i_f("op1") / (int)i_f("op2")) || (i_f("op1") / (int)i_f("op2") != i_f("op1") / i_f("op2")) || (i_f("op1") / i_f("op2") != i_f("op1") / i_f("op2")) || ((decimal)(i_f("op1") / i_f("op2")) != i_f("op1") / i_f("op2")) || (i_f("op1") / i_f("op2") != i_f("op1") / i_f("op2")) || (i_f("op1") / i_f("op2") != 2))
            {
                Console.WriteLine("testcase 19 failed");
                passed = false;
            }
            if ((i_f("op1") / cl1.i_cl_op2 != i_f("op1") / cl1.ui_cl_op2) || (i_f("op1") / cl1.ui_cl_op2 != i_f("op1") / cl1.l_cl_op2) || (i_f("op1") / cl1.l_cl_op2 != i_f("op1") / (int)cl1.ul_cl_op2) || (i_f("op1") / (int)cl1.ul_cl_op2 != i_f("op1") / cl1.f_cl_op2) || (i_f("op1") / cl1.f_cl_op2 != i_f("op1") / cl1.d_cl_op2) || ((decimal)(i_f("op1") / cl1.d_cl_op2) != i_f("op1") / cl1.m_cl_op2) || (i_f("op1") / cl1.m_cl_op2 != i_f("op1") / cl1.i_cl_op2) || (i_f("op1") / cl1.i_cl_op2 != 2))
            {
                Console.WriteLine("testcase 20 failed");
                passed = false;
            }
            if ((i_f("op1") / vt1.i_vt_op2 != i_f("op1") / vt1.ui_vt_op2) || (i_f("op1") / vt1.ui_vt_op2 != i_f("op1") / vt1.l_vt_op2) || (i_f("op1") / vt1.l_vt_op2 != i_f("op1") / (int)vt1.ul_vt_op2) || (i_f("op1") / (int)vt1.ul_vt_op2 != i_f("op1") / vt1.f_vt_op2) || (i_f("op1") / vt1.f_vt_op2 != i_f("op1") / vt1.d_vt_op2) || ((decimal)(i_f("op1") / vt1.d_vt_op2) != i_f("op1") / vt1.m_vt_op2) || (i_f("op1") / vt1.m_vt_op2 != i_f("op1") / vt1.i_vt_op2) || (i_f("op1") / vt1.i_vt_op2 != 2))
            {
                Console.WriteLine("testcase 21 failed");
                passed = false;
            }
            if ((i_f("op1") / i_arr1d_op2[0] != i_f("op1") / ui_arr1d_op2[0]) || (i_f("op1") / ui_arr1d_op2[0] != i_f("op1") / l_arr1d_op2[0]) || (i_f("op1") / l_arr1d_op2[0] != i_f("op1") / (int)ul_arr1d_op2[0]) || (i_f("op1") / (int)ul_arr1d_op2[0] != i_f("op1") / f_arr1d_op2[0]) || (i_f("op1") / f_arr1d_op2[0] != i_f("op1") / d_arr1d_op2[0]) || ((decimal)(i_f("op1") / d_arr1d_op2[0]) != i_f("op1") / m_arr1d_op2[0]) || (i_f("op1") / m_arr1d_op2[0] != i_f("op1") / i_arr1d_op2[0]) || (i_f("op1") / i_arr1d_op2[0] != 2))
            {
                Console.WriteLine("testcase 22 failed");
                passed = false;
            }
            if ((i_f("op1") / i_arr2d_op2[index[0, 1], index[1, 0]] != i_f("op1") / ui_arr2d_op2[index[0, 1], index[1, 0]]) || (i_f("op1") / ui_arr2d_op2[index[0, 1], index[1, 0]] != i_f("op1") / l_arr2d_op2[index[0, 1], index[1, 0]]) || (i_f("op1") / l_arr2d_op2[index[0, 1], index[1, 0]] != i_f("op1") / (int)ul_arr2d_op2[index[0, 1], index[1, 0]]) || (i_f("op1") / (int)ul_arr2d_op2[index[0, 1], index[1, 0]] != i_f("op1") / f_arr2d_op2[index[0, 1], index[1, 0]]) || (i_f("op1") / f_arr2d_op2[index[0, 1], index[1, 0]] != i_f("op1") / d_arr2d_op2[index[0, 1], index[1, 0]]) || ((decimal)(i_f("op1") / d_arr2d_op2[index[0, 1], index[1, 0]]) != i_f("op1") / m_arr2d_op2[index[0, 1], index[1, 0]]) || (i_f("op1") / m_arr2d_op2[index[0, 1], index[1, 0]] != i_f("op1") / i_arr2d_op2[index[0, 1], index[1, 0]]) || (i_f("op1") / i_arr2d_op2[index[0, 1], index[1, 0]] != 2))
            {
                Console.WriteLine("testcase 23 failed");
                passed = false;
            }
            if ((i_f("op1") / i_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_f("op1") / ui_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_f("op1") / ui_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_f("op1") / l_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_f("op1") / l_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_f("op1") / (int)ul_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_f("op1") / (int)ul_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_f("op1") / f_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_f("op1") / f_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_f("op1") / d_arr3d_op2[index[0, 0], 0, index[1, 1]]) || ((decimal)(i_f("op1") / d_arr3d_op2[index[0, 0], 0, index[1, 1]]) != i_f("op1") / m_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_f("op1") / m_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_f("op1") / i_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_f("op1") / i_arr3d_op2[index[0, 0], 0, index[1, 1]] != 2))
            {
                Console.WriteLine("testcase 24 failed");
                passed = false;
            }
            if ((cl1.i_cl_op1 / i_l_op2 != cl1.i_cl_op1 / ui_l_op2) || (cl1.i_cl_op1 / ui_l_op2 != cl1.i_cl_op1 / l_l_op2) || (cl1.i_cl_op1 / l_l_op2 != cl1.i_cl_op1 / (int)ul_l_op2) || (cl1.i_cl_op1 / (int)ul_l_op2 != cl1.i_cl_op1 / f_l_op2) || (cl1.i_cl_op1 / f_l_op2 != cl1.i_cl_op1 / d_l_op2) || ((decimal)(cl1.i_cl_op1 / d_l_op2) != cl1.i_cl_op1 / m_l_op2) || (cl1.i_cl_op1 / m_l_op2 != cl1.i_cl_op1 / i_l_op2) || (cl1.i_cl_op1 / i_l_op2 != 2))
            {
                Console.WriteLine("testcase 25 failed");
                passed = false;
            }
            if ((cl1.i_cl_op1 / s_i_s_op2 != cl1.i_cl_op1 / s_ui_s_op2) || (cl1.i_cl_op1 / s_ui_s_op2 != cl1.i_cl_op1 / s_l_s_op2) || (cl1.i_cl_op1 / s_l_s_op2 != cl1.i_cl_op1 / (int)s_ul_s_op2) || (cl1.i_cl_op1 / (int)s_ul_s_op2 != cl1.i_cl_op1 / s_f_s_op2) || (cl1.i_cl_op1 / s_f_s_op2 != cl1.i_cl_op1 / s_d_s_op2) || ((decimal)(cl1.i_cl_op1 / s_d_s_op2) != cl1.i_cl_op1 / s_m_s_op2) || (cl1.i_cl_op1 / s_m_s_op2 != cl1.i_cl_op1 / s_i_s_op2) || (cl1.i_cl_op1 / s_i_s_op2 != 2))
            {
                Console.WriteLine("testcase 26 failed");
                passed = false;
            }
            if ((cl1.i_cl_op1 / i_f("op2") != cl1.i_cl_op1 / i_f("op2")) || (cl1.i_cl_op1 / i_f("op2") != cl1.i_cl_op1 / i_f("op2")) || (cl1.i_cl_op1 / i_f("op2") != cl1.i_cl_op1 / (int)i_f("op2")) || (cl1.i_cl_op1 / (int)i_f("op2") != cl1.i_cl_op1 / i_f("op2")) || (cl1.i_cl_op1 / i_f("op2") != cl1.i_cl_op1 / i_f("op2")) || ((decimal)(cl1.i_cl_op1 / i_f("op2")) != cl1.i_cl_op1 / i_f("op2")) || (cl1.i_cl_op1 / i_f("op2") != cl1.i_cl_op1 / i_f("op2")) || (cl1.i_cl_op1 / i_f("op2") != 2))
            {
                Console.WriteLine("testcase 27 failed");
                passed = false;
            }
            if ((cl1.i_cl_op1 / cl1.i_cl_op2 != cl1.i_cl_op1 / cl1.ui_cl_op2) || (cl1.i_cl_op1 / cl1.ui_cl_op2 != cl1.i_cl_op1 / cl1.l_cl_op2) || (cl1.i_cl_op1 / cl1.l_cl_op2 != cl1.i_cl_op1 / (int)cl1.ul_cl_op2) || (cl1.i_cl_op1 / (int)cl1.ul_cl_op2 != cl1.i_cl_op1 / cl1.f_cl_op2) || (cl1.i_cl_op1 / cl1.f_cl_op2 != cl1.i_cl_op1 / cl1.d_cl_op2) || ((decimal)(cl1.i_cl_op1 / cl1.d_cl_op2) != cl1.i_cl_op1 / cl1.m_cl_op2) || (cl1.i_cl_op1 / cl1.m_cl_op2 != cl1.i_cl_op1 / cl1.i_cl_op2) || (cl1.i_cl_op1 / cl1.i_cl_op2 != 2))
            {
                Console.WriteLine("testcase 28 failed");
                passed = false;
            }
            if ((cl1.i_cl_op1 / vt1.i_vt_op2 != cl1.i_cl_op1 / vt1.ui_vt_op2) || (cl1.i_cl_op1 / vt1.ui_vt_op2 != cl1.i_cl_op1 / vt1.l_vt_op2) || (cl1.i_cl_op1 / vt1.l_vt_op2 != cl1.i_cl_op1 / (int)vt1.ul_vt_op2) || (cl1.i_cl_op1 / (int)vt1.ul_vt_op2 != cl1.i_cl_op1 / vt1.f_vt_op2) || (cl1.i_cl_op1 / vt1.f_vt_op2 != cl1.i_cl_op1 / vt1.d_vt_op2) || ((decimal)(cl1.i_cl_op1 / vt1.d_vt_op2) != cl1.i_cl_op1 / vt1.m_vt_op2) || (cl1.i_cl_op1 / vt1.m_vt_op2 != cl1.i_cl_op1 / vt1.i_vt_op2) || (cl1.i_cl_op1 / vt1.i_vt_op2 != 2))
            {
                Console.WriteLine("testcase 29 failed");
                passed = false;
            }
            if ((cl1.i_cl_op1 / i_arr1d_op2[0] != cl1.i_cl_op1 / ui_arr1d_op2[0]) || (cl1.i_cl_op1 / ui_arr1d_op2[0] != cl1.i_cl_op1 / l_arr1d_op2[0]) || (cl1.i_cl_op1 / l_arr1d_op2[0] != cl1.i_cl_op1 / (int)ul_arr1d_op2[0]) || (cl1.i_cl_op1 / (int)ul_arr1d_op2[0] != cl1.i_cl_op1 / f_arr1d_op2[0]) || (cl1.i_cl_op1 / f_arr1d_op2[0] != cl1.i_cl_op1 / d_arr1d_op2[0]) || ((decimal)(cl1.i_cl_op1 / d_arr1d_op2[0]) != cl1.i_cl_op1 / m_arr1d_op2[0]) || (cl1.i_cl_op1 / m_arr1d_op2[0] != cl1.i_cl_op1 / i_arr1d_op2[0]) || (cl1.i_cl_op1 / i_arr1d_op2[0] != 2))
            {
                Console.WriteLine("testcase 30 failed");
                passed = false;
            }
            if ((cl1.i_cl_op1 / i_arr2d_op2[index[0, 1], index[1, 0]] != cl1.i_cl_op1 / ui_arr2d_op2[index[0, 1], index[1, 0]]) || (cl1.i_cl_op1 / ui_arr2d_op2[index[0, 1], index[1, 0]] != cl1.i_cl_op1 / l_arr2d_op2[index[0, 1], index[1, 0]]) || (cl1.i_cl_op1 / l_arr2d_op2[index[0, 1], index[1, 0]] != cl1.i_cl_op1 / (int)ul_arr2d_op2[index[0, 1], index[1, 0]]) || (cl1.i_cl_op1 / (int)ul_arr2d_op2[index[0, 1], index[1, 0]] != cl1.i_cl_op1 / f_arr2d_op2[index[0, 1], index[1, 0]]) || (cl1.i_cl_op1 / f_arr2d_op2[index[0, 1], index[1, 0]] != cl1.i_cl_op1 / d_arr2d_op2[index[0, 1], index[1, 0]]) || ((decimal)(cl1.i_cl_op1 / d_arr2d_op2[index[0, 1], index[1, 0]]) != cl1.i_cl_op1 / m_arr2d_op2[index[0, 1], index[1, 0]]) || (cl1.i_cl_op1 / m_arr2d_op2[index[0, 1], index[1, 0]] != cl1.i_cl_op1 / i_arr2d_op2[index[0, 1], index[1, 0]]) || (cl1.i_cl_op1 / i_arr2d_op2[index[0, 1], index[1, 0]] != 2))
            {
                Console.WriteLine("testcase 31 failed");
                passed = false;
            }
            if ((cl1.i_cl_op1 / i_arr3d_op2[index[0, 0], 0, index[1, 1]] != cl1.i_cl_op1 / ui_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (cl1.i_cl_op1 / ui_arr3d_op2[index[0, 0], 0, index[1, 1]] != cl1.i_cl_op1 / l_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (cl1.i_cl_op1 / l_arr3d_op2[index[0, 0], 0, index[1, 1]] != cl1.i_cl_op1 / (int)ul_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (cl1.i_cl_op1 / (int)ul_arr3d_op2[index[0, 0], 0, index[1, 1]] != cl1.i_cl_op1 / f_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (cl1.i_cl_op1 / f_arr3d_op2[index[0, 0], 0, index[1, 1]] != cl1.i_cl_op1 / d_arr3d_op2[index[0, 0], 0, index[1, 1]]) || ((decimal)(cl1.i_cl_op1 / d_arr3d_op2[index[0, 0], 0, index[1, 1]]) != cl1.i_cl_op1 / m_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (cl1.i_cl_op1 / m_arr3d_op2[index[0, 0], 0, index[1, 1]] != cl1.i_cl_op1 / i_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (cl1.i_cl_op1 / i_arr3d_op2[index[0, 0], 0, index[1, 1]] != 2))
            {
                Console.WriteLine("testcase 32 failed");
                passed = false;
            }
            if ((vt1.i_vt_op1 / i_l_op2 != vt1.i_vt_op1 / ui_l_op2) || (vt1.i_vt_op1 / ui_l_op2 != vt1.i_vt_op1 / l_l_op2) || (vt1.i_vt_op1 / l_l_op2 != vt1.i_vt_op1 / (int)ul_l_op2) || (vt1.i_vt_op1 / (int)ul_l_op2 != vt1.i_vt_op1 / f_l_op2) || (vt1.i_vt_op1 / f_l_op2 != vt1.i_vt_op1 / d_l_op2) || ((decimal)(vt1.i_vt_op1 / d_l_op2) != vt1.i_vt_op1 / m_l_op2) || (vt1.i_vt_op1 / m_l_op2 != vt1.i_vt_op1 / i_l_op2) || (vt1.i_vt_op1 / i_l_op2 != 2))
            {
                Console.WriteLine("testcase 33 failed");
                passed = false;
            }
            if ((vt1.i_vt_op1 / s_i_s_op2 != vt1.i_vt_op1 / s_ui_s_op2) || (vt1.i_vt_op1 / s_ui_s_op2 != vt1.i_vt_op1 / s_l_s_op2) || (vt1.i_vt_op1 / s_l_s_op2 != vt1.i_vt_op1 / (int)s_ul_s_op2) || (vt1.i_vt_op1 / (int)s_ul_s_op2 != vt1.i_vt_op1 / s_f_s_op2) || (vt1.i_vt_op1 / s_f_s_op2 != vt1.i_vt_op1 / s_d_s_op2) || ((decimal)(vt1.i_vt_op1 / s_d_s_op2) != vt1.i_vt_op1 / s_m_s_op2) || (vt1.i_vt_op1 / s_m_s_op2 != vt1.i_vt_op1 / s_i_s_op2) || (vt1.i_vt_op1 / s_i_s_op2 != 2))
            {
                Console.WriteLine("testcase 34 failed");
                passed = false;
            }
            if ((vt1.i_vt_op1 / i_f("op2") != vt1.i_vt_op1 / i_f("op2")) || (vt1.i_vt_op1 / i_f("op2") != vt1.i_vt_op1 / i_f("op2")) || (vt1.i_vt_op1 / i_f("op2") != vt1.i_vt_op1 / (int)i_f("op2")) || (vt1.i_vt_op1 / (int)i_f("op2") != vt1.i_vt_op1 / i_f("op2")) || (vt1.i_vt_op1 / i_f("op2") != vt1.i_vt_op1 / i_f("op2")) || ((decimal)(vt1.i_vt_op1 / i_f("op2")) != vt1.i_vt_op1 / i_f("op2")) || (vt1.i_vt_op1 / i_f("op2") != vt1.i_vt_op1 / i_f("op2")) || (vt1.i_vt_op1 / i_f("op2") != 2))
            {
                Console.WriteLine("testcase 35 failed");
                passed = false;
            }
            if ((vt1.i_vt_op1 / cl1.i_cl_op2 != vt1.i_vt_op1 / cl1.ui_cl_op2) || (vt1.i_vt_op1 / cl1.ui_cl_op2 != vt1.i_vt_op1 / cl1.l_cl_op2) || (vt1.i_vt_op1 / cl1.l_cl_op2 != vt1.i_vt_op1 / (int)cl1.ul_cl_op2) || (vt1.i_vt_op1 / (int)cl1.ul_cl_op2 != vt1.i_vt_op1 / cl1.f_cl_op2) || (vt1.i_vt_op1 / cl1.f_cl_op2 != vt1.i_vt_op1 / cl1.d_cl_op2) || ((decimal)(vt1.i_vt_op1 / cl1.d_cl_op2) != vt1.i_vt_op1 / cl1.m_cl_op2) || (vt1.i_vt_op1 / cl1.m_cl_op2 != vt1.i_vt_op1 / cl1.i_cl_op2) || (vt1.i_vt_op1 / cl1.i_cl_op2 != 2))
            {
                Console.WriteLine("testcase 36 failed");
                passed = false;
            }
            if ((vt1.i_vt_op1 / vt1.i_vt_op2 != vt1.i_vt_op1 / vt1.ui_vt_op2) || (vt1.i_vt_op1 / vt1.ui_vt_op2 != vt1.i_vt_op1 / vt1.l_vt_op2) || (vt1.i_vt_op1 / vt1.l_vt_op2 != vt1.i_vt_op1 / (int)vt1.ul_vt_op2) || (vt1.i_vt_op1 / (int)vt1.ul_vt_op2 != vt1.i_vt_op1 / vt1.f_vt_op2) || (vt1.i_vt_op1 / vt1.f_vt_op2 != vt1.i_vt_op1 / vt1.d_vt_op2) || ((decimal)(vt1.i_vt_op1 / vt1.d_vt_op2) != vt1.i_vt_op1 / vt1.m_vt_op2) || (vt1.i_vt_op1 / vt1.m_vt_op2 != vt1.i_vt_op1 / vt1.i_vt_op2) || (vt1.i_vt_op1 / vt1.i_vt_op2 != 2))
            {
                Console.WriteLine("testcase 37 failed");
                passed = false;
            }
            if ((vt1.i_vt_op1 / i_arr1d_op2[0] != vt1.i_vt_op1 / ui_arr1d_op2[0]) || (vt1.i_vt_op1 / ui_arr1d_op2[0] != vt1.i_vt_op1 / l_arr1d_op2[0]) || (vt1.i_vt_op1 / l_arr1d_op2[0] != vt1.i_vt_op1 / (int)ul_arr1d_op2[0]) || (vt1.i_vt_op1 / (int)ul_arr1d_op2[0] != vt1.i_vt_op1 / f_arr1d_op2[0]) || (vt1.i_vt_op1 / f_arr1d_op2[0] != vt1.i_vt_op1 / d_arr1d_op2[0]) || ((decimal)(vt1.i_vt_op1 / d_arr1d_op2[0]) != vt1.i_vt_op1 / m_arr1d_op2[0]) || (vt1.i_vt_op1 / m_arr1d_op2[0] != vt1.i_vt_op1 / i_arr1d_op2[0]) || (vt1.i_vt_op1 / i_arr1d_op2[0] != 2))
            {
                Console.WriteLine("testcase 38 failed");
                passed = false;
            }
            if ((vt1.i_vt_op1 / i_arr2d_op2[index[0, 1], index[1, 0]] != vt1.i_vt_op1 / ui_arr2d_op2[index[0, 1], index[1, 0]]) || (vt1.i_vt_op1 / ui_arr2d_op2[index[0, 1], index[1, 0]] != vt1.i_vt_op1 / l_arr2d_op2[index[0, 1], index[1, 0]]) || (vt1.i_vt_op1 / l_arr2d_op2[index[0, 1], index[1, 0]] != vt1.i_vt_op1 / (int)ul_arr2d_op2[index[0, 1], index[1, 0]]) || (vt1.i_vt_op1 / (int)ul_arr2d_op2[index[0, 1], index[1, 0]] != vt1.i_vt_op1 / f_arr2d_op2[index[0, 1], index[1, 0]]) || (vt1.i_vt_op1 / f_arr2d_op2[index[0, 1], index[1, 0]] != vt1.i_vt_op1 / d_arr2d_op2[index[0, 1], index[1, 0]]) || ((decimal)(vt1.i_vt_op1 / d_arr2d_op2[index[0, 1], index[1, 0]]) != vt1.i_vt_op1 / m_arr2d_op2[index[0, 1], index[1, 0]]) || (vt1.i_vt_op1 / m_arr2d_op2[index[0, 1], index[1, 0]] != vt1.i_vt_op1 / i_arr2d_op2[index[0, 1], index[1, 0]]) || (vt1.i_vt_op1 / i_arr2d_op2[index[0, 1], index[1, 0]] != 2))
            {
                Console.WriteLine("testcase 39 failed");
                passed = false;
            }
            if ((vt1.i_vt_op1 / i_arr3d_op2[index[0, 0], 0, index[1, 1]] != vt1.i_vt_op1 / ui_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (vt1.i_vt_op1 / ui_arr3d_op2[index[0, 0], 0, index[1, 1]] != vt1.i_vt_op1 / l_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (vt1.i_vt_op1 / l_arr3d_op2[index[0, 0], 0, index[1, 1]] != vt1.i_vt_op1 / (int)ul_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (vt1.i_vt_op1 / (int)ul_arr3d_op2[index[0, 0], 0, index[1, 1]] != vt1.i_vt_op1 / f_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (vt1.i_vt_op1 / f_arr3d_op2[index[0, 0], 0, index[1, 1]] != vt1.i_vt_op1 / d_arr3d_op2[index[0, 0], 0, index[1, 1]]) || ((decimal)(vt1.i_vt_op1 / d_arr3d_op2[index[0, 0], 0, index[1, 1]]) != vt1.i_vt_op1 / m_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (vt1.i_vt_op1 / m_arr3d_op2[index[0, 0], 0, index[1, 1]] != vt1.i_vt_op1 / i_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (vt1.i_vt_op1 / i_arr3d_op2[index[0, 0], 0, index[1, 1]] != 2))
            {
                Console.WriteLine("testcase 40 failed");
                passed = false;
            }
            if ((i_arr1d_op1[1] / i_l_op2 != i_arr1d_op1[1] / ui_l_op2) || (i_arr1d_op1[1] / ui_l_op2 != i_arr1d_op1[1] / l_l_op2) || (i_arr1d_op1[1] / l_l_op2 != i_arr1d_op1[1] / (int)ul_l_op2) || (i_arr1d_op1[1] / (int)ul_l_op2 != i_arr1d_op1[1] / f_l_op2) || (i_arr1d_op1[1] / f_l_op2 != i_arr1d_op1[1] / d_l_op2) || ((decimal)(i_arr1d_op1[1] / d_l_op2) != i_arr1d_op1[1] / m_l_op2) || (i_arr1d_op1[1] / m_l_op2 != i_arr1d_op1[1] / i_l_op2) || (i_arr1d_op1[1] / i_l_op2 != 2))
            {
                Console.WriteLine("testcase 41 failed");
                passed = false;
            }
            if ((i_arr1d_op1[1] / s_i_s_op2 != i_arr1d_op1[1] / s_ui_s_op2) || (i_arr1d_op1[1] / s_ui_s_op2 != i_arr1d_op1[1] / s_l_s_op2) || (i_arr1d_op1[1] / s_l_s_op2 != i_arr1d_op1[1] / (int)s_ul_s_op2) || (i_arr1d_op1[1] / (int)s_ul_s_op2 != i_arr1d_op1[1] / s_f_s_op2) || (i_arr1d_op1[1] / s_f_s_op2 != i_arr1d_op1[1] / s_d_s_op2) || ((decimal)(i_arr1d_op1[1] / s_d_s_op2) != i_arr1d_op1[1] / s_m_s_op2) || (i_arr1d_op1[1] / s_m_s_op2 != i_arr1d_op1[1] / s_i_s_op2) || (i_arr1d_op1[1] / s_i_s_op2 != 2))
            {
                Console.WriteLine("testcase 42 failed");
                passed = false;
            }
            if ((i_arr1d_op1[1] / i_f("op2") != i_arr1d_op1[1] / i_f("op2")) || (i_arr1d_op1[1] / i_f("op2") != i_arr1d_op1[1] / i_f("op2")) || (i_arr1d_op1[1] / i_f("op2") != i_arr1d_op1[1] / (int)i_f("op2")) || (i_arr1d_op1[1] / (int)i_f("op2") != i_arr1d_op1[1] / i_f("op2")) || (i_arr1d_op1[1] / i_f("op2") != i_arr1d_op1[1] / i_f("op2")) || ((decimal)(i_arr1d_op1[1] / i_f("op2")) != i_arr1d_op1[1] / i_f("op2")) || (i_arr1d_op1[1] / i_f("op2") != i_arr1d_op1[1] / i_f("op2")) || (i_arr1d_op1[1] / i_f("op2") != 2))
            {
                Console.WriteLine("testcase 43 failed");
                passed = false;
            }
            if ((i_arr1d_op1[1] / cl1.i_cl_op2 != i_arr1d_op1[1] / cl1.ui_cl_op2) || (i_arr1d_op1[1] / cl1.ui_cl_op2 != i_arr1d_op1[1] / cl1.l_cl_op2) || (i_arr1d_op1[1] / cl1.l_cl_op2 != i_arr1d_op1[1] / (int)cl1.ul_cl_op2) || (i_arr1d_op1[1] / (int)cl1.ul_cl_op2 != i_arr1d_op1[1] / cl1.f_cl_op2) || (i_arr1d_op1[1] / cl1.f_cl_op2 != i_arr1d_op1[1] / cl1.d_cl_op2) || ((decimal)(i_arr1d_op1[1] / cl1.d_cl_op2) != i_arr1d_op1[1] / cl1.m_cl_op2) || (i_arr1d_op1[1] / cl1.m_cl_op2 != i_arr1d_op1[1] / cl1.i_cl_op2) || (i_arr1d_op1[1] / cl1.i_cl_op2 != 2))
            {
                Console.WriteLine("testcase 44 failed");
                passed = false;
            }
            if ((i_arr1d_op1[1] / vt1.i_vt_op2 != i_arr1d_op1[1] / vt1.ui_vt_op2) || (i_arr1d_op1[1] / vt1.ui_vt_op2 != i_arr1d_op1[1] / vt1.l_vt_op2) || (i_arr1d_op1[1] / vt1.l_vt_op2 != i_arr1d_op1[1] / (int)vt1.ul_vt_op2) || (i_arr1d_op1[1] / (int)vt1.ul_vt_op2 != i_arr1d_op1[1] / vt1.f_vt_op2) || (i_arr1d_op1[1] / vt1.f_vt_op2 != i_arr1d_op1[1] / vt1.d_vt_op2) || ((decimal)(i_arr1d_op1[1] / vt1.d_vt_op2) != i_arr1d_op1[1] / vt1.m_vt_op2) || (i_arr1d_op1[1] / vt1.m_vt_op2 != i_arr1d_op1[1] / vt1.i_vt_op2) || (i_arr1d_op1[1] / vt1.i_vt_op2 != 2))
            {
                Console.WriteLine("testcase 45 failed");
                passed = false;
            }
            if ((i_arr1d_op1[1] / i_arr1d_op2[0] != i_arr1d_op1[1] / ui_arr1d_op2[0]) || (i_arr1d_op1[1] / ui_arr1d_op2[0] != i_arr1d_op1[1] / l_arr1d_op2[0]) || (i_arr1d_op1[1] / l_arr1d_op2[0] != i_arr1d_op1[1] / (int)ul_arr1d_op2[0]) || (i_arr1d_op1[1] / (int)ul_arr1d_op2[0] != i_arr1d_op1[1] / f_arr1d_op2[0]) || (i_arr1d_op1[1] / f_arr1d_op2[0] != i_arr1d_op1[1] / d_arr1d_op2[0]) || ((decimal)(i_arr1d_op1[1] / d_arr1d_op2[0]) != i_arr1d_op1[1] / m_arr1d_op2[0]) || (i_arr1d_op1[1] / m_arr1d_op2[0] != i_arr1d_op1[1] / i_arr1d_op2[0]) || (i_arr1d_op1[1] / i_arr1d_op2[0] != 2))
            {
                Console.WriteLine("testcase 46 failed");
                passed = false;
            }
            if ((i_arr1d_op1[1] / i_arr2d_op2[index[0, 1], index[1, 0]] != i_arr1d_op1[1] / ui_arr2d_op2[index[0, 1], index[1, 0]]) || (i_arr1d_op1[1] / ui_arr2d_op2[index[0, 1], index[1, 0]] != i_arr1d_op1[1] / l_arr2d_op2[index[0, 1], index[1, 0]]) || (i_arr1d_op1[1] / l_arr2d_op2[index[0, 1], index[1, 0]] != i_arr1d_op1[1] / (int)ul_arr2d_op2[index[0, 1], index[1, 0]]) || (i_arr1d_op1[1] / (int)ul_arr2d_op2[index[0, 1], index[1, 0]] != i_arr1d_op1[1] / f_arr2d_op2[index[0, 1], index[1, 0]]) || (i_arr1d_op1[1] / f_arr2d_op2[index[0, 1], index[1, 0]] != i_arr1d_op1[1] / d_arr2d_op2[index[0, 1], index[1, 0]]) || ((decimal)(i_arr1d_op1[1] / d_arr2d_op2[index[0, 1], index[1, 0]]) != i_arr1d_op1[1] / m_arr2d_op2[index[0, 1], index[1, 0]]) || (i_arr1d_op1[1] / m_arr2d_op2[index[0, 1], index[1, 0]] != i_arr1d_op1[1] / i_arr2d_op2[index[0, 1], index[1, 0]]) || (i_arr1d_op1[1] / i_arr2d_op2[index[0, 1], index[1, 0]] != 2))
            {
                Console.WriteLine("testcase 47 failed");
                passed = false;
            }
            if ((i_arr1d_op1[1] / i_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_arr1d_op1[1] / ui_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_arr1d_op1[1] / ui_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_arr1d_op1[1] / l_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_arr1d_op1[1] / l_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_arr1d_op1[1] / (int)ul_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_arr1d_op1[1] / (int)ul_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_arr1d_op1[1] / f_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_arr1d_op1[1] / f_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_arr1d_op1[1] / d_arr3d_op2[index[0, 0], 0, index[1, 1]]) || ((decimal)(i_arr1d_op1[1] / d_arr3d_op2[index[0, 0], 0, index[1, 1]]) != i_arr1d_op1[1] / m_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_arr1d_op1[1] / m_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_arr1d_op1[1] / i_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_arr1d_op1[1] / i_arr3d_op2[index[0, 0], 0, index[1, 1]] != 2))
            {
                Console.WriteLine("testcase 48 failed");
                passed = false;
            }
            if ((i_arr2d_op1[index[0, 1], index[1, 0]] / i_l_op2 != i_arr2d_op1[index[0, 1], index[1, 0]] / ui_l_op2) || (i_arr2d_op1[index[0, 1], index[1, 0]] / ui_l_op2 != i_arr2d_op1[index[0, 1], index[1, 0]] / l_l_op2) || (i_arr2d_op1[index[0, 1], index[1, 0]] / l_l_op2 != i_arr2d_op1[index[0, 1], index[1, 0]] / (int)ul_l_op2) || (i_arr2d_op1[index[0, 1], index[1, 0]] / (int)ul_l_op2 != i_arr2d_op1[index[0, 1], index[1, 0]] / f_l_op2) || (i_arr2d_op1[index[0, 1], index[1, 0]] / f_l_op2 != i_arr2d_op1[index[0, 1], index[1, 0]] / d_l_op2) || ((decimal)(i_arr2d_op1[index[0, 1], index[1, 0]] / d_l_op2) != i_arr2d_op1[index[0, 1], index[1, 0]] / m_l_op2) || (i_arr2d_op1[index[0, 1], index[1, 0]] / m_l_op2 != i_arr2d_op1[index[0, 1], index[1, 0]] / i_l_op2) || (i_arr2d_op1[index[0, 1], index[1, 0]] / i_l_op2 != 2))
            {
                Console.WriteLine("testcase 49 failed");
                passed = false;
            }
            if ((i_arr2d_op1[index[0, 1], index[1, 0]] / s_i_s_op2 != i_arr2d_op1[index[0, 1], index[1, 0]] / s_ui_s_op2) || (i_arr2d_op1[index[0, 1], index[1, 0]] / s_ui_s_op2 != i_arr2d_op1[index[0, 1], index[1, 0]] / s_l_s_op2) || (i_arr2d_op1[index[0, 1], index[1, 0]] / s_l_s_op2 != i_arr2d_op1[index[0, 1], index[1, 0]] / (int)s_ul_s_op2) || (i_arr2d_op1[index[0, 1], index[1, 0]] / (int)s_ul_s_op2 != i_arr2d_op1[index[0, 1], index[1, 0]] / s_f_s_op2) || (i_arr2d_op1[index[0, 1], index[1, 0]] / s_f_s_op2 != i_arr2d_op1[index[0, 1], index[1, 0]] / s_d_s_op2) || ((decimal)(i_arr2d_op1[index[0, 1], index[1, 0]] / s_d_s_op2) != i_arr2d_op1[index[0, 1], index[1, 0]] / s_m_s_op2) || (i_arr2d_op1[index[0, 1], index[1, 0]] / s_m_s_op2 != i_arr2d_op1[index[0, 1], index[1, 0]] / s_i_s_op2) || (i_arr2d_op1[index[0, 1], index[1, 0]] / s_i_s_op2 != 2))
            {
                Console.WriteLine("testcase 50 failed");
                passed = false;
            }
            if ((i_arr2d_op1[index[0, 1], index[1, 0]] / i_f("op2") != i_arr2d_op1[index[0, 1], index[1, 0]] / i_f("op2")) || (i_arr2d_op1[index[0, 1], index[1, 0]] / i_f("op2") != i_arr2d_op1[index[0, 1], index[1, 0]] / i_f("op2")) || (i_arr2d_op1[index[0, 1], index[1, 0]] / i_f("op2") != i_arr2d_op1[index[0, 1], index[1, 0]] / (int)i_f("op2")) || (i_arr2d_op1[index[0, 1], index[1, 0]] / (int)i_f("op2") != i_arr2d_op1[index[0, 1], index[1, 0]] / i_f("op2")) || (i_arr2d_op1[index[0, 1], index[1, 0]] / i_f("op2") != i_arr2d_op1[index[0, 1], index[1, 0]] / i_f("op2")) || ((decimal)(i_arr2d_op1[index[0, 1], index[1, 0]] / i_f("op2")) != i_arr2d_op1[index[0, 1], index[1, 0]] / i_f("op2")) || (i_arr2d_op1[index[0, 1], index[1, 0]] / i_f("op2") != i_arr2d_op1[index[0, 1], index[1, 0]] / i_f("op2")) || (i_arr2d_op1[index[0, 1], index[1, 0]] / i_f("op2") != 2))
            {
                Console.WriteLine("testcase 51 failed");
                passed = false;
            }
            if ((i_arr2d_op1[index[0, 1], index[1, 0]] / cl1.i_cl_op2 != i_arr2d_op1[index[0, 1], index[1, 0]] / cl1.ui_cl_op2) || (i_arr2d_op1[index[0, 1], index[1, 0]] / cl1.ui_cl_op2 != i_arr2d_op1[index[0, 1], index[1, 0]] / cl1.l_cl_op2) || (i_arr2d_op1[index[0, 1], index[1, 0]] / cl1.l_cl_op2 != i_arr2d_op1[index[0, 1], index[1, 0]] / (int)cl1.ul_cl_op2) || (i_arr2d_op1[index[0, 1], index[1, 0]] / (int)cl1.ul_cl_op2 != i_arr2d_op1[index[0, 1], index[1, 0]] / cl1.f_cl_op2) || (i_arr2d_op1[index[0, 1], index[1, 0]] / cl1.f_cl_op2 != i_arr2d_op1[index[0, 1], index[1, 0]] / cl1.d_cl_op2) || ((decimal)(i_arr2d_op1[index[0, 1], index[1, 0]] / cl1.d_cl_op2) != i_arr2d_op1[index[0, 1], index[1, 0]] / cl1.m_cl_op2) || (i_arr2d_op1[index[0, 1], index[1, 0]] / cl1.m_cl_op2 != i_arr2d_op1[index[0, 1], index[1, 0]] / cl1.i_cl_op2) || (i_arr2d_op1[index[0, 1], index[1, 0]] / cl1.i_cl_op2 != 2))
            {
                Console.WriteLine("testcase 52 failed");
                passed = false;
            }
            if ((i_arr2d_op1[index[0, 1], index[1, 0]] / vt1.i_vt_op2 != i_arr2d_op1[index[0, 1], index[1, 0]] / vt1.ui_vt_op2) || (i_arr2d_op1[index[0, 1], index[1, 0]] / vt1.ui_vt_op2 != i_arr2d_op1[index[0, 1], index[1, 0]] / vt1.l_vt_op2) || (i_arr2d_op1[index[0, 1], index[1, 0]] / vt1.l_vt_op2 != i_arr2d_op1[index[0, 1], index[1, 0]] / (int)vt1.ul_vt_op2) || (i_arr2d_op1[index[0, 1], index[1, 0]] / (int)vt1.ul_vt_op2 != i_arr2d_op1[index[0, 1], index[1, 0]] / vt1.f_vt_op2) || (i_arr2d_op1[index[0, 1], index[1, 0]] / vt1.f_vt_op2 != i_arr2d_op1[index[0, 1], index[1, 0]] / vt1.d_vt_op2) || ((decimal)(i_arr2d_op1[index[0, 1], index[1, 0]] / vt1.d_vt_op2) != i_arr2d_op1[index[0, 1], index[1, 0]] / vt1.m_vt_op2) || (i_arr2d_op1[index[0, 1], index[1, 0]] / vt1.m_vt_op2 != i_arr2d_op1[index[0, 1], index[1, 0]] / vt1.i_vt_op2) || (i_arr2d_op1[index[0, 1], index[1, 0]] / vt1.i_vt_op2 != 2))
            {
                Console.WriteLine("testcase 53 failed");
                passed = false;
            }
            if ((i_arr2d_op1[index[0, 1], index[1, 0]] / i_arr1d_op2[0] != i_arr2d_op1[index[0, 1], index[1, 0]] / ui_arr1d_op2[0]) || (i_arr2d_op1[index[0, 1], index[1, 0]] / ui_arr1d_op2[0] != i_arr2d_op1[index[0, 1], index[1, 0]] / l_arr1d_op2[0]) || (i_arr2d_op1[index[0, 1], index[1, 0]] / l_arr1d_op2[0] != i_arr2d_op1[index[0, 1], index[1, 0]] / (int)ul_arr1d_op2[0]) || (i_arr2d_op1[index[0, 1], index[1, 0]] / (int)ul_arr1d_op2[0] != i_arr2d_op1[index[0, 1], index[1, 0]] / f_arr1d_op2[0]) || (i_arr2d_op1[index[0, 1], index[1, 0]] / f_arr1d_op2[0] != i_arr2d_op1[index[0, 1], index[1, 0]] / d_arr1d_op2[0]) || ((decimal)(i_arr2d_op1[index[0, 1], index[1, 0]] / d_arr1d_op2[0]) != i_arr2d_op1[index[0, 1], index[1, 0]] / m_arr1d_op2[0]) || (i_arr2d_op1[index[0, 1], index[1, 0]] / m_arr1d_op2[0] != i_arr2d_op1[index[0, 1], index[1, 0]] / i_arr1d_op2[0]) || (i_arr2d_op1[index[0, 1], index[1, 0]] / i_arr1d_op2[0] != 2))
            {
                Console.WriteLine("testcase 54 failed");
                passed = false;
            }
            if ((i_arr2d_op1[index[0, 1], index[1, 0]] / i_arr2d_op2[index[0, 1], index[1, 0]] != i_arr2d_op1[index[0, 1], index[1, 0]] / ui_arr2d_op2[index[0, 1], index[1, 0]]) || (i_arr2d_op1[index[0, 1], index[1, 0]] / ui_arr2d_op2[index[0, 1], index[1, 0]] != i_arr2d_op1[index[0, 1], index[1, 0]] / l_arr2d_op2[index[0, 1], index[1, 0]]) || (i_arr2d_op1[index[0, 1], index[1, 0]] / l_arr2d_op2[index[0, 1], index[1, 0]] != i_arr2d_op1[index[0, 1], index[1, 0]] / (int)ul_arr2d_op2[index[0, 1], index[1, 0]]) || (i_arr2d_op1[index[0, 1], index[1, 0]] / (int)ul_arr2d_op2[index[0, 1], index[1, 0]] != i_arr2d_op1[index[0, 1], index[1, 0]] / f_arr2d_op2[index[0, 1], index[1, 0]]) || (i_arr2d_op1[index[0, 1], index[1, 0]] / f_arr2d_op2[index[0, 1], index[1, 0]] != i_arr2d_op1[index[0, 1], index[1, 0]] / d_arr2d_op2[index[0, 1], index[1, 0]]) || ((decimal)(i_arr2d_op1[index[0, 1], index[1, 0]] / d_arr2d_op2[index[0, 1], index[1, 0]]) != i_arr2d_op1[index[0, 1], index[1, 0]] / m_arr2d_op2[index[0, 1], index[1, 0]]) || (i_arr2d_op1[index[0, 1], index[1, 0]] / m_arr2d_op2[index[0, 1], index[1, 0]] != i_arr2d_op1[index[0, 1], index[1, 0]] / i_arr2d_op2[index[0, 1], index[1, 0]]) || (i_arr2d_op1[index[0, 1], index[1, 0]] / i_arr2d_op2[index[0, 1], index[1, 0]] != 2))
            {
                Console.WriteLine("testcase 55 failed");
                passed = false;
            }
            if ((i_arr2d_op1[index[0, 1], index[1, 0]] / i_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_arr2d_op1[index[0, 1], index[1, 0]] / ui_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_arr2d_op1[index[0, 1], index[1, 0]] / ui_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_arr2d_op1[index[0, 1], index[1, 0]] / l_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_arr2d_op1[index[0, 1], index[1, 0]] / l_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_arr2d_op1[index[0, 1], index[1, 0]] / (int)ul_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_arr2d_op1[index[0, 1], index[1, 0]] / (int)ul_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_arr2d_op1[index[0, 1], index[1, 0]] / f_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_arr2d_op1[index[0, 1], index[1, 0]] / f_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_arr2d_op1[index[0, 1], index[1, 0]] / d_arr3d_op2[index[0, 0], 0, index[1, 1]]) || ((decimal)(i_arr2d_op1[index[0, 1], index[1, 0]] / d_arr3d_op2[index[0, 0], 0, index[1, 1]]) != i_arr2d_op1[index[0, 1], index[1, 0]] / m_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_arr2d_op1[index[0, 1], index[1, 0]] / m_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_arr2d_op1[index[0, 1], index[1, 0]] / i_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_arr2d_op1[index[0, 1], index[1, 0]] / i_arr3d_op2[index[0, 0], 0, index[1, 1]] != 2))
            {
                Console.WriteLine("testcase 56 failed");
                passed = false;
            }
            if ((i_arr3d_op1[index[0, 0], 0, index[1, 1]] / i_l_op2 != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / ui_l_op2) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / ui_l_op2 != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / l_l_op2) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / l_l_op2 != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / (int)ul_l_op2) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / (int)ul_l_op2 != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / f_l_op2) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / f_l_op2 != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / d_l_op2) || ((decimal)(i_arr3d_op1[index[0, 0], 0, index[1, 1]] / d_l_op2) != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / m_l_op2) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / m_l_op2 != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / i_l_op2) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / i_l_op2 != 2))
            {
                Console.WriteLine("testcase 57 failed");
                passed = false;
            }
            if ((i_arr3d_op1[index[0, 0], 0, index[1, 1]] / s_i_s_op2 != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / s_ui_s_op2) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / s_ui_s_op2 != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / s_l_s_op2) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / s_l_s_op2 != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / (int)s_ul_s_op2) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / (int)s_ul_s_op2 != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / s_f_s_op2) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / s_f_s_op2 != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / s_d_s_op2) || ((decimal)(i_arr3d_op1[index[0, 0], 0, index[1, 1]] / s_d_s_op2) != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / s_m_s_op2) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / s_m_s_op2 != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / s_i_s_op2) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / s_i_s_op2 != 2))
            {
                Console.WriteLine("testcase 58 failed");
                passed = false;
            }
            if ((i_arr3d_op1[index[0, 0], 0, index[1, 1]] / i_f("op2") != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / i_f("op2")) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / i_f("op2") != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / i_f("op2")) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / i_f("op2") != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / (int)i_f("op2")) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / (int)i_f("op2") != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / i_f("op2")) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / i_f("op2") != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / i_f("op2")) || ((decimal)(i_arr3d_op1[index[0, 0], 0, index[1, 1]] / i_f("op2")) != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / i_f("op2")) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / i_f("op2") != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / i_f("op2")) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / i_f("op2") != 2))
            {
                Console.WriteLine("testcase 59 failed");
                passed = false;
            }
            if ((i_arr3d_op1[index[0, 0], 0, index[1, 1]] / cl1.i_cl_op2 != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / cl1.ui_cl_op2) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / cl1.ui_cl_op2 != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / cl1.l_cl_op2) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / cl1.l_cl_op2 != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / (int)cl1.ul_cl_op2) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / (int)cl1.ul_cl_op2 != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / cl1.f_cl_op2) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / cl1.f_cl_op2 != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / cl1.d_cl_op2) || ((decimal)(i_arr3d_op1[index[0, 0], 0, index[1, 1]] / cl1.d_cl_op2) != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / cl1.m_cl_op2) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / cl1.m_cl_op2 != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / cl1.i_cl_op2) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / cl1.i_cl_op2 != 2))
            {
                Console.WriteLine("testcase 60 failed");
                passed = false;
            }
            if ((i_arr3d_op1[index[0, 0], 0, index[1, 1]] / vt1.i_vt_op2 != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / vt1.ui_vt_op2) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / vt1.ui_vt_op2 != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / vt1.l_vt_op2) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / vt1.l_vt_op2 != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / (int)vt1.ul_vt_op2) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / (int)vt1.ul_vt_op2 != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / vt1.f_vt_op2) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / vt1.f_vt_op2 != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / vt1.d_vt_op2) || ((decimal)(i_arr3d_op1[index[0, 0], 0, index[1, 1]] / vt1.d_vt_op2) != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / vt1.m_vt_op2) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / vt1.m_vt_op2 != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / vt1.i_vt_op2) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / vt1.i_vt_op2 != 2))
            {
                Console.WriteLine("testcase 61 failed");
                passed = false;
            }
            if ((i_arr3d_op1[index[0, 0], 0, index[1, 1]] / i_arr1d_op2[0] != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / ui_arr1d_op2[0]) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / ui_arr1d_op2[0] != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / l_arr1d_op2[0]) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / l_arr1d_op2[0] != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / (int)ul_arr1d_op2[0]) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / (int)ul_arr1d_op2[0] != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / f_arr1d_op2[0]) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / f_arr1d_op2[0] != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / d_arr1d_op2[0]) || ((decimal)(i_arr3d_op1[index[0, 0], 0, index[1, 1]] / d_arr1d_op2[0]) != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / m_arr1d_op2[0]) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / m_arr1d_op2[0] != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / i_arr1d_op2[0]) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / i_arr1d_op2[0] != 2))
            {
                Console.WriteLine("testcase 62 failed");
                passed = false;
            }
            if ((i_arr3d_op1[index[0, 0], 0, index[1, 1]] / i_arr2d_op2[index[0, 1], index[1, 0]] != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / ui_arr2d_op2[index[0, 1], index[1, 0]]) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / ui_arr2d_op2[index[0, 1], index[1, 0]] != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / l_arr2d_op2[index[0, 1], index[1, 0]]) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / l_arr2d_op2[index[0, 1], index[1, 0]] != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / (int)ul_arr2d_op2[index[0, 1], index[1, 0]]) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / (int)ul_arr2d_op2[index[0, 1], index[1, 0]] != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / f_arr2d_op2[index[0, 1], index[1, 0]]) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / f_arr2d_op2[index[0, 1], index[1, 0]] != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / d_arr2d_op2[index[0, 1], index[1, 0]]) || ((decimal)(i_arr3d_op1[index[0, 0], 0, index[1, 1]] / d_arr2d_op2[index[0, 1], index[1, 0]]) != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / m_arr2d_op2[index[0, 1], index[1, 0]]) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / m_arr2d_op2[index[0, 1], index[1, 0]] != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / i_arr2d_op2[index[0, 1], index[1, 0]]) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / i_arr2d_op2[index[0, 1], index[1, 0]] != 2))
            {
                Console.WriteLine("testcase 63 failed");
                passed = false;
            }
            if ((i_arr3d_op1[index[0, 0], 0, index[1, 1]] / i_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / ui_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / ui_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / l_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / l_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / (int)ul_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / (int)ul_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / f_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / f_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / d_arr3d_op2[index[0, 0], 0, index[1, 1]]) || ((decimal)(i_arr3d_op1[index[0, 0], 0, index[1, 1]] / d_arr3d_op2[index[0, 0], 0, index[1, 1]]) != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / m_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / m_arr3d_op2[index[0, 0], 0, index[1, 1]] != i_arr3d_op1[index[0, 0], 0, index[1, 1]] / i_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (i_arr3d_op1[index[0, 0], 0, index[1, 1]] / i_arr3d_op2[index[0, 0], 0, index[1, 1]] != 2))
            {
                Console.WriteLine("testcase 64 failed");
                passed = false;
            }
        }

        if (!passed)
        {
            Console.WriteLine("FAILED");
            return 1;
        }
        else
        {
            Console.WriteLine("PASSED");
            return 100;
        }
    }
}
