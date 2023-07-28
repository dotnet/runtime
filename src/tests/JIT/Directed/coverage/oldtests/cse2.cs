// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//Testing common sub-expression elimination in random code

using System;
using Xunit;
public unsafe class testout1
{
    public static int sa = 2;
    public static int sb = 1;
    public struct VT_0_1_2_5_2
    {
        public double a4_0_1_2_5_2;
    }


    public struct VT_0_1_2_5_1
    {
        public double a1_0_1_2_5_1;
    }


    public struct VT_0_1_2_4_3
    {
        public long a0_0_1_2_4_3;
    }


    public struct VT_0_1_2_4_2
    {
        public double a2_0_1_2_4_2;
    }


    public struct VT_0_1_2_3_2
    {
        public long a2_0_1_2_3_2;
    }


    public struct VT_0_1_2_3_1
    {
        public int a1_0_1_2_3_1;
        public long a4_0_1_2_3_1;
    }


    public struct VT_0_1_2_2_2
    {
        public double a3_0_1_2_2_2;
    }


    public struct VT_0_1_2_2_1
    {
        public long a0_0_1_2_2_1;
        public double a1_0_1_2_2_1;
        public double a4_0_1_2_2_1;
        public long a5_0_1_2_2_1;
    }


    public struct VT_0_1_2_1_2
    {
        public int a1_0_1_2_1_2;
        public double a4_0_1_2_1_2;
    }


    public struct VT_0_1_2_1_1
    {
        public double a0_0_1_2_1_1;
        public double a3_0_1_2_1_1;
        public double a4_0_1_2_1_1;
    }


    public struct VT_0_1_2_5
    {
        public double a1_0_1_2_5;
        public double a2_0_1_2_5;
    }


    public struct VT_0_1_2_3
    {
        public int a1_0_1_2_3;
        public long a2_0_1_2_3;
    }


    public struct VT_0_1_2_2
    {
        public double a3_0_1_2_2;
    }


    public struct VT_0_1_2_1
    {
        public double a1_0_1_2_1;
        public double a3_0_1_2_1;
    }


    public struct VT_0_1_2
    {
        public double a0_0_1_2;
    }


    public struct VT_0_1
    {
        public float a2_0_1;
        public int a3_0_1;
    }


    public struct VT_0
    {
        public float a2_0;
        public double a3_0;
        public double a4_0;
    }


    public class CL_0_1_2_5_2
    {
        public long a0_0_1_2_5_2 = -sa * sb;
    }


    public class CL_0_1_2_5_1
    {
        public double[,,] arr3d_0_1_2_5_1 = new double[5, 11, 4];
    }


    public class CL_0_1_2_4_1
    {
        public long[,,] arr3d_0_1_2_4_1 = new long[5, 11, 4];
        public long a1_0_1_2_4_1 = sa + sb;
    }


    public class CL_0_1_2_3_2
    {
        public long[,,] arr3d_0_1_2_3_2 = new long[5, 11, 4];
        public int[,] arr2d_0_1_2_3_2 = new int[3, 11];
    }


    public class CL_0_1_2_3_1
    {
        public long[] arr1d_0_1_2_3_1 = new long[11];
        public int[,,] arr3d_0_1_2_3_1 = new int[5, 11, 4];
        public int a5_0_1_2_3_1 = -sa / sb;
    }


    public class CL_0_1_2_2_2
    {
        public double a4_0_1_2_2_2 = sa - sb;
    }


    public class CL_0_1_2_2_1
    {
        public int[,] arr2d_0_1_2_2_1 = new int[3, 11];
    }


    public class CL_0_1_2_1_2
    {
        public double a0_0_1_2_1_2 = sa - sb;
    }


    public class CL_0_1_2_5
    {
        public int[,] arr2d_0_1_2_5 = new int[3, 11];
    }


    public class CL_0_1_2_1
    {
        public long[,] arr2d_0_1_2_1 = new long[3, 11];
    }


    public class CL_0_1
    {
        public double a1_0_1 = sa + sb;
    }


    public static VT_0_1_2_5_2 vtstatic_0_1_2_5_2 = new VT_0_1_2_5_2();




    public static VT_0_1_2_4_3 vtstatic_0_1_2_4_3 = new VT_0_1_2_4_3();




    public static CL_0_1_2_4_1 clstatic_0_1_2_4_1 = new CL_0_1_2_4_1();


