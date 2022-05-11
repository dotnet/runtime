// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using Xunit;
namespace Test_200w1d_02
{
public unsafe class testout1
{
    public struct VT_0
    {
        public double a0_0;
        public double a2_0;
        public double a11_0;
        public double a21_0;
        public long a25_0;
        public long a31_0;
        public long a38_0;
        public short a70_0;
        public ushort a74_0;
        public VT_0(int i)
        {
            a0_0 = 1;
            a2_0 = 1;
            a11_0 = 1;
            a21_0 = 1;
            a25_0 = 1;
            a31_0 = 1;
            a38_0 = 1;
            a70_0 = 1;
            a74_0 = 1;
        }
    }
    public class CL_0
    {
        public float a6_0 = -11373.0F;
        public uint a17_0 = 1746910336U;
        public ulong a26_0 = 8UL;
        public long a27_0 = -70368744147702L;
        public int[] arr1d_0 = new int[201];
        public double a40_0 = -43182.324443081954;
        public long a48_0 = -37004L;
        public double a50_0 = -2109710489.3686249;
        public int[,,] arr3d_0 = new int[5, 201, 4];
        public double a57_0 = -1.5997536627634779E-17;
        public long a68_0 = -4000615939668494203L;
        public double a88_0 = -9.3946168668667283E-15;
        public long a89_0 = 66676L;
        public double a94_0 = 0.015625;
        public long a96_0 = 4L;
        public long a98_0 = 1746910335L;
    }
    private static double s_a8_0 = 156520.0;
    private static int[,] s_arr2d_0 = new int[3, 201];
    private static long s_a16_0 = -4000615937921583867L;
    private static int s_a18_0 = 1;
    private static long s_a42_0 = 29672L;
    private static long s_a61_0 = 16L;
    private static double s_a66_0 = -272396.4479494674;
    private static Decimal s_a75_0 = 1.3122549019607875M;
    private static double s_a83_0 = 156519.0;
    private static long s_a86_0 = -4000615938918935246L;
    private static Decimal s_a91_0 = -137438953472M;

    public static VT_0 vtstatic_0 = new VT_0(1);
    public static CL_0 clstatic_0 = new CL_0();

