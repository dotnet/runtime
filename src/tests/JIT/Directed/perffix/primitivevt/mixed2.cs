// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
public unsafe class testout1
{
    public struct VT_0_1_1_1_1_1
    {
        public int a1_0_1_1_1_1_1;
        public VT_0_1_1_1_1_1(int i)
        {
            a1_0_1_1_1_1_1 = 1;
        }
    }

    public struct VT_0_1_1_1_1
    {
        public uint a1_0_1_1_1_1;
        public VT_0_1_1_1_1(int i)
        {
            a1_0_1_1_1_1 = 1;
        }
    }

    public struct VT_0_1_1
    {
        public int[] arr1d_0_1_1;
        public VT_0_1_1(int i)
        {
            arr1d_0_1_1 = new int[9];
        }
    }

    public struct VT_0_1
    {
        public int a1_0_1;
        public VT_0_1(int i)
        {
            a1_0_1 = 1;
        }
    }
    public class CL_0_1
    {
        public int a2_0_1 = 2111165575;
    }
    private static double[] s_arr1d_0_1_1_1_1_1_1_1_1_1 = new double[9];


    private static float[,] s_arr2d_0_1_1_1_1_1_1_1_1 = new float[3, 9];




    private static long[,] s_arr2d_0_1_1_1_1_1_1 = new long[3, 9];


    private static int[,] s_arr2d_0_1_1_1_1_1 = new int[3, 9];


    private static long[,,] s_arr3d_0_1_1_1_1 = new long[5, 9, 4];


    private static double[] s_arr1d_0_1_1_1 = new double[9];



    public static VT_0_1_1 vtstatic_0_1_1 = new VT_0_1_1(1);


    public static VT_0_1 vtstatic_0_1 = new VT_0_1(1);

    private static Decimal[,,] s_arr3d_0 = new Decimal[5, 9, 4];
    private static long s_a1_0 = 512L;
    private static ushort s_a2_0 = 54198;


    public static float Func_0_1_1_1_1_1_1_1_1_1()
    {
        double* a1_0_1_1_1_1_1_1_1_1_1 = stackalloc double[1];
        *a1_0_1_1_1_1_1_1_1_1_1 = 4.420911011650241E-09;
        double* a3_0_1_1_1_1_1_1_1_1_1 = stackalloc double[1];
        *a3_0_1_1_1_1_1_1_1_1_1 = 3.7252902984619141E-09;

        s_arr1d_0_1_1_1_1_1_1_1_1_1[0] = 16384.0;
        double asgop0 = s_arr1d_0_1_1_1_1_1_1_1_1_1[0];
        asgop0 += ((s_arr1d_0_1_1_1_1_1_1_1_1_1[0] + -32512.0));
        (*a3_0_1_1_1_1_1_1_1_1_1) *= ((206158430208.0));
        if ((asgop0) >= ((*a1_0_1_1_1_1_1_1_1_1_1)))
        {
            float if0_0retval_0_1_1_1_1_1_1_1_1_1 = Convert.ToSingle(Convert.ToSingle((Convert.ToInt32((Convert.ToInt32(1218424101 * 0.37129554777249107)) * ((*a1_0_1_1_1_1_1_1_1_1_1)))) * (asgop0 + (*a3_0_1_1_1_1_1_1_1_1_1))));
            return if0_0retval_0_1_1_1_1_1_1_1_1_1;
        }
        return Convert.ToSingle(Convert.ToSingle((Convert.ToInt32((Convert.ToInt32(1218424101 * 0.37129554777249107)) * ((*a1_0_1_1_1_1_1_1_1_1_1)))) * (asgop0 + (*a3_0_1_1_1_1_1_1_1_1_1))));
    }

    public static double Func_0_1_1_1_1_1_1_1_1()
    {
        double a2_0_1_1_1_1_1_1_1_1 = 2.0524928800798607E-08;

        s_arr2d_0_1_1_1_1_1_1_1_1[2, 0] = -1920.0F;
        float val_0_1_1_1_1_1_1_1_1_1 = Func_0_1_1_1_1_1_1_1_1_1();
        double retval_0_1_1_1_1_1_1_1_1 = Convert.ToDouble((Convert.ToUInt16(val_0_1_1_1_1_1_1_1_1_1 + s_arr2d_0_1_1_1_1_1_1_1_1[2, 0]) / (29637 * (4.6566128730773926E-10 + a2_0_1_1_1_1_1_1_1_1))));
        return retval_0_1_1_1_1_1_1_1_1;
    }

