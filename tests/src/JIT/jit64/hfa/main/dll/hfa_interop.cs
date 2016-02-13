// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace HFATest
{
    public struct Config
    {
#if NESTED_HFA
        public const string hfaType = "nested";
#else
        public const string hfaType = "simple";
#endif

#if NATIVE_IJW
	public const string dllType = "native_ijw";
#else
        public const string dllType = "native_cpp";
#endif

#if FLOAT64
        public const string floatType = "f64";
#else
        public const string floatType = "f32";
#endif

        public const string DllName = "hfa" + "_" + hfaType + "_" + floatType + "_" + dllType;
    }


    public class TestMan
    {
        //---------------------------------------------
        // Init Methods
        // ---------------------------------------------------

        [DllImport(Config.DllName, EntryPoint = "init_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern void Init_HFA01(out HFA01 hfa);

        [DllImport(Config.DllName, EntryPoint = "init_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern void Init_HFA02(out HFA02 hfa);

        [DllImport(Config.DllName, EntryPoint = "init_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern void Init_HFA03(out HFA03 hfa);

        [DllImport(Config.DllName, EntryPoint = "init_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern void Init_HFA05(out HFA05 hfa);

        [DllImport(Config.DllName, EntryPoint = "init_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern void Init_HFA08(out HFA08 hfa);

        [DllImport(Config.DllName, EntryPoint = "init_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern void Init_HFA11(out HFA11 hfa);

        [DllImport(Config.DllName, EntryPoint = "init_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern void Init_HFA19(out HFA19 hfa);



        // ---------------------------------------------------
        // Identity Methods
        // ---------------------------------------------------

        [DllImport(Config.DllName, EntryPoint = "identity_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern HFA01 Identity_HFA01(HFA01 hfa);

        [DllImport(Config.DllName, EntryPoint = "identity_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern HFA02 Identity_HFA02(HFA02 hfa);

        [DllImport(Config.DllName, EntryPoint = "identity_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern HFA03 Identity_HFA03(HFA03 hfa);

        [DllImport(Config.DllName, EntryPoint = "identity_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern HFA05 Identity_HFA05(HFA05 hfa);

        [DllImport(Config.DllName, EntryPoint = "identity_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern HFA08 Identity_HFA08(HFA08 hfa);

        [DllImport(Config.DllName, EntryPoint = "identity_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern HFA11 Identity_HFA11(HFA11 hfa);

        [DllImport(Config.DllName, EntryPoint = "identity_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern HFA19 Identity_HFA19(HFA19 hfa);



        // ---------------------------------------------------
        // Get Methods
        // ---------------------------------------------------

        [DllImport(Config.DllName, EntryPoint = "get_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern HFA01 Get_HFA01();

        [DllImport(Config.DllName, EntryPoint = "get_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern HFA02 Get_HFA02();

        [DllImport(Config.DllName, EntryPoint = "get_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern HFA03 Get_HFA03();

        [DllImport(Config.DllName, EntryPoint = "get_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern HFA05 Get_HFA05();

        [DllImport(Config.DllName, EntryPoint = "get_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern HFA08 Get_HFA08();

        [DllImport(Config.DllName, EntryPoint = "get_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern HFA11 Get_HFA11();

        [DllImport(Config.DllName, EntryPoint = "get_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern HFA19 Get_HFA19();



#if FLOAT64

        public static readonly double EXPECTED_SUM_HFA01 = Get_EXPECTED_SUM_HFA01();
        public static readonly double EXPECTED_SUM_HFA02 = Get_EXPECTED_SUM_HFA02();
        public static readonly double EXPECTED_SUM_HFA03 = Get_EXPECTED_SUM_HFA03();
        public static readonly double EXPECTED_SUM_HFA05 = Get_EXPECTED_SUM_HFA05();
        public static readonly double EXPECTED_SUM_HFA08 = Get_EXPECTED_SUM_HFA08();
        public static readonly double EXPECTED_SUM_HFA11 = Get_EXPECTED_SUM_HFA11();
        public static readonly double EXPECTED_SUM_HFA19 = Get_EXPECTED_SUM_HFA19();

        [DllImport(Config.DllName, EntryPoint = "get_EXPECTED_SUM_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern double Get_EXPECTED_SUM_HFA01();

        [DllImport(Config.DllName, EntryPoint = "get_EXPECTED_SUM_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern double Get_EXPECTED_SUM_HFA02();

        [DllImport(Config.DllName, EntryPoint = "get_EXPECTED_SUM_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern double Get_EXPECTED_SUM_HFA03();

        [DllImport(Config.DllName, EntryPoint = "get_EXPECTED_SUM_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern double Get_EXPECTED_SUM_HFA05();

        [DllImport(Config.DllName, EntryPoint = "get_EXPECTED_SUM_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern double Get_EXPECTED_SUM_HFA08();

        [DllImport(Config.DllName, EntryPoint = "get_EXPECTED_SUM_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern double Get_EXPECTED_SUM_HFA11();

        [DllImport(Config.DllName, EntryPoint = "get_EXPECTED_SUM_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern double Get_EXPECTED_SUM_HFA19();


        [DllImport(Config.DllName, EntryPoint = "sum_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum_HFA01(HFA01 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum_HFA02(HFA02 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum_HFA03(HFA03 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum_HFA05(HFA05 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum_HFA08(HFA08 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum_HFA11(HFA11 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum_HFA19(HFA19 hfa);


        [DllImport(Config.DllName, EntryPoint = "sum3_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum3_HFA01(float v1, long v2, HFA01 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum3_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum3_HFA02(float v1, long v2, HFA02 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum3_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum3_HFA03(float v1, long v2, HFA03 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum3_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum3_HFA05(float v1, long v2, HFA05 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum3_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum3_HFA08(float v1, long v2, HFA08 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum3_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum3_HFA11(float v1, long v2, HFA11 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum3_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum3_HFA19(float v1, long v2, HFA19 hfa);


        [DllImport(Config.DllName, EntryPoint = "sum5_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum5_HFA01(long v1, double v2, int v3, sbyte v4, HFA01 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum5_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum5_HFA02(long v1, double v2, int v3, sbyte v4, HFA02 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum5_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum5_HFA03(long v1, double v2, int v3, sbyte v4, HFA03 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum5_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum5_HFA05(long v1, double v2, int v3, sbyte v4, HFA05 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum5_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum5_HFA08(long v1, double v2, int v3, sbyte v4, HFA08 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum5_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum5_HFA11(long v1, double v2, int v3, sbyte v4, HFA11 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum5_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum5_HFA19(long v1, double v2, int v3, sbyte v4, HFA19 hfa);


        [DllImport(Config.DllName, EntryPoint = "sum8_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum8_HFA01(float v1, double v2, long v3, sbyte v4, double v5, HFA01 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum8_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum8_HFA02(float v1, double v2, long v3, sbyte v4, double v5, HFA02 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum8_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum8_HFA03(float v1, double v2, long v3, sbyte v4, double v5, HFA03 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum8_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum8_HFA05(float v1, double v2, long v3, sbyte v4, double v5, HFA05 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum8_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum8_HFA08(float v1, double v2, long v3, sbyte v4, double v5, HFA08 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum8_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum8_HFA11(float v1, double v2, long v3, sbyte v4, double v5, HFA11 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum8_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum8_HFA19(float v1, double v2, long v3, sbyte v4, double v5, HFA19 hfa);


        [DllImport(Config.DllName, EntryPoint = "sum11_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum11_HFA01(double v1, float v2, float v3, int v4, float v5, long v6, double v7, float v8, HFA01 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum11_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum11_HFA02(double v1, float v2, float v3, int v4, float v5, long v6, double v7, float v8, HFA02 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum11_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum11_HFA03(double v1, float v2, float v3, int v4, float v5, long v6, double v7, float v8, HFA03 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum11_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum11_HFA05(double v1, float v2, float v3, int v4, float v5, long v6, double v7, float v8, HFA05 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum11_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum11_HFA08(double v1, float v2, float v3, int v4, float v5, long v6, double v7, float v8, HFA08 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum11_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum11_HFA11(double v1, float v2, float v3, int v4, float v5, long v6, double v7, float v8, HFA11 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum11_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum11_HFA19(double v1, float v2, float v3, int v4, float v5, long v6, double v7, float v8, HFA19 hfa);


        [DllImport(Config.DllName, EntryPoint = "sum19_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum19_HFA01(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA01 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum19_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum19_HFA02(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA02 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum19_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum19_HFA03(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA03 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum19_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum19_HFA05(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA05 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum19_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum19_HFA08(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA08 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum19_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum19_HFA11(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA11 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum19_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern double Sum19_HFA19(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA19 hfa);



        // ---------------------------------------------------
        // Average Methods
        // ---------------------------------------------------


        [DllImport(Config.DllName, EntryPoint = "average_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average_HFA01(HFA01 hfa);

        [DllImport(Config.DllName, EntryPoint = "average_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average_HFA02(HFA02 hfa);

        [DllImport(Config.DllName, EntryPoint = "average_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average_HFA03(HFA03 hfa);

        [DllImport(Config.DllName, EntryPoint = "average_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average_HFA05(HFA05 hfa);

        [DllImport(Config.DllName, EntryPoint = "average_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average_HFA08(HFA08 hfa);

        [DllImport(Config.DllName, EntryPoint = "average_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average_HFA11(HFA11 hfa);

        [DllImport(Config.DllName, EntryPoint = "average_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average_HFA19(HFA19 hfa);


        [DllImport(Config.DllName, EntryPoint = "average3_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average3_HFA01(HFA01 hfa1, HFA01 hfa2, HFA01 hfa3);

        [DllImport(Config.DllName, EntryPoint = "average3_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average3_HFA02(HFA02 hfa1, HFA02 hfa2, HFA02 hfa3);

        [DllImport(Config.DllName, EntryPoint = "average3_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average3_HFA03(HFA03 hfa1, HFA03 hfa2, HFA03 hfa3);

        [DllImport(Config.DllName, EntryPoint = "average3_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average3_HFA05(HFA05 hfa1, HFA05 hfa2, HFA05 hfa3);

        [DllImport(Config.DllName, EntryPoint = "average3_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average3_HFA08(HFA08 hfa1, HFA08 hfa2, HFA08 hfa3);

        [DllImport(Config.DllName, EntryPoint = "average3_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average3_HFA11(HFA11 hfa1, HFA11 hfa2, HFA11 hfa3);

        [DllImport(Config.DllName, EntryPoint = "average3_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average3_HFA19(HFA19 hfa1, HFA19 hfa2, HFA19 hfa3);


        [DllImport(Config.DllName, EntryPoint = "average5_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average5_HFA01(HFA01 hfa1, HFA01 hfa2, HFA01 hfa3, HFA01 hfa4, HFA01 hfa5);

        [DllImport(Config.DllName, EntryPoint = "average5_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average5_HFA02(HFA02 hfa1, HFA02 hfa2, HFA02 hfa3, HFA02 hfa4, HFA02 hfa5);

        [DllImport(Config.DllName, EntryPoint = "average5_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average5_HFA03(HFA03 hfa1, HFA03 hfa2, HFA03 hfa3, HFA03 hfa4, HFA03 hfa5);

        [DllImport(Config.DllName, EntryPoint = "average5_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average5_HFA05(HFA05 hfa1, HFA05 hfa2, HFA05 hfa3, HFA05 hfa4, HFA05 hfa5);

        [DllImport(Config.DllName, EntryPoint = "average5_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average5_HFA08(HFA08 hfa1, HFA08 hfa2, HFA08 hfa3, HFA08 hfa4, HFA08 hfa5);

        [DllImport(Config.DllName, EntryPoint = "average5_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average5_HFA11(HFA11 hfa1, HFA11 hfa2, HFA11 hfa3, HFA11 hfa4, HFA11 hfa5);

        [DllImport(Config.DllName, EntryPoint = "average5_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average5_HFA19(HFA19 hfa1, HFA19 hfa2, HFA19 hfa3, HFA19 hfa4, HFA19 hfa5);


        [DllImport(Config.DllName, EntryPoint = "average8_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average8_HFA01(HFA01 hfa1, HFA01 hfa2, HFA01 hfa3, HFA01 hfa4, HFA01 hfa5, HFA01 hfa6, HFA01 hfa7, HFA01 hfa8);

        [DllImport(Config.DllName, EntryPoint = "average8_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average8_HFA02(HFA02 hfa1, HFA02 hfa2, HFA02 hfa3, HFA02 hfa4, HFA02 hfa5, HFA02 hfa6, HFA02 hfa7, HFA02 hfa8);

        [DllImport(Config.DllName, EntryPoint = "average8_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average8_HFA03(HFA03 hfa1, HFA03 hfa2, HFA03 hfa3, HFA03 hfa4, HFA03 hfa5, HFA03 hfa6, HFA03 hfa7, HFA03 hfa8);

        [DllImport(Config.DllName, EntryPoint = "average8_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average8_HFA05(HFA05 hfa1, HFA05 hfa2, HFA05 hfa3, HFA05 hfa4, HFA05 hfa5, HFA05 hfa6, HFA05 hfa7, HFA05 hfa8);

        [DllImport(Config.DllName, EntryPoint = "average8_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average8_HFA08(HFA08 hfa1, HFA08 hfa2, HFA08 hfa3, HFA08 hfa4, HFA08 hfa5, HFA08 hfa6, HFA08 hfa7, HFA08 hfa8);

        [DllImport(Config.DllName, EntryPoint = "average8_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average8_HFA11(HFA11 hfa1, HFA11 hfa2, HFA11 hfa3, HFA11 hfa4, HFA11 hfa5, HFA11 hfa6, HFA11 hfa7, HFA11 hfa8);

        [DllImport(Config.DllName, EntryPoint = "average8_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average8_HFA19(HFA19 hfa1, HFA19 hfa2, HFA19 hfa3, HFA19 hfa4, HFA19 hfa5, HFA19 hfa6, HFA19 hfa7, HFA19 hfa8);


        [DllImport(Config.DllName, EntryPoint = "average11_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average11_HFA01(HFA01 hfa1, HFA01 hfa2, HFA01 hfa3, HFA01 hfa4, HFA01 hfa5, HFA01 hfa6, HFA01 hfa7, HFA01 hfa8, HFA01 hfa9, HFA01 hfa10, HFA01 hfa11);

        [DllImport(Config.DllName, EntryPoint = "average11_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average11_HFA02(HFA02 hfa1, HFA02 hfa2, HFA02 hfa3, HFA02 hfa4, HFA02 hfa5, HFA02 hfa6, HFA02 hfa7, HFA02 hfa8, HFA02 hfa9, HFA02 hfa10, HFA02 hfa11);

        [DllImport(Config.DllName, EntryPoint = "average11_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average11_HFA03(HFA03 hfa1, HFA03 hfa2, HFA03 hfa3, HFA03 hfa4, HFA03 hfa5, HFA03 hfa6, HFA03 hfa7, HFA03 hfa8, HFA03 hfa9, HFA03 hfa10, HFA03 hfa11);

        [DllImport(Config.DllName, EntryPoint = "average11_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average11_HFA05(HFA05 hfa1, HFA05 hfa2, HFA05 hfa3, HFA05 hfa4, HFA05 hfa5, HFA05 hfa6, HFA05 hfa7, HFA05 hfa8, HFA05 hfa9, HFA05 hfa10, HFA05 hfa11);

        [DllImport(Config.DllName, EntryPoint = "average11_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average11_HFA08(HFA08 hfa1, HFA08 hfa2, HFA08 hfa3, HFA08 hfa4, HFA08 hfa5, HFA08 hfa6, HFA08 hfa7, HFA08 hfa8, HFA08 hfa9, HFA08 hfa10, HFA08 hfa11);

        [DllImport(Config.DllName, EntryPoint = "average11_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average11_HFA11(HFA11 hfa1, HFA11 hfa2, HFA11 hfa3, HFA11 hfa4, HFA11 hfa5, HFA11 hfa6, HFA11 hfa7, HFA11 hfa8, HFA11 hfa9, HFA11 hfa10, HFA11 hfa11);

        [DllImport(Config.DllName, EntryPoint = "average11_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average11_HFA19(HFA19 hfa1, HFA19 hfa2, HFA19 hfa3, HFA19 hfa4, HFA19 hfa5, HFA19 hfa6, HFA19 hfa7, HFA19 hfa8, HFA19 hfa9, HFA19 hfa10, HFA19 hfa11);


        [DllImport(Config.DllName, EntryPoint = "average19_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average19_HFA01(HFA01 hfa1, HFA01 hfa2, HFA01 hfa3, HFA01 hfa4, HFA01 hfa5, HFA01 hfa6, HFA01 hfa7, HFA01 hfa8, HFA01 hfa9, HFA01 hfa10, HFA01 hfa11, HFA01 hfa12, HFA01 hfa13, HFA01 hfa14, HFA01 hfa15, HFA01 hfa16, HFA01 hfa17, HFA01 hfa18, HFA01 hfa19);

        [DllImport(Config.DllName, EntryPoint = "average19_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average19_HFA02(HFA02 hfa1, HFA02 hfa2, HFA02 hfa3, HFA02 hfa4, HFA02 hfa5, HFA02 hfa6, HFA02 hfa7, HFA02 hfa8, HFA02 hfa9, HFA02 hfa10, HFA02 hfa11, HFA02 hfa12, HFA02 hfa13, HFA02 hfa14, HFA02 hfa15, HFA02 hfa16, HFA02 hfa17, HFA02 hfa18, HFA02 hfa19);

        [DllImport(Config.DllName, EntryPoint = "average19_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average19_HFA03(HFA03 hfa1, HFA03 hfa2, HFA03 hfa3, HFA03 hfa4, HFA03 hfa5, HFA03 hfa6, HFA03 hfa7, HFA03 hfa8, HFA03 hfa9, HFA03 hfa10, HFA03 hfa11, HFA03 hfa12, HFA03 hfa13, HFA03 hfa14, HFA03 hfa15, HFA03 hfa16, HFA03 hfa17, HFA03 hfa18, HFA03 hfa19);

        [DllImport(Config.DllName, EntryPoint = "average19_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average19_HFA05(HFA05 hfa1, HFA05 hfa2, HFA05 hfa3, HFA05 hfa4, HFA05 hfa5, HFA05 hfa6, HFA05 hfa7, HFA05 hfa8, HFA05 hfa9, HFA05 hfa10, HFA05 hfa11, HFA05 hfa12, HFA05 hfa13, HFA05 hfa14, HFA05 hfa15, HFA05 hfa16, HFA05 hfa17, HFA05 hfa18, HFA05 hfa19);

        [DllImport(Config.DllName, EntryPoint = "average19_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average19_HFA08(HFA08 hfa1, HFA08 hfa2, HFA08 hfa3, HFA08 hfa4, HFA08 hfa5, HFA08 hfa6, HFA08 hfa7, HFA08 hfa8, HFA08 hfa9, HFA08 hfa10, HFA08 hfa11, HFA08 hfa12, HFA08 hfa13, HFA08 hfa14, HFA08 hfa15, HFA08 hfa16, HFA08 hfa17, HFA08 hfa18, HFA08 hfa19);

        [DllImport(Config.DllName, EntryPoint = "average19_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average19_HFA11(HFA11 hfa1, HFA11 hfa2, HFA11 hfa3, HFA11 hfa4, HFA11 hfa5, HFA11 hfa6, HFA11 hfa7, HFA11 hfa8, HFA11 hfa9, HFA11 hfa10, HFA11 hfa11, HFA11 hfa12, HFA11 hfa13, HFA11 hfa14, HFA11 hfa15, HFA11 hfa16, HFA11 hfa17, HFA11 hfa18, HFA11 hfa19);

        [DllImport(Config.DllName, EntryPoint = "average19_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern double Average19_HFA19(HFA19 hfa1, HFA19 hfa2, HFA19 hfa3, HFA19 hfa4, HFA19 hfa5, HFA19 hfa6, HFA19 hfa7, HFA19 hfa8, HFA19 hfa9, HFA19 hfa10, HFA19 hfa11, HFA19 hfa12, HFA19 hfa13, HFA19 hfa14, HFA19 hfa15, HFA19 hfa16, HFA19 hfa17, HFA19 hfa18, HFA19 hfa19);



        // ---------------------------------------------------
        // Add Methods
        // ---------------------------------------------------


        [DllImport(Config.DllName, EntryPoint = "add01_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern double Add01_HFA01(HFA01 hfa1, float v1, HFA01 hfa2, int v2, HFA01 hfa3, short v3, double v4, HFA01 hfa4, HFA01 hfa5, float v5, long v6, float v7, HFA01 hfa6, float v8, HFA01 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add01_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern double Add01_HFA02(HFA02 hfa1, float v1, HFA02 hfa2, int v2, HFA02 hfa3, short v3, double v4, HFA02 hfa4, HFA02 hfa5, float v5, long v6, float v7, HFA02 hfa6, float v8, HFA02 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add01_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern double Add01_HFA03(HFA03 hfa1, float v1, HFA03 hfa2, int v2, HFA03 hfa3, short v3, double v4, HFA03 hfa4, HFA03 hfa5, float v5, long v6, float v7, HFA03 hfa6, float v8, HFA03 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add01_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern double Add01_HFA05(HFA05 hfa1, float v1, HFA05 hfa2, int v2, HFA05 hfa3, short v3, double v4, HFA05 hfa4, HFA05 hfa5, float v5, long v6, float v7, HFA05 hfa6, float v8, HFA05 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add01_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern double Add01_HFA08(HFA08 hfa1, float v1, HFA08 hfa2, int v2, HFA08 hfa3, short v3, double v4, HFA08 hfa4, HFA08 hfa5, float v5, long v6, float v7, HFA08 hfa6, float v8, HFA08 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add01_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern double Add01_HFA11(HFA11 hfa1, float v1, HFA11 hfa2, int v2, HFA11 hfa3, short v3, double v4, HFA11 hfa4, HFA11 hfa5, float v5, long v6, float v7, HFA11 hfa6, float v8, HFA11 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add01_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern double Add01_HFA19(HFA19 hfa1, float v1, HFA19 hfa2, int v2, HFA19 hfa3, short v3, double v4, HFA19 hfa4, HFA19 hfa5, float v5, long v6, float v7, HFA19 hfa6, float v8, HFA19 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add01_HFA00", CallingConvention = CallingConvention.StdCall)]
        public static extern double Add01_HFA00(HFA03 hfa1, float v1, HFA02 hfa2, int v2, HFA19 hfa3, short v3, double v4, HFA05 hfa4, HFA08 hfa5, float v5, long v6, float v7, HFA11 hfa6, float v8, HFA01 hfa7);


        [DllImport(Config.DllName, EntryPoint = "add02_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern double Add02_HFA01(HFA01 hfa1, HFA01 hfa2, long v1, short v2, float v3, int v4, double v5, float v6, HFA01 hfa3, double v7, float v8, HFA01 hfa4, short v9, HFA01 hfa5, float v10, HFA01 hfa6, HFA01 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add02_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern double Add02_HFA02(HFA02 hfa1, HFA02 hfa2, long v1, short v2, float v3, int v4, double v5, float v6, HFA02 hfa3, double v7, float v8, HFA02 hfa4, short v9, HFA02 hfa5, float v10, HFA02 hfa6, HFA02 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add02_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern double Add02_HFA03(HFA03 hfa1, HFA03 hfa2, long v1, short v2, float v3, int v4, double v5, float v6, HFA03 hfa3, double v7, float v8, HFA03 hfa4, short v9, HFA03 hfa5, float v10, HFA03 hfa6, HFA03 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add02_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern double Add02_HFA05(HFA05 hfa1, HFA05 hfa2, long v1, short v2, float v3, int v4, double v5, float v6, HFA05 hfa3, double v7, float v8, HFA05 hfa4, short v9, HFA05 hfa5, float v10, HFA05 hfa6, HFA05 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add02_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern double Add02_HFA08(HFA08 hfa1, HFA08 hfa2, long v1, short v2, float v3, int v4, double v5, float v6, HFA08 hfa3, double v7, float v8, HFA08 hfa4, short v9, HFA08 hfa5, float v10, HFA08 hfa6, HFA08 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add02_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern double Add02_HFA11(HFA11 hfa1, HFA11 hfa2, long v1, short v2, float v3, int v4, double v5, float v6, HFA11 hfa3, double v7, float v8, HFA11 hfa4, short v9, HFA11 hfa5, float v10, HFA11 hfa6, HFA11 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add02_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern double Add02_HFA19(HFA19 hfa1, HFA19 hfa2, long v1, short v2, float v3, int v4, double v5, float v6, HFA19 hfa3, double v7, float v8, HFA19 hfa4, short v9, HFA19 hfa5, float v10, HFA19 hfa6, HFA19 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add02_HFA00", CallingConvention = CallingConvention.StdCall)]
        public static extern double Add02_HFA00(HFA01 hfa1, HFA05 hfa2, long v1, short v2, float v3, int v4, double v5, float v6, HFA03 hfa3, double v7, float v8, HFA11 hfa4, short v9, HFA19 hfa5, float v10, HFA08 hfa6, HFA02 hfa7);



        [DllImport(Config.DllName, EntryPoint = "add03_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern double Add03_HFA01(float v1, sbyte v2, HFA01 hfa1, double v3, sbyte v4, HFA01 hfa2, long v5, short v6, int v7, HFA01 hfa3, HFA01 hfa4, float v8, HFA01 hfa5, float v9, HFA01 hfa6, float v10, HFA01 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add03_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern double Add03_HFA02(float v1, sbyte v2, HFA02 hfa1, double v3, sbyte v4, HFA02 hfa2, long v5, short v6, int v7, HFA02 hfa3, HFA02 hfa4, float v8, HFA02 hfa5, float v9, HFA02 hfa6, float v10, HFA02 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add03_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern double Add03_HFA03(float v1, sbyte v2, HFA03 hfa1, double v3, sbyte v4, HFA03 hfa2, long v5, short v6, int v7, HFA03 hfa3, HFA03 hfa4, float v8, HFA03 hfa5, float v9, HFA03 hfa6, float v10, HFA03 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add03_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern double Add03_HFA05(float v1, sbyte v2, HFA05 hfa1, double v3, sbyte v4, HFA05 hfa2, long v5, short v6, int v7, HFA05 hfa3, HFA05 hfa4, float v8, HFA05 hfa5, float v9, HFA05 hfa6, float v10, HFA05 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add03_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern double Add03_HFA08(float v1, sbyte v2, HFA08 hfa1, double v3, sbyte v4, HFA08 hfa2, long v5, short v6, int v7, HFA08 hfa3, HFA08 hfa4, float v8, HFA08 hfa5, float v9, HFA08 hfa6, float v10, HFA08 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add03_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern double Add03_HFA11(float v1, sbyte v2, HFA11 hfa1, double v3, sbyte v4, HFA11 hfa2, long v5, short v6, int v7, HFA11 hfa3, HFA11 hfa4, float v8, HFA11 hfa5, float v9, HFA11 hfa6, float v10, HFA11 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add03_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern double Add03_HFA19(float v1, sbyte v2, HFA19 hfa1, double v3, sbyte v4, HFA19 hfa2, long v5, short v6, int v7, HFA19 hfa3, HFA19 hfa4, float v8, HFA19 hfa5, float v9, HFA19 hfa6, float v10, HFA19 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add03_HFA00", CallingConvention = CallingConvention.StdCall)]
        public static extern double Add03_HFA00(float v1, sbyte v2, HFA08 hfa1, double v3, sbyte v4, HFA19 hfa2, long v5, short v6, int v7, HFA03 hfa3, HFA01 hfa4, float v8, HFA11 hfa5, float v9, HFA02 hfa6, float v10, HFA05 hfa7);

#else // FLOAT64

        public static readonly float EXPECTED_SUM_HFA01 = Get_EXPECTED_SUM_HFA01();
        public static readonly float EXPECTED_SUM_HFA02 = Get_EXPECTED_SUM_HFA02();
        public static readonly float EXPECTED_SUM_HFA03 = Get_EXPECTED_SUM_HFA03();
        public static readonly float EXPECTED_SUM_HFA05 = Get_EXPECTED_SUM_HFA05();
        public static readonly float EXPECTED_SUM_HFA08 = Get_EXPECTED_SUM_HFA08();
        public static readonly float EXPECTED_SUM_HFA11 = Get_EXPECTED_SUM_HFA11();
        public static readonly float EXPECTED_SUM_HFA19 = Get_EXPECTED_SUM_HFA19();

        [DllImport(Config.DllName, EntryPoint = "get_EXPECTED_SUM_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern float Get_EXPECTED_SUM_HFA01();

        [DllImport(Config.DllName, EntryPoint = "get_EXPECTED_SUM_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern float Get_EXPECTED_SUM_HFA02();

        [DllImport(Config.DllName, EntryPoint = "get_EXPECTED_SUM_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern float Get_EXPECTED_SUM_HFA03();

        [DllImport(Config.DllName, EntryPoint = "get_EXPECTED_SUM_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern float Get_EXPECTED_SUM_HFA05();

        [DllImport(Config.DllName, EntryPoint = "get_EXPECTED_SUM_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern float Get_EXPECTED_SUM_HFA08();

        [DllImport(Config.DllName, EntryPoint = "get_EXPECTED_SUM_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern float Get_EXPECTED_SUM_HFA11();

        [DllImport(Config.DllName, EntryPoint = "get_EXPECTED_SUM_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern float Get_EXPECTED_SUM_HFA19();


        [DllImport(Config.DllName, EntryPoint = "sum_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum_HFA01(HFA01 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum_HFA02(HFA02 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum_HFA03(HFA03 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum_HFA05(HFA05 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum_HFA08(HFA08 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum_HFA11(HFA11 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum_HFA19(HFA19 hfa);


        [DllImport(Config.DllName, EntryPoint = "sum3_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum3_HFA01(float v1, long v2, HFA01 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum3_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum3_HFA02(float v1, long v2, HFA02 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum3_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum3_HFA03(float v1, long v2, HFA03 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum3_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum3_HFA05(float v1, long v2, HFA05 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum3_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum3_HFA08(float v1, long v2, HFA08 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum3_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum3_HFA11(float v1, long v2, HFA11 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum3_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum3_HFA19(float v1, long v2, HFA19 hfa);


        [DllImport(Config.DllName, EntryPoint = "sum5_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum5_HFA01(long v1, double v2, int v3, sbyte v4, HFA01 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum5_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum5_HFA02(long v1, double v2, int v3, sbyte v4, HFA02 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum5_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum5_HFA03(long v1, double v2, int v3, sbyte v4, HFA03 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum5_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum5_HFA05(long v1, double v2, int v3, sbyte v4, HFA05 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum5_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum5_HFA08(long v1, double v2, int v3, sbyte v4, HFA08 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum5_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum5_HFA11(long v1, double v2, int v3, sbyte v4, HFA11 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum5_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum5_HFA19(long v1, double v2, int v3, sbyte v4, HFA19 hfa);


        [DllImport(Config.DllName, EntryPoint = "sum8_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum8_HFA01(float v1, double v2, long v3, sbyte v4, double v5, HFA01 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum8_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum8_HFA02(float v1, double v2, long v3, sbyte v4, double v5, HFA02 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum8_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum8_HFA03(float v1, double v2, long v3, sbyte v4, double v5, HFA03 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum8_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum8_HFA05(float v1, double v2, long v3, sbyte v4, double v5, HFA05 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum8_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum8_HFA08(float v1, double v2, long v3, sbyte v4, double v5, HFA08 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum8_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum8_HFA11(float v1, double v2, long v3, sbyte v4, double v5, HFA11 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum8_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum8_HFA19(float v1, double v2, long v3, sbyte v4, double v5, HFA19 hfa);


        [DllImport(Config.DllName, EntryPoint = "sum11_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum11_HFA01(double v1, float v2, float v3, int v4, float v5, long v6, double v7, float v8, HFA01 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum11_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum11_HFA02(double v1, float v2, float v3, int v4, float v5, long v6, double v7, float v8, HFA02 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum11_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum11_HFA03(double v1, float v2, float v3, int v4, float v5, long v6, double v7, float v8, HFA03 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum11_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum11_HFA05(double v1, float v2, float v3, int v4, float v5, long v6, double v7, float v8, HFA05 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum11_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum11_HFA08(double v1, float v2, float v3, int v4, float v5, long v6, double v7, float v8, HFA08 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum11_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum11_HFA11(double v1, float v2, float v3, int v4, float v5, long v6, double v7, float v8, HFA11 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum11_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum11_HFA19(double v1, float v2, float v3, int v4, float v5, long v6, double v7, float v8, HFA19 hfa);


        [DllImport(Config.DllName, EntryPoint = "sum19_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum19_HFA01(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA01 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum19_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum19_HFA02(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA02 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum19_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum19_HFA03(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA03 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum19_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum19_HFA05(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA05 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum19_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum19_HFA08(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA08 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum19_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum19_HFA11(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA11 hfa);

        [DllImport(Config.DllName, EntryPoint = "sum19_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern float Sum19_HFA19(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA19 hfa);



        // ---------------------------------------------------
        // Average Methods
        // ---------------------------------------------------


        [DllImport(Config.DllName, EntryPoint = "average_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average_HFA01(HFA01 hfa);

        [DllImport(Config.DllName, EntryPoint = "average_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average_HFA02(HFA02 hfa);

        [DllImport(Config.DllName, EntryPoint = "average_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average_HFA03(HFA03 hfa);

        [DllImport(Config.DllName, EntryPoint = "average_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average_HFA05(HFA05 hfa);

        [DllImport(Config.DllName, EntryPoint = "average_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average_HFA08(HFA08 hfa);

        [DllImport(Config.DllName, EntryPoint = "average_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average_HFA11(HFA11 hfa);

        [DllImport(Config.DllName, EntryPoint = "average_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average_HFA19(HFA19 hfa);


        [DllImport(Config.DllName, EntryPoint = "average3_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average3_HFA01(HFA01 hfa1, HFA01 hfa2, HFA01 hfa3);

        [DllImport(Config.DllName, EntryPoint = "average3_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average3_HFA02(HFA02 hfa1, HFA02 hfa2, HFA02 hfa3);

        [DllImport(Config.DllName, EntryPoint = "average3_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average3_HFA03(HFA03 hfa1, HFA03 hfa2, HFA03 hfa3);

        [DllImport(Config.DllName, EntryPoint = "average3_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average3_HFA05(HFA05 hfa1, HFA05 hfa2, HFA05 hfa3);

        [DllImport(Config.DllName, EntryPoint = "average3_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average3_HFA08(HFA08 hfa1, HFA08 hfa2, HFA08 hfa3);

        [DllImport(Config.DllName, EntryPoint = "average3_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average3_HFA11(HFA11 hfa1, HFA11 hfa2, HFA11 hfa3);

        [DllImport(Config.DllName, EntryPoint = "average3_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average3_HFA19(HFA19 hfa1, HFA19 hfa2, HFA19 hfa3);


        [DllImport(Config.DllName, EntryPoint = "average5_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average5_HFA01(HFA01 hfa1, HFA01 hfa2, HFA01 hfa3, HFA01 hfa4, HFA01 hfa5);

        [DllImport(Config.DllName, EntryPoint = "average5_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average5_HFA02(HFA02 hfa1, HFA02 hfa2, HFA02 hfa3, HFA02 hfa4, HFA02 hfa5);

        [DllImport(Config.DllName, EntryPoint = "average5_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average5_HFA03(HFA03 hfa1, HFA03 hfa2, HFA03 hfa3, HFA03 hfa4, HFA03 hfa5);

        [DllImport(Config.DllName, EntryPoint = "average5_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average5_HFA05(HFA05 hfa1, HFA05 hfa2, HFA05 hfa3, HFA05 hfa4, HFA05 hfa5);

        [DllImport(Config.DllName, EntryPoint = "average5_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average5_HFA08(HFA08 hfa1, HFA08 hfa2, HFA08 hfa3, HFA08 hfa4, HFA08 hfa5);

        [DllImport(Config.DllName, EntryPoint = "average5_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average5_HFA11(HFA11 hfa1, HFA11 hfa2, HFA11 hfa3, HFA11 hfa4, HFA11 hfa5);

        [DllImport(Config.DllName, EntryPoint = "average5_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average5_HFA19(HFA19 hfa1, HFA19 hfa2, HFA19 hfa3, HFA19 hfa4, HFA19 hfa5);


        [DllImport(Config.DllName, EntryPoint = "average8_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average8_HFA01(HFA01 hfa1, HFA01 hfa2, HFA01 hfa3, HFA01 hfa4, HFA01 hfa5, HFA01 hfa6, HFA01 hfa7, HFA01 hfa8);

        [DllImport(Config.DllName, EntryPoint = "average8_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average8_HFA02(HFA02 hfa1, HFA02 hfa2, HFA02 hfa3, HFA02 hfa4, HFA02 hfa5, HFA02 hfa6, HFA02 hfa7, HFA02 hfa8);

        [DllImport(Config.DllName, EntryPoint = "average8_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average8_HFA03(HFA03 hfa1, HFA03 hfa2, HFA03 hfa3, HFA03 hfa4, HFA03 hfa5, HFA03 hfa6, HFA03 hfa7, HFA03 hfa8);

        [DllImport(Config.DllName, EntryPoint = "average8_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average8_HFA05(HFA05 hfa1, HFA05 hfa2, HFA05 hfa3, HFA05 hfa4, HFA05 hfa5, HFA05 hfa6, HFA05 hfa7, HFA05 hfa8);

        [DllImport(Config.DllName, EntryPoint = "average8_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average8_HFA08(HFA08 hfa1, HFA08 hfa2, HFA08 hfa3, HFA08 hfa4, HFA08 hfa5, HFA08 hfa6, HFA08 hfa7, HFA08 hfa8);

        [DllImport(Config.DllName, EntryPoint = "average8_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average8_HFA11(HFA11 hfa1, HFA11 hfa2, HFA11 hfa3, HFA11 hfa4, HFA11 hfa5, HFA11 hfa6, HFA11 hfa7, HFA11 hfa8);

        [DllImport(Config.DllName, EntryPoint = "average8_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average8_HFA19(HFA19 hfa1, HFA19 hfa2, HFA19 hfa3, HFA19 hfa4, HFA19 hfa5, HFA19 hfa6, HFA19 hfa7, HFA19 hfa8);


        [DllImport(Config.DllName, EntryPoint = "average11_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average11_HFA01(HFA01 hfa1, HFA01 hfa2, HFA01 hfa3, HFA01 hfa4, HFA01 hfa5, HFA01 hfa6, HFA01 hfa7, HFA01 hfa8, HFA01 hfa9, HFA01 hfa10, HFA01 hfa11);

        [DllImport(Config.DllName, EntryPoint = "average11_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average11_HFA02(HFA02 hfa1, HFA02 hfa2, HFA02 hfa3, HFA02 hfa4, HFA02 hfa5, HFA02 hfa6, HFA02 hfa7, HFA02 hfa8, HFA02 hfa9, HFA02 hfa10, HFA02 hfa11);

        [DllImport(Config.DllName, EntryPoint = "average11_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average11_HFA03(HFA03 hfa1, HFA03 hfa2, HFA03 hfa3, HFA03 hfa4, HFA03 hfa5, HFA03 hfa6, HFA03 hfa7, HFA03 hfa8, HFA03 hfa9, HFA03 hfa10, HFA03 hfa11);

        [DllImport(Config.DllName, EntryPoint = "average11_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average11_HFA05(HFA05 hfa1, HFA05 hfa2, HFA05 hfa3, HFA05 hfa4, HFA05 hfa5, HFA05 hfa6, HFA05 hfa7, HFA05 hfa8, HFA05 hfa9, HFA05 hfa10, HFA05 hfa11);

        [DllImport(Config.DllName, EntryPoint = "average11_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average11_HFA08(HFA08 hfa1, HFA08 hfa2, HFA08 hfa3, HFA08 hfa4, HFA08 hfa5, HFA08 hfa6, HFA08 hfa7, HFA08 hfa8, HFA08 hfa9, HFA08 hfa10, HFA08 hfa11);

        [DllImport(Config.DllName, EntryPoint = "average11_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average11_HFA11(HFA11 hfa1, HFA11 hfa2, HFA11 hfa3, HFA11 hfa4, HFA11 hfa5, HFA11 hfa6, HFA11 hfa7, HFA11 hfa8, HFA11 hfa9, HFA11 hfa10, HFA11 hfa11);

        [DllImport(Config.DllName, EntryPoint = "average11_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average11_HFA19(HFA19 hfa1, HFA19 hfa2, HFA19 hfa3, HFA19 hfa4, HFA19 hfa5, HFA19 hfa6, HFA19 hfa7, HFA19 hfa8, HFA19 hfa9, HFA19 hfa10, HFA19 hfa11);


        [DllImport(Config.DllName, EntryPoint = "average19_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average19_HFA01(HFA01 hfa1, HFA01 hfa2, HFA01 hfa3, HFA01 hfa4, HFA01 hfa5, HFA01 hfa6, HFA01 hfa7, HFA01 hfa8, HFA01 hfa9, HFA01 hfa10, HFA01 hfa11, HFA01 hfa12, HFA01 hfa13, HFA01 hfa14, HFA01 hfa15, HFA01 hfa16, HFA01 hfa17, HFA01 hfa18, HFA01 hfa19);

        [DllImport(Config.DllName, EntryPoint = "average19_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average19_HFA02(HFA02 hfa1, HFA02 hfa2, HFA02 hfa3, HFA02 hfa4, HFA02 hfa5, HFA02 hfa6, HFA02 hfa7, HFA02 hfa8, HFA02 hfa9, HFA02 hfa10, HFA02 hfa11, HFA02 hfa12, HFA02 hfa13, HFA02 hfa14, HFA02 hfa15, HFA02 hfa16, HFA02 hfa17, HFA02 hfa18, HFA02 hfa19);

        [DllImport(Config.DllName, EntryPoint = "average19_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average19_HFA03(HFA03 hfa1, HFA03 hfa2, HFA03 hfa3, HFA03 hfa4, HFA03 hfa5, HFA03 hfa6, HFA03 hfa7, HFA03 hfa8, HFA03 hfa9, HFA03 hfa10, HFA03 hfa11, HFA03 hfa12, HFA03 hfa13, HFA03 hfa14, HFA03 hfa15, HFA03 hfa16, HFA03 hfa17, HFA03 hfa18, HFA03 hfa19);

        [DllImport(Config.DllName, EntryPoint = "average19_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average19_HFA05(HFA05 hfa1, HFA05 hfa2, HFA05 hfa3, HFA05 hfa4, HFA05 hfa5, HFA05 hfa6, HFA05 hfa7, HFA05 hfa8, HFA05 hfa9, HFA05 hfa10, HFA05 hfa11, HFA05 hfa12, HFA05 hfa13, HFA05 hfa14, HFA05 hfa15, HFA05 hfa16, HFA05 hfa17, HFA05 hfa18, HFA05 hfa19);

        [DllImport(Config.DllName, EntryPoint = "average19_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average19_HFA08(HFA08 hfa1, HFA08 hfa2, HFA08 hfa3, HFA08 hfa4, HFA08 hfa5, HFA08 hfa6, HFA08 hfa7, HFA08 hfa8, HFA08 hfa9, HFA08 hfa10, HFA08 hfa11, HFA08 hfa12, HFA08 hfa13, HFA08 hfa14, HFA08 hfa15, HFA08 hfa16, HFA08 hfa17, HFA08 hfa18, HFA08 hfa19);

        [DllImport(Config.DllName, EntryPoint = "average19_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average19_HFA11(HFA11 hfa1, HFA11 hfa2, HFA11 hfa3, HFA11 hfa4, HFA11 hfa5, HFA11 hfa6, HFA11 hfa7, HFA11 hfa8, HFA11 hfa9, HFA11 hfa10, HFA11 hfa11, HFA11 hfa12, HFA11 hfa13, HFA11 hfa14, HFA11 hfa15, HFA11 hfa16, HFA11 hfa17, HFA11 hfa18, HFA11 hfa19);

        [DllImport(Config.DllName, EntryPoint = "average19_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern float Average19_HFA19(HFA19 hfa1, HFA19 hfa2, HFA19 hfa3, HFA19 hfa4, HFA19 hfa5, HFA19 hfa6, HFA19 hfa7, HFA19 hfa8, HFA19 hfa9, HFA19 hfa10, HFA19 hfa11, HFA19 hfa12, HFA19 hfa13, HFA19 hfa14, HFA19 hfa15, HFA19 hfa16, HFA19 hfa17, HFA19 hfa18, HFA19 hfa19);



        // ---------------------------------------------------
        // Add Methods
        // ---------------------------------------------------


        [DllImport(Config.DllName, EntryPoint = "add01_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern float Add01_HFA01(HFA01 hfa1, float v1, HFA01 hfa2, int v2, HFA01 hfa3, short v3, double v4, HFA01 hfa4, HFA01 hfa5, float v5, long v6, float v7, HFA01 hfa6, float v8, HFA01 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add01_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern float Add01_HFA02(HFA02 hfa1, float v1, HFA02 hfa2, int v2, HFA02 hfa3, short v3, double v4, HFA02 hfa4, HFA02 hfa5, float v5, long v6, float v7, HFA02 hfa6, float v8, HFA02 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add01_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern float Add01_HFA03(HFA03 hfa1, float v1, HFA03 hfa2, int v2, HFA03 hfa3, short v3, double v4, HFA03 hfa4, HFA03 hfa5, float v5, long v6, float v7, HFA03 hfa6, float v8, HFA03 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add01_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern float Add01_HFA05(HFA05 hfa1, float v1, HFA05 hfa2, int v2, HFA05 hfa3, short v3, double v4, HFA05 hfa4, HFA05 hfa5, float v5, long v6, float v7, HFA05 hfa6, float v8, HFA05 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add01_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern float Add01_HFA08(HFA08 hfa1, float v1, HFA08 hfa2, int v2, HFA08 hfa3, short v3, double v4, HFA08 hfa4, HFA08 hfa5, float v5, long v6, float v7, HFA08 hfa6, float v8, HFA08 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add01_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern float Add01_HFA11(HFA11 hfa1, float v1, HFA11 hfa2, int v2, HFA11 hfa3, short v3, double v4, HFA11 hfa4, HFA11 hfa5, float v5, long v6, float v7, HFA11 hfa6, float v8, HFA11 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add01_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern float Add01_HFA19(HFA19 hfa1, float v1, HFA19 hfa2, int v2, HFA19 hfa3, short v3, double v4, HFA19 hfa4, HFA19 hfa5, float v5, long v6, float v7, HFA19 hfa6, float v8, HFA19 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add01_HFA00", CallingConvention = CallingConvention.StdCall)]
        public static extern float Add01_HFA00(HFA03 hfa1, float v1, HFA02 hfa2, int v2, HFA19 hfa3, short v3, double v4, HFA05 hfa4, HFA08 hfa5, float v5, long v6, float v7, HFA11 hfa6, float v8, HFA01 hfa7);


        [DllImport(Config.DllName, EntryPoint = "add02_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern float Add02_HFA01(HFA01 hfa1, HFA01 hfa2, long v1, short v2, float v3, int v4, double v5, float v6, HFA01 hfa3, double v7, float v8, HFA01 hfa4, short v9, HFA01 hfa5, float v10, HFA01 hfa6, HFA01 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add02_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern float Add02_HFA02(HFA02 hfa1, HFA02 hfa2, long v1, short v2, float v3, int v4, double v5, float v6, HFA02 hfa3, double v7, float v8, HFA02 hfa4, short v9, HFA02 hfa5, float v10, HFA02 hfa6, HFA02 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add02_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern float Add02_HFA03(HFA03 hfa1, HFA03 hfa2, long v1, short v2, float v3, int v4, double v5, float v6, HFA03 hfa3, double v7, float v8, HFA03 hfa4, short v9, HFA03 hfa5, float v10, HFA03 hfa6, HFA03 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add02_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern float Add02_HFA05(HFA05 hfa1, HFA05 hfa2, long v1, short v2, float v3, int v4, double v5, float v6, HFA05 hfa3, double v7, float v8, HFA05 hfa4, short v9, HFA05 hfa5, float v10, HFA05 hfa6, HFA05 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add02_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern float Add02_HFA08(HFA08 hfa1, HFA08 hfa2, long v1, short v2, float v3, int v4, double v5, float v6, HFA08 hfa3, double v7, float v8, HFA08 hfa4, short v9, HFA08 hfa5, float v10, HFA08 hfa6, HFA08 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add02_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern float Add02_HFA11(HFA11 hfa1, HFA11 hfa2, long v1, short v2, float v3, int v4, double v5, float v6, HFA11 hfa3, double v7, float v8, HFA11 hfa4, short v9, HFA11 hfa5, float v10, HFA11 hfa6, HFA11 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add02_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern float Add02_HFA19(HFA19 hfa1, HFA19 hfa2, long v1, short v2, float v3, int v4, double v5, float v6, HFA19 hfa3, double v7, float v8, HFA19 hfa4, short v9, HFA19 hfa5, float v10, HFA19 hfa6, HFA19 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add02_HFA00", CallingConvention = CallingConvention.StdCall)]
        public static extern float Add02_HFA00(HFA01 hfa1, HFA05 hfa2, long v1, short v2, float v3, int v4, double v5, float v6, HFA03 hfa3, double v7, float v8, HFA11 hfa4, short v9, HFA19 hfa5, float v10, HFA08 hfa6, HFA02 hfa7);



        [DllImport(Config.DllName, EntryPoint = "add03_HFA01", CallingConvention = CallingConvention.StdCall)]
        public static extern float Add03_HFA01(float v1, sbyte v2, HFA01 hfa1, double v3, sbyte v4, HFA01 hfa2, long v5, short v6, int v7, HFA01 hfa3, HFA01 hfa4, float v8, HFA01 hfa5, float v9, HFA01 hfa6, float v10, HFA01 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add03_HFA02", CallingConvention = CallingConvention.StdCall)]
        public static extern float Add03_HFA02(float v1, sbyte v2, HFA02 hfa1, double v3, sbyte v4, HFA02 hfa2, long v5, short v6, int v7, HFA02 hfa3, HFA02 hfa4, float v8, HFA02 hfa5, float v9, HFA02 hfa6, float v10, HFA02 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add03_HFA03", CallingConvention = CallingConvention.StdCall)]
        public static extern float Add03_HFA03(float v1, sbyte v2, HFA03 hfa1, double v3, sbyte v4, HFA03 hfa2, long v5, short v6, int v7, HFA03 hfa3, HFA03 hfa4, float v8, HFA03 hfa5, float v9, HFA03 hfa6, float v10, HFA03 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add03_HFA05", CallingConvention = CallingConvention.StdCall)]
        public static extern float Add03_HFA05(float v1, sbyte v2, HFA05 hfa1, double v3, sbyte v4, HFA05 hfa2, long v5, short v6, int v7, HFA05 hfa3, HFA05 hfa4, float v8, HFA05 hfa5, float v9, HFA05 hfa6, float v10, HFA05 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add03_HFA08", CallingConvention = CallingConvention.StdCall)]
        public static extern float Add03_HFA08(float v1, sbyte v2, HFA08 hfa1, double v3, sbyte v4, HFA08 hfa2, long v5, short v6, int v7, HFA08 hfa3, HFA08 hfa4, float v8, HFA08 hfa5, float v9, HFA08 hfa6, float v10, HFA08 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add03_HFA11", CallingConvention = CallingConvention.StdCall)]
        public static extern float Add03_HFA11(float v1, sbyte v2, HFA11 hfa1, double v3, sbyte v4, HFA11 hfa2, long v5, short v6, int v7, HFA11 hfa3, HFA11 hfa4, float v8, HFA11 hfa5, float v9, HFA11 hfa6, float v10, HFA11 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add03_HFA19", CallingConvention = CallingConvention.StdCall)]
        public static extern float Add03_HFA19(float v1, sbyte v2, HFA19 hfa1, double v3, sbyte v4, HFA19 hfa2, long v5, short v6, int v7, HFA19 hfa3, HFA19 hfa4, float v8, HFA19 hfa5, float v9, HFA19 hfa6, float v10, HFA19 hfa7);

        [DllImport(Config.DllName, EntryPoint = "add03_HFA00", CallingConvention = CallingConvention.StdCall)]
        public static extern float Add03_HFA00(float v1, sbyte v2, HFA08 hfa1, double v3, sbyte v4, HFA19 hfa2, long v5, short v6, int v7, HFA03 hfa3, HFA01 hfa4, float v8, HFA11 hfa5, float v9, HFA02 hfa6, float v10, HFA05 hfa7);

#endif // FLOAT64
    }
}
