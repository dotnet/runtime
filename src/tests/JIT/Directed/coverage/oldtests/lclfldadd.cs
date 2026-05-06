// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//Testing simple math on local vars and fields - add

#pragma warning disable 0414
using System;
using Xunit;
public class lclfldadd
{
    //user-defined class that overloads operator +
    public class numHolder
    {
        private int _i_num;
        private uint _ui_num;
        private long _l_num;
        private ulong _ul_num;
        private float _f_num;
        private double _d_num;
        private decimal _m_num;
        public numHolder(int i_num)
        {
            _i_num = Convert.ToInt32(i_num);
            _ui_num = Convert.ToUInt32(i_num);
            _l_num = Convert.ToInt64(i_num);
            _ul_num = Convert.ToUInt64(i_num);
            _f_num = Convert.ToSingle(i_num);
            _d_num = Convert.ToDouble(i_num);
            _m_num = Convert.ToDecimal(i_num);
        }

        public static int operator +(numHolder a, int b)
        {
            return a._i_num + b;
        }

        public numHolder(uint ui_num)
        {
            _i_num = Convert.ToInt32(ui_num);
            _ui_num = Convert.ToUInt32(ui_num);
            _l_num = Convert.ToInt64(ui_num);
            _ul_num = Convert.ToUInt64(ui_num);
            _f_num = Convert.ToSingle(ui_num);
            _d_num = Convert.ToDouble(ui_num);
            _m_num = Convert.ToDecimal(ui_num);
        }

        public static uint operator +(numHolder a, uint b)
        {
            return a._ui_num + b;
        }

        public numHolder(long l_num)
        {
            _i_num = Convert.ToInt32(l_num);
            _ui_num = Convert.ToUInt32(l_num);
            _l_num = Convert.ToInt64(l_num);
            _ul_num = Convert.ToUInt64(l_num);
            _f_num = Convert.ToSingle(l_num);
            _d_num = Convert.ToDouble(l_num);
            _m_num = Convert.ToDecimal(l_num);
        }

        public static long operator +(numHolder a, long b)
        {
            return a._l_num + b;
        }

        public numHolder(ulong ul_num)
        {
            _i_num = Convert.ToInt32(ul_num);
            _ui_num = Convert.ToUInt32(ul_num);
            _l_num = Convert.ToInt64(ul_num);
            _ul_num = Convert.ToUInt64(ul_num);
            _f_num = Convert.ToSingle(ul_num);
            _d_num = Convert.ToDouble(ul_num);
            _m_num = Convert.ToDecimal(ul_num);
        }

        public static long operator +(numHolder a, ulong b)
        {
            return (long)(a._ul_num + b);
        }

        public numHolder(float f_num)
        {
            _i_num = Convert.ToInt32(f_num);
            _ui_num = Convert.ToUInt32(f_num);
            _l_num = Convert.ToInt64(f_num);
            _ul_num = Convert.ToUInt64(f_num);
            _f_num = Convert.ToSingle(f_num);
            _d_num = Convert.ToDouble(f_num);
            _m_num = Convert.ToDecimal(f_num);
        }

        public static float operator +(numHolder a, float b)
        {
            return a._f_num + b;
        }

        public numHolder(double d_num)
        {
            _i_num = Convert.ToInt32(d_num);
            _ui_num = Convert.ToUInt32(d_num);
            _l_num = Convert.ToInt64(d_num);
            _ul_num = Convert.ToUInt64(d_num);
            _f_num = Convert.ToSingle(d_num);
            _d_num = Convert.ToDouble(d_num);
            _m_num = Convert.ToDecimal(d_num);
        }

        public static double operator +(numHolder a, double b)
        {
            return a._d_num + b;
        }

        public numHolder(decimal m_num)
        {
            _i_num = Convert.ToInt32(m_num);
            _ui_num = Convert.ToUInt32(m_num);
            _l_num = Convert.ToInt64(m_num);
            _ul_num = Convert.ToUInt64(m_num);
            _f_num = Convert.ToSingle(m_num);
            _d_num = Convert.ToDouble(m_num);
            _m_num = Convert.ToDecimal(m_num);
        }

        public static int operator +(numHolder a, decimal b)
        {
            return (int)(a._m_num + b);
        }

        public static int operator +(numHolder a, numHolder b)
        {
            return a._i_num + b._i_num;
        }
    }

    private static int s_i_s_op1 = 1;
    private static uint s_ui_s_op1 = 1;
    private static long s_l_s_op1 = 1;
    private static ulong s_ul_s_op1 = 1;
    private static float s_f_s_op1 = 1;
    private static double s_d_s_op1 = 1;
    private static decimal s_m_s_op1 = 1;

