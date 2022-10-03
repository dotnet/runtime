// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/****************************************************************************
op1%op2
op1 is of a user-defined class numHolder that overloads operator %
op2 can be i4, u4, i8, u8, r4, r8, decimal and user-defined class numHolder
op1 and op2 can be static, local, class/struct member, function retval, 1D/2D/3D array
*****************************************************************************/

using System;
using Xunit;
public class overldrem
{
    //user-defined class that overloads operator %
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

        public static int operator %(numHolder a, int b)
        {
            return a._i_num % b;
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

        public static uint operator %(numHolder a, uint b)
        {
            return a._ui_num % b;
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

        public static long operator %(numHolder a, long b)
        {
            return a._l_num % b;
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

        public static long operator %(numHolder a, ulong b)
        {
            return (long)(a._ul_num % b);
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

        public static float operator %(numHolder a, float b)
        {
            return a._f_num % b;
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

        public static double operator %(numHolder a, double b)
        {
            return a._d_num % b;
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

        public static int operator %(numHolder a, decimal b)
        {
            return (int)(a._m_num % b);
        }

        public static int operator %(numHolder a, numHolder b)
        {
            return a._i_num % b._i_num;
        }
    }

    private static numHolder s_nHldr_s_op1 = new numHolder(65);

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
            return 65;
        else
            return 8;
    }
    public static uint ui_f(String s)
    {
        if (s == "op1")
            return 65;
        else
            return 8;
    }
    public static long l_f(String s)
    {
        if (s == "op1")
            return 65;
        else
            return 8;
    }
    public static ulong ul_f(String s)
    {
        if (s == "op1")
            return 65;
        else
            return 8;
    }
    public static float f_f(String s)
    {
        if (s == "op1")
            return 65;
        else
            return 8;
    }
    public static double d_f(String s)
    {
        if (s == "op1")
            return 65;
        else
            return 8;
    }
    public static decimal m_f(String s)
    {
        if (s == "op1")
            return 65;
        else
            return 8;
    }
    public static numHolder nHldr_f(String s)
    {
        if (s == "op1")
            return new numHolder(65);
        else
            return new numHolder(8);
    }
    private class CL
    {
        public numHolder nHldr_cl_op1 = new numHolder(65);

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
        public numHolder nHldr_vt_op1;

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
        vt1.nHldr_vt_op1 = new numHolder(65);
        vt1.i_vt_op2 = 8;
        vt1.ui_vt_op2 = 8;
        vt1.l_vt_op2 = 8;
        vt1.ul_vt_op2 = 8;
        vt1.f_vt_op2 = 8;
        vt1.d_vt_op2 = 8;
        vt1.m_vt_op2 = 8;
        vt1.nHldr_vt_op2 = new numHolder(8);

        numHolder[] nHldr_arr1d_op1 = { new numHolder(0), new numHolder(65) };
        numHolder[,] nHldr_arr2d_op1 = { { new numHolder(0), new numHolder(65) }, { new numHolder(1), new numHolder(1) } };
        numHolder[,,] nHldr_arr3d_op1 = { { { new numHolder(0), new numHolder(65) }, { new numHolder(1), new numHolder(1) } } };

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
            numHolder nHldr_l_op1 = new numHolder(65);
            int i_l_op2 = 8;
            uint ui_l_op2 = 8;
            long l_l_op2 = 8;
            ulong ul_l_op2 = 8;
            float f_l_op2 = 8;
            double d_l_op2 = 8;
            decimal m_l_op2 = 8;
            numHolder nHldr_l_op2 = new numHolder(8);
            if ((nHldr_l_op1 % i_l_op2 != nHldr_l_op1 % ui_l_op2) || (nHldr_l_op1 % ui_l_op2 != nHldr_l_op1 % l_l_op2) || (nHldr_l_op1 % l_l_op2 != nHldr_l_op1 % ul_l_op2) || (nHldr_l_op1 % ul_l_op2 != nHldr_l_op1 % f_l_op2) || (nHldr_l_op1 % f_l_op2 != nHldr_l_op1 % d_l_op2) || (nHldr_l_op1 % d_l_op2 != nHldr_l_op1 % m_l_op2) || (nHldr_l_op1 % m_l_op2 != nHldr_l_op1 % i_l_op2) || (nHldr_l_op1 % i_l_op2 != 1))
            {
                Console.WriteLine("testcase 1 failed");
                passed = false;
            }
            if ((nHldr_l_op1 % s_i_s_op2 != nHldr_l_op1 % s_ui_s_op2) || (nHldr_l_op1 % s_ui_s_op2 != nHldr_l_op1 % s_l_s_op2) || (nHldr_l_op1 % s_l_s_op2 != nHldr_l_op1 % s_ul_s_op2) || (nHldr_l_op1 % s_ul_s_op2 != nHldr_l_op1 % s_f_s_op2) || (nHldr_l_op1 % s_f_s_op2 != nHldr_l_op1 % s_d_s_op2) || (nHldr_l_op1 % s_d_s_op2 != nHldr_l_op1 % s_m_s_op2) || (nHldr_l_op1 % s_m_s_op2 != nHldr_l_op1 % s_i_s_op2) || (nHldr_l_op1 % s_i_s_op2 != 1))
            {
                Console.WriteLine("testcase 2 failed");
                passed = false;
            }
            if ((nHldr_l_op1 % nHldr_f("op2") != nHldr_l_op1 % nHldr_f("op2")) || (nHldr_l_op1 % nHldr_f("op2") != nHldr_l_op1 % nHldr_f("op2")) || (nHldr_l_op1 % nHldr_f("op2") != nHldr_l_op1 % nHldr_f("op2")) || (nHldr_l_op1 % nHldr_f("op2") != nHldr_l_op1 % nHldr_f("op2")) || (nHldr_l_op1 % nHldr_f("op2") != nHldr_l_op1 % nHldr_f("op2")) || (nHldr_l_op1 % nHldr_f("op2") != nHldr_l_op1 % nHldr_f("op2")) || (nHldr_l_op1 % nHldr_f("op2") != nHldr_l_op1 % nHldr_f("op2")) || (nHldr_l_op1 % nHldr_f("op2") != 1))
            {
                Console.WriteLine("testcase 3 failed");
                passed = false;
            }
            if ((nHldr_l_op1 % cl1.i_cl_op2 != nHldr_l_op1 % cl1.ui_cl_op2) || (nHldr_l_op1 % cl1.ui_cl_op2 != nHldr_l_op1 % cl1.l_cl_op2) || (nHldr_l_op1 % cl1.l_cl_op2 != nHldr_l_op1 % cl1.ul_cl_op2) || (nHldr_l_op1 % cl1.ul_cl_op2 != nHldr_l_op1 % cl1.f_cl_op2) || (nHldr_l_op1 % cl1.f_cl_op2 != nHldr_l_op1 % cl1.d_cl_op2) || (nHldr_l_op1 % cl1.d_cl_op2 != nHldr_l_op1 % cl1.m_cl_op2) || (nHldr_l_op1 % cl1.m_cl_op2 != nHldr_l_op1 % cl1.i_cl_op2) || (nHldr_l_op1 % cl1.i_cl_op2 != 1))
            {
                Console.WriteLine("testcase 4 failed");
                passed = false;
            }
            if ((nHldr_l_op1 % vt1.i_vt_op2 != nHldr_l_op1 % vt1.ui_vt_op2) || (nHldr_l_op1 % vt1.ui_vt_op2 != nHldr_l_op1 % vt1.l_vt_op2) || (nHldr_l_op1 % vt1.l_vt_op2 != nHldr_l_op1 % vt1.ul_vt_op2) || (nHldr_l_op1 % vt1.ul_vt_op2 != nHldr_l_op1 % vt1.f_vt_op2) || (nHldr_l_op1 % vt1.f_vt_op2 != nHldr_l_op1 % vt1.d_vt_op2) || (nHldr_l_op1 % vt1.d_vt_op2 != nHldr_l_op1 % vt1.m_vt_op2) || (nHldr_l_op1 % vt1.m_vt_op2 != nHldr_l_op1 % vt1.i_vt_op2) || (nHldr_l_op1 % vt1.i_vt_op2 != 1))
            {
                Console.WriteLine("testcase 5 failed");
                passed = false;
            }
            if ((nHldr_l_op1 % i_arr1d_op2[0] != nHldr_l_op1 % ui_arr1d_op2[0]) || (nHldr_l_op1 % ui_arr1d_op2[0] != nHldr_l_op1 % l_arr1d_op2[0]) || (nHldr_l_op1 % l_arr1d_op2[0] != nHldr_l_op1 % ul_arr1d_op2[0]) || (nHldr_l_op1 % ul_arr1d_op2[0] != nHldr_l_op1 % f_arr1d_op2[0]) || (nHldr_l_op1 % f_arr1d_op2[0] != nHldr_l_op1 % d_arr1d_op2[0]) || (nHldr_l_op1 % d_arr1d_op2[0] != nHldr_l_op1 % m_arr1d_op2[0]) || (nHldr_l_op1 % m_arr1d_op2[0] != nHldr_l_op1 % i_arr1d_op2[0]) || (nHldr_l_op1 % i_arr1d_op2[0] != 1))
            {
                Console.WriteLine("testcase 6 failed");
                passed = false;
            }
            if ((nHldr_l_op1 % i_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_l_op1 % ui_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_l_op1 % ui_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_l_op1 % l_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_l_op1 % l_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_l_op1 % ul_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_l_op1 % ul_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_l_op1 % f_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_l_op1 % f_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_l_op1 % d_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_l_op1 % d_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_l_op1 % m_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_l_op1 % m_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_l_op1 % i_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_l_op1 % i_arr2d_op2[index[0, 1], index[1, 0]] != 1))
            {
                Console.WriteLine("testcase 7 failed");
                passed = false;
            }
            if ((nHldr_l_op1 % i_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_l_op1 % ui_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_l_op1 % ui_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_l_op1 % l_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_l_op1 % l_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_l_op1 % ul_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_l_op1 % ul_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_l_op1 % f_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_l_op1 % f_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_l_op1 % d_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_l_op1 % d_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_l_op1 % m_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_l_op1 % m_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_l_op1 % i_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_l_op1 % i_arr3d_op2[index[0, 0], 0, index[1, 1]] != 1))
            {
                Console.WriteLine("testcase 8 failed");
                passed = false;
            }
            if ((s_nHldr_s_op1 % i_l_op2 != s_nHldr_s_op1 % ui_l_op2) || (s_nHldr_s_op1 % ui_l_op2 != s_nHldr_s_op1 % l_l_op2) || (s_nHldr_s_op1 % l_l_op2 != s_nHldr_s_op1 % ul_l_op2) || (s_nHldr_s_op1 % ul_l_op2 != s_nHldr_s_op1 % f_l_op2) || (s_nHldr_s_op1 % f_l_op2 != s_nHldr_s_op1 % d_l_op2) || (s_nHldr_s_op1 % d_l_op2 != s_nHldr_s_op1 % m_l_op2) || (s_nHldr_s_op1 % m_l_op2 != s_nHldr_s_op1 % i_l_op2) || (s_nHldr_s_op1 % i_l_op2 != 1))
            {
                Console.WriteLine("testcase 9 failed");
                passed = false;
            }
            if ((s_nHldr_s_op1 % s_i_s_op2 != s_nHldr_s_op1 % s_ui_s_op2) || (s_nHldr_s_op1 % s_ui_s_op2 != s_nHldr_s_op1 % s_l_s_op2) || (s_nHldr_s_op1 % s_l_s_op2 != s_nHldr_s_op1 % s_ul_s_op2) || (s_nHldr_s_op1 % s_ul_s_op2 != s_nHldr_s_op1 % s_f_s_op2) || (s_nHldr_s_op1 % s_f_s_op2 != s_nHldr_s_op1 % s_d_s_op2) || (s_nHldr_s_op1 % s_d_s_op2 != s_nHldr_s_op1 % s_m_s_op2) || (s_nHldr_s_op1 % s_m_s_op2 != s_nHldr_s_op1 % s_i_s_op2) || (s_nHldr_s_op1 % s_i_s_op2 != 1))
            {
                Console.WriteLine("testcase 10 failed");
                passed = false;
            }
            if ((s_nHldr_s_op1 % nHldr_f("op2") != s_nHldr_s_op1 % nHldr_f("op2")) || (s_nHldr_s_op1 % nHldr_f("op2") != s_nHldr_s_op1 % nHldr_f("op2")) || (s_nHldr_s_op1 % nHldr_f("op2") != s_nHldr_s_op1 % nHldr_f("op2")) || (s_nHldr_s_op1 % nHldr_f("op2") != s_nHldr_s_op1 % nHldr_f("op2")) || (s_nHldr_s_op1 % nHldr_f("op2") != s_nHldr_s_op1 % nHldr_f("op2")) || (s_nHldr_s_op1 % nHldr_f("op2") != s_nHldr_s_op1 % nHldr_f("op2")) || (s_nHldr_s_op1 % nHldr_f("op2") != s_nHldr_s_op1 % nHldr_f("op2")) || (s_nHldr_s_op1 % nHldr_f("op2") != 1))
            {
                Console.WriteLine("testcase 11 failed");
                passed = false;
            }
            if ((s_nHldr_s_op1 % cl1.i_cl_op2 != s_nHldr_s_op1 % cl1.ui_cl_op2) || (s_nHldr_s_op1 % cl1.ui_cl_op2 != s_nHldr_s_op1 % cl1.l_cl_op2) || (s_nHldr_s_op1 % cl1.l_cl_op2 != s_nHldr_s_op1 % cl1.ul_cl_op2) || (s_nHldr_s_op1 % cl1.ul_cl_op2 != s_nHldr_s_op1 % cl1.f_cl_op2) || (s_nHldr_s_op1 % cl1.f_cl_op2 != s_nHldr_s_op1 % cl1.d_cl_op2) || (s_nHldr_s_op1 % cl1.d_cl_op2 != s_nHldr_s_op1 % cl1.m_cl_op2) || (s_nHldr_s_op1 % cl1.m_cl_op2 != s_nHldr_s_op1 % cl1.i_cl_op2) || (s_nHldr_s_op1 % cl1.i_cl_op2 != 1))
            {
                Console.WriteLine("testcase 12 failed");
                passed = false;
            }
            if ((s_nHldr_s_op1 % vt1.i_vt_op2 != s_nHldr_s_op1 % vt1.ui_vt_op2) || (s_nHldr_s_op1 % vt1.ui_vt_op2 != s_nHldr_s_op1 % vt1.l_vt_op2) || (s_nHldr_s_op1 % vt1.l_vt_op2 != s_nHldr_s_op1 % vt1.ul_vt_op2) || (s_nHldr_s_op1 % vt1.ul_vt_op2 != s_nHldr_s_op1 % vt1.f_vt_op2) || (s_nHldr_s_op1 % vt1.f_vt_op2 != s_nHldr_s_op1 % vt1.d_vt_op2) || (s_nHldr_s_op1 % vt1.d_vt_op2 != s_nHldr_s_op1 % vt1.m_vt_op2) || (s_nHldr_s_op1 % vt1.m_vt_op2 != s_nHldr_s_op1 % vt1.i_vt_op2) || (s_nHldr_s_op1 % vt1.i_vt_op2 != 1))
            {
                Console.WriteLine("testcase 13 failed");
                passed = false;
            }
            if ((s_nHldr_s_op1 % i_arr1d_op2[0] != s_nHldr_s_op1 % ui_arr1d_op2[0]) || (s_nHldr_s_op1 % ui_arr1d_op2[0] != s_nHldr_s_op1 % l_arr1d_op2[0]) || (s_nHldr_s_op1 % l_arr1d_op2[0] != s_nHldr_s_op1 % ul_arr1d_op2[0]) || (s_nHldr_s_op1 % ul_arr1d_op2[0] != s_nHldr_s_op1 % f_arr1d_op2[0]) || (s_nHldr_s_op1 % f_arr1d_op2[0] != s_nHldr_s_op1 % d_arr1d_op2[0]) || (s_nHldr_s_op1 % d_arr1d_op2[0] != s_nHldr_s_op1 % m_arr1d_op2[0]) || (s_nHldr_s_op1 % m_arr1d_op2[0] != s_nHldr_s_op1 % i_arr1d_op2[0]) || (s_nHldr_s_op1 % i_arr1d_op2[0] != 1))
            {
                Console.WriteLine("testcase 14 failed");
                passed = false;
            }
            if ((s_nHldr_s_op1 % i_arr2d_op2[index[0, 1], index[1, 0]] != s_nHldr_s_op1 % ui_arr2d_op2[index[0, 1], index[1, 0]]) || (s_nHldr_s_op1 % ui_arr2d_op2[index[0, 1], index[1, 0]] != s_nHldr_s_op1 % l_arr2d_op2[index[0, 1], index[1, 0]]) || (s_nHldr_s_op1 % l_arr2d_op2[index[0, 1], index[1, 0]] != s_nHldr_s_op1 % ul_arr2d_op2[index[0, 1], index[1, 0]]) || (s_nHldr_s_op1 % ul_arr2d_op2[index[0, 1], index[1, 0]] != s_nHldr_s_op1 % f_arr2d_op2[index[0, 1], index[1, 0]]) || (s_nHldr_s_op1 % f_arr2d_op2[index[0, 1], index[1, 0]] != s_nHldr_s_op1 % d_arr2d_op2[index[0, 1], index[1, 0]]) || (s_nHldr_s_op1 % d_arr2d_op2[index[0, 1], index[1, 0]] != s_nHldr_s_op1 % m_arr2d_op2[index[0, 1], index[1, 0]]) || (s_nHldr_s_op1 % m_arr2d_op2[index[0, 1], index[1, 0]] != s_nHldr_s_op1 % i_arr2d_op2[index[0, 1], index[1, 0]]) || (s_nHldr_s_op1 % i_arr2d_op2[index[0, 1], index[1, 0]] != 1))
            {
                Console.WriteLine("testcase 15 failed");
                passed = false;
            }
            if ((s_nHldr_s_op1 % i_arr3d_op2[index[0, 0], 0, index[1, 1]] != s_nHldr_s_op1 % ui_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (s_nHldr_s_op1 % ui_arr3d_op2[index[0, 0], 0, index[1, 1]] != s_nHldr_s_op1 % l_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (s_nHldr_s_op1 % l_arr3d_op2[index[0, 0], 0, index[1, 1]] != s_nHldr_s_op1 % ul_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (s_nHldr_s_op1 % ul_arr3d_op2[index[0, 0], 0, index[1, 1]] != s_nHldr_s_op1 % f_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (s_nHldr_s_op1 % f_arr3d_op2[index[0, 0], 0, index[1, 1]] != s_nHldr_s_op1 % d_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (s_nHldr_s_op1 % d_arr3d_op2[index[0, 0], 0, index[1, 1]] != s_nHldr_s_op1 % m_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (s_nHldr_s_op1 % m_arr3d_op2[index[0, 0], 0, index[1, 1]] != s_nHldr_s_op1 % i_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (s_nHldr_s_op1 % i_arr3d_op2[index[0, 0], 0, index[1, 1]] != 1))
            {
                Console.WriteLine("testcase 16 failed");
                passed = false;
            }
            if ((nHldr_f("op1") % i_l_op2 != nHldr_f("op1") % ui_l_op2) || (nHldr_f("op1") % ui_l_op2 != nHldr_f("op1") % l_l_op2) || (nHldr_f("op1") % l_l_op2 != nHldr_f("op1") % ul_l_op2) || (nHldr_f("op1") % ul_l_op2 != nHldr_f("op1") % f_l_op2) || (nHldr_f("op1") % f_l_op2 != nHldr_f("op1") % d_l_op2) || (nHldr_f("op1") % d_l_op2 != nHldr_f("op1") % m_l_op2) || (nHldr_f("op1") % m_l_op2 != nHldr_f("op1") % i_l_op2) || (nHldr_f("op1") % i_l_op2 != 1))
            {
                Console.WriteLine("testcase 17 failed");
                passed = false;
            }
            if ((nHldr_f("op1") % s_i_s_op2 != nHldr_f("op1") % s_ui_s_op2) || (nHldr_f("op1") % s_ui_s_op2 != nHldr_f("op1") % s_l_s_op2) || (nHldr_f("op1") % s_l_s_op2 != nHldr_f("op1") % s_ul_s_op2) || (nHldr_f("op1") % s_ul_s_op2 != nHldr_f("op1") % s_f_s_op2) || (nHldr_f("op1") % s_f_s_op2 != nHldr_f("op1") % s_d_s_op2) || (nHldr_f("op1") % s_d_s_op2 != nHldr_f("op1") % s_m_s_op2) || (nHldr_f("op1") % s_m_s_op2 != nHldr_f("op1") % s_i_s_op2) || (nHldr_f("op1") % s_i_s_op2 != 1))
            {
                Console.WriteLine("testcase 18 failed");
                passed = false;
            }
            if ((nHldr_f("op1") % nHldr_f("op2") != nHldr_f("op1") % nHldr_f("op2")) || (nHldr_f("op1") % nHldr_f("op2") != nHldr_f("op1") % nHldr_f("op2")) || (nHldr_f("op1") % nHldr_f("op2") != nHldr_f("op1") % nHldr_f("op2")) || (nHldr_f("op1") % nHldr_f("op2") != nHldr_f("op1") % nHldr_f("op2")) || (nHldr_f("op1") % nHldr_f("op2") != nHldr_f("op1") % nHldr_f("op2")) || (nHldr_f("op1") % nHldr_f("op2") != nHldr_f("op1") % nHldr_f("op2")) || (nHldr_f("op1") % nHldr_f("op2") != nHldr_f("op1") % nHldr_f("op2")) || (nHldr_f("op1") % nHldr_f("op2") != 1))
            {
                Console.WriteLine("testcase 19 failed");
                passed = false;
            }
            if ((nHldr_f("op1") % cl1.i_cl_op2 != nHldr_f("op1") % cl1.ui_cl_op2) || (nHldr_f("op1") % cl1.ui_cl_op2 != nHldr_f("op1") % cl1.l_cl_op2) || (nHldr_f("op1") % cl1.l_cl_op2 != nHldr_f("op1") % cl1.ul_cl_op2) || (nHldr_f("op1") % cl1.ul_cl_op2 != nHldr_f("op1") % cl1.f_cl_op2) || (nHldr_f("op1") % cl1.f_cl_op2 != nHldr_f("op1") % cl1.d_cl_op2) || (nHldr_f("op1") % cl1.d_cl_op2 != nHldr_f("op1") % cl1.m_cl_op2) || (nHldr_f("op1") % cl1.m_cl_op2 != nHldr_f("op1") % cl1.i_cl_op2) || (nHldr_f("op1") % cl1.i_cl_op2 != 1))
            {
                Console.WriteLine("testcase 20 failed");
                passed = false;
            }
            if ((nHldr_f("op1") % vt1.i_vt_op2 != nHldr_f("op1") % vt1.ui_vt_op2) || (nHldr_f("op1") % vt1.ui_vt_op2 != nHldr_f("op1") % vt1.l_vt_op2) || (nHldr_f("op1") % vt1.l_vt_op2 != nHldr_f("op1") % vt1.ul_vt_op2) || (nHldr_f("op1") % vt1.ul_vt_op2 != nHldr_f("op1") % vt1.f_vt_op2) || (nHldr_f("op1") % vt1.f_vt_op2 != nHldr_f("op1") % vt1.d_vt_op2) || (nHldr_f("op1") % vt1.d_vt_op2 != nHldr_f("op1") % vt1.m_vt_op2) || (nHldr_f("op1") % vt1.m_vt_op2 != nHldr_f("op1") % vt1.i_vt_op2) || (nHldr_f("op1") % vt1.i_vt_op2 != 1))
            {
                Console.WriteLine("testcase 21 failed");
                passed = false;
            }
            if ((nHldr_f("op1") % i_arr1d_op2[0] != nHldr_f("op1") % ui_arr1d_op2[0]) || (nHldr_f("op1") % ui_arr1d_op2[0] != nHldr_f("op1") % l_arr1d_op2[0]) || (nHldr_f("op1") % l_arr1d_op2[0] != nHldr_f("op1") % ul_arr1d_op2[0]) || (nHldr_f("op1") % ul_arr1d_op2[0] != nHldr_f("op1") % f_arr1d_op2[0]) || (nHldr_f("op1") % f_arr1d_op2[0] != nHldr_f("op1") % d_arr1d_op2[0]) || (nHldr_f("op1") % d_arr1d_op2[0] != nHldr_f("op1") % m_arr1d_op2[0]) || (nHldr_f("op1") % m_arr1d_op2[0] != nHldr_f("op1") % i_arr1d_op2[0]) || (nHldr_f("op1") % i_arr1d_op2[0] != 1))
            {
                Console.WriteLine("testcase 22 failed");
                passed = false;
            }
            if ((nHldr_f("op1") % i_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_f("op1") % ui_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_f("op1") % ui_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_f("op1") % l_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_f("op1") % l_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_f("op1") % ul_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_f("op1") % ul_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_f("op1") % f_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_f("op1") % f_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_f("op1") % d_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_f("op1") % d_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_f("op1") % m_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_f("op1") % m_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_f("op1") % i_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_f("op1") % i_arr2d_op2[index[0, 1], index[1, 0]] != 1))
            {
                Console.WriteLine("testcase 23 failed");
                passed = false;
            }
            if ((nHldr_f("op1") % i_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_f("op1") % ui_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_f("op1") % ui_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_f("op1") % l_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_f("op1") % l_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_f("op1") % ul_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_f("op1") % ul_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_f("op1") % f_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_f("op1") % f_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_f("op1") % d_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_f("op1") % d_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_f("op1") % m_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_f("op1") % m_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_f("op1") % i_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_f("op1") % i_arr3d_op2[index[0, 0], 0, index[1, 1]] != 1))
            {
                Console.WriteLine("testcase 24 failed");
                passed = false;
            }
            if ((cl1.nHldr_cl_op1 % i_l_op2 != cl1.nHldr_cl_op1 % ui_l_op2) || (cl1.nHldr_cl_op1 % ui_l_op2 != cl1.nHldr_cl_op1 % l_l_op2) || (cl1.nHldr_cl_op1 % l_l_op2 != cl1.nHldr_cl_op1 % ul_l_op2) || (cl1.nHldr_cl_op1 % ul_l_op2 != cl1.nHldr_cl_op1 % f_l_op2) || (cl1.nHldr_cl_op1 % f_l_op2 != cl1.nHldr_cl_op1 % d_l_op2) || (cl1.nHldr_cl_op1 % d_l_op2 != cl1.nHldr_cl_op1 % m_l_op2) || (cl1.nHldr_cl_op1 % m_l_op2 != cl1.nHldr_cl_op1 % i_l_op2) || (cl1.nHldr_cl_op1 % i_l_op2 != 1))
            {
                Console.WriteLine("testcase 25 failed");
                passed = false;
            }
            if ((cl1.nHldr_cl_op1 % s_i_s_op2 != cl1.nHldr_cl_op1 % s_ui_s_op2) || (cl1.nHldr_cl_op1 % s_ui_s_op2 != cl1.nHldr_cl_op1 % s_l_s_op2) || (cl1.nHldr_cl_op1 % s_l_s_op2 != cl1.nHldr_cl_op1 % s_ul_s_op2) || (cl1.nHldr_cl_op1 % s_ul_s_op2 != cl1.nHldr_cl_op1 % s_f_s_op2) || (cl1.nHldr_cl_op1 % s_f_s_op2 != cl1.nHldr_cl_op1 % s_d_s_op2) || (cl1.nHldr_cl_op1 % s_d_s_op2 != cl1.nHldr_cl_op1 % s_m_s_op2) || (cl1.nHldr_cl_op1 % s_m_s_op2 != cl1.nHldr_cl_op1 % s_i_s_op2) || (cl1.nHldr_cl_op1 % s_i_s_op2 != 1))
            {
                Console.WriteLine("testcase 26 failed");
                passed = false;
            }
            if ((cl1.nHldr_cl_op1 % nHldr_f("op2") != cl1.nHldr_cl_op1 % nHldr_f("op2")) || (cl1.nHldr_cl_op1 % nHldr_f("op2") != cl1.nHldr_cl_op1 % nHldr_f("op2")) || (cl1.nHldr_cl_op1 % nHldr_f("op2") != cl1.nHldr_cl_op1 % nHldr_f("op2")) || (cl1.nHldr_cl_op1 % nHldr_f("op2") != cl1.nHldr_cl_op1 % nHldr_f("op2")) || (cl1.nHldr_cl_op1 % nHldr_f("op2") != cl1.nHldr_cl_op1 % nHldr_f("op2")) || (cl1.nHldr_cl_op1 % nHldr_f("op2") != cl1.nHldr_cl_op1 % nHldr_f("op2")) || (cl1.nHldr_cl_op1 % nHldr_f("op2") != cl1.nHldr_cl_op1 % nHldr_f("op2")) || (cl1.nHldr_cl_op1 % nHldr_f("op2") != 1))
            {
                Console.WriteLine("testcase 27 failed");
                passed = false;
            }
            if ((cl1.nHldr_cl_op1 % cl1.i_cl_op2 != cl1.nHldr_cl_op1 % cl1.ui_cl_op2) || (cl1.nHldr_cl_op1 % cl1.ui_cl_op2 != cl1.nHldr_cl_op1 % cl1.l_cl_op2) || (cl1.nHldr_cl_op1 % cl1.l_cl_op2 != cl1.nHldr_cl_op1 % cl1.ul_cl_op2) || (cl1.nHldr_cl_op1 % cl1.ul_cl_op2 != cl1.nHldr_cl_op1 % cl1.f_cl_op2) || (cl1.nHldr_cl_op1 % cl1.f_cl_op2 != cl1.nHldr_cl_op1 % cl1.d_cl_op2) || (cl1.nHldr_cl_op1 % cl1.d_cl_op2 != cl1.nHldr_cl_op1 % cl1.m_cl_op2) || (cl1.nHldr_cl_op1 % cl1.m_cl_op2 != cl1.nHldr_cl_op1 % cl1.i_cl_op2) || (cl1.nHldr_cl_op1 % cl1.i_cl_op2 != 1))
            {
                Console.WriteLine("testcase 28 failed");
                passed = false;
            }
            if ((cl1.nHldr_cl_op1 % vt1.i_vt_op2 != cl1.nHldr_cl_op1 % vt1.ui_vt_op2) || (cl1.nHldr_cl_op1 % vt1.ui_vt_op2 != cl1.nHldr_cl_op1 % vt1.l_vt_op2) || (cl1.nHldr_cl_op1 % vt1.l_vt_op2 != cl1.nHldr_cl_op1 % vt1.ul_vt_op2) || (cl1.nHldr_cl_op1 % vt1.ul_vt_op2 != cl1.nHldr_cl_op1 % vt1.f_vt_op2) || (cl1.nHldr_cl_op1 % vt1.f_vt_op2 != cl1.nHldr_cl_op1 % vt1.d_vt_op2) || (cl1.nHldr_cl_op1 % vt1.d_vt_op2 != cl1.nHldr_cl_op1 % vt1.m_vt_op2) || (cl1.nHldr_cl_op1 % vt1.m_vt_op2 != cl1.nHldr_cl_op1 % vt1.i_vt_op2) || (cl1.nHldr_cl_op1 % vt1.i_vt_op2 != 1))
            {
                Console.WriteLine("testcase 29 failed");
                passed = false;
            }
            if ((cl1.nHldr_cl_op1 % i_arr1d_op2[0] != cl1.nHldr_cl_op1 % ui_arr1d_op2[0]) || (cl1.nHldr_cl_op1 % ui_arr1d_op2[0] != cl1.nHldr_cl_op1 % l_arr1d_op2[0]) || (cl1.nHldr_cl_op1 % l_arr1d_op2[0] != cl1.nHldr_cl_op1 % ul_arr1d_op2[0]) || (cl1.nHldr_cl_op1 % ul_arr1d_op2[0] != cl1.nHldr_cl_op1 % f_arr1d_op2[0]) || (cl1.nHldr_cl_op1 % f_arr1d_op2[0] != cl1.nHldr_cl_op1 % d_arr1d_op2[0]) || (cl1.nHldr_cl_op1 % d_arr1d_op2[0] != cl1.nHldr_cl_op1 % m_arr1d_op2[0]) || (cl1.nHldr_cl_op1 % m_arr1d_op2[0] != cl1.nHldr_cl_op1 % i_arr1d_op2[0]) || (cl1.nHldr_cl_op1 % i_arr1d_op2[0] != 1))
            {
                Console.WriteLine("testcase 30 failed");
                passed = false;
            }
            if ((cl1.nHldr_cl_op1 % i_arr2d_op2[index[0, 1], index[1, 0]] != cl1.nHldr_cl_op1 % ui_arr2d_op2[index[0, 1], index[1, 0]]) || (cl1.nHldr_cl_op1 % ui_arr2d_op2[index[0, 1], index[1, 0]] != cl1.nHldr_cl_op1 % l_arr2d_op2[index[0, 1], index[1, 0]]) || (cl1.nHldr_cl_op1 % l_arr2d_op2[index[0, 1], index[1, 0]] != cl1.nHldr_cl_op1 % ul_arr2d_op2[index[0, 1], index[1, 0]]) || (cl1.nHldr_cl_op1 % ul_arr2d_op2[index[0, 1], index[1, 0]] != cl1.nHldr_cl_op1 % f_arr2d_op2[index[0, 1], index[1, 0]]) || (cl1.nHldr_cl_op1 % f_arr2d_op2[index[0, 1], index[1, 0]] != cl1.nHldr_cl_op1 % d_arr2d_op2[index[0, 1], index[1, 0]]) || (cl1.nHldr_cl_op1 % d_arr2d_op2[index[0, 1], index[1, 0]] != cl1.nHldr_cl_op1 % m_arr2d_op2[index[0, 1], index[1, 0]]) || (cl1.nHldr_cl_op1 % m_arr2d_op2[index[0, 1], index[1, 0]] != cl1.nHldr_cl_op1 % i_arr2d_op2[index[0, 1], index[1, 0]]) || (cl1.nHldr_cl_op1 % i_arr2d_op2[index[0, 1], index[1, 0]] != 1))
            {
                Console.WriteLine("testcase 31 failed");
                passed = false;
            }
            if ((cl1.nHldr_cl_op1 % i_arr3d_op2[index[0, 0], 0, index[1, 1]] != cl1.nHldr_cl_op1 % ui_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (cl1.nHldr_cl_op1 % ui_arr3d_op2[index[0, 0], 0, index[1, 1]] != cl1.nHldr_cl_op1 % l_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (cl1.nHldr_cl_op1 % l_arr3d_op2[index[0, 0], 0, index[1, 1]] != cl1.nHldr_cl_op1 % ul_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (cl1.nHldr_cl_op1 % ul_arr3d_op2[index[0, 0], 0, index[1, 1]] != cl1.nHldr_cl_op1 % f_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (cl1.nHldr_cl_op1 % f_arr3d_op2[index[0, 0], 0, index[1, 1]] != cl1.nHldr_cl_op1 % d_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (cl1.nHldr_cl_op1 % d_arr3d_op2[index[0, 0], 0, index[1, 1]] != cl1.nHldr_cl_op1 % m_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (cl1.nHldr_cl_op1 % m_arr3d_op2[index[0, 0], 0, index[1, 1]] != cl1.nHldr_cl_op1 % i_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (cl1.nHldr_cl_op1 % i_arr3d_op2[index[0, 0], 0, index[1, 1]] != 1))
            {
                Console.WriteLine("testcase 32 failed");
                passed = false;
            }
            if ((vt1.nHldr_vt_op1 % i_l_op2 != vt1.nHldr_vt_op1 % ui_l_op2) || (vt1.nHldr_vt_op1 % ui_l_op2 != vt1.nHldr_vt_op1 % l_l_op2) || (vt1.nHldr_vt_op1 % l_l_op2 != vt1.nHldr_vt_op1 % ul_l_op2) || (vt1.nHldr_vt_op1 % ul_l_op2 != vt1.nHldr_vt_op1 % f_l_op2) || (vt1.nHldr_vt_op1 % f_l_op2 != vt1.nHldr_vt_op1 % d_l_op2) || (vt1.nHldr_vt_op1 % d_l_op2 != vt1.nHldr_vt_op1 % m_l_op2) || (vt1.nHldr_vt_op1 % m_l_op2 != vt1.nHldr_vt_op1 % i_l_op2) || (vt1.nHldr_vt_op1 % i_l_op2 != 1))
            {
                Console.WriteLine("testcase 33 failed");
                passed = false;
            }
            if ((vt1.nHldr_vt_op1 % s_i_s_op2 != vt1.nHldr_vt_op1 % s_ui_s_op2) || (vt1.nHldr_vt_op1 % s_ui_s_op2 != vt1.nHldr_vt_op1 % s_l_s_op2) || (vt1.nHldr_vt_op1 % s_l_s_op2 != vt1.nHldr_vt_op1 % s_ul_s_op2) || (vt1.nHldr_vt_op1 % s_ul_s_op2 != vt1.nHldr_vt_op1 % s_f_s_op2) || (vt1.nHldr_vt_op1 % s_f_s_op2 != vt1.nHldr_vt_op1 % s_d_s_op2) || (vt1.nHldr_vt_op1 % s_d_s_op2 != vt1.nHldr_vt_op1 % s_m_s_op2) || (vt1.nHldr_vt_op1 % s_m_s_op2 != vt1.nHldr_vt_op1 % s_i_s_op2) || (vt1.nHldr_vt_op1 % s_i_s_op2 != 1))
            {
                Console.WriteLine("testcase 34 failed");
                passed = false;
            }
            if ((vt1.nHldr_vt_op1 % nHldr_f("op2") != vt1.nHldr_vt_op1 % nHldr_f("op2")) || (vt1.nHldr_vt_op1 % nHldr_f("op2") != vt1.nHldr_vt_op1 % nHldr_f("op2")) || (vt1.nHldr_vt_op1 % nHldr_f("op2") != vt1.nHldr_vt_op1 % nHldr_f("op2")) || (vt1.nHldr_vt_op1 % nHldr_f("op2") != vt1.nHldr_vt_op1 % nHldr_f("op2")) || (vt1.nHldr_vt_op1 % nHldr_f("op2") != vt1.nHldr_vt_op1 % nHldr_f("op2")) || (vt1.nHldr_vt_op1 % nHldr_f("op2") != vt1.nHldr_vt_op1 % nHldr_f("op2")) || (vt1.nHldr_vt_op1 % nHldr_f("op2") != vt1.nHldr_vt_op1 % nHldr_f("op2")) || (vt1.nHldr_vt_op1 % nHldr_f("op2") != 1))
            {
                Console.WriteLine("testcase 35 failed");
                passed = false;
            }
            if ((vt1.nHldr_vt_op1 % cl1.i_cl_op2 != vt1.nHldr_vt_op1 % cl1.ui_cl_op2) || (vt1.nHldr_vt_op1 % cl1.ui_cl_op2 != vt1.nHldr_vt_op1 % cl1.l_cl_op2) || (vt1.nHldr_vt_op1 % cl1.l_cl_op2 != vt1.nHldr_vt_op1 % cl1.ul_cl_op2) || (vt1.nHldr_vt_op1 % cl1.ul_cl_op2 != vt1.nHldr_vt_op1 % cl1.f_cl_op2) || (vt1.nHldr_vt_op1 % cl1.f_cl_op2 != vt1.nHldr_vt_op1 % cl1.d_cl_op2) || (vt1.nHldr_vt_op1 % cl1.d_cl_op2 != vt1.nHldr_vt_op1 % cl1.m_cl_op2) || (vt1.nHldr_vt_op1 % cl1.m_cl_op2 != vt1.nHldr_vt_op1 % cl1.i_cl_op2) || (vt1.nHldr_vt_op1 % cl1.i_cl_op2 != 1))
            {
                Console.WriteLine("testcase 36 failed");
                passed = false;
            }
            if ((vt1.nHldr_vt_op1 % vt1.i_vt_op2 != vt1.nHldr_vt_op1 % vt1.ui_vt_op2) || (vt1.nHldr_vt_op1 % vt1.ui_vt_op2 != vt1.nHldr_vt_op1 % vt1.l_vt_op2) || (vt1.nHldr_vt_op1 % vt1.l_vt_op2 != vt1.nHldr_vt_op1 % vt1.ul_vt_op2) || (vt1.nHldr_vt_op1 % vt1.ul_vt_op2 != vt1.nHldr_vt_op1 % vt1.f_vt_op2) || (vt1.nHldr_vt_op1 % vt1.f_vt_op2 != vt1.nHldr_vt_op1 % vt1.d_vt_op2) || (vt1.nHldr_vt_op1 % vt1.d_vt_op2 != vt1.nHldr_vt_op1 % vt1.m_vt_op2) || (vt1.nHldr_vt_op1 % vt1.m_vt_op2 != vt1.nHldr_vt_op1 % vt1.i_vt_op2) || (vt1.nHldr_vt_op1 % vt1.i_vt_op2 != 1))
            {
                Console.WriteLine("testcase 37 failed");
                passed = false;
            }
            if ((vt1.nHldr_vt_op1 % i_arr1d_op2[0] != vt1.nHldr_vt_op1 % ui_arr1d_op2[0]) || (vt1.nHldr_vt_op1 % ui_arr1d_op2[0] != vt1.nHldr_vt_op1 % l_arr1d_op2[0]) || (vt1.nHldr_vt_op1 % l_arr1d_op2[0] != vt1.nHldr_vt_op1 % ul_arr1d_op2[0]) || (vt1.nHldr_vt_op1 % ul_arr1d_op2[0] != vt1.nHldr_vt_op1 % f_arr1d_op2[0]) || (vt1.nHldr_vt_op1 % f_arr1d_op2[0] != vt1.nHldr_vt_op1 % d_arr1d_op2[0]) || (vt1.nHldr_vt_op1 % d_arr1d_op2[0] != vt1.nHldr_vt_op1 % m_arr1d_op2[0]) || (vt1.nHldr_vt_op1 % m_arr1d_op2[0] != vt1.nHldr_vt_op1 % i_arr1d_op2[0]) || (vt1.nHldr_vt_op1 % i_arr1d_op2[0] != 1))
            {
                Console.WriteLine("testcase 38 failed");
                passed = false;
            }
            if ((vt1.nHldr_vt_op1 % i_arr2d_op2[index[0, 1], index[1, 0]] != vt1.nHldr_vt_op1 % ui_arr2d_op2[index[0, 1], index[1, 0]]) || (vt1.nHldr_vt_op1 % ui_arr2d_op2[index[0, 1], index[1, 0]] != vt1.nHldr_vt_op1 % l_arr2d_op2[index[0, 1], index[1, 0]]) || (vt1.nHldr_vt_op1 % l_arr2d_op2[index[0, 1], index[1, 0]] != vt1.nHldr_vt_op1 % ul_arr2d_op2[index[0, 1], index[1, 0]]) || (vt1.nHldr_vt_op1 % ul_arr2d_op2[index[0, 1], index[1, 0]] != vt1.nHldr_vt_op1 % f_arr2d_op2[index[0, 1], index[1, 0]]) || (vt1.nHldr_vt_op1 % f_arr2d_op2[index[0, 1], index[1, 0]] != vt1.nHldr_vt_op1 % d_arr2d_op2[index[0, 1], index[1, 0]]) || (vt1.nHldr_vt_op1 % d_arr2d_op2[index[0, 1], index[1, 0]] != vt1.nHldr_vt_op1 % m_arr2d_op2[index[0, 1], index[1, 0]]) || (vt1.nHldr_vt_op1 % m_arr2d_op2[index[0, 1], index[1, 0]] != vt1.nHldr_vt_op1 % i_arr2d_op2[index[0, 1], index[1, 0]]) || (vt1.nHldr_vt_op1 % i_arr2d_op2[index[0, 1], index[1, 0]] != 1))
            {
                Console.WriteLine("testcase 39 failed");
                passed = false;
            }
            if ((vt1.nHldr_vt_op1 % i_arr3d_op2[index[0, 0], 0, index[1, 1]] != vt1.nHldr_vt_op1 % ui_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (vt1.nHldr_vt_op1 % ui_arr3d_op2[index[0, 0], 0, index[1, 1]] != vt1.nHldr_vt_op1 % l_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (vt1.nHldr_vt_op1 % l_arr3d_op2[index[0, 0], 0, index[1, 1]] != vt1.nHldr_vt_op1 % ul_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (vt1.nHldr_vt_op1 % ul_arr3d_op2[index[0, 0], 0, index[1, 1]] != vt1.nHldr_vt_op1 % f_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (vt1.nHldr_vt_op1 % f_arr3d_op2[index[0, 0], 0, index[1, 1]] != vt1.nHldr_vt_op1 % d_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (vt1.nHldr_vt_op1 % d_arr3d_op2[index[0, 0], 0, index[1, 1]] != vt1.nHldr_vt_op1 % m_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (vt1.nHldr_vt_op1 % m_arr3d_op2[index[0, 0], 0, index[1, 1]] != vt1.nHldr_vt_op1 % i_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (vt1.nHldr_vt_op1 % i_arr3d_op2[index[0, 0], 0, index[1, 1]] != 1))
            {
                Console.WriteLine("testcase 40 failed");
                passed = false;
            }
            if ((nHldr_arr1d_op1[1] % i_l_op2 != nHldr_arr1d_op1[1] % ui_l_op2) || (nHldr_arr1d_op1[1] % ui_l_op2 != nHldr_arr1d_op1[1] % l_l_op2) || (nHldr_arr1d_op1[1] % l_l_op2 != nHldr_arr1d_op1[1] % ul_l_op2) || (nHldr_arr1d_op1[1] % ul_l_op2 != nHldr_arr1d_op1[1] % f_l_op2) || (nHldr_arr1d_op1[1] % f_l_op2 != nHldr_arr1d_op1[1] % d_l_op2) || (nHldr_arr1d_op1[1] % d_l_op2 != nHldr_arr1d_op1[1] % m_l_op2) || (nHldr_arr1d_op1[1] % m_l_op2 != nHldr_arr1d_op1[1] % i_l_op2) || (nHldr_arr1d_op1[1] % i_l_op2 != 1))
            {
                Console.WriteLine("testcase 41 failed");
                passed = false;
            }
            if ((nHldr_arr1d_op1[1] % s_i_s_op2 != nHldr_arr1d_op1[1] % s_ui_s_op2) || (nHldr_arr1d_op1[1] % s_ui_s_op2 != nHldr_arr1d_op1[1] % s_l_s_op2) || (nHldr_arr1d_op1[1] % s_l_s_op2 != nHldr_arr1d_op1[1] % s_ul_s_op2) || (nHldr_arr1d_op1[1] % s_ul_s_op2 != nHldr_arr1d_op1[1] % s_f_s_op2) || (nHldr_arr1d_op1[1] % s_f_s_op2 != nHldr_arr1d_op1[1] % s_d_s_op2) || (nHldr_arr1d_op1[1] % s_d_s_op2 != nHldr_arr1d_op1[1] % s_m_s_op2) || (nHldr_arr1d_op1[1] % s_m_s_op2 != nHldr_arr1d_op1[1] % s_i_s_op2) || (nHldr_arr1d_op1[1] % s_i_s_op2 != 1))
            {
                Console.WriteLine("testcase 42 failed");
                passed = false;
            }
            if ((nHldr_arr1d_op1[1] % nHldr_f("op2") != nHldr_arr1d_op1[1] % nHldr_f("op2")) || (nHldr_arr1d_op1[1] % nHldr_f("op2") != nHldr_arr1d_op1[1] % nHldr_f("op2")) || (nHldr_arr1d_op1[1] % nHldr_f("op2") != nHldr_arr1d_op1[1] % nHldr_f("op2")) || (nHldr_arr1d_op1[1] % nHldr_f("op2") != nHldr_arr1d_op1[1] % nHldr_f("op2")) || (nHldr_arr1d_op1[1] % nHldr_f("op2") != nHldr_arr1d_op1[1] % nHldr_f("op2")) || (nHldr_arr1d_op1[1] % nHldr_f("op2") != nHldr_arr1d_op1[1] % nHldr_f("op2")) || (nHldr_arr1d_op1[1] % nHldr_f("op2") != nHldr_arr1d_op1[1] % nHldr_f("op2")) || (nHldr_arr1d_op1[1] % nHldr_f("op2") != 1))
            {
                Console.WriteLine("testcase 43 failed");
                passed = false;
            }
            if ((nHldr_arr1d_op1[1] % cl1.i_cl_op2 != nHldr_arr1d_op1[1] % cl1.ui_cl_op2) || (nHldr_arr1d_op1[1] % cl1.ui_cl_op2 != nHldr_arr1d_op1[1] % cl1.l_cl_op2) || (nHldr_arr1d_op1[1] % cl1.l_cl_op2 != nHldr_arr1d_op1[1] % cl1.ul_cl_op2) || (nHldr_arr1d_op1[1] % cl1.ul_cl_op2 != nHldr_arr1d_op1[1] % cl1.f_cl_op2) || (nHldr_arr1d_op1[1] % cl1.f_cl_op2 != nHldr_arr1d_op1[1] % cl1.d_cl_op2) || (nHldr_arr1d_op1[1] % cl1.d_cl_op2 != nHldr_arr1d_op1[1] % cl1.m_cl_op2) || (nHldr_arr1d_op1[1] % cl1.m_cl_op2 != nHldr_arr1d_op1[1] % cl1.i_cl_op2) || (nHldr_arr1d_op1[1] % cl1.i_cl_op2 != 1))
            {
                Console.WriteLine("testcase 44 failed");
                passed = false;
            }
            if ((nHldr_arr1d_op1[1] % vt1.i_vt_op2 != nHldr_arr1d_op1[1] % vt1.ui_vt_op2) || (nHldr_arr1d_op1[1] % vt1.ui_vt_op2 != nHldr_arr1d_op1[1] % vt1.l_vt_op2) || (nHldr_arr1d_op1[1] % vt1.l_vt_op2 != nHldr_arr1d_op1[1] % vt1.ul_vt_op2) || (nHldr_arr1d_op1[1] % vt1.ul_vt_op2 != nHldr_arr1d_op1[1] % vt1.f_vt_op2) || (nHldr_arr1d_op1[1] % vt1.f_vt_op2 != nHldr_arr1d_op1[1] % vt1.d_vt_op2) || (nHldr_arr1d_op1[1] % vt1.d_vt_op2 != nHldr_arr1d_op1[1] % vt1.m_vt_op2) || (nHldr_arr1d_op1[1] % vt1.m_vt_op2 != nHldr_arr1d_op1[1] % vt1.i_vt_op2) || (nHldr_arr1d_op1[1] % vt1.i_vt_op2 != 1))
            {
                Console.WriteLine("testcase 45 failed");
                passed = false;
            }
            if ((nHldr_arr1d_op1[1] % i_arr1d_op2[0] != nHldr_arr1d_op1[1] % ui_arr1d_op2[0]) || (nHldr_arr1d_op1[1] % ui_arr1d_op2[0] != nHldr_arr1d_op1[1] % l_arr1d_op2[0]) || (nHldr_arr1d_op1[1] % l_arr1d_op2[0] != nHldr_arr1d_op1[1] % ul_arr1d_op2[0]) || (nHldr_arr1d_op1[1] % ul_arr1d_op2[0] != nHldr_arr1d_op1[1] % f_arr1d_op2[0]) || (nHldr_arr1d_op1[1] % f_arr1d_op2[0] != nHldr_arr1d_op1[1] % d_arr1d_op2[0]) || (nHldr_arr1d_op1[1] % d_arr1d_op2[0] != nHldr_arr1d_op1[1] % m_arr1d_op2[0]) || (nHldr_arr1d_op1[1] % m_arr1d_op2[0] != nHldr_arr1d_op1[1] % i_arr1d_op2[0]) || (nHldr_arr1d_op1[1] % i_arr1d_op2[0] != 1))
            {
                Console.WriteLine("testcase 46 failed");
                passed = false;
            }
            if ((nHldr_arr1d_op1[1] % i_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_arr1d_op1[1] % ui_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_arr1d_op1[1] % ui_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_arr1d_op1[1] % l_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_arr1d_op1[1] % l_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_arr1d_op1[1] % ul_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_arr1d_op1[1] % ul_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_arr1d_op1[1] % f_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_arr1d_op1[1] % f_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_arr1d_op1[1] % d_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_arr1d_op1[1] % d_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_arr1d_op1[1] % m_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_arr1d_op1[1] % m_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_arr1d_op1[1] % i_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_arr1d_op1[1] % i_arr2d_op2[index[0, 1], index[1, 0]] != 1))
            {
                Console.WriteLine("testcase 47 failed");
                passed = false;
            }
            if ((nHldr_arr1d_op1[1] % i_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_arr1d_op1[1] % ui_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_arr1d_op1[1] % ui_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_arr1d_op1[1] % l_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_arr1d_op1[1] % l_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_arr1d_op1[1] % ul_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_arr1d_op1[1] % ul_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_arr1d_op1[1] % f_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_arr1d_op1[1] % f_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_arr1d_op1[1] % d_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_arr1d_op1[1] % d_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_arr1d_op1[1] % m_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_arr1d_op1[1] % m_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_arr1d_op1[1] % i_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_arr1d_op1[1] % i_arr3d_op2[index[0, 0], 0, index[1, 1]] != 1))
            {
                Console.WriteLine("testcase 48 failed");
                passed = false;
            }
            if ((nHldr_arr2d_op1[index[0, 1], index[1, 0]] % i_l_op2 != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % ui_l_op2) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % ui_l_op2 != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % l_l_op2) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % l_l_op2 != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % ul_l_op2) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % ul_l_op2 != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % f_l_op2) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % f_l_op2 != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % d_l_op2) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % d_l_op2 != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % m_l_op2) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % m_l_op2 != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % i_l_op2) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % i_l_op2 != 1))
            {
                Console.WriteLine("testcase 49 failed");
                passed = false;
            }
            if ((nHldr_arr2d_op1[index[0, 1], index[1, 0]] % s_i_s_op2 != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % s_ui_s_op2) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % s_ui_s_op2 != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % s_l_s_op2) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % s_l_s_op2 != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % s_ul_s_op2) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % s_ul_s_op2 != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % s_f_s_op2) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % s_f_s_op2 != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % s_d_s_op2) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % s_d_s_op2 != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % s_m_s_op2) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % s_m_s_op2 != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % s_i_s_op2) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % s_i_s_op2 != 1))
            {
                Console.WriteLine("testcase 50 failed");
                passed = false;
            }
            if ((nHldr_arr2d_op1[index[0, 1], index[1, 0]] % nHldr_f("op2") != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % nHldr_f("op2")) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % nHldr_f("op2") != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % nHldr_f("op2")) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % nHldr_f("op2") != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % nHldr_f("op2")) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % nHldr_f("op2") != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % nHldr_f("op2")) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % nHldr_f("op2") != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % nHldr_f("op2")) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % nHldr_f("op2") != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % nHldr_f("op2")) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % nHldr_f("op2") != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % nHldr_f("op2")) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % nHldr_f("op2") != 1))
            {
                Console.WriteLine("testcase 51 failed");
                passed = false;
            }
            if ((nHldr_arr2d_op1[index[0, 1], index[1, 0]] % cl1.i_cl_op2 != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % cl1.ui_cl_op2) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % cl1.ui_cl_op2 != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % cl1.l_cl_op2) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % cl1.l_cl_op2 != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % cl1.ul_cl_op2) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % cl1.ul_cl_op2 != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % cl1.f_cl_op2) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % cl1.f_cl_op2 != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % cl1.d_cl_op2) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % cl1.d_cl_op2 != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % cl1.m_cl_op2) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % cl1.m_cl_op2 != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % cl1.i_cl_op2) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % cl1.i_cl_op2 != 1))
            {
                Console.WriteLine("testcase 52 failed");
                passed = false;
            }
            if ((nHldr_arr2d_op1[index[0, 1], index[1, 0]] % vt1.i_vt_op2 != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % vt1.ui_vt_op2) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % vt1.ui_vt_op2 != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % vt1.l_vt_op2) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % vt1.l_vt_op2 != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % vt1.ul_vt_op2) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % vt1.ul_vt_op2 != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % vt1.f_vt_op2) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % vt1.f_vt_op2 != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % vt1.d_vt_op2) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % vt1.d_vt_op2 != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % vt1.m_vt_op2) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % vt1.m_vt_op2 != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % vt1.i_vt_op2) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % vt1.i_vt_op2 != 1))
            {
                Console.WriteLine("testcase 53 failed");
                passed = false;
            }
            if ((nHldr_arr2d_op1[index[0, 1], index[1, 0]] % i_arr1d_op2[0] != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % ui_arr1d_op2[0]) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % ui_arr1d_op2[0] != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % l_arr1d_op2[0]) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % l_arr1d_op2[0] != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % ul_arr1d_op2[0]) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % ul_arr1d_op2[0] != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % f_arr1d_op2[0]) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % f_arr1d_op2[0] != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % d_arr1d_op2[0]) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % d_arr1d_op2[0] != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % m_arr1d_op2[0]) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % m_arr1d_op2[0] != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % i_arr1d_op2[0]) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % i_arr1d_op2[0] != 1))
            {
                Console.WriteLine("testcase 54 failed");
                passed = false;
            }
            if ((nHldr_arr2d_op1[index[0, 1], index[1, 0]] % i_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % ui_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % ui_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % l_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % l_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % ul_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % ul_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % f_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % f_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % d_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % d_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % m_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % m_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % i_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % i_arr2d_op2[index[0, 1], index[1, 0]] != 1))
            {
                Console.WriteLine("testcase 55 failed");
                passed = false;
            }
            if ((nHldr_arr2d_op1[index[0, 1], index[1, 0]] % i_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % ui_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % ui_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % l_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % l_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % ul_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % ul_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % f_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % f_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % d_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % d_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % m_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % m_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_arr2d_op1[index[0, 1], index[1, 0]] % i_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_arr2d_op1[index[0, 1], index[1, 0]] % i_arr3d_op2[index[0, 0], 0, index[1, 1]] != 1))
            {
                Console.WriteLine("testcase 56 failed");
                passed = false;
            }
            if ((nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % i_l_op2 != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % ui_l_op2) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % ui_l_op2 != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % l_l_op2) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % l_l_op2 != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % ul_l_op2) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % ul_l_op2 != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % f_l_op2) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % f_l_op2 != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % d_l_op2) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % d_l_op2 != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % m_l_op2) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % m_l_op2 != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % i_l_op2) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % i_l_op2 != 1))
            {
                Console.WriteLine("testcase 57 failed");
                passed = false;
            }
            if ((nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % s_i_s_op2 != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % s_ui_s_op2) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % s_ui_s_op2 != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % s_l_s_op2) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % s_l_s_op2 != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % s_ul_s_op2) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % s_ul_s_op2 != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % s_f_s_op2) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % s_f_s_op2 != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % s_d_s_op2) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % s_d_s_op2 != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % s_m_s_op2) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % s_m_s_op2 != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % s_i_s_op2) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % s_i_s_op2 != 1))
            {
                Console.WriteLine("testcase 58 failed");
                passed = false;
            }
            if ((nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % nHldr_f("op2") != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % nHldr_f("op2")) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % nHldr_f("op2") != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % nHldr_f("op2")) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % nHldr_f("op2") != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % nHldr_f("op2")) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % nHldr_f("op2") != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % nHldr_f("op2")) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % nHldr_f("op2") != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % nHldr_f("op2")) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % nHldr_f("op2") != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % nHldr_f("op2")) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % nHldr_f("op2") != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % nHldr_f("op2")) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % nHldr_f("op2") != 1))
            {
                Console.WriteLine("testcase 59 failed");
                passed = false;
            }
            if ((nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % cl1.i_cl_op2 != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % cl1.ui_cl_op2) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % cl1.ui_cl_op2 != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % cl1.l_cl_op2) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % cl1.l_cl_op2 != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % cl1.ul_cl_op2) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % cl1.ul_cl_op2 != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % cl1.f_cl_op2) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % cl1.f_cl_op2 != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % cl1.d_cl_op2) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % cl1.d_cl_op2 != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % cl1.m_cl_op2) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % cl1.m_cl_op2 != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % cl1.i_cl_op2) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % cl1.i_cl_op2 != 1))
            {
                Console.WriteLine("testcase 60 failed");
                passed = false;
            }
            if ((nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % vt1.i_vt_op2 != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % vt1.ui_vt_op2) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % vt1.ui_vt_op2 != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % vt1.l_vt_op2) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % vt1.l_vt_op2 != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % vt1.ul_vt_op2) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % vt1.ul_vt_op2 != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % vt1.f_vt_op2) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % vt1.f_vt_op2 != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % vt1.d_vt_op2) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % vt1.d_vt_op2 != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % vt1.m_vt_op2) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % vt1.m_vt_op2 != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % vt1.i_vt_op2) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % vt1.i_vt_op2 != 1))
            {
                Console.WriteLine("testcase 61 failed");
                passed = false;
            }
            if ((nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % i_arr1d_op2[0] != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % ui_arr1d_op2[0]) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % ui_arr1d_op2[0] != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % l_arr1d_op2[0]) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % l_arr1d_op2[0] != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % ul_arr1d_op2[0]) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % ul_arr1d_op2[0] != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % f_arr1d_op2[0]) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % f_arr1d_op2[0] != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % d_arr1d_op2[0]) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % d_arr1d_op2[0] != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % m_arr1d_op2[0]) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % m_arr1d_op2[0] != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % i_arr1d_op2[0]) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % i_arr1d_op2[0] != 1))
            {
                Console.WriteLine("testcase 62 failed");
                passed = false;
            }
            if ((nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % i_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % ui_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % ui_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % l_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % l_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % ul_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % ul_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % f_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % f_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % d_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % d_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % m_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % m_arr2d_op2[index[0, 1], index[1, 0]] != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % i_arr2d_op2[index[0, 1], index[1, 0]]) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % i_arr2d_op2[index[0, 1], index[1, 0]] != 1))
            {
                Console.WriteLine("testcase 63 failed");
                passed = false;
            }
            if ((nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % i_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % ui_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % ui_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % l_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % l_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % ul_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % ul_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % f_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % f_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % d_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % d_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % m_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % m_arr3d_op2[index[0, 0], 0, index[1, 1]] != nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % i_arr3d_op2[index[0, 0], 0, index[1, 1]]) || (nHldr_arr3d_op1[index[0, 0], 0, index[1, 1]] % i_arr3d_op2[index[0, 0], 0, index[1, 1]] != 1))
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