    public static VT_0_1_2_3_2 vtstatic_0_1_2_3_2 = new VT_0_1_2_3_2();

    private static int s_a2_0_1_2_3_1 = sa / sb;

    public static VT_0_1_2_3_1 vtstatic_0_1_2_3_1 = new VT_0_1_2_3_1();

    private static double s_a1_0_1_2_2_2 = -sa * sb;



    public static VT_0_1_2_2_1 vtstatic_0_1_2_2_1 = new VT_0_1_2_2_1();
    public static CL_0_1_2_2_1 clstatic_0_1_2_2_1 = new CL_0_1_2_2_1();






    public static VT_0_1_2_5 vtstatic_0_1_2_5 = new VT_0_1_2_5();






    public static VT_0_1_2_2 vtstatic_0_1_2_2 = new VT_0_1_2_2();








    public static VT_0_1 vtstatic_0_1 = new VT_0_1();


    public static VT_0 vtstatic_0 = new VT_0();

    public static double Func_0_1_2_5_2(CL_0_1_2_5_2 cl_0_1_2_5_2, double* a5_0_1_2_5_2)
    {
        double a2_0_1_2_5_2 = sa + sb;
        double a3_0_1_2_5_2 = sa - sb;

        vtstatic_0_1_2_5_2.a4_0_1_2_5_2 = sa + sb;
        double retval_0_1_2_5_2 = Convert.ToDouble((a2_0_1_2_5_2 + ((sa + sb - ((sa - sb + cl_0_1_2_5_2.a0_0_1_2_5_2) / (*a5_0_1_2_5_2))) * (((*a5_0_1_2_5_2) / a3_0_1_2_5_2) + vtstatic_0_1_2_5_2.a4_0_1_2_5_2))));
        Console.WriteLine("retval_0_1_2_5_2 is {0}", retval_0_1_2_5_2);
        return retval_0_1_2_5_2;
    }

    public static double Func_0_1_2_5_1(CL_0_1_2_5_1 cl_0_1_2_5_1)
    {
        VT_0_1_2_5_1 vt_0_1_2_5_1 = new VT_0_1_2_5_1();
        vt_0_1_2_5_1.a1_0_1_2_5_1 = sa * sb;

        double retval_0_1_2_5_1 = Convert.ToDouble((((vt_0_1_2_5_1.a1_0_1_2_5_1 + -sa * sb) - vt_0_1_2_5_1.a1_0_1_2_5_1) * ((vt_0_1_2_5_1.a1_0_1_2_5_1 * 1.0) + ((vt_0_1_2_5_1.a1_0_1_2_5_1 + cl_0_1_2_5_1.arr3d_0_1_2_5_1[4, 0, 3]) - (-sa * sb)))));
        Console.WriteLine("retval_0_1_2_5_1 is {0}", retval_0_1_2_5_1);
        return retval_0_1_2_5_1;
    }

    public static double Func_0_1_2_4_3()
    {
        int[,] arr2d_0_1_2_4_3 = new int[3, 11];

        vtstatic_0_1_2_4_3.a0_0_1_2_4_3 = -sa / sb;
        arr2d_0_1_2_4_3[2, 1] = sa / sb;
        double retval_0_1_2_4_3 = Convert.ToDouble(((((double)((long)(Convert.ToInt32(arr2d_0_1_2_4_3[2, 1]) + (long)(vtstatic_0_1_2_4_3.a0_0_1_2_4_3)) * 0.25)) + (arr2d_0_1_2_4_3[2, 1] * (0.25 - (-sa / sb)))) - (((-sa / sb + sa + sb) + (-sa + sb * sa * sb)))));
        Console.WriteLine("retval_0_1_2_4_3 is {0}", retval_0_1_2_4_3);
        return retval_0_1_2_4_3;
    }

    public static double Func_0_1_2_4_2(VT_0_1_2_4_2 vt_0_1_2_4_2, double a3_0_1_2_4_2)
    {
        double a0_0_1_2_4_2 = -sa / sb;

        double retval_0_1_2_4_2 = Convert.ToDouble(((a0_0_1_2_4_2 + ((a0_0_1_2_4_2 - sa / sb) * a3_0_1_2_4_2)) + ((-sa / sb * ((a3_0_1_2_4_2 - sa / sb) - (vt_0_1_2_4_2.a2_0_1_2_4_2))) + (a0_0_1_2_4_2 + sa / sb))));
        Console.WriteLine("retval_0_1_2_4_2 is {0}", retval_0_1_2_4_2);
        return retval_0_1_2_4_2;
    }