    private static int s_i_s_op2 = 8;
    private static uint s_ui_s_op2 = 8;
    private static long s_l_s_op2 = 8;
    private static ulong s_ul_s_op2 = 8;
    private static float s_f_s_op2 = 8;
    private static double s_d_s_op2 = 8;
    private static decimal s_m_s_op2 = 8;
    private static numHolder s_nHldr_s_op2 = new numHolder(8);

    public static int i_f(String s)
    {
        if (s == "op1")
            return 1;
        else
            return 8;
    }
    public static uint ui_f(String s)
    {
        if (s == "op1")
            return 1;
        else
            return 8;
    }
    public static long l_f(String s)
    {
        if (s == "op1")
            return 1;
        else
            return 8;
    }
    public static ulong ul_f(String s)
    {
        if (s == "op1")
            return 1;
        else
            return 8;
    }
    public static float f_f(String s)
    {
        if (s == "op1")
            return 1;
        else
            return 8;
    }
    public static double d_f(String s)
    {
        if (s == "op1")
            return 1;
        else
            return 8;
    }
    public static decimal m_f(String s)
    {
        if (s == "op1")
            return 1;
        else
            return 8;
    }
    public static numHolder nHldr_f(String s)
    {
        if (s == "op1")
            return new numHolder(1);
        else
            return new numHolder(8);
    }
    private class CL
    {
        public int i_cl_op1 = 1;
        public uint ui_cl_op1 = 1;
        public long l_cl_op1 = 1;
        public ulong ul_cl_op1 = 1;
        public float f_cl_op1 = 1;
        public double d_cl_op1 = 1;
        public decimal m_cl_op1 = 1;

        public int i_cl_op2 = 8;
        public uint ui_cl_op2 = 8;
        public long l_cl_op2 = 8;
        public ulong ul_cl_op2 = 8;
        public float f_cl_op2 = 8;
        public double d_cl_op2 = 8;
        public decimal m_cl_op2 = 8;
        public numHolder nHldr_cl_op2 = new numHolder(8);
    }

    private struct VT
    {
        public int i_vt_op1;
        public uint ui_vt_op1;
        public long l_vt_op1;
        public ulong ul_vt_op1;
        public float f_vt_op1;
        public double d_vt_op1;
        public decimal m_vt_op1;

        public int i_vt_op2;
        public uint ui_vt_op2;
        public long l_vt_op2;
        public ulong ul_vt_op2;
        public float f_vt_op2;
        public double d_vt_op2;
        public decimal m_vt_op2;
        public numHolder nHldr_vt_op2;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool passed = true;
        //initialize class
        CL cl1 = new CL();
        //initialize struct
        VT vt1;
        vt1.i_vt_op1 = 1;
        vt1.ui_vt_op1 = 1;
        vt1.l_vt_op1 = 1;
        vt1.ul_vt_op1 = 1;
        vt1.f_vt_op1 = 1;
        vt1.d_vt_op1 = 1;
        vt1.m_vt_op1 = 1;
        vt1.i_vt_op2 = 8;
        vt1.ui_vt_op2 = 8;
        vt1.l_vt_op2 = 8;
        vt1.ul_vt_op2 = 8;
        vt1.f_vt_op2 = 8;
        vt1.d_vt_op2 = 8;
        vt1.m_vt_op2 = 8;
        vt1.nHldr_vt_op2 = new numHolder(8);

        int[] i_arr1d_op1 = { 0, 1 };
        int[,] i_arr2d_op1 = { { 0, 1 }, { 1, 1 } };
        int[,,] i_arr3d_op1 = { { { 0, 1 }, { 1, 1 } } };
        uint[] ui_arr1d_op1 = { 0, 1 };
        uint[,] ui_arr2d_op1 = { { 0, 1 }, { 1, 1 } };
        uint[,,] ui_arr3d_op1 = { { { 0, 1 }, { 1, 1 } } };
        long[] l_arr1d_op1 = { 0, 1 };
        long[,] l_arr2d_op1 = { { 0, 1 }, { 1, 1 } };
        long[,,] l_arr3d_op1 = { { { 0, 1 }, { 1, 1 } } };
        ulong[] ul_arr1d_op1 = { 0, 1 };
        ulong[,] ul_arr2d_op1 = { { 0, 1 }, { 1, 1 } };
        ulong[,,] ul_arr3d_op1 = { { { 0, 1 }, { 1, 1 } } };
        float[] f_arr1d_op1 = { 0, 1 };
        float[,] f_arr2d_op1 = { { 0, 1 }, { 1, 1 } };
        float[,,] f_arr3d_op1 = { { { 0, 1 }, { 1, 1 } } };
        double[] d_arr1d_op1 = { 0, 1 };
        double[,] d_arr2d_op1 = { { 0, 1 }, { 1, 1 } };
        double[,,] d_arr3d_op1 = { { { 0, 1 }, { 1, 1 } } };
        decimal[] m_arr1d_op1 = { 0, 1 };
        decimal[,] m_arr2d_op1 = { { 0, 1 }, { 1, 1 } };
        decimal[,,] m_arr3d_op1 = { { { 0, 1 }, { 1, 1 } } };

