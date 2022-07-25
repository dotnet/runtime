// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;
public class r4NaNadd
{

    //user-defined class that overloads operator +
    public class numHolder
    {
        float f_num;
        public numHolder(float f_num)
        {
            this.f_num = Convert.ToSingle(f_num);
        }

        public static float operator +(numHolder a, float b)
        {
            return a.f_num + b;
        }

        public static float operator +(numHolder a, numHolder b)
        {
            return a.f_num + b.f_num;
        }

    }

    static float f_s_test1_op1 = 0.0F;
    static float f_s_test1_op2 = Single.NaN;
    static float f_s_test2_op1 = Single.NegativeInfinity;
    static float f_s_test2_op2 = Single.NaN;
    static float f_s_test3_op1 = Single.NaN;
    static float f_s_test3_op2 = 6.2F;

    public static float f_test1_f(String s)
    {
        if (s == "test1_op1")
            return 0.0F;
        else
            return Single.NaN;
    }

    public static float f_test2_f(String s)
    {
        if (s == "test2_op1")
            return Single.NegativeInfinity;
        else
            return Single.NaN;
    }

    public static float f_test3_f(String s)
    {
        if (s == "test3_op1")
            return Single.NaN;
        else
            return 6.2F;
    }

    class CL
    {
        public float f_cl_test1_op1 = 0.0F;
        public float f_cl_test1_op2 = Single.NaN;
        public float f_cl_test2_op1 = Single.NegativeInfinity;
        public float f_cl_test2_op2 = Single.NaN;
        public float f_cl_test3_op1 = Single.NaN;
        public float f_cl_test3_op2 = 6.2F;
    }

    struct VT
    {
        public float f_vt_test1_op1;
        public float f_vt_test1_op2;
        public float f_vt_test2_op1;
        public float f_vt_test2_op2;
        public float f_vt_test3_op1;
        public float f_vt_test3_op2;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool passed = true;
        //initialize class
        CL cl1 = new CL();
        //initialize struct
        VT vt1;
        vt1.f_vt_test1_op1 = 0.0F;
        vt1.f_vt_test1_op2 = Single.NaN;
        vt1.f_vt_test2_op1 = Single.NegativeInfinity;
        vt1.f_vt_test2_op2 = Single.NaN;
        vt1.f_vt_test3_op1 = Single.NaN;
        vt1.f_vt_test3_op2 = 6.2F;

        float[] f_arr1d_test1_op1 = { 0, 0.0F };
        float[,] f_arr2d_test1_op1 = { { 0, 0.0F }, { 1, 1 } };
        float[, ,] f_arr3d_test1_op1 = { { { 0, 0.0F }, { 1, 1 } } };
        float[] f_arr1d_test1_op2 = { Single.NaN, 0, 1 };
        float[,] f_arr2d_test1_op2 = { { 0, Single.NaN }, { 1, 1 } };
        float[, ,] f_arr3d_test1_op2 = { { { 0, Single.NaN }, { 1, 1 } } };
        float[] f_arr1d_test2_op1 = { 0, Single.NegativeInfinity };
        float[,] f_arr2d_test2_op1 = { { 0, Single.NegativeInfinity }, { 1, 1 } };
        float[, ,] f_arr3d_test2_op1 = { { { 0, Single.NegativeInfinity }, { 1, 1 } } };
        float[] f_arr1d_test2_op2 = { Single.NaN, 0, 1 };
        float[,] f_arr2d_test2_op2 = { { 0, Single.NaN }, { 1, 1 } };
        float[, ,] f_arr3d_test2_op2 = { { { 0, Single.NaN }, { 1, 1 } } };
        float[] f_arr1d_test3_op1 = { 0, Single.NaN };
        float[,] f_arr2d_test3_op1 = { { 0, Single.NaN }, { 1, 1 } };
        float[, ,] f_arr3d_test3_op1 = { { { 0, Single.NaN }, { 1, 1 } } };
        float[] f_arr1d_test3_op2 = { 6.2F, 0, 1 };
        float[,] f_arr2d_test3_op2 = { { 0, 6.2F }, { 1, 1 } };
        float[, ,] f_arr3d_test3_op2 = { { { 0, 6.2F }, { 1, 1 } } };

        int[,] index = { { 0, 0 }, { 1, 1 } };