    public static ushort Func_0_1_1_1_1_1_1_1()
    {
        ulong[] arr1d_0_1_1_1_1_1_1_1 = new ulong[9];

        arr1d_0_1_1_1_1_1_1_1[0] = 10247887074558758525UL;
        double val_0_1_1_1_1_1_1_1_1 = Func_0_1_1_1_1_1_1_1_1();
        if ((arr1d_0_1_1_1_1_1_1_1[0]) <= (10247887076011802624UL))
        {
            ushort if0_0retval_0_1_1_1_1_1_1_1 = Convert.ToUInt16((Convert.ToUInt16(Convert.ToInt32(Convert.ToUInt64(10247887076011802624UL) - Convert.ToUInt64(arr1d_0_1_1_1_1_1_1_1[0])) / val_0_1_1_1_1_1_1_1_1)));
            return if0_0retval_0_1_1_1_1_1_1_1;
        }
        ushort retval_0_1_1_1_1_1_1_1 = Convert.ToUInt16((Convert.ToUInt16(Convert.ToInt32(Convert.ToUInt64(10247887076011802624UL) - Convert.ToUInt64(arr1d_0_1_1_1_1_1_1_1[0])) / val_0_1_1_1_1_1_1_1_1)));
        return retval_0_1_1_1_1_1_1_1;
    }

    public static long Func_0_1_1_1_1_1_1()
    {
        short* a2_0_1_1_1_1_1_1 = stackalloc short[1];
        *a2_0_1_1_1_1_1_1 = 4375;

        s_arr2d_0_1_1_1_1_1_1[2, 0] = -8828430758416264124L;
        ushort val_0_1_1_1_1_1_1_1 = Func_0_1_1_1_1_1_1_1();
        if ((s_arr2d_0_1_1_1_1_1_1[2, 0]) > (Convert.ToInt64(Convert.ToInt16(((*a2_0_1_1_1_1_1_1))) + Convert.ToInt64(s_arr2d_0_1_1_1_1_1_1[2, 0]))))
            Console.WriteLine("Func_0_1_1_1_1_1_1: > true");
        long retval_0_1_1_1_1_1_1 = Convert.ToInt64(Convert.ToInt64(Convert.ToUInt16(val_0_1_1_1_1_1_1_1) + Convert.ToInt64(Convert.ToInt64(Convert.ToInt16(((*a2_0_1_1_1_1_1_1))) + Convert.ToInt64(s_arr2d_0_1_1_1_1_1_1[2, 0])))));
        return retval_0_1_1_1_1_1_1;
    }