        int[] i_arr1d_op2 = { 8, 0, 1 };
        int[,] i_arr2d_op2 = { { 0, 8 }, { 1, 1 } };
        int[,,] i_arr3d_op2 = { { { 0, 8 }, { 1, 1 } } };
        uint[] ui_arr1d_op2 = { 8, 0, 1 };
        uint[,] ui_arr2d_op2 = { { 0, 8 }, { 1, 1 } };
        uint[,,] ui_arr3d_op2 = { { { 0, 8 }, { 1, 1 } } };
        long[] l_arr1d_op2 = { 8, 0, 1 };
        long[,] l_arr2d_op2 = { { 0, 8 }, { 1, 1 } };
        long[,,] l_arr3d_op2 = { { { 0, 8 }, { 1, 1 } } };
        ulong[] ul_arr1d_op2 = { 8, 0, 1 };
        ulong[,] ul_arr2d_op2 = { { 0, 8 }, { 1, 1 } };
        ulong[,,] ul_arr3d_op2 = { { { 0, 8 }, { 1, 1 } } };
        float[] f_arr1d_op2 = { 8, 0, 1 };
        float[,] f_arr2d_op2 = { { 0, 8 }, { 1, 1 } };
        float[,,] f_arr3d_op2 = { { { 0, 8 }, { 1, 1 } } };
        double[] d_arr1d_op2 = { 8, 0, 1 };
        double[,] d_arr2d_op2 = { { 0, 8 }, { 1, 1 } };
        double[,,] d_arr3d_op2 = { { { 0, 8 }, { 1, 1 } } };
        decimal[] m_arr1d_op2 = { 8, 0, 1 };
        decimal[,] m_arr2d_op2 = { { 0, 8 }, { 1, 1 } };
        decimal[,,] m_arr3d_op2 = { { { 0, 8 }, { 1, 1 } } };
        numHolder[] nHldr_arr1d_op2 = { new numHolder(8), new numHolder(0), new numHolder(1) };
        numHolder[,] nHldr_arr2d_op2 = { { new numHolder(0), new numHolder(8) }, { new numHolder(1), new numHolder(1) } };
        numHolder[,,] nHldr_arr3d_op2 = { { { new numHolder(0), new numHolder(8) }, { new numHolder(1), new numHolder(1) } } };

        int[,] index = { { 0, 0 }, { 1, 1 } };

        {
            int i_l_op1 = 1;
            int i_l_op2 = 8;
            uint ui_l_op2 = 8;
            long l_l_op2 = 8;
            ulong ul_l_op2 = 8;
            float f_l_op2 = 8;
            double d_l_op2 = 8;
            decimal m_l_op2 = 8;
            numHolder nHldr_l_op2 = new numHolder(8);
            if ((i_l_op1 + i_l_op2 != i_l_op1 + ui_l_op2) || (i_l_op1 + ui_l_op2 != i_l_op1 + l_l_op2) || (i_l_op1 + l_l_op2 != i_l_op1 + (int)ul_l_op2) || (i_l_op1 + (int)ul_l_op2 != i_l_op1 + f_l_op2) || (i_l_op1 + f_l_op2 != i_l_op1 + d_l_op2) || ((decimal)(i_l_op1 + d_l_op2) != i_l_op1 + m_l_op2) || (i_l_op1 + m_l_op2 != i_l_op1 + i_l_op2) || (i_l_op1 + i_l_op2 != 9))
            {
                Console.WriteLine("testcase 1 failed");
                passed = false;
            }
            if ((i_l_op1 + s_i_s_op2 != i_l_op1 + s_ui_s_op2) || (i_l_op1 + s_ui_s_op2 != i_l_op1 + s_l_s_op2) || (i_l_op1 + s_l_s_op2 != i_l_op1 + (int)s_ul_s_op2) || (i_l_op1 + (int)s_ul_s_op2 != i_l_op1 + s_f_s_op2) || (i_l_op1 + s_f_s_op2 != i_l_op1 + s_d_s_op2) || ((decimal)(i_l_op1 + s_d_s_op2) != i_l_op1 + s_m_s_op2) || (i_l_op1 + s_m_s_op2 != i_l_op1 + s_i_s_op2) || (i_l_op1 + s_i_s_op2 != 9))
            {
                Console.WriteLine("testcase 2 failed");
                passed = false;
            }
            if ((s_i_s_op1 + i_l_op2 != s_i_s_op1 + ui_l_op2) || (s_i_s_op1 + ui_l_op2 != s_i_s_op1 + l_l_op2) || (s_i_s_op1 + l_l_op2 != s_i_s_op1 + (int)ul_l_op2) || (s_i_s_op1 + (int)ul_l_op2 != s_i_s_op1 + f_l_op2) || (s_i_s_op1 + f_l_op2 != s_i_s_op1 + d_l_op2) || ((decimal)(s_i_s_op1 + d_l_op2) != s_i_s_op1 + m_l_op2) || (s_i_s_op1 + m_l_op2 != s_i_s_op1 + i_l_op2) || (s_i_s_op1 + i_l_op2 != 9))
            {
                Console.WriteLine("testcase 3 failed");
                passed = false;
            }
            if ((s_i_s_op1 + s_i_s_op2 != s_i_s_op1 + s_ui_s_op2) || (s_i_s_op1 + s_ui_s_op2 != s_i_s_op1 + s_l_s_op2) || (s_i_s_op1 + s_l_s_op2 != s_i_s_op1 + (int)s_ul_s_op2) || (s_i_s_op1 + (int)s_ul_s_op2 != s_i_s_op1 + s_f_s_op2) || (s_i_s_op1 + s_f_s_op2 != s_i_s_op1 + s_d_s_op2) || ((decimal)(s_i_s_op1 + s_d_s_op2) != s_i_s_op1 + s_m_s_op2) || (s_i_s_op1 + s_m_s_op2 != s_i_s_op1 + s_i_s_op2) || (s_i_s_op1 + s_i_s_op2 != 9))
            {
                Console.WriteLine("testcase 4 failed");
                passed = false;
            }
        }