    public static int Func_0()
    {
        Decimal* a36_0 = stackalloc Decimal[1];
        *a36_0 = 137438953472M;

        vtstatic_0.a0_0 = 3.7914999471280252E-09;
        vtstatic_0.a2_0 = 5.0980403367171836E-10;
        vtstatic_0.a11_0 = 43182.316630581954;
        vtstatic_0.a21_0 = 0.03125;
        vtstatic_0.a25_0 = 2228713468L;
        vtstatic_0.a31_0 = 246082L;
        vtstatic_0.a38_0 = 37004L;
        vtstatic_0.a70_0 = 7745;
        vtstatic_0.a74_0 = 37005;
        s_arr2d_0[2, 10] = 1267746681;
        clstatic_0.arr1d_0[28] = 54765;
        clstatic_0.arr3d_0[4, 54, 3] = -1267746680;
        double asgop0 = 8.3079175512520556E-13;
        asgop0 += ((8.3079175512520556E-13 + -43182.324443081954));
        Decimal asgop1 = s_a91_0;
        asgop1 -= (Convert.ToDecimal(Convert.ToDecimal((Convert.ToDecimal(clstatic_0.a26_0) * (Convert.ToDecimal((Convert.ToDecimal(s_a91_0) + Convert.ToDecimal(0M))) - Convert.ToDecimal((Convert.ToDecimal(clstatic_0.a17_0) * -78.675448098098607849784913059M)))))));
        long asgop2 = s_a16_0;
        asgop2 /= (Convert.ToInt64((s_a16_0 + 3938106313891559120L)));
        double asgop3 = clstatic_0.a57_0;
        asgop3 -= (((clstatic_0.a57_0 + clstatic_0.a40_0)));
        double asgop4 = -0.00384521484375;
        asgop4 -= (156519.99615478516);
        double asgop5 = clstatic_0.a94_0;
        asgop5 -= (((clstatic_0.a94_0 - (clstatic_0.a94_0 - ((1 * -0.14578177034854889))))));
        short asgop6 = 16062;
        asgop6 /= (1);
        double asgop7 = vtstatic_0.a21_0;
        asgop7 -= (43182.324443081954);
        Decimal asgop8 = 10.4980392156863M;
        asgop8 -= (Convert.ToDecimal(Convert.ToDecimal((Convert.ToDecimal(10.4980392156863M) - Convert.ToDecimal(37.0933021099920M)))));
        float asgop9 = 1.0F;
        asgop9 += (8191.0F);
        Decimal asgop10 = s_a91_0;
        asgop10 += (Convert.ToDecimal(Convert.ToDecimal(0M)));
        asgop7 += (vtstatic_0.a11_0);
        Decimal asgop12 = s_a91_0;
        asgop12 -= (Convert.ToDecimal(Convert.ToDecimal(0M)));
        asgop12 += (Convert.ToDecimal(Convert.ToDecimal((Convert.ToDecimal(asgop10) - Convert.ToDecimal(-270597565911.875M)))));
        long asgop14 = vtstatic_0.a31_0;
        asgop14 /= (Convert.ToInt64(vtstatic_0.a31_0));
        long asgop15 = 1L;
        asgop15 /= (Convert.ToInt64(1L));
        int asgop16 = s_arr2d_0[2, 10];
        asgop16 *= ((Convert.ToInt32(vtstatic_0.a74_0) + (Convert.ToInt32(s_arr2d_0[2, 10] * -2.91887965905075E-05))));
        long asgop17 = 1L;
        asgop17 -= ((Convert.ToInt64(Convert.ToUInt16(vtstatic_0.a74_0) - Convert.ToInt64(clstatic_0.a89_0))));
        long asgop18 = 64L;
        asgop18 /= (Convert.ToInt64(1L));
        double asgop19 = -0.00384521484375;
        asgop19 -= (((-0.00384521484375 + asgop4)));
        long asgop20 = clstatic_0.a27_0;
        asgop20 *= (Convert.ToInt64(Convert.ToInt64(1L)));
        (*a36_0) -= (Convert.ToDecimal((Convert.ToDecimal(asgop1))));
        long asgop22 = clstatic_0.a96_0;
        asgop22 *= (Convert.ToInt64(Convert.ToInt64(Convert.ToInt64(Convert.ToUInt16(vtstatic_0.a74_0) + Convert.ToInt64(clstatic_0.a48_0)))));
        asgop3 += ((((asgop9 * 1.0F) * -5.2712798392434026) + vtstatic_0.a21_0));
        double asgop24 = -0.14578177034854889;
        asgop24 /= (1.0);
        long asgop25 = vtstatic_0.a31_0;
        asgop25 *= (Convert.ToInt64(Convert.ToInt64((Convert.ToInt64((Convert.ToInt64(vtstatic_0.a31_0) * Convert.ToInt64(1L)) / (Convert.ToInt64(vtstatic_0.a31_0 / asgop14)))))));
        int asgop26 = s_arr2d_0[2, 10];
        asgop26 /= ((Convert.ToInt32((Convert.ToInt32((Convert.ToInt32(Convert.ToInt64(vtstatic_0.a31_0) - Convert.ToInt64((-1625069246L)))))) % (Convert.ToInt32((Convert.ToInt32(s_arr2d_0[2, 10] * 1.2820505479201487)))))));
        int asgop27 = s_a18_0;
        asgop27 -= (clstatic_0.arr1d_0[28]);
        asgop3 -= (asgop7);
        asgop3 += ((((vtstatic_0.a70_0 - (((vtstatic_0.a70_0 - 62509) - 997234106))) / (vtstatic_0.a70_0 * clstatic_0.a50_0)) + ((vtstatic_0.a70_0 * s_a66_0) + ((s_a66_0 - 0.0) + (clstatic_0.a26_0 / vtstatic_0.a0_0)))));
        asgop3 += ((Convert.ToSingle(Convert.ToInt16(8192.0F - (clstatic_0.a6_0)) / asgop19) / (((s_a8_0 - s_a83_0) - ((s_a83_0 - 0.0) - 156518.00390625)) - ((Convert.ToUInt64(Convert.ToUInt16(Convert.ToUInt16(vtstatic_0.a70_0 * Convert.ToSingle(4.777921F))) + Convert.ToInt64(Convert.ToInt64(Convert.ToDouble(3938106313891559120L) * clstatic_0.a88_0))) / (clstatic_0.a88_0 - 74.645457766197779))))));
        short asgop31 = 32537;
        asgop31 %= Convert.ToInt16((Convert.ToInt16((Convert.ToInt16(16475)))));
        double asgop32 = -2517.6143380671915;
        asgop32 += (2517.6768380671915);
        asgop24 /= (vtstatic_0.a2_0);
        int asgop34 = s_arr2d_0[2, 10];
        asgop34 += (clstatic_0.arr3d_0[4, 54, 3]);
        double asgop35 = 8.3079175512520556E-13;
        asgop35 -= ((asgop0));
        return Convert.ToInt32((Convert.ToInt32((Convert.ToInt32((Convert.ToInt32(Convert.ToInt64(Convert.ToInt64(Convert.ToDouble(Convert.ToInt64(Convert.ToUInt16(Convert.ToUInt16(asgop26 / Convert.ToSingle(Convert.ToSingle(Convert.ToUInt64(Convert.ToInt16(asgop6) + Convert.ToInt64(Convert.ToInt64(Convert.ToUInt16(vtstatic_0.a74_0) - Convert.ToInt64((Convert.ToInt64(clstatic_0.a17_0) + -1747119413L))))) * asgop5)))) - Convert.ToInt64(Convert.ToInt64(Convert.ToDouble(asgop25) * asgop24)))) * (Convert.ToDouble((Convert.ToInt64((Convert.ToInt64(Convert.ToInt64(Convert.ToDouble(Convert.ToInt64(Convert.ToDouble(clstatic_0.a27_0) * -1.1920928960153885E-07)) * (Convert.ToInt16(1 * s_a18_0) * (clstatic_0.a27_0 / -4503599625452928.0)))) * Convert.ToInt64(Convert.ToInt64(Convert.ToDouble(asgop20) / (Convert.ToUInt32(vtstatic_0.a74_0 + 1746873331) * -2517.6143380671915))))) * Convert.ToInt64((Convert.ToInt64((Convert.ToInt64(Convert.ToInt64(Convert.ToDouble(s_a61_0) * asgop32)) * Convert.ToInt64(64L)) / (Convert.ToInt64((Convert.ToInt64((asgop18 + 0L) / asgop15)) / clstatic_0.a96_0)))))) * (Convert.ToUInt32(Convert.ToUInt32(Convert.ToInt32(s_a18_0 / 7.8880111854144145E-10) / Convert.ToDouble(Convert.ToDecimal(10.4980392156863M) / Convert.ToDecimal(35.3437719552881165816490907M))) % Convert.ToUInt32(Convert.ToInt64(asgop22) + Convert.ToInt64(vtstatic_0.a25_0))) * (Convert.ToDouble((Convert.ToInt64(Convert.ToInt64(Convert.ToDouble(vtstatic_0.a25_0) * 1.7947574048581106E-09)) * Convert.ToInt64(Convert.ToInt64(Convert.ToDouble(vtstatic_0.a25_0) * 4.4868935121452766E-10))) * 8.3079175512520556E-13))))))) + Convert.ToInt64(Convert.ToInt64(Convert.ToUInt16((Convert.ToUInt16(Convert.ToUInt16(Convert.ToInt16(Convert.ToDecimal(asgop31) / (Convert.ToDecimal(clstatic_0.a26_0) * s_a75_0)) + Convert.ToInt16(Convert.ToInt64(Convert.ToInt64(clstatic_0.a26_0 - 7UL)) + Convert.ToInt64(asgop17))) % (Convert.ToUInt16(asgop16 / asgop35))))) - Convert.ToInt64(Convert.ToInt64(Convert.ToUInt16(Convert.ToUInt16(Convert.ToInt16(Convert.ToInt16(16475 / s_a18_0) * Convert.ToSingle(0.4701062F)) * Convert.ToSingle(Convert.ToSingle(vtstatic_0.a70_0 * (s_a42_0 / 223377976.25057024))))) - Convert.ToInt64((Convert.ToInt64(Convert.ToInt64(Convert.ToUInt16(Convert.ToUInt16(0.00013283314898831372 + (0.00013283314898831372 + (0.00013283314898831372 + 37004.999601500553)))) - Convert.ToInt64(vtstatic_0.a38_0))) * Convert.ToInt64((Convert.ToInt64(Convert.ToInt64(Convert.ToUInt32(clstatic_0.a17_0) - Convert.ToInt64(clstatic_0.a98_0))) * Convert.ToInt64((Convert.ToInt64(clstatic_0.a17_0) + clstatic_0.a68_0)))))))))))))) % (Convert.ToInt32(Convert.ToInt32((Convert.ToInt32((Convert.ToInt32(Convert.ToInt32(Convert.ToDecimal((Convert.ToInt32((Convert.ToUInt16((Convert.ToUInt16(Convert.ToInt32(asgop34) - Convert.ToInt32((asgop27)))) % vtstatic_0.a74_0))) + (Convert.ToInt32(Convert.ToInt64(s_a16_0) + Convert.ToInt64(Convert.ToInt64(Convert.ToInt32((clstatic_0.arr1d_0[28] + -109529)) - Convert.ToInt64(s_a86_0))))))) * (Convert.ToDecimal(Convert.ToInt64(Convert.ToDouble(s_a16_0) * clstatic_0.a57_0)) / asgop8)))) % (Convert.ToInt32(Convert.ToInt32((*a36_0) + (Convert.ToDecimal(asgop2 * asgop12))))))) / asgop3))))));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        vtstatic_0.a0_0 = 3.7914999471280252E-09;
        vtstatic_0.a2_0 = 5.0980403367171836E-10;
        vtstatic_0.a11_0 = 43182.316630581954;
        vtstatic_0.a21_0 = 0.03125;
        vtstatic_0.a25_0 = 2228713468L;
        vtstatic_0.a31_0 = 246082L;
        vtstatic_0.a38_0 = 37004L;
        vtstatic_0.a70_0 = 7745;
        vtstatic_0.a74_0 = 37005;
        s_arr2d_0[2, 10] = 1267746681;
        clstatic_0.arr1d_0[28] = 54765;
        clstatic_0.arr3d_0[4, 54, 3] = -1267746680;

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
}