    public static long Func_0_1_1_1_1_1()
    {
        VT_0_1_1_1_1_1 vt_0_1_1_1_1_1 = new VT_0_1_1_1_1_1(1);
        vt_0_1_1_1_1_1.a1_0_1_1_1_1_1 = 1977572086;
        float[] arr1d_0_1_1_1_1_1 = new float[9];

        arr1d_0_1_1_1_1_1[0] = 0.5699924F;
        s_arr2d_0_1_1_1_1_1[2, 2] = 708678031;
        long val_0_1_1_1_1_1_1 = Func_0_1_1_1_1_1_1();
        int asgop0 = vt_0_1_1_1_1_1.a1_0_1_1_1_1_1;
        asgop0 %= (Convert.ToInt32((Convert.ToInt32((Convert.ToInt32(((Convert.ToInt32(64462) + 1977507624) - 1268894055)))))));
        if ((arr1d_0_1_1_1_1_1[0]) <= 10)
        {
            if ((arr1d_0_1_1_1_1_1[0]) == 10)
            {
                long if1_0retval_0_1_1_1_1_1 = Convert.ToInt64(Convert.ToInt64(Convert.ToUInt32(Convert.ToUInt32(asgop0 / Convert.ToSingle(arr1d_0_1_1_1_1_1[0]))) - Convert.ToInt64(Convert.ToInt64(Convert.ToInt32(s_arr2d_0_1_1_1_1_1[2, 2]) + Convert.ToInt64(val_0_1_1_1_1_1_1)))));
                return if1_0retval_0_1_1_1_1_1;
            }
        }
        else
        {
            long else0_0retval_0_1_1_1_1_1 = Convert.ToInt64(Convert.ToInt64(Convert.ToUInt32(Convert.ToUInt32(asgop0 / Convert.ToSingle(arr1d_0_1_1_1_1_1[0]))) - Convert.ToInt64(Convert.ToInt64(Convert.ToInt32(s_arr2d_0_1_1_1_1_1[2, 2]) + Convert.ToInt64(val_0_1_1_1_1_1_1)))));
            return else0_0retval_0_1_1_1_1_1;
        }
        long retval_0_1_1_1_1_1 = Convert.ToInt64(Convert.ToInt64(Convert.ToUInt32(Convert.ToUInt32(asgop0 / Convert.ToSingle(arr1d_0_1_1_1_1_1[0]))) - Convert.ToInt64(Convert.ToInt64(Convert.ToInt32(s_arr2d_0_1_1_1_1_1[2, 2]) + Convert.ToInt64(val_0_1_1_1_1_1_1)))));
        return retval_0_1_1_1_1_1;
    }

    public static long Func_0_1_1_1_1()
    {
        VT_0_1_1_1_1 vt_0_1_1_1_1 = new VT_0_1_1_1_1(1);
        vt_0_1_1_1_1.a1_0_1_1_1_1 = 2399073536U;
        double* a2_0_1_1_1_1 = stackalloc double[1];
        *a2_0_1_1_1_1 = 1.0000000000002376;

        s_arr3d_0_1_1_1_1[4, 0, 3] = -2399073472L;
        long val_0_1_1_1_1_1 = Func_0_1_1_1_1_1();
        if ((Convert.ToInt64(Convert.ToDouble(8828430758690422784L) * ((*a2_0_1_1_1_1)))) < (8828430758690422784L))
        {
            return Convert.ToInt64((Convert.ToInt64((Convert.ToInt64(Convert.ToDouble(8828430758690422784L) * ((*a2_0_1_1_1_1))) - val_0_1_1_1_1_1) / (Convert.ToInt64((Convert.ToInt64(vt_0_1_1_1_1.a1_0_1_1_1_1) + s_arr3d_0_1_1_1_1[4, 0, 3]) / (Convert.ToInt64(Convert.ToInt64(Convert.ToDouble(s_arr3d_0_1_1_1_1[4, 0, 3]) * -8.3365516869005671E-10)) * Convert.ToInt64((Convert.ToInt64(s_arr3d_0_1_1_1_1[4, 0, 3] / s_arr3d_0_1_1_1_1[4, 0, 3])))))))));
        }
        return Convert.ToInt64((Convert.ToInt64((Convert.ToInt64(Convert.ToDouble(8828430758690422784L) * ((*a2_0_1_1_1_1))) - val_0_1_1_1_1_1) / (Convert.ToInt64((Convert.ToInt64(vt_0_1_1_1_1.a1_0_1_1_1_1) + s_arr3d_0_1_1_1_1[4, 0, 3]) / (Convert.ToInt64(Convert.ToInt64(Convert.ToDouble(s_arr3d_0_1_1_1_1[4, 0, 3]) * -8.3365516869005671E-10)) * Convert.ToInt64((Convert.ToInt64(s_arr3d_0_1_1_1_1[4, 0, 3] / s_arr3d_0_1_1_1_1[4, 0, 3])))))))));
    }

    public static short Func_0_1_1_1()
    {
        s_arr1d_0_1_1_1[0] = -4.0;
        long val_0_1_1_1_1 = Func_0_1_1_1_1();
        double asgop0 = -1.798828125;
        asgop0 -= ((-5.798828125));
        double asgop1 = -5.798828125;
        asgop1 -= ((s_arr1d_0_1_1_1[0]));
        return Convert.ToInt16((Convert.ToInt16((val_0_1_1_1_1 / asgop0) - ((Convert.ToDouble(8192UL * asgop1))))));
    }