        {
            uint ui_l_op1 = 1;
            int i_l_op2 = 8;
            uint ui_l_op2 = 8;
            long l_l_op2 = 8;
            ulong ul_l_op2 = 8;
            float f_l_op2 = 8;
            double d_l_op2 = 8;
            decimal m_l_op2 = 8;
            numHolder nHldr_l_op2 = new numHolder(8);
            if ((ui_l_op1 + i_l_op2 != ui_l_op1 + ui_l_op2) || (ui_l_op1 + ui_l_op2 != ui_l_op1 + l_l_op2) || ((ulong)(ui_l_op1 + l_l_op2) != ui_l_op1 + ul_l_op2) || (ui_l_op1 + ul_l_op2 != ui_l_op1 + f_l_op2) || (ui_l_op1 + f_l_op2 != ui_l_op1 + d_l_op2) || ((decimal)(ui_l_op1 + d_l_op2) != ui_l_op1 + m_l_op2) || (ui_l_op1 + m_l_op2 != ui_l_op1 + i_l_op2) || (ui_l_op1 + i_l_op2 != 9))
            {
                Console.WriteLine("testcase 5 failed");
                passed = false;
            }
            if ((ui_l_op1 + s_i_s_op2 != ui_l_op1 + s_ui_s_op2) || (ui_l_op1 + s_ui_s_op2 != ui_l_op1 + s_l_s_op2) || ((ulong)(ui_l_op1 + s_l_s_op2) != ui_l_op1 + s_ul_s_op2) || (ui_l_op1 + s_ul_s_op2 != ui_l_op1 + s_f_s_op2) || (ui_l_op1 + s_f_s_op2 != ui_l_op1 + s_d_s_op2) || ((decimal)(ui_l_op1 + s_d_s_op2) != ui_l_op1 + s_m_s_op2) || (ui_l_op1 + s_m_s_op2 != ui_l_op1 + s_i_s_op2) || (ui_l_op1 + s_i_s_op2 != 9))
            {
                Console.WriteLine("testcase 6 failed");
                passed = false;
            }
            if ((s_ui_s_op1 + i_l_op2 != s_ui_s_op1 + ui_l_op2) || (s_ui_s_op1 + ui_l_op2 != s_ui_s_op1 + l_l_op2) || ((ulong)(s_ui_s_op1 + l_l_op2) != s_ui_s_op1 + ul_l_op2) || (s_ui_s_op1 + ul_l_op2 != s_ui_s_op1 + f_l_op2) || (s_ui_s_op1 + f_l_op2 != s_ui_s_op1 + d_l_op2) || ((decimal)(s_ui_s_op1 + d_l_op2) != s_ui_s_op1 + m_l_op2) || (s_ui_s_op1 + m_l_op2 != s_ui_s_op1 + i_l_op2) || (s_ui_s_op1 + i_l_op2 != 9))
            {
                Console.WriteLine("testcase 7 failed");
                passed = false;
            }
            if ((s_ui_s_op1 + s_i_s_op2 != s_ui_s_op1 + s_ui_s_op2) || (s_ui_s_op1 + s_ui_s_op2 != s_ui_s_op1 + s_l_s_op2) || ((ulong)(s_ui_s_op1 + s_l_s_op2) != s_ui_s_op1 + s_ul_s_op2) || (s_ui_s_op1 + s_ul_s_op2 != s_ui_s_op1 + s_f_s_op2) || (s_ui_s_op1 + s_f_s_op2 != s_ui_s_op1 + s_d_s_op2) || ((decimal)(s_ui_s_op1 + s_d_s_op2) != s_ui_s_op1 + s_m_s_op2) || (s_ui_s_op1 + s_m_s_op2 != s_ui_s_op1 + s_i_s_op2) || (s_ui_s_op1 + s_i_s_op2 != 9))
            {
                Console.WriteLine("testcase 8 failed");
                passed = false;
            }
        }