    public static long Func_0_1_2_4_1(int[,,] arr3d_0_1_2_4_1)
    {
        CL_0_1_2_4_1 cl_0_1_2_4_1 = new CL_0_1_2_4_1();

        clstatic_0_1_2_4_1.arr3d_0_1_2_4_1[4, 0, 3] = -sa % sb;
        long retval_0_1_2_4_1 = Convert.ToInt64(((long)(Convert.ToInt32(arr3d_0_1_2_4_1[4, 2, 3]) - (long)((clstatic_0_1_2_4_1.arr3d_0_1_2_4_1[4, 0, 3] - cl_0_1_2_4_1.a1_0_1_2_4_1))) + clstatic_0_1_2_4_1.arr3d_0_1_2_4_1[4, 0, 3]));
        Console.WriteLine("retval_0_1_2_4_1 is {0}", retval_0_1_2_4_1);
        return retval_0_1_2_4_1;
    }

    public static long Func_0_1_2_3_2(int[,] arr2d_0_1_2_3_2, CL_0_1_2_3_2 cl_0_1_2_3_2)
    {
        vtstatic_0_1_2_3_2.a2_0_1_2_3_2 = -sa * sb;
        cl_0_1_2_3_2.arr3d_0_1_2_3_2[4, 0, 3] = sa * sb;
        long retval_0_1_2_3_2 = Convert.ToInt64((((long)(((long)(vtstatic_0_1_2_3_2.a2_0_1_2_3_2) * (long)(sa + sb)) / -sa * sb)) - (long)(Convert.ToInt32((Convert.ToInt32((Convert.ToInt32(cl_0_1_2_3_2.arr2d_0_1_2_3_2[2, 4])) % (Convert.ToInt32(arr2d_0_1_2_3_2[2, 1]))))) - (long)(((long)(vtstatic_0_1_2_3_2.a2_0_1_2_3_2 / (vtstatic_0_1_2_3_2.a2_0_1_2_3_2 - cl_0_1_2_3_2.arr3d_0_1_2_3_2[4, 0, 3])))))));
        Console.WriteLine("retval_0_1_2_3_2 is {0}", retval_0_1_2_3_2);
        return retval_0_1_2_3_2;
    }

    public static long Func_0_1_2_3_1(CL_0_1_2_3_1 cl_0_1_2_3_1)
    {
        VT_0_1_2_3_1 vt_0_1_2_3_1 = new VT_0_1_2_3_1();
        vt_0_1_2_3_1.a1_0_1_2_3_1 = -sa / sb;
        vt_0_1_2_3_1.a4_0_1_2_3_1 = sa + sb + sa / sb;

        vtstatic_0_1_2_3_1.a1_0_1_2_3_1 = -sa / sb;
        vtstatic_0_1_2_3_1.a4_0_1_2_3_1 = sa + sb;
        cl_0_1_2_3_1.arr1d_0_1_2_3_1[0] = sa / sb;
        long retval_0_1_2_3_1 = Convert.ToInt64((long)(Convert.ToInt32(((Convert.ToInt32((Convert.ToInt32(cl_0_1_2_3_1.arr3d_0_1_2_3_1[4, 3, 3])) % (Convert.ToInt32(s_a2_0_1_2_3_1)))) + cl_0_1_2_3_1.a5_0_1_2_3_1)) + (long)((long)(Convert.ToInt32(((cl_0_1_2_3_1.a5_0_1_2_3_1 + 0) - ((Convert.ToInt32((Convert.ToInt32(vt_0_1_2_3_1.a1_0_1_2_3_1)) % (Convert.ToInt32(sa + sb))))))) + (long)((vtstatic_0_1_2_3_1.a4_0_1_2_3_1 - cl_0_1_2_3_1.arr1d_0_1_2_3_1[0]))))));
        Console.WriteLine("retval_0_1_2_3_1 is {0}", retval_0_1_2_3_1);
        return retval_0_1_2_3_1;
    }