    public static ushort Func_0_1_1()
    {
        double[,,] arr3d_0_1_1 = new double[5, 9, 4];

        vtstatic_0_1_1.arr1d_0_1_1[1] = 1335221039;
        arr3d_0_1_1[4, 0, 3] = 5.339914645886728E-10;
        arr3d_0_1_1[4, 2, 3] = 1.2023524862987123;
        short val_0_1_1_1 = Func_0_1_1_1();
        if ((arr3d_0_1_1[4, 0, 3]) >= (arr3d_0_1_1[4, 2, 3]))
        {
            if ((val_0_1_1_1) >= (Convert.ToInt16((Convert.ToInt16(val_0_1_1_1)) % (Convert.ToInt16(15783)))))
            {
                return Convert.ToUInt16(Convert.ToUInt16(Convert.ToInt16((Convert.ToInt16(val_0_1_1_1)) % (Convert.ToInt16(15783))) * Convert.ToSingle(Convert.ToSingle(Convert.ToUInt32(vtstatic_0_1_1.arr1d_0_1_1[1] * arr3d_0_1_1[4, 2, 3]) * arr3d_0_1_1[4, 0, 3]))));
            }
            else
            {
                if ((val_0_1_1_1) > (Convert.ToInt16((Convert.ToInt16(val_0_1_1_1)) % (Convert.ToInt16(15783)))))
                    Console.WriteLine("Func_0_1_1: > true");
                else
                {
                    return Convert.ToUInt16(Convert.ToUInt16(Convert.ToInt16((Convert.ToInt16(val_0_1_1_1)) % (Convert.ToInt16(15783))) * Convert.ToSingle(Convert.ToSingle(Convert.ToUInt32(vtstatic_0_1_1.arr1d_0_1_1[1] * arr3d_0_1_1[4, 2, 3]) * arr3d_0_1_1[4, 0, 3]))));
                }
            }
        }
        return Convert.ToUInt16(Convert.ToUInt16(Convert.ToInt16((Convert.ToInt16(val_0_1_1_1)) % (Convert.ToInt16(15783))) * Convert.ToSingle(Convert.ToSingle(Convert.ToUInt32(vtstatic_0_1_1.arr1d_0_1_1[1] * arr3d_0_1_1[4, 2, 3]) * arr3d_0_1_1[4, 0, 3]))));
    }

    public static int Func_0_1()
    {
        CL_0_1 cl_0_1 = new CL_0_1();
        long[,] arr2d_0_1 = new long[3, 9];

        vtstatic_0_1.a1_0_1 = 2111178723;
        arr2d_0_1[2, 0] = 3029300220748914301L;
        ushort val_0_1_1 = Func_0_1_1();
        int asgop0 = vtstatic_0_1.a1_0_1;
        asgop0 += ((Convert.ToInt32(Convert.ToInt64(3029300219813560320L) - Convert.ToInt64(arr2d_0_1[2, 0]))));
        return Convert.ToInt32((Convert.ToInt32((Convert.ToInt32((Convert.ToInt32(val_0_1_1) + cl_0_1.a2_0_1))) % (Convert.ToInt32(asgop0)))));
    }

    public static int Func_0()
    {
        s_arr3d_0[4, 0, 3] = 112.75552824827484409018782981M;
        int val_0_1 = Func_0_1();
        int retval_0 = Convert.ToInt32(Convert.ToInt32(Convert.ToDecimal((Convert.ToInt32(val_0_1 * (s_a2_0 / 41477.078575715459)))) / (Convert.ToDecimal(Convert.ToInt64(Convert.ToDouble(s_a1_0) * 0.00390625) * (Convert.ToDecimal(s_a2_0) * s_arr3d_0[4, 0, 3])))));
        return retval_0;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        s_arr3d_0[4, 0, 3] = 112.75552824827484409018782981M;

        int retval;
        retval = Convert.ToInt32(Func_0());
        if ((retval >= 99) && (retval < 100))
            retval = 100;
        if ((retval > 100) && (retval <= 101))
            retval = 100;
        Console.WriteLine(retval);
        return retval;
    }
}