        {
            long l_l_op1 = 1;
            int i_l_op2 = 8;
            uint ui_l_op2 = 8;
            long l_l_op2 = 8;
            ulong ul_l_op2 = 8;
            float f_l_op2 = 8;
            double d_l_op2 = 8;
            decimal m_l_op2 = 8;
            numHolder nHldr_l_op2 = new numHolder(8);
            if ((l_l_op1 + i_l_op2 != l_l_op1 + ui_l_op2) || (l_l_op1 + ui_l_op2 != l_l_op1 + l_l_op2) || (l_l_op1 + l_l_op2 != l_l_op1 + (long)ul_l_op2) || (l_l_op1 + (long)ul_l_op2 != l_l_op1 + f_l_op2) || (l_l_op1 + f_l_op2 != l_l_op1 + d_l_op2) || ((decimal)(l_l_op1 + d_l_op2) != l_l_op1 + m_l_op2) || (l_l_op1 + m_l_op2 != l_l_op1 + i_l_op2) || (l_l_op1 + i_l_op2 != 9))
            {
                Console.WriteLine("testcase 9 failed");
                passed = false;
            }
            if ((l_l_op1 + s_i_s_op2 != l_l_op1 + s_ui_s_op2) || (l_l_op1 + s_ui_s_op2 != l_l_op1 + s_l_s_op2) || (l_l_op1 + s_l_s_op2 != l_l_op1 + (long)s_ul_s_op2) || (l_l_op1 + (long)s_ul_s_op2 != l_l_op1 + s_f_s_op2) || (l_l_op1 + s_f_s_op2 != l_l_op1 + s_d_s_op2) || ((decimal)(l_l_op1 + s_d_s_op2) != l_l_op1 + s_m_s_op2) || (l_l_op1 + s_m_s_op2 != l_l_op1 + s_i_s_op2) || (l_l_op1 + s_i_s_op2 != 9))
            {
                Console.WriteLine("testcase 10 failed");
                passed = false;
            }
            if ((s_l_s_op1 + i_l_op2 != s_l_s_op1 + ui_l_op2) || (s_l_s_op1 + ui_l_op2 != s_l_s_op1 + l_l_op2) || (s_l_s_op1 + l_l_op2 != s_l_s_op1 + (long)ul_l_op2) || (s_l_s_op1 + (long)ul_l_op2 != s_l_s_op1 + f_l_op2) || (s_l_s_op1 + f_l_op2 != s_l_s_op1 + d_l_op2) || ((decimal)(s_l_s_op1 + d_l_op2) != s_l_s_op1 + m_l_op2) || (s_l_s_op1 + m_l_op2 != s_l_s_op1 + i_l_op2) || (s_l_s_op1 + i_l_op2 != 9))
            {
                Console.WriteLine("testcase 11 failed");
                passed = false;
            }
            if ((s_l_s_op1 + s_i_s_op2 != s_l_s_op1 + s_ui_s_op2) || (s_l_s_op1 + s_ui_s_op2 != s_l_s_op1 + s_l_s_op2) || (s_l_s_op1 + s_l_s_op2 != s_l_s_op1 + (long)s_ul_s_op2) || (s_l_s_op1 + (long)s_ul_s_op2 != s_l_s_op1 + s_f_s_op2) || (s_l_s_op1 + s_f_s_op2 != s_l_s_op1 + s_d_s_op2) || ((decimal)(s_l_s_op1 + s_d_s_op2) != s_l_s_op1 + s_m_s_op2) || (s_l_s_op1 + s_m_s_op2 != s_l_s_op1 + s_i_s_op2) || (s_l_s_op1 + s_i_s_op2 != 9))
            {
                Console.WriteLine("testcase 12 failed");
                passed = false;
            }
        }