    public static double Func_0_1_2_2_2(int[,] arr2d_0_1_2_2_2, VT_0_1_2_2_2 vt_0_1_2_2_2, CL_0_1_2_2_2 cl_0_1_2_2_2)
    {
        double retval_0_1_2_2_2 = Convert.ToDouble(((-sa * sb * (cl_0_1_2_2_2.a4_0_1_2_2_2 + (cl_0_1_2_2_2.a4_0_1_2_2_2 - (vt_0_1_2_2_2.a3_0_1_2_2_2)))) - ((arr2d_0_1_2_2_2[2, 0] - (Convert.ToInt32(arr2d_0_1_2_2_2[2, 0] * sa * sb))) / (-sa * sb / s_a1_0_1_2_2_2))));
        Console.WriteLine("retval_0_1_2_2_2 is {0}", retval_0_1_2_2_2);
        return retval_0_1_2_2_2;
    }

    public static double Func_0_1_2_2_1(VT_0_1_2_2_1 vt_0_1_2_2_1)
    {
        vtstatic_0_1_2_2_1.a0_0_1_2_2_1 = -sa + sb;
        vtstatic_0_1_2_2_1.a1_0_1_2_2_1 = sa + sb * sb;
        vtstatic_0_1_2_2_1.a4_0_1_2_2_1 = sb * sa + sb * sa;
        vtstatic_0_1_2_2_1.a5_0_1_2_2_1 = sa - sb * sb;
        clstatic_0_1_2_2_1.arr2d_0_1_2_2_1[2, 3] = sa * sb - sb;
        double retval_0_1_2_2_1 = Convert.ToDouble((((sa + sb * sb - vtstatic_0_1_2_2_1.a1_0_1_2_2_1) + ((vtstatic_0_1_2_2_1.a1_0_1_2_2_1 + 0.0) / (20.0 - (sb * sa + sb * sa)))) - (((double)(((long)(vtstatic_0_1_2_2_1.a0_0_1_2_2_1 / (vtstatic_0_1_2_2_1.a0_0_1_2_2_1 + vt_0_1_2_2_1.a5_0_1_2_2_1 + sa - sb))) * (clstatic_0_1_2_2_1.arr2d_0_1_2_2_1[2, 3] * vtstatic_0_1_2_2_1.a4_0_1_2_2_1))))));
        Console.WriteLine("retval_0_1_2_2_1 is {0}", retval_0_1_2_2_1);
        return retval_0_1_2_2_1;
    }

    public static double Func_0_1_2_1_2(VT_0_1_2_1_2 vt_0_1_2_1_2)
    {
        CL_0_1_2_1_2 cl_0_1_2_1_2 = new CL_0_1_2_1_2();

        double retval_0_1_2_1_2 = Convert.ToDouble(((vt_0_1_2_1_2.a1_0_1_2_1_2 / (cl_0_1_2_1_2.a0_0_1_2_1_2 - (vt_0_1_2_1_2.a4_0_1_2_1_2))) - cl_0_1_2_1_2.a0_0_1_2_1_2));
        Console.WriteLine("retval_0_1_2_1_2 is {0}", retval_0_1_2_1_2);
        return retval_0_1_2_1_2;
    }

    public static double Func_0_1_2_1_1()
    {
        VT_0_1_2_1_1 vt_0_1_2_1_1 = new VT_0_1_2_1_1();
        vt_0_1_2_1_1.a0_0_1_2_1_1 = (sa + sb) * (sa + sb);
        vt_0_1_2_1_1.a3_0_1_2_1_1 = -(sa + sb) / (sa - sb);
        vt_0_1_2_1_1.a4_0_1_2_1_1 = -(sa + sb) / (sa * sb);

        double retval_0_1_2_1_1 = Convert.ToDouble((((vt_0_1_2_1_1.a3_0_1_2_1_1 - vt_0_1_2_1_1.a0_0_1_2_1_1) + vt_0_1_2_1_1.a3_0_1_2_1_1) - ((vt_0_1_2_1_1.a3_0_1_2_1_1 - vt_0_1_2_1_1.a0_0_1_2_1_1) - ((vt_0_1_2_1_1.a0_0_1_2_1_1 + vt_0_1_2_1_1.a4_0_1_2_1_1)))));
        Console.WriteLine("retval_0_1_2_1_1 is {0}", retval_0_1_2_1_1);
        return retval_0_1_2_1_1;
    }