        {
            float f_l_test1_op1 = 0.0F;
            float f_l_test1_op2 = Single.NaN;
            if (!Single.IsNaN(f_l_test1_op1 + f_l_test1_op2))
            {
                Console.WriteLine("Test1_testcase 1 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_l_test1_op1 + f_s_test1_op2))
            {
                Console.WriteLine("Test1_testcase 2 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_l_test1_op1 + f_test1_f("test1_op2")))
            {
                Console.WriteLine("Test1_testcase 3 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_l_test1_op1 + cl1.f_cl_test1_op2))
            {
                Console.WriteLine("Test1_testcase 4 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_l_test1_op1 + vt1.f_vt_test1_op2))
            {
                Console.WriteLine("Test1_testcase 5 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_l_test1_op1 + f_arr1d_test1_op2[0]))
            {
                Console.WriteLine("Test1_testcase 6 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_l_test1_op1 + f_arr2d_test1_op2[index[0, 1], index[1, 0]]))
            {
                Console.WriteLine("Test1_testcase 7 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_l_test1_op1 + f_arr3d_test1_op2[index[0, 0], 0, index[1, 1]]))
            {
                Console.WriteLine("Test1_testcase 8 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_s_test1_op1 + f_l_test1_op2))
            {
                Console.WriteLine("Test1_testcase 9 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_s_test1_op1 + f_s_test1_op2))
            {
                Console.WriteLine("Test1_testcase 10 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_s_test1_op1 + f_test1_f("test1_op2")))
            {
                Console.WriteLine("Test1_testcase 11 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_s_test1_op1 + cl1.f_cl_test1_op2))
            {
                Console.WriteLine("Test1_testcase 12 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_s_test1_op1 + vt1.f_vt_test1_op2))
            {
                Console.WriteLine("Test1_testcase 13 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_s_test1_op1 + f_arr1d_test1_op2[0]))
            {
                Console.WriteLine("Test1_testcase 14 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_s_test1_op1 + f_arr2d_test1_op2[index[0, 1], index[1, 0]]))
            {
                Console.WriteLine("Test1_testcase 15 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_s_test1_op1 + f_arr3d_test1_op2[index[0, 0], 0, index[1, 1]]))
            {
                Console.WriteLine("Test1_testcase 16 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_test1_f("test1_op1") + f_l_test1_op2))
            {
                Console.WriteLine("Test1_testcase 17 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_test1_f("test1_op1") + f_s_test1_op2))
            {
                Console.WriteLine("Test1_testcase 18 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_test1_f("test1_op1") + f_test1_f("test1_op2")))
            {
                Console.WriteLine("Test1_testcase 19 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_test1_f("test1_op1") + cl1.f_cl_test1_op2))
            {
                Console.WriteLine("Test1_testcase 20 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_test1_f("test1_op1") + vt1.f_vt_test1_op2))
            {
                Console.WriteLine("Test1_testcase 21 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_test1_f("test1_op1") + f_arr1d_test1_op2[0]))
            {
                Console.WriteLine("Test1_testcase 22 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_test1_f("test1_op1") + f_arr2d_test1_op2[index[0, 1], index[1, 0]]))
            {
                Console.WriteLine("Test1_testcase 23 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_test1_f("test1_op1") + f_arr3d_test1_op2[index[0, 0], 0, index[1, 1]]))
            {
                Console.WriteLine("Test1_testcase 24 failed");
                passed = false;
            }
            if (!Single.IsNaN(cl1.f_cl_test1_op1 + f_l_test1_op2))
            {
                Console.WriteLine("Test1_testcase 25 failed");
                passed = false;
            }
            if (!Single.IsNaN(cl1.f_cl_test1_op1 + f_s_test1_op2))
            {
                Console.WriteLine("Test1_testcase 26 failed");
                passed = false;
            }
            if (!Single.IsNaN(cl1.f_cl_test1_op1 + f_test1_f("test1_op2")))
            {
                Console.WriteLine("Test1_testcase 27 failed");
                passed = false;
            }
            if (!Single.IsNaN(cl1.f_cl_test1_op1 + cl1.f_cl_test1_op2))
            {
                Console.WriteLine("Test1_testcase 28 failed");
                passed = false;
            }
            if (!Single.IsNaN(cl1.f_cl_test1_op1 + vt1.f_vt_test1_op2))
            {
                Console.WriteLine("Test1_testcase 29 failed");
                passed = false;
            }
            if (!Single.IsNaN(cl1.f_cl_test1_op1 + f_arr1d_test1_op2[0]))
            {
                Console.WriteLine("Test1_testcase 30 failed");
                passed = false;
            }
            if (!Single.IsNaN(cl1.f_cl_test1_op1 + f_arr2d_test1_op2[index[0, 1], index[1, 0]]))
            {
                Console.WriteLine("Test1_testcase 31 failed");
                passed = false;
            }
            if (!Single.IsNaN(cl1.f_cl_test1_op1 + f_arr3d_test1_op2[index[0, 0], 0, index[1, 1]]))
            {
                Console.WriteLine("Test1_testcase 32 failed");
                passed = false;
            }
            if (!Single.IsNaN(vt1.f_vt_test1_op1 + f_l_test1_op2))
            {
                Console.WriteLine("Test1_testcase 33 failed");
                passed = false;
            }
            if (!Single.IsNaN(vt1.f_vt_test1_op1 + f_s_test1_op2))
            {
                Console.WriteLine("Test1_testcase 34 failed");
                passed = false;
            }
            if (!Single.IsNaN(vt1.f_vt_test1_op1 + f_test1_f("test1_op2")))
            {
                Console.WriteLine("Test1_testcase 35 failed");
                passed = false;
            }
            if (!Single.IsNaN(vt1.f_vt_test1_op1 + cl1.f_cl_test1_op2))
            {
                Console.WriteLine("Test1_testcase 36 failed");
                passed = false;
            }
            if (!Single.IsNaN(vt1.f_vt_test1_op1 + vt1.f_vt_test1_op2))
            {
                Console.WriteLine("Test1_testcase 37 failed");
                passed = false;
            }
            if (!Single.IsNaN(vt1.f_vt_test1_op1 + f_arr1d_test1_op2[0]))
            {
                Console.WriteLine("Test1_testcase 38 failed");
                passed = false;
            }
            if (!Single.IsNaN(vt1.f_vt_test1_op1 + f_arr2d_test1_op2[index[0, 1], index[1, 0]]))
            {
                Console.WriteLine("Test1_testcase 39 failed");
                passed = false;
            }
            if (!Single.IsNaN(vt1.f_vt_test1_op1 + f_arr3d_test1_op2[index[0, 0], 0, index[1, 1]]))
            {
                Console.WriteLine("Test1_testcase 40 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr1d_test1_op1[1] + f_l_test1_op2))
            {
                Console.WriteLine("Test1_testcase 41 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr1d_test1_op1[1] + f_s_test1_op2))
            {
                Console.WriteLine("Test1_testcase 42 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr1d_test1_op1[1] + f_test1_f("test1_op2")))
            {
                Console.WriteLine("Test1_testcase 43 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr1d_test1_op1[1] + cl1.f_cl_test1_op2))
            {
                Console.WriteLine("Test1_testcase 44 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr1d_test1_op1[1] + vt1.f_vt_test1_op2))
            {
                Console.WriteLine("Test1_testcase 45 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr1d_test1_op1[1] + f_arr1d_test1_op2[0]))
            {
                Console.WriteLine("Test1_testcase 46 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr1d_test1_op1[1] + f_arr2d_test1_op2[index[0, 1], index[1, 0]]))
            {
                Console.WriteLine("Test1_testcase 47 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr1d_test1_op1[1] + f_arr3d_test1_op2[index[0, 0], 0, index[1, 1]]))
            {
                Console.WriteLine("Test1_testcase 48 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr2d_test1_op1[index[0, 1], index[1, 0]] + f_l_test1_op2))
            {
                Console.WriteLine("Test1_testcase 49 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr2d_test1_op1[index[0, 1], index[1, 0]] + f_s_test1_op2))
            {
                Console.WriteLine("Test1_testcase 50 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr2d_test1_op1[index[0, 1], index[1, 0]] + f_test1_f("test1_op2")))
            {
                Console.WriteLine("Test1_testcase 51 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr2d_test1_op1[index[0, 1], index[1, 0]] + cl1.f_cl_test1_op2))
            {
                Console.WriteLine("Test1_testcase 52 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr2d_test1_op1[index[0, 1], index[1, 0]] + vt1.f_vt_test1_op2))
            {
                Console.WriteLine("Test1_testcase 53 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr2d_test1_op1[index[0, 1], index[1, 0]] + f_arr1d_test1_op2[0]))
            {
                Console.WriteLine("Test1_testcase 54 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr2d_test1_op1[index[0, 1], index[1, 0]] + f_arr2d_test1_op2[index[0, 1], index[1, 0]]))
            {
                Console.WriteLine("Test1_testcase 55 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr2d_test1_op1[index[0, 1], index[1, 0]] + f_arr3d_test1_op2[index[0, 0], 0, index[1, 1]]))
            {
                Console.WriteLine("Test1_testcase 56 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr3d_test1_op1[index[0, 0], 0, index[1, 1]] + f_l_test1_op2))
            {
                Console.WriteLine("Test1_testcase 57 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr3d_test1_op1[index[0, 0], 0, index[1, 1]] + f_s_test1_op2))
            {
                Console.WriteLine("Test1_testcase 58 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr3d_test1_op1[index[0, 0], 0, index[1, 1]] + f_test1_f("test1_op2")))
            {
                Console.WriteLine("Test1_testcase 59 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr3d_test1_op1[index[0, 0], 0, index[1, 1]] + cl1.f_cl_test1_op2))
            {
                Console.WriteLine("Test1_testcase 60 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr3d_test1_op1[index[0, 0], 0, index[1, 1]] + vt1.f_vt_test1_op2))
            {
                Console.WriteLine("Test1_testcase 61 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr3d_test1_op1[index[0, 0], 0, index[1, 1]] + f_arr1d_test1_op2[0]))
            {
                Console.WriteLine("Test1_testcase 62 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr3d_test1_op1[index[0, 0], 0, index[1, 1]] + f_arr2d_test1_op2[index[0, 1], index[1, 0]]))
            {
                Console.WriteLine("Test1_testcase 63 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr3d_test1_op1[index[0, 0], 0, index[1, 1]] + f_arr3d_test1_op2[index[0, 0], 0, index[1, 1]]))
            {
                Console.WriteLine("Test1_testcase 64 failed");
                passed = false;
            }
        }

        {
            float f_l_test2_op1 = Single.NegativeInfinity;
            float f_l_test2_op2 = Single.NaN;
            if (!Single.IsNaN(f_l_test2_op1 + f_l_test2_op2))
            {
                Console.WriteLine("Test2_testcase 1 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_l_test2_op1 + f_s_test2_op2))
            {
                Console.WriteLine("Test2_testcase 2 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_l_test2_op1 + f_test2_f("test2_op2")))
            {
                Console.WriteLine("Test2_testcase 3 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_l_test2_op1 + cl1.f_cl_test2_op2))
            {
                Console.WriteLine("Test2_testcase 4 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_l_test2_op1 + vt1.f_vt_test2_op2))
            {
                Console.WriteLine("Test2_testcase 5 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_l_test2_op1 + f_arr1d_test2_op2[0]))
            {
                Console.WriteLine("Test2_testcase 6 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_l_test2_op1 + f_arr2d_test2_op2[index[0, 1], index[1, 0]]))
            {
                Console.WriteLine("Test2_testcase 7 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_l_test2_op1 + f_arr3d_test2_op2[index[0, 0], 0, index[1, 1]]))
            {
                Console.WriteLine("Test2_testcase 8 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_s_test2_op1 + f_l_test2_op2))
            {
                Console.WriteLine("Test2_testcase 9 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_s_test2_op1 + f_s_test2_op2))
            {
                Console.WriteLine("Test2_testcase 10 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_s_test2_op1 + f_test2_f("test2_op2")))
            {
                Console.WriteLine("Test2_testcase 11 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_s_test2_op1 + cl1.f_cl_test2_op2))
            {
                Console.WriteLine("Test2_testcase 12 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_s_test2_op1 + vt1.f_vt_test2_op2))
            {
                Console.WriteLine("Test2_testcase 13 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_s_test2_op1 + f_arr1d_test2_op2[0]))
            {
                Console.WriteLine("Test2_testcase 14 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_s_test2_op1 + f_arr2d_test2_op2[index[0, 1], index[1, 0]]))
            {
                Console.WriteLine("Test2_testcase 15 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_s_test2_op1 + f_arr3d_test2_op2[index[0, 0], 0, index[1, 1]]))
            {
                Console.WriteLine("Test2_testcase 16 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_test2_f("test2_op1") + f_l_test2_op2))
            {
                Console.WriteLine("Test2_testcase 17 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_test2_f("test2_op1") + f_s_test2_op2))
            {
                Console.WriteLine("Test2_testcase 18 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_test2_f("test2_op1") + f_test2_f("test2_op2")))
            {
                Console.WriteLine("Test2_testcase 19 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_test2_f("test2_op1") + cl1.f_cl_test2_op2))
            {
                Console.WriteLine("Test2_testcase 20 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_test2_f("test2_op1") + vt1.f_vt_test2_op2))
            {
                Console.WriteLine("Test2_testcase 21 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_test2_f("test2_op1") + f_arr1d_test2_op2[0]))
            {
                Console.WriteLine("Test2_testcase 22 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_test2_f("test2_op1") + f_arr2d_test2_op2[index[0, 1], index[1, 0]]))
            {
                Console.WriteLine("Test2_testcase 23 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_test2_f("test2_op1") + f_arr3d_test2_op2[index[0, 0], 0, index[1, 1]]))
            {
                Console.WriteLine("Test2_testcase 24 failed");
                passed = false;
            }
            if (!Single.IsNaN(cl1.f_cl_test2_op1 + f_l_test2_op2))
            {
                Console.WriteLine("Test2_testcase 25 failed");
                passed = false;
            }
            if (!Single.IsNaN(cl1.f_cl_test2_op1 + f_s_test2_op2))
            {
                Console.WriteLine("Test2_testcase 26 failed");
                passed = false;
            }
            if (!Single.IsNaN(cl1.f_cl_test2_op1 + f_test2_f("test2_op2")))
            {
                Console.WriteLine("Test2_testcase 27 failed");
                passed = false;
            }
            if (!Single.IsNaN(cl1.f_cl_test2_op1 + cl1.f_cl_test2_op2))
            {
                Console.WriteLine("Test2_testcase 28 failed");
                passed = false;
            }
            if (!Single.IsNaN(cl1.f_cl_test2_op1 + vt1.f_vt_test2_op2))
            {
                Console.WriteLine("Test2_testcase 29 failed");
                passed = false;
            }
            if (!Single.IsNaN(cl1.f_cl_test2_op1 + f_arr1d_test2_op2[0]))
            {
                Console.WriteLine("Test2_testcase 30 failed");
                passed = false;
            }
            if (!Single.IsNaN(cl1.f_cl_test2_op1 + f_arr2d_test2_op2[index[0, 1], index[1, 0]]))
            {
                Console.WriteLine("Test2_testcase 31 failed");
                passed = false;
            }
            if (!Single.IsNaN(cl1.f_cl_test2_op1 + f_arr3d_test2_op2[index[0, 0], 0, index[1, 1]]))
            {
                Console.WriteLine("Test2_testcase 32 failed");
                passed = false;
            }
            if (!Single.IsNaN(vt1.f_vt_test2_op1 + f_l_test2_op2))
            {
                Console.WriteLine("Test2_testcase 33 failed");
                passed = false;
            }
            if (!Single.IsNaN(vt1.f_vt_test2_op1 + f_s_test2_op2))
            {
                Console.WriteLine("Test2_testcase 34 failed");
                passed = false;
            }
            if (!Single.IsNaN(vt1.f_vt_test2_op1 + f_test2_f("test2_op2")))
            {
                Console.WriteLine("Test2_testcase 35 failed");
                passed = false;
            }
            if (!Single.IsNaN(vt1.f_vt_test2_op1 + cl1.f_cl_test2_op2))
            {
                Console.WriteLine("Test2_testcase 36 failed");
                passed = false;
            }
            if (!Single.IsNaN(vt1.f_vt_test2_op1 + vt1.f_vt_test2_op2))
            {
                Console.WriteLine("Test2_testcase 37 failed");
                passed = false;
            }
            if (!Single.IsNaN(vt1.f_vt_test2_op1 + f_arr1d_test2_op2[0]))
            {
                Console.WriteLine("Test2_testcase 38 failed");
                passed = false;
            }
            if (!Single.IsNaN(vt1.f_vt_test2_op1 + f_arr2d_test2_op2[index[0, 1], index[1, 0]]))
            {
                Console.WriteLine("Test2_testcase 39 failed");
                passed = false;
            }
            if (!Single.IsNaN(vt1.f_vt_test2_op1 + f_arr3d_test2_op2[index[0, 0], 0, index[1, 1]]))
            {
                Console.WriteLine("Test2_testcase 40 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr1d_test2_op1[1] + f_l_test2_op2))
            {
                Console.WriteLine("Test2_testcase 41 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr1d_test2_op1[1] + f_s_test2_op2))
            {
                Console.WriteLine("Test2_testcase 42 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr1d_test2_op1[1] + f_test2_f("test2_op2")))
            {
                Console.WriteLine("Test2_testcase 43 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr1d_test2_op1[1] + cl1.f_cl_test2_op2))
            {
                Console.WriteLine("Test2_testcase 44 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr1d_test2_op1[1] + vt1.f_vt_test2_op2))
            {
                Console.WriteLine("Test2_testcase 45 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr1d_test2_op1[1] + f_arr1d_test2_op2[0]))
            {
                Console.WriteLine("Test2_testcase 46 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr1d_test2_op1[1] + f_arr2d_test2_op2[index[0, 1], index[1, 0]]))
            {
                Console.WriteLine("Test2_testcase 47 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr1d_test2_op1[1] + f_arr3d_test2_op2[index[0, 0], 0, index[1, 1]]))
            {
                Console.WriteLine("Test2_testcase 48 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr2d_test2_op1[index[0, 1], index[1, 0]] + f_l_test2_op2))
            {
                Console.WriteLine("Test2_testcase 49 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr2d_test2_op1[index[0, 1], index[1, 0]] + f_s_test2_op2))
            {
                Console.WriteLine("Test2_testcase 50 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr2d_test2_op1[index[0, 1], index[1, 0]] + f_test2_f("test2_op2")))
            {
                Console.WriteLine("Test2_testcase 51 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr2d_test2_op1[index[0, 1], index[1, 0]] + cl1.f_cl_test2_op2))
            {
                Console.WriteLine("Test2_testcase 52 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr2d_test2_op1[index[0, 1], index[1, 0]] + vt1.f_vt_test2_op2))
            {
                Console.WriteLine("Test2_testcase 53 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr2d_test2_op1[index[0, 1], index[1, 0]] + f_arr1d_test2_op2[0]))
            {
                Console.WriteLine("Test2_testcase 54 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr2d_test2_op1[index[0, 1], index[1, 0]] + f_arr2d_test2_op2[index[0, 1], index[1, 0]]))
            {
                Console.WriteLine("Test2_testcase 55 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr2d_test2_op1[index[0, 1], index[1, 0]] + f_arr3d_test2_op2[index[0, 0], 0, index[1, 1]]))
            {
                Console.WriteLine("Test2_testcase 56 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr3d_test2_op1[index[0, 0], 0, index[1, 1]] + f_l_test2_op2))
            {
                Console.WriteLine("Test2_testcase 57 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr3d_test2_op1[index[0, 0], 0, index[1, 1]] + f_s_test2_op2))
            {
                Console.WriteLine("Test2_testcase 58 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr3d_test2_op1[index[0, 0], 0, index[1, 1]] + f_test2_f("test2_op2")))
            {
                Console.WriteLine("Test2_testcase 59 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr3d_test2_op1[index[0, 0], 0, index[1, 1]] + cl1.f_cl_test2_op2))
            {
                Console.WriteLine("Test2_testcase 60 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr3d_test2_op1[index[0, 0], 0, index[1, 1]] + vt1.f_vt_test2_op2))
            {
                Console.WriteLine("Test2_testcase 61 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr3d_test2_op1[index[0, 0], 0, index[1, 1]] + f_arr1d_test2_op2[0]))
            {
                Console.WriteLine("Test2_testcase 62 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr3d_test2_op1[index[0, 0], 0, index[1, 1]] + f_arr2d_test2_op2[index[0, 1], index[1, 0]]))
            {
                Console.WriteLine("Test2_testcase 63 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr3d_test2_op1[index[0, 0], 0, index[1, 1]] + f_arr3d_test2_op2[index[0, 0], 0, index[1, 1]]))
            {
                Console.WriteLine("Test2_testcase 64 failed");
                passed = false;
            }
        }

        {
            float f_l_test3_op1 = Single.NaN;
            float f_l_test3_op2 = 6.2F;
            if (!Single.IsNaN(f_l_test3_op1 + f_l_test3_op2))
            {
                Console.WriteLine("Test3_testcase 1 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_l_test3_op1 + f_s_test3_op2))
            {
                Console.WriteLine("Test3_testcase 2 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_l_test3_op1 + f_test3_f("test3_op2")))
            {
                Console.WriteLine("Test3_testcase 3 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_l_test3_op1 + cl1.f_cl_test3_op2))
            {
                Console.WriteLine("Test3_testcase 4 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_l_test3_op1 + vt1.f_vt_test3_op2))
            {
                Console.WriteLine("Test3_testcase 5 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_l_test3_op1 + f_arr1d_test3_op2[0]))
            {
                Console.WriteLine("Test3_testcase 6 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_l_test3_op1 + f_arr2d_test3_op2[index[0, 1], index[1, 0]]))
            {
                Console.WriteLine("Test3_testcase 7 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_l_test3_op1 + f_arr3d_test3_op2[index[0, 0], 0, index[1, 1]]))
            {
                Console.WriteLine("Test3_testcase 8 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_s_test3_op1 + f_l_test3_op2))
            {
                Console.WriteLine("Test3_testcase 9 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_s_test3_op1 + f_s_test3_op2))
            {
                Console.WriteLine("Test3_testcase 10 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_s_test3_op1 + f_test3_f("test3_op2")))
            {
                Console.WriteLine("Test3_testcase 11 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_s_test3_op1 + cl1.f_cl_test3_op2))
            {
                Console.WriteLine("Test3_testcase 12 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_s_test3_op1 + vt1.f_vt_test3_op2))
            {
                Console.WriteLine("Test3_testcase 13 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_s_test3_op1 + f_arr1d_test3_op2[0]))
            {
                Console.WriteLine("Test3_testcase 14 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_s_test3_op1 + f_arr2d_test3_op2[index[0, 1], index[1, 0]]))
            {
                Console.WriteLine("Test3_testcase 15 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_s_test3_op1 + f_arr3d_test3_op2[index[0, 0], 0, index[1, 1]]))
            {
                Console.WriteLine("Test3_testcase 16 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_test3_f("test3_op1") + f_l_test3_op2))
            {
                Console.WriteLine("Test3_testcase 17 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_test3_f("test3_op1") + f_s_test3_op2))
            {
                Console.WriteLine("Test3_testcase 18 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_test3_f("test3_op1") + f_test3_f("test3_op2")))
            {
                Console.WriteLine("Test3_testcase 19 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_test3_f("test3_op1") + cl1.f_cl_test3_op2))
            {
                Console.WriteLine("Test3_testcase 20 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_test3_f("test3_op1") + vt1.f_vt_test3_op2))
            {
                Console.WriteLine("Test3_testcase 21 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_test3_f("test3_op1") + f_arr1d_test3_op2[0]))
            {
                Console.WriteLine("Test3_testcase 22 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_test3_f("test3_op1") + f_arr2d_test3_op2[index[0, 1], index[1, 0]]))
            {
                Console.WriteLine("Test3_testcase 23 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_test3_f("test3_op1") + f_arr3d_test3_op2[index[0, 0], 0, index[1, 1]]))
            {
                Console.WriteLine("Test3_testcase 24 failed");
                passed = false;
            }
            if (!Single.IsNaN(cl1.f_cl_test3_op1 + f_l_test3_op2))
            {
                Console.WriteLine("Test3_testcase 25 failed");
                passed = false;
            }
            if (!Single.IsNaN(cl1.f_cl_test3_op1 + f_s_test3_op2))
            {
                Console.WriteLine("Test3_testcase 26 failed");
                passed = false;
            }
            if (!Single.IsNaN(cl1.f_cl_test3_op1 + f_test3_f("test3_op2")))
            {
                Console.WriteLine("Test3_testcase 27 failed");
                passed = false;
            }
            if (!Single.IsNaN(cl1.f_cl_test3_op1 + cl1.f_cl_test3_op2))
            {
                Console.WriteLine("Test3_testcase 28 failed");
                passed = false;
            }
            if (!Single.IsNaN(cl1.f_cl_test3_op1 + vt1.f_vt_test3_op2))
            {
                Console.WriteLine("Test3_testcase 29 failed");
                passed = false;
            }
            if (!Single.IsNaN(cl1.f_cl_test3_op1 + f_arr1d_test3_op2[0]))
            {
                Console.WriteLine("Test3_testcase 30 failed");
                passed = false;
            }
            if (!Single.IsNaN(cl1.f_cl_test3_op1 + f_arr2d_test3_op2[index[0, 1], index[1, 0]]))
            {
                Console.WriteLine("Test3_testcase 31 failed");
                passed = false;
            }
            if (!Single.IsNaN(cl1.f_cl_test3_op1 + f_arr3d_test3_op2[index[0, 0], 0, index[1, 1]]))
            {
                Console.WriteLine("Test3_testcase 32 failed");
                passed = false;
            }
            if (!Single.IsNaN(vt1.f_vt_test3_op1 + f_l_test3_op2))
            {
                Console.WriteLine("Test3_testcase 33 failed");
                passed = false;
            }
            if (!Single.IsNaN(vt1.f_vt_test3_op1 + f_s_test3_op2))
            {
                Console.WriteLine("Test3_testcase 34 failed");
                passed = false;
            }
            if (!Single.IsNaN(vt1.f_vt_test3_op1 + f_test3_f("test3_op2")))
            {
                Console.WriteLine("Test3_testcase 35 failed");
                passed = false;
            }
            if (!Single.IsNaN(vt1.f_vt_test3_op1 + cl1.f_cl_test3_op2))
            {
                Console.WriteLine("Test3_testcase 36 failed");
                passed = false;
            }
            if (!Single.IsNaN(vt1.f_vt_test3_op1 + vt1.f_vt_test3_op2))
            {
                Console.WriteLine("Test3_testcase 37 failed");
                passed = false;
            }
            if (!Single.IsNaN(vt1.f_vt_test3_op1 + f_arr1d_test3_op2[0]))
            {
                Console.WriteLine("Test3_testcase 38 failed");
                passed = false;
            }
            if (!Single.IsNaN(vt1.f_vt_test3_op1 + f_arr2d_test3_op2[index[0, 1], index[1, 0]]))
            {
                Console.WriteLine("Test3_testcase 39 failed");
                passed = false;
            }
            if (!Single.IsNaN(vt1.f_vt_test3_op1 + f_arr3d_test3_op2[index[0, 0], 0, index[1, 1]]))
            {
                Console.WriteLine("Test3_testcase 40 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr1d_test3_op1[1] + f_l_test3_op2))
            {
                Console.WriteLine("Test3_testcase 41 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr1d_test3_op1[1] + f_s_test3_op2))
            {
                Console.WriteLine("Test3_testcase 42 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr1d_test3_op1[1] + f_test3_f("test3_op2")))
            {
                Console.WriteLine("Test3_testcase 43 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr1d_test3_op1[1] + cl1.f_cl_test3_op2))
            {
                Console.WriteLine("Test3_testcase 44 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr1d_test3_op1[1] + vt1.f_vt_test3_op2))
            {
                Console.WriteLine("Test3_testcase 45 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr1d_test3_op1[1] + f_arr1d_test3_op2[0]))
            {
                Console.WriteLine("Test3_testcase 46 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr1d_test3_op1[1] + f_arr2d_test3_op2[index[0, 1], index[1, 0]]))
            {
                Console.WriteLine("Test3_testcase 47 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr1d_test3_op1[1] + f_arr3d_test3_op2[index[0, 0], 0, index[1, 1]]))
            {
                Console.WriteLine("Test3_testcase 48 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr2d_test3_op1[index[0, 1], index[1, 0]] + f_l_test3_op2))
            {
                Console.WriteLine("Test3_testcase 49 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr2d_test3_op1[index[0, 1], index[1, 0]] + f_s_test3_op2))
            {
                Console.WriteLine("Test3_testcase 50 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr2d_test3_op1[index[0, 1], index[1, 0]] + f_test3_f("test3_op2")))
            {
                Console.WriteLine("Test3_testcase 51 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr2d_test3_op1[index[0, 1], index[1, 0]] + cl1.f_cl_test3_op2))
            {
                Console.WriteLine("Test3_testcase 52 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr2d_test3_op1[index[0, 1], index[1, 0]] + vt1.f_vt_test3_op2))
            {
                Console.WriteLine("Test3_testcase 53 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr2d_test3_op1[index[0, 1], index[1, 0]] + f_arr1d_test3_op2[0]))
            {
                Console.WriteLine("Test3_testcase 54 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr2d_test3_op1[index[0, 1], index[1, 0]] + f_arr2d_test3_op2[index[0, 1], index[1, 0]]))
            {
                Console.WriteLine("Test3_testcase 55 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr2d_test3_op1[index[0, 1], index[1, 0]] + f_arr3d_test3_op2[index[0, 0], 0, index[1, 1]]))
            {
                Console.WriteLine("Test3_testcase 56 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr3d_test3_op1[index[0, 0], 0, index[1, 1]] + f_l_test3_op2))
            {
                Console.WriteLine("Test3_testcase 57 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr3d_test3_op1[index[0, 0], 0, index[1, 1]] + f_s_test3_op2))
            {
                Console.WriteLine("Test3_testcase 58 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr3d_test3_op1[index[0, 0], 0, index[1, 1]] + f_test3_f("test3_op2")))
            {
                Console.WriteLine("Test3_testcase 59 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr3d_test3_op1[index[0, 0], 0, index[1, 1]] + cl1.f_cl_test3_op2))
            {
                Console.WriteLine("Test3_testcase 60 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr3d_test3_op1[index[0, 0], 0, index[1, 1]] + vt1.f_vt_test3_op2))
            {
                Console.WriteLine("Test3_testcase 61 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr3d_test3_op1[index[0, 0], 0, index[1, 1]] + f_arr1d_test3_op2[0]))
            {
                Console.WriteLine("Test3_testcase 62 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr3d_test3_op1[index[0, 0], 0, index[1, 1]] + f_arr2d_test3_op2[index[0, 1], index[1, 0]]))
            {
                Console.WriteLine("Test3_testcase 63 failed");
                passed = false;
            }
            if (!Single.IsNaN(f_arr3d_test3_op1[index[0, 0], 0, index[1, 1]] + f_arr3d_test3_op2[index[0, 0], 0, index[1, 1]]))
            {
                Console.WriteLine("Test3_testcase 64 failed");
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