        {
            ulong ul_l_op1 = 1;
            int i_l_op2 = 8;
            uint ui_l_op2 = 8;
            long l_l_op2 = 8;
            ulong ul_l_op2 = 8;
            float f_l_op2 = 8;
            double d_l_op2 = 8;
            decimal m_l_op2 = 8;
            numHolder nHldr_l_op2 = new numHolder(8);
            if ((ul_l_op1 + (ulong)i_l_op2 != ul_l_op1 + ui_l_op2) || (ul_l_op1 + ui_l_op2 != ul_l_op1 + (ulong)l_l_op2) || (ul_l_op1 + (ulong)l_l_op2 != ul_l_op1 + ul_l_op2) || (ul_l_op1 + ul_l_op2 != ul_l_op1 + f_l_op2) || (ul_l_op1 + f_l_op2 != ul_l_op1 + d_l_op2) || ((decimal)(ul_l_op1 + d_l_op2) != ul_l_op1 + m_l_op2) || (ul_l_op1 + m_l_op2 != ul_l_op1 + (ulong)i_l_op2) || (ul_l_op1 + (ulong)i_l_op2 != 9))
            {
                Console.WriteLine("testcase 13 failed");
                passed = false;
            }
            if ((ul_l_op1 + (ulong)s_i_s_op2 != ul_l_op1 + s_ui_s_op2) || (ul_l_op1 + s_ui_s_op2 != ul_l_op1 + (ulong)s_l_s_op2) || (ul_l_op1 + (ulong)s_l_s_op2 != ul_l_op1 + s_ul_s_op2) || (ul_l_op1 + s_ul_s_op2 != ul_l_op1 + s_f_s_op2) || (ul_l_op1 + s_f_s_op2 != ul_l_op1 + s_d_s_op2) || ((decimal)(ul_l_op1 + s_d_s_op2) != ul_l_op1 + s_m_s_op2) || (ul_l_op1 + s_m_s_op2 != ul_l_op1 + (ulong)s_i_s_op2) || (ul_l_op1 + (ulong)s_i_s_op2 != 9))
            {
                Console.WriteLine("testcase 14 failed");
                passed = false;
            }
            if ((s_ul_s_op1 + (ulong)i_l_op2 != s_ul_s_op1 + ui_l_op2) || (s_ul_s_op1 + ui_l_op2 != s_ul_s_op1 + (ulong)l_l_op2) || (s_ul_s_op1 + (ulong)l_l_op2 != s_ul_s_op1 + ul_l_op2) || (s_ul_s_op1 + ul_l_op2 != s_ul_s_op1 + f_l_op2) || (s_ul_s_op1 + f_l_op2 != s_ul_s_op1 + d_l_op2) || ((decimal)(s_ul_s_op1 + d_l_op2) != s_ul_s_op1 + m_l_op2) || (s_ul_s_op1 + m_l_op2 != s_ul_s_op1 + (ulong)i_l_op2) || (s_ul_s_op1 + (ulong)i_l_op2 != 9))
            {
                Console.WriteLine("testcase 15 failed");
                passed = false;
            }
            if ((s_ul_s_op1 + (ulong)s_i_s_op2 != s_ul_s_op1 + s_ui_s_op2) || (s_ul_s_op1 + s_ui_s_op2 != s_ul_s_op1 + (ulong)s_l_s_op2) || (s_ul_s_op1 + (ulong)s_l_s_op2 != s_ul_s_op1 + s_ul_s_op2) || (s_ul_s_op1 + s_ul_s_op2 != s_ul_s_op1 + s_f_s_op2) || (s_ul_s_op1 + s_f_s_op2 != s_ul_s_op1 + s_d_s_op2) || ((decimal)(s_ul_s_op1 + s_d_s_op2) != s_ul_s_op1 + s_m_s_op2) || (s_ul_s_op1 + s_m_s_op2 != s_ul_s_op1 + (ulong)s_i_s_op2) || (s_ul_s_op1 + (ulong)s_i_s_op2 != 9))
            {
                Console.WriteLine("testcase 16 failed");
                passed = false;
            }
        }