    public static double Func_0_1_2_5(CL_0_1_2_5 cl_0_1_2_5)
    {
        VT_0_1_2_5 vt_0_1_2_5 = new VT_0_1_2_5();
        vt_0_1_2_5.a1_0_1_2_5 = sa - sb;
        vt_0_1_2_5.a2_0_1_2_5 = sa * sb;

        vtstatic_0_1_2_5.a1_0_1_2_5 = sa - sb;
        vtstatic_0_1_2_5.a2_0_1_2_5 = sa - sb;
        CL_0_1_2_5_2 cl_0_1_2_5_2 = new CL_0_1_2_5_2();
        double* a5_0_1_2_5_2 = stackalloc double[1];
        *a5_0_1_2_5_2 = sa * sb;
        double val_0_1_2_5_2 = Func_0_1_2_5_2(cl_0_1_2_5_2, a5_0_1_2_5_2);
        CL_0_1_2_5_1 cl_0_1_2_5_1 = new CL_0_1_2_5_1();
        cl_0_1_2_5_1.arr3d_0_1_2_5_1[4, 0, 3] = sa * sb;
        double val_0_1_2_5_1 = Func_0_1_2_5_1(cl_0_1_2_5_1);
        double retval_0_1_2_5 = Convert.ToDouble(((Convert.ToInt32((cl_0_1_2_5.arr2d_0_1_2_5[2, 0] * vt_0_1_2_5.a1_0_1_2_5) - (vtstatic_0_1_2_5.a2_0_1_2_5 + (vtstatic_0_1_2_5.a2_0_1_2_5 + (vtstatic_0_1_2_5.a2_0_1_2_5 + val_0_1_2_5_2))))) * val_0_1_2_5_1));
        Console.WriteLine("retval_0_1_2_5 is {0}", retval_0_1_2_5);
        return retval_0_1_2_5;
    }

    public static long Func_0_1_2_4(long* a0_0_1_2_4)
    {
        double val_0_1_2_4_3 = Func_0_1_2_4_3();
        VT_0_1_2_4_2 vt_0_1_2_4_2 = new VT_0_1_2_4_2();
        vt_0_1_2_4_2.a2_0_1_2_4_2 = -sa * sb;
        double a3_0_1_2_4_2 = -sa * sb;
        double val_0_1_2_4_2 = Func_0_1_2_4_2(vt_0_1_2_4_2, a3_0_1_2_4_2);
        int[,,] arr3d_0_1_2_4_1 = new int[5, 11, 4];
        arr3d_0_1_2_4_1[4, 2, 3] = sa * sb;
        long val_0_1_2_4_1 = Func_0_1_2_4_1(arr3d_0_1_2_4_1);
        long retval_0_1_2_4 = Convert.ToInt64((long)(Convert.ToInt32((Convert.ToInt32(((*a0_0_1_2_4) / val_0_1_2_4_3) + val_0_1_2_4_2))) - (long)(val_0_1_2_4_1)));
        Console.WriteLine("retval_0_1_2_4 is {0}", retval_0_1_2_4);
        return retval_0_1_2_4;
    }

    public static int Func_0_1_2_3()
    {
        VT_0_1_2_3 vt_0_1_2_3 = new VT_0_1_2_3();
        vt_0_1_2_3.a1_0_1_2_3 = -sa - sb;
        vt_0_1_2_3.a2_0_1_2_3 = sa + sb;
        long[,] arr2d_0_1_2_3 = new long[3, 11];
        int a3_0_1_2_3 = sa / sb;

        arr2d_0_1_2_3[2, 0] = sa / sb;
        CL_0_1_2_3_2 cl_0_1_2_3_2 = new CL_0_1_2_3_2();
        int[,] arr2d_0_1_2_3_2 = new int[3, 11];
        arr2d_0_1_2_3_2[2, 1] = sa + sb;
        cl_0_1_2_3_2.arr2d_0_1_2_3_2[2, 4] = sa - sb;
        long val_0_1_2_3_2 = Func_0_1_2_3_2(arr2d_0_1_2_3_2, cl_0_1_2_3_2);
        CL_0_1_2_3_1 cl_0_1_2_3_1 = new CL_0_1_2_3_1();
        cl_0_1_2_3_1.arr3d_0_1_2_3_1[4, 3, 3] = sa - sb;
        long val_0_1_2_3_1 = Func_0_1_2_3_1(cl_0_1_2_3_1);
        int retval_0_1_2_3 = Convert.ToInt32((Convert.ToInt32((long)((long)(Convert.ToInt32((a3_0_1_2_3 - (vt_0_1_2_3.a1_0_1_2_3))) + (long)((long)(Convert.ToInt32((Convert.ToInt32((long)(arr2d_0_1_2_3[2, 0]) - (long)(val_0_1_2_3_2)))) + (long)(arr2d_0_1_2_3[2, 0]))))) - (long)((long)(Convert.ToInt32(((sa + sb) / vt_0_1_2_3.a2_0_1_2_3)) - (long)(val_0_1_2_3_1))))));
        Console.WriteLine("retval_0_1_2_3 is {0}", retval_0_1_2_3);
        return retval_0_1_2_3;
    }

    public static long Func_0_1_2_2(long[,] arr2d_0_1_2_2)
    {
        vtstatic_0_1_2_2.a3_0_1_2_2 = -(sa - sb);
        VT_0_1_2_2_2 vt_0_1_2_2_2 = new VT_0_1_2_2_2();
        vt_0_1_2_2_2.a3_0_1_2_2_2 = -sa / sb;
        CL_0_1_2_2_2 cl_0_1_2_2_2 = new CL_0_1_2_2_2();
        int[,] arr2d_0_1_2_2_2 = new int[3, 11];
        arr2d_0_1_2_2_2[2, 0] = sa - sb;
        double val_0_1_2_2_2 = Func_0_1_2_2_2(arr2d_0_1_2_2_2, vt_0_1_2_2_2, cl_0_1_2_2_2);
        VT_0_1_2_2_1 vt_0_1_2_2_1 = new VT_0_1_2_2_1();
        vt_0_1_2_2_1.a0_0_1_2_2_1 = -sa / sb;
        vt_0_1_2_2_1.a1_0_1_2_2_1 = sa / sb;
        vt_0_1_2_2_1.a4_0_1_2_2_1 = sa - sb;
        vt_0_1_2_2_1.a5_0_1_2_2_1 = sa - sb;
        double val_0_1_2_2_1 = Func_0_1_2_2_1(vt_0_1_2_2_1);
        long retval_0_1_2_2 = Convert.ToInt64(((long)(Convert.ToInt32((Convert.ToInt32((val_0_1_2_2_1 - (val_0_1_2_2_2)) + vtstatic_0_1_2_2.a3_0_1_2_2))) - (long)(arr2d_0_1_2_2[2, 0])) - arr2d_0_1_2_2[2, 1]));
        Console.WriteLine("retval_0_1_2_2 is {0}", retval_0_1_2_2);
        return retval_0_1_2_2;
    }

    public static double Func_0_1_2_1(CL_0_1_2_1 cl_0_1_2_1, VT_0_1_2_1 vt_0_1_2_1)
    {
        VT_0_1_2_1_2 vt_0_1_2_1_2 = new VT_0_1_2_1_2();
        vt_0_1_2_1_2.a1_0_1_2_1_2 = 1;
        vt_0_1_2_1_2.a4_0_1_2_1_2 = -(sa / sb);
        double val_0_1_2_1_2 = Func_0_1_2_1_2(vt_0_1_2_1_2);
        double val_0_1_2_1_1 = Func_0_1_2_1_1();
        double retval_0_1_2_1 = Convert.ToDouble(((((vt_0_1_2_1.a1_0_1_2_1 + ((double)(cl_0_1_2_1.arr2d_0_1_2_1[2, 0] * (sa / sb)))) * vt_0_1_2_1.a1_0_1_2_1) + val_0_1_2_1_1) / (((double)(cl_0_1_2_1.arr2d_0_1_2_1[2, 0] * val_0_1_2_1_2)) - (vt_0_1_2_1.a3_0_1_2_1))));
        Console.WriteLine("retval_0_1_2_1 is {0}", retval_0_1_2_1);
        return retval_0_1_2_1;
    }