        {
            float f_l_op1 = 1;
            int i_l_op2 = 8;
            uint ui_l_op2 = 8;
            long l_l_op2 = 8;
            ulong ul_l_op2 = 8;
            float f_l_op2 = 8;
            double d_l_op2 = 8;
            decimal m_l_op2 = 8;
            numHolder nHldr_l_op2 = new numHolder(8);
            if ((f_l_op1 + i_l_op2 != f_l_op1 + ui_l_op2) || (f_l_op1 + ui_l_op2 != f_l_op1 + l_l_op2) || (f_l_op1 + l_l_op2 != f_l_op1 + ul_l_op2) || (f_l_op1 + ul_l_op2 != f_l_op1 + f_l_op2) || (f_l_op1 + f_l_op2 != f_l_op1 + d_l_op2) || (f_l_op1 + d_l_op2 != f_l_op1 + (float)m_l_op2) || (f_l_op1 + (float)m_l_op2 != f_l_op1 + i_l_op2) || (f_l_op1 + i_l_op2 != 9))
            {
                Console.WriteLine("testcase 17 failed");
                passed = false;
            }
            if ((f_l_op1 + s_i_s_op2 != f_l_op1 + s_ui_s_op2) || (f_l_op1 + s_ui_s_op2 != f_l_op1 + s_l_s_op2) || (f_l_op1 + s_l_s_op2 != f_l_op1 + s_ul_s_op2) || (f_l_op1 + s_ul_s_op2 != f_l_op1 + s_f_s_op2) || (f_l_op1 + s_f_s_op2 != f_l_op1 + s_d_s_op2) || (f_l_op1 + s_d_s_op2 != f_l_op1 + (float)s_m_s_op2) || (f_l_op1 + (float)s_m_s_op2 != f_l_op1 + s_i_s_op2) || (f_l_op1 + s_i_s_op2 != 9))
            {
                Console.WriteLine("testcase 18 failed");
                passed = false;
            }
            if ((s_f_s_op1 + i_l_op2 != s_f_s_op1 + ui_l_op2) || (s_f_s_op1 + ui_l_op2 != s_f_s_op1 + l_l_op2) || (s_f_s_op1 + l_l_op2 != s_f_s_op1 + ul_l_op2) || (s_f_s_op1 + ul_l_op2 != s_f_s_op1 + f_l_op2) || (s_f_s_op1 + f_l_op2 != s_f_s_op1 + d_l_op2) || (s_f_s_op1 + d_l_op2 != s_f_s_op1 + (float)m_l_op2) || (s_f_s_op1 + (float)m_l_op2 != s_f_s_op1 + i_l_op2) || (s_f_s_op1 + i_l_op2 != 9))
            {
                Console.WriteLine("testcase 19 failed");
                passed = false;
            }
            if ((s_f_s_op1 + s_i_s_op2 != s_f_s_op1 + s_ui_s_op2) || (s_f_s_op1 + s_ui_s_op2 != s_f_s_op1 + s_l_s_op2) || (s_f_s_op1 + s_l_s_op2 != s_f_s_op1 + s_ul_s_op2) || (s_f_s_op1 + s_ul_s_op2 != s_f_s_op1 + s_f_s_op2) || (s_f_s_op1 + s_f_s_op2 != s_f_s_op1 + s_d_s_op2) || (s_f_s_op1 + s_d_s_op2 != s_f_s_op1 + (float)s_m_s_op2) || (s_f_s_op1 + (float)s_m_s_op2 != s_f_s_op1 + s_i_s_op2) || (s_f_s_op1 + s_i_s_op2 != 9))
            {
                Console.WriteLine("testcase 20 failed");
                passed = false;
            }
        }

        {
            double d_l_op1 = 1;
            int i_l_op2 = 8;
            uint ui_l_op2 = 8;
            long l_l_op2 = 8;
            ulong ul_l_op2 = 8;
            float f_l_op2 = 8;
            double d_l_op2 = 8;
            decimal m_l_op2 = 8;
            numHolder nHldr_l_op2 = new numHolder(8);
            if ((d_l_op1 + i_l_op2 != d_l_op1 + ui_l_op2) || (d_l_op1 + ui_l_op2 != d_l_op1 + l_l_op2) || (d_l_op1 + l_l_op2 != d_l_op1 + ul_l_op2) || (d_l_op1 + ul_l_op2 != d_l_op1 + f_l_op2) || (d_l_op1 + f_l_op2 != d_l_op1 + d_l_op2) || (d_l_op1 + d_l_op2 != d_l_op1 + (double)m_l_op2) || (d_l_op1 + (double)m_l_op2 != d_l_op1 + i_l_op2) || (d_l_op1 + i_l_op2 != 9))
            {
                Console.WriteLine("testcase 21 failed");
                passed = false;
            }
            if ((d_l_op1 + s_i_s_op2 != d_l_op1 + s_ui_s_op2) || (d_l_op1 + s_ui_s_op2 != d_l_op1 + s_l_s_op2) || (d_l_op1 + s_l_s_op2 != d_l_op1 + s_ul_s_op2) || (d_l_op1 + s_ul_s_op2 != d_l_op1 + s_f_s_op2) || (d_l_op1 + s_f_s_op2 != d_l_op1 + s_d_s_op2) || (d_l_op1 + s_d_s_op2 != d_l_op1 + (double)s_m_s_op2) || (d_l_op1 + (double)s_m_s_op2 != d_l_op1 + s_i_s_op2) || (d_l_op1 + s_i_s_op2 != 9))
            {
                Console.WriteLine("testcase 22 failed");
                passed = false;
            }
            if ((s_d_s_op1 + i_l_op2 != s_d_s_op1 + ui_l_op2) || (s_d_s_op1 + ui_l_op2 != s_d_s_op1 + l_l_op2) || (s_d_s_op1 + l_l_op2 != s_d_s_op1 + ul_l_op2) || (s_d_s_op1 + ul_l_op2 != s_d_s_op1 + f_l_op2) || (s_d_s_op1 + f_l_op2 != s_d_s_op1 + d_l_op2) || (s_d_s_op1 + d_l_op2 != s_d_s_op1 + (double)m_l_op2) || (s_d_s_op1 + (double)m_l_op2 != s_d_s_op1 + i_l_op2) || (s_d_s_op1 + i_l_op2 != 9))
            {
                Console.WriteLine("testcase 23 failed");
                passed = false;
            }
            if ((s_d_s_op1 + s_i_s_op2 != s_d_s_op1 + s_ui_s_op2) || (s_d_s_op1 + s_ui_s_op2 != s_d_s_op1 + s_l_s_op2) || (s_d_s_op1 + s_l_s_op2 != s_d_s_op1 + s_ul_s_op2) || (s_d_s_op1 + s_ul_s_op2 != s_d_s_op1 + s_f_s_op2) || (s_d_s_op1 + s_f_s_op2 != s_d_s_op1 + s_d_s_op2) || (s_d_s_op1 + s_d_s_op2 != s_d_s_op1 + (double)s_m_s_op2) || (s_d_s_op1 + (double)s_m_s_op2 != s_d_s_op1 + s_i_s_op2) || (s_d_s_op1 + s_i_s_op2 != 9))
            {
                Console.WriteLine("testcase 24 failed");
                passed = false;
            }
        }

        {
            decimal m_l_op1 = 1;
            int i_l_op2 = 8;
            uint ui_l_op2 = 8;
            long l_l_op2 = 8;
            ulong ul_l_op2 = 8;
            float f_l_op2 = 8;
            double d_l_op2 = 8;
            decimal m_l_op2 = 8;
            numHolder nHldr_l_op2 = new numHolder(8);
            if ((m_l_op1 + i_l_op2 != m_l_op1 + ui_l_op2) || (m_l_op1 + ui_l_op2 != m_l_op1 + l_l_op2) || (m_l_op1 + l_l_op2 != m_l_op1 + ul_l_op2) || (m_l_op1 + ul_l_op2 != m_l_op1 + (decimal)f_l_op2) || (m_l_op1 + (decimal)f_l_op2 != m_l_op1 + (decimal)d_l_op2) || (m_l_op1 + (decimal)d_l_op2 != m_l_op1 + m_l_op2) || (m_l_op1 + m_l_op2 != m_l_op1 + i_l_op2) || (m_l_op1 + i_l_op2 != 9))
            {
                Console.WriteLine("testcase 25 failed");
                passed = false;
            }
            if ((m_l_op1 + s_i_s_op2 != m_l_op1 + s_ui_s_op2) || (m_l_op1 + s_ui_s_op2 != m_l_op1 + s_l_s_op2) || (m_l_op1 + s_l_s_op2 != m_l_op1 + s_ul_s_op2) || (m_l_op1 + s_ul_s_op2 != m_l_op1 + (decimal)s_f_s_op2) || (m_l_op1 + (decimal)s_f_s_op2 != m_l_op1 + (decimal)s_d_s_op2) || (m_l_op1 + (decimal)s_d_s_op2 != m_l_op1 + s_m_s_op2) || (m_l_op1 + s_m_s_op2 != m_l_op1 + s_i_s_op2) || (m_l_op1 + s_i_s_op2 != 9))
            {
                Console.WriteLine("testcase 26 failed");
                passed = false;
            }
            if ((s_m_s_op1 + i_l_op2 != s_m_s_op1 + ui_l_op2) || (s_m_s_op1 + ui_l_op2 != s_m_s_op1 + l_l_op2) || (s_m_s_op1 + l_l_op2 != s_m_s_op1 + ul_l_op2) || (s_m_s_op1 + ul_l_op2 != s_m_s_op1 + (decimal)f_l_op2) || (s_m_s_op1 + (decimal)f_l_op2 != s_m_s_op1 + (decimal)d_l_op2) || (s_m_s_op1 + (decimal)d_l_op2 != s_m_s_op1 + m_l_op2) || (s_m_s_op1 + m_l_op2 != s_m_s_op1 + i_l_op2) || (s_m_s_op1 + i_l_op2 != 9))
            {
                Console.WriteLine("testcase 27 failed");
                passed = false;
            }
            if ((s_m_s_op1 + s_i_s_op2 != s_m_s_op1 + s_ui_s_op2) || (s_m_s_op1 + s_ui_s_op2 != s_m_s_op1 + s_l_s_op2) || (s_m_s_op1 + s_l_s_op2 != s_m_s_op1 + s_ul_s_op2) || (s_m_s_op1 + s_ul_s_op2 != s_m_s_op1 + (decimal)s_f_s_op2) || (s_m_s_op1 + (decimal)s_f_s_op2 != s_m_s_op1 + (decimal)s_d_s_op2) || (s_m_s_op1 + (decimal)s_d_s_op2 != s_m_s_op1 + s_m_s_op2) || (s_m_s_op1 + s_m_s_op2 != s_m_s_op1 + s_i_s_op2) || (s_m_s_op1 + s_i_s_op2 != 9))
            {
                Console.WriteLine("testcase 28 failed");
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