    public static long Func_0_1_2(VT_0_1_2 vt_0_1_2)
    {
        CL_0_1_2_5 cl_0_1_2_5 = new CL_0_1_2_5();
        cl_0_1_2_5.arr2d_0_1_2_5[2, 0] = sa * sb;
        double val_0_1_2_5 = Func_0_1_2_5(cl_0_1_2_5);
        long* a0_0_1_2_4 = stackalloc long[1];
        *a0_0_1_2_4 = sa + sb;
        long val_0_1_2_4 = Func_0_1_2_4(a0_0_1_2_4);
        int val_0_1_2_3 = Func_0_1_2_3();
        long[,] arr2d_0_1_2_2 = new long[3, 11];
        arr2d_0_1_2_2[2, 0] = -sa * sb;
        arr2d_0_1_2_2[2, 1] = sa * (sa + sb);
        long val_0_1_2_2 = Func_0_1_2_2(arr2d_0_1_2_2);
        VT_0_1_2_1 vt_0_1_2_1 = new VT_0_1_2_1();
        vt_0_1_2_1.a1_0_1_2_1 = -(sa * sb);
        vt_0_1_2_1.a3_0_1_2_1 = -sa + sb;
        CL_0_1_2_1 cl_0_1_2_1 = new CL_0_1_2_1();
        cl_0_1_2_1.arr2d_0_1_2_1[2, 0] = 2L;
        double val_0_1_2_1 = Func_0_1_2_1(cl_0_1_2_1, vt_0_1_2_1);
        long retval_0_1_2 = Convert.ToInt64((long)(Convert.ToInt32((Convert.ToInt32(val_0_1_2_5 - ((val_0_1_2_3 * vt_0_1_2.a0_0_1_2))))) + (long)((((long)((long)(Convert.ToInt32(sa + sb) - (long)(val_0_1_2_2)) / val_0_1_2_1)) + val_0_1_2_4))));
        Console.WriteLine("retval_0_1_2 is {0}", retval_0_1_2);
        return retval_0_1_2;
    }

    public static double Func_0_1_1()
    {
        double[,] arr2d_0_1_1 = new double[3, 11];

        arr2d_0_1_1[2, 0] = 0.0;
        double retval_0_1_1 = Convert.ToDouble(arr2d_0_1_1[2, 0]);
        Console.WriteLine("retval_0_1_1 is {0}", retval_0_1_1);
        return retval_0_1_1;
    }

    public static double Func_0_1(long[] arr1d_0_1, VT_0_1 vt_0_1)
    {
        CL_0_1 cl_0_1 = new CL_0_1();

        vtstatic_0_1.a2_0_1 = sa + sb;
        vtstatic_0_1.a3_0_1 = sa + sb;
        VT_0_1_2 vt_0_1_2 = new VT_0_1_2();
        vt_0_1_2.a0_0_1_2 = -(sa + sb);
        long val_0_1_2 = Func_0_1_2(vt_0_1_2);
        double val_0_1_1 = Func_0_1_1();
        double retval_0_1 = Convert.ToDouble((((((long)(val_0_1_2 / arr1d_0_1[0])) / (vtstatic_0_1.a3_0_1 * (sa + sb))) + val_0_1_1) * ((vt_0_1.a2_0_1 * (sa + sb)) * (cl_0_1.a1_0_1 - ((arr1d_0_1[0] / -(sa + sb)))))));
        Console.WriteLine("retval_0_1 is {0}", retval_0_1);
        return retval_0_1;
    }

    public static int Func_0(double[,] arr2d_0, VT_0 vt_0)
    {
        vtstatic_0.a2_0 = sa / sb;
        vtstatic_0.a3_0 = sa - sb;
        vtstatic_0.a4_0 = sa - sb;
        VT_0_1 vt_0_1 = new VT_0_1();
        vt_0_1.a2_0_1 = sa + sb;
        vt_0_1.a3_0_1 = sa / sb;
        long[] arr1d_0_1 = new long[11];
        arr1d_0_1[0] = 2L;
        double val_0_1 = Func_0_1(arr1d_0_1, vt_0_1);
        int retval_0 = Convert.ToInt32((Convert.ToInt32((Convert.ToInt32((val_0_1 - vtstatic_0.a3_0) + (vtstatic_0.a3_0 + (sa * sb)))) * (vtstatic_0.a4_0 / (((vt_0.a2_0 - (sb - sa)) * (vtstatic_0.a4_0 * sa * sb)) - (arr2d_0[2, 0]))))));
        Console.WriteLine("retval_0 is {0}", retval_0);
        return retval_0;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        sa = 10;
        sb = 5;
        vtstatic_0.a2_0 = sa + sb;
        vtstatic_0.a3_0 = sa * sb;
        vtstatic_0.a4_0 = sa - sb;
        VT_0 vt_0 = new VT_0();
        vt_0.a2_0 = sa * sb;
        vt_0.a3_0 = sa + sb;
        vt_0.a4_0 = sa - sb;
        double[,] arr2d_0 = new double[3, 11];
        arr2d_0[2, 0] = sa * sb;

        int retval;
        retval = Func_0(arr2d_0, vt_0);
        if (retval != 4858)
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
