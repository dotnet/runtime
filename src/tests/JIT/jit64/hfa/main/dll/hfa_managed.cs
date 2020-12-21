// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace HFATest
{
    public class TestMan
    {
#if NESTED_HFA
#if FLOAT64
        public const double EXPECTED_SUM_HFA01 = 1;
        public const double EXPECTED_SUM_HFA02 = 3;
        public const double EXPECTED_SUM_HFA03 = 4;
        public const double EXPECTED_SUM_HFA05 = 7;
        public const double EXPECTED_SUM_HFA08 = 11;
        public const double EXPECTED_SUM_HFA11 = 15;
        public const double EXPECTED_SUM_HFA19 = 26;
#else
        public const float EXPECTED_SUM_HFA01 = 1;
        public const float EXPECTED_SUM_HFA02 = 3;
        public const float EXPECTED_SUM_HFA03 = 4;
        public const float EXPECTED_SUM_HFA05 = 7;
        public const float EXPECTED_SUM_HFA08 = 11;
        public const float EXPECTED_SUM_HFA11 = 15;
        public const float EXPECTED_SUM_HFA19 = 26;
#endif
#else
#if FLOAT64
        public const double EXPECTED_SUM_HFA01 = 1;
        public const double EXPECTED_SUM_HFA02 = 3;
        public const double EXPECTED_SUM_HFA03 = 6;
        public const double EXPECTED_SUM_HFA05 = 15;
        public const double EXPECTED_SUM_HFA08 = 36;
        public const double EXPECTED_SUM_HFA11 = 66;
        public const double EXPECTED_SUM_HFA19 = 190;
#else
        public const float EXPECTED_SUM_HFA01 = 1;
        public const float EXPECTED_SUM_HFA02 = 3;
        public const float EXPECTED_SUM_HFA03 = 6;
        public const float EXPECTED_SUM_HFA05 = 15;
        public const float EXPECTED_SUM_HFA08 = 36;
        public const float EXPECTED_SUM_HFA11 = 66;
        public const float EXPECTED_SUM_HFA19 = 190;
#endif
#endif


        // --------------------------------------------------------------
        // Init methods
        // --------------------------------------------------------------

        public static void Init_HFA01(out HFA01 hfa)
        {
            hfa.f1 = 1;
        }

        public static void Init_HFA02(out HFA02 hfa)
        {
#if NESTED_HFA
            Init_HFA01(out hfa.hfa01);
            hfa.f2 = 2;
#else
            hfa.f1 = 1;
            hfa.f2 = 2;
#endif
        }

        public static void Init_HFA03(out HFA03 hfa)
        {
#if NESTED_HFA
            Init_HFA01(out hfa.hfa01);
            Init_HFA02(out hfa.hfa02);
#else
            hfa.f1 = 1;
            hfa.f2 = 2;
            hfa.f3 = 3;
#endif
        }

        public static void Init_HFA05(out HFA05 hfa)
        {
#if NESTED_HFA
            Init_HFA02(out hfa.hfa02);
            Init_HFA03(out hfa.hfa03);
#else
            hfa.f1 = 1;
            hfa.f2 = 2;
            hfa.f3 = 3;
            hfa.f4 = 4;
            hfa.f5 = 5;
#endif
        }

        public static void Init_HFA08(out HFA08 hfa)
        {
#if NESTED_HFA
            Init_HFA03(out hfa.hfa03);
            Init_HFA05(out hfa.hfa05);
#else
            hfa.f1 = 1;
            hfa.f2 = 2;
            hfa.f3 = 3;
            hfa.f4 = 4;
            hfa.f5 = 5;
            hfa.f6 = 6;
            hfa.f7 = 7;
            hfa.f8 = 8;
#endif
        }

        public static void Init_HFA11(out HFA11 hfa)
        {
#if NESTED_HFA
            Init_HFA03(out hfa.hfa03);
            Init_HFA08(out hfa.hfa08);
#else
            hfa.f1 = 1;
            hfa.f2 = 2;
            hfa.f3 = 3;
            hfa.f4 = 4;
            hfa.f5 = 5;
            hfa.f6 = 6;
            hfa.f7 = 7;
            hfa.f8 = 8;
            hfa.f9 = 9;
            hfa.f10 = 10;
            hfa.f11 = 11;
#endif
        }

        public static void Init_HFA19(out HFA19 hfa)
        {
#if NESTED_HFA
            Init_HFA08(out hfa.hfa08);
            Init_HFA11(out hfa.hfa11);
#else
            hfa.f1 = 1;
            hfa.f2 = 2;
            hfa.f3 = 3;
            hfa.f4 = 4;
            hfa.f5 = 5;
            hfa.f6 = 6;
            hfa.f7 = 7;
            hfa.f8 = 8;
            hfa.f9 = 9;
            hfa.f10 = 10;
            hfa.f11 = 11;
            hfa.f12 = 12;
            hfa.f13 = 13;
            hfa.f14 = 14;
            hfa.f15 = 15;
            hfa.f16 = 16;
            hfa.f17 = 17;
            hfa.f18 = 18;
            hfa.f19 = 19;
#endif
        }



        // --------------------------------------------------------------
        // Identity methods
        // --------------------------------------------------------------

        public static HFA01 Identity_HFA01(HFA01 hfa)
        {
            return hfa;
        }

        public static HFA02 Identity_HFA02(HFA02 hfa)
        {
            return hfa;
        }

        public static HFA03 Identity_HFA03(HFA03 hfa)
        {
            return hfa;
        }

        public static HFA05 Identity_HFA05(HFA05 hfa)
        {
            return hfa;
        }

        public static HFA08 Identity_HFA08(HFA08 hfa)
        {
            return hfa;
        }

        public static HFA11 Identity_HFA11(HFA11 hfa)
        {
            return hfa;
        }

        public static HFA19 Identity_HFA19(HFA19 hfa)
        {
            return hfa;
        }


        // --------------------------------------------------------------
        // Get methods
        // --------------------------------------------------------------

        public static HFA01 Get_HFA01()
        {
            HFA01 hfa;
            Init_HFA01(out hfa);
            return hfa;
        }

        public static HFA02 Get_HFA02()
        {
            HFA02 hfa;
            Init_HFA02(out hfa);
            return hfa;
        }

        public static HFA03 Get_HFA03()
        {
            HFA03 hfa;
            Init_HFA03(out hfa);
            return hfa;
        }

        public static HFA05 Get_HFA05()
        {
            HFA05 hfa;
            Init_HFA05(out hfa);
            return hfa;
        }

        public static HFA08 Get_HFA08()
        {
            HFA08 hfa;
            Init_HFA08(out hfa);
            return hfa;
        }

        public static HFA11 Get_HFA11()
        {
            HFA11 hfa;
            Init_HFA11(out hfa);
            return hfa;
        }

        public static HFA19 Get_HFA19()
        {
            HFA19 hfa;
            Init_HFA19(out hfa);
            return hfa;
        }


        // --------------------------------------------------------------
        // Sum methods
        // --------------------------------------------------------------

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum_HFA01(HFA01 hfa)
        {
            return hfa.f1;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum_HFA02(HFA02 hfa)
        {
#if NESTED_HFA
            return Sum_HFA01(hfa.hfa01) + hfa.f2;
#else
            return hfa.f1 + hfa.f2;
#endif
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum_HFA03(HFA03 hfa)
        {
#if NESTED_HFA
            return Sum_HFA01(hfa.hfa01) + Sum_HFA02(hfa.hfa02);
#else
            return hfa.f1 + hfa.f2 + hfa.f3;
#endif
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum_HFA05(HFA05 hfa)
        {
#if NESTED_HFA
            return Sum_HFA02(hfa.hfa02) + Sum_HFA03(hfa.hfa03);
#else
            return hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4 + hfa.f5;
#endif
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum_HFA08(HFA08 hfa)
        {
#if NESTED_HFA
            return Sum_HFA03(hfa.hfa03) + Sum_HFA05(hfa.hfa05);
#else
            return hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4 + hfa.f5 + hfa.f6 + hfa.f7 + hfa.f8;
#endif
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum_HFA11(HFA11 hfa)
        {
#if NESTED_HFA
            return Sum_HFA03(hfa.hfa03) + Sum_HFA08(hfa.hfa08);
#else
            return hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4 + hfa.f5 + hfa.f6 + hfa.f7 + hfa.f8 + hfa.f9 + hfa.f10 + hfa.f11;
#endif
        }


#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum_HFA19(HFA19 hfa)
        {
#if NESTED_HFA
            return Sum_HFA08(hfa.hfa08) + Sum_HFA11(hfa.hfa11);
#else
            return hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4 + hfa.f5 + hfa.f6 + hfa.f7 + hfa.f8 + hfa.f9 + hfa.f10 + hfa.f11 + hfa.f12 + hfa.f13 + hfa.f14 + hfa.f15 + hfa.f16 + hfa.f17 + hfa.f18 + hfa.f19;
#endif
        }



        // --------------------------------------------------------------
        // Sum3 methods
        // --------------------------------------------------------------

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum3_HFA01(float v1, long v2, HFA01 hfa)
        {
            return (float)v1 + (float)v2 + hfa.f1;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum3_HFA02(float v1, long v2, HFA02 hfa)
        {
            return (float)v1 + (float)v2 +
#if NESTED_HFA
                Sum_HFA01(hfa.hfa01) + hfa.f2;
#else
                hfa.f1 + hfa.f2;
#endif
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum3_HFA03(float v1, long v2, HFA03 hfa)
        {
            return (float)v1 + (float)v2 +
#if NESTED_HFA
                Sum_HFA01(hfa.hfa01) + Sum_HFA02(hfa.hfa02);
#else
                hfa.f1 + hfa.f2 + hfa.f3;
#endif
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum3_HFA05(float v1, long v2, HFA05 hfa)
        {
            return (float)v1 + (float)v2 +
#if NESTED_HFA
                Sum_HFA02(hfa.hfa02) + Sum_HFA03(hfa.hfa03);
#else
                hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4 + hfa.f5;
#endif
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum3_HFA08(float v1, long v2, HFA08 hfa)
        {
            return (float)v1 + (float)v2 +
#if NESTED_HFA
                Sum_HFA03(hfa.hfa03) + Sum_HFA05(hfa.hfa05);
#else
                hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4 + hfa.f5 + hfa.f6 + hfa.f7 + hfa.f8;
#endif
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum3_HFA11(float v1, long v2, HFA11 hfa)
        {
            return (float)v1 + (float)v2 +
#if NESTED_HFA
                Sum_HFA03(hfa.hfa03) + Sum_HFA08(hfa.hfa08);
#else
                hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4 + hfa.f5 + hfa.f6 + hfa.f7 + hfa.f8 + hfa.f9 + hfa.f10 + hfa.f11;
#endif
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum3_HFA19(float v1, long v2, HFA19 hfa)
        {
            return (float)v1 + (float)v2 +
#if NESTED_HFA
                Sum_HFA08(hfa.hfa08) + Sum_HFA11(hfa.hfa11);
#else
                hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4 + hfa.f5 + hfa.f6 + hfa.f7 + hfa.f8 + hfa.f9 + hfa.f10 + hfa.f11 + hfa.f12 + hfa.f13 + hfa.f14 + hfa.f15 + hfa.f16 + hfa.f17 + hfa.f18 + hfa.f19;
#endif
        }



        // --------------------------------------------------------------
        // Sum5 methods
        // --------------------------------------------------------------

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum5_HFA01(long v1, double v2, int v3, sbyte v4, HFA01 hfa)
        {
            return (float)v1 + (float)v2 + (float)v3 + (float)v4 +
                hfa.f1;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum5_HFA02(long v1, double v2, int v3, sbyte v4, HFA02 hfa)
        {
            return (float)v1 + (float)v2 + (float)v3 + (float)v4 +
#if NESTED_HFA
                Sum_HFA01(hfa.hfa01) + hfa.f2;
#else
                hfa.f1 + hfa.f2;
#endif
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum5_HFA03(long v1, double v2, int v3, sbyte v4, HFA03 hfa)
        {
            return (float)v1 + (float)v2 + (float)v3 + (float)v4 +
#if NESTED_HFA
                Sum_HFA01(hfa.hfa01) + Sum_HFA02(hfa.hfa02);
#else
                hfa.f1 + hfa.f2 + hfa.f3;
#endif
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum5_HFA05(long v1, double v2, int v3, sbyte v4, HFA05 hfa)
        {
            return (float)v1 + (float)v2 + (float)v3 + (float)v4 +
#if NESTED_HFA
                Sum_HFA02(hfa.hfa02) + Sum_HFA03(hfa.hfa03);
#else
                hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4 + hfa.f5;
#endif
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum5_HFA08(long v1, double v2, int v3, sbyte v4, HFA08 hfa)
        {
            return (float)v1 + (float)v2 + (float)v3 + (float)v4 +
#if NESTED_HFA
                Sum_HFA03(hfa.hfa03) + Sum_HFA05(hfa.hfa05);
#else
                hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4 + hfa.f5 + hfa.f6 + hfa.f7 + hfa.f8;
#endif
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum5_HFA11(long v1, double v2, int v3, sbyte v4, HFA11 hfa)
        {
            return (float)v1 + (float)v2 + (float)v3 + (float)v4 +
#if NESTED_HFA
                Sum_HFA03(hfa.hfa03) + Sum_HFA08(hfa.hfa08);
#else
                hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4 + hfa.f5 + hfa.f6 + hfa.f7 + hfa.f8 + hfa.f9 + hfa.f10 + hfa.f11;
#endif
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum5_HFA19(long v1, double v2, int v3, sbyte v4, HFA19 hfa)
        {
            return (float)v1 + (float)v2 + (float)v3 + (float)v4 +
#if NESTED_HFA
                Sum_HFA08(hfa.hfa08) + Sum_HFA11(hfa.hfa11);
#else
                hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4 + hfa.f5 + hfa.f6 + hfa.f7 + hfa.f8 + hfa.f9 + hfa.f10 + hfa.f11 + hfa.f12 + hfa.f13 + hfa.f14 + hfa.f15 + hfa.f16 + hfa.f17 + hfa.f18 + hfa.f19;
#endif
        }



        // --------------------------------------------------------------
        // Sum8 methods
        // --------------------------------------------------------------

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum8_HFA01(float v1, double v2, long v3, sbyte v4, double v5, HFA01 hfa)
        {
            return (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 +
                hfa.f1;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum8_HFA02(float v1, double v2, long v3, sbyte v4, double v5, HFA02 hfa)
        {
            return (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 +
#if NESTED_HFA
                Sum_HFA01(hfa.hfa01) + hfa.f2;
#else
                hfa.f1 + hfa.f2;
#endif
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum8_HFA03(float v1, double v2, long v3, sbyte v4, double v5, HFA03 hfa)
        {
            return (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 +
#if NESTED_HFA
                Sum_HFA01(hfa.hfa01) + Sum_HFA02(hfa.hfa02);
#else
                hfa.f1 + hfa.f2 + hfa.f3;
#endif
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum8_HFA05(float v1, double v2, long v3, sbyte v4, double v5, HFA05 hfa)
        {
            return (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 +
#if NESTED_HFA
                Sum_HFA02(hfa.hfa02) + Sum_HFA03(hfa.hfa03);
#else
                hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4 + hfa.f5;
#endif
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum8_HFA08(float v1, double v2, long v3, sbyte v4, double v5, HFA08 hfa)
        {
            return (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 +
#if NESTED_HFA
                Sum_HFA03(hfa.hfa03) + Sum_HFA05(hfa.hfa05);
#else
                hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4 + hfa.f5 + hfa.f6 + hfa.f7 + hfa.f8;
#endif
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum8_HFA11(float v1, double v2, long v3, sbyte v4, double v5, HFA11 hfa)
        {
            return (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 +
#if NESTED_HFA
                Sum_HFA03(hfa.hfa03) + Sum_HFA08(hfa.hfa08);
#else
                hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4 + hfa.f5 + hfa.f6 + hfa.f7 + hfa.f8 + hfa.f9 + hfa.f10 + hfa.f11;
#endif
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum8_HFA19(float v1, double v2, long v3, sbyte v4, double v5, HFA19 hfa)
        {
            return (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 +
#if NESTED_HFA
                Sum_HFA08(hfa.hfa08) + Sum_HFA11(hfa.hfa11);
#else
                hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4 + hfa.f5 + hfa.f6 + hfa.f7 + hfa.f8 + hfa.f9 + hfa.f10 + hfa.f11 + hfa.f12 + hfa.f13 + hfa.f14 + hfa.f15 + hfa.f16 + hfa.f17 + hfa.f18 + hfa.f19;
#endif
        }



        // --------------------------------------------------------------
        // Sum11 methods
        // --------------------------------------------------------------

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum11_HFA01(double v1, float v2, float v3, int v4, float v5, long v6, double v7, float v8, HFA01 hfa)
        {
            return (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 +
                hfa.f1;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum11_HFA02(double v1, float v2, float v3, int v4, float v5, long v6, double v7, float v8, HFA02 hfa)
        {
            return (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 +
#if NESTED_HFA
                Sum_HFA01(hfa.hfa01) + hfa.f2;
#else
                hfa.f1 + hfa.f2;
#endif
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum11_HFA03(double v1, float v2, float v3, int v4, float v5, long v6, double v7, float v8, HFA03 hfa)
        {
            return (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 +
#if NESTED_HFA
                Sum_HFA01(hfa.hfa01) + Sum_HFA02(hfa.hfa02);
#else
                hfa.f1 + hfa.f2 + hfa.f3;
#endif
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum11_HFA05(double v1, float v2, float v3, int v4, float v5, long v6, double v7, float v8, HFA05 hfa)
        {
            return (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 +
#if NESTED_HFA
                Sum_HFA02(hfa.hfa02) + Sum_HFA03(hfa.hfa03);
#else
                hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4 + hfa.f5;
#endif
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum11_HFA08(double v1, float v2, float v3, int v4, float v5, long v6, double v7, float v8, HFA08 hfa)
        {
            return (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 +
#if NESTED_HFA
                Sum_HFA03(hfa.hfa03) + Sum_HFA05(hfa.hfa05);
#else
                hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4 + hfa.f5 + hfa.f6 + hfa.f7 + hfa.f8;
#endif
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum11_HFA11(double v1, float v2, float v3, int v4, float v5, long v6, double v7, float v8, HFA11 hfa)
        {
            return (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 +
#if NESTED_HFA
                Sum_HFA03(hfa.hfa03) + Sum_HFA08(hfa.hfa08);
#else
                hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4 + hfa.f5 + hfa.f6 + hfa.f7 + hfa.f8 + hfa.f9 + hfa.f10 + hfa.f11;
#endif
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum11_HFA19(double v1, float v2, float v3, int v4, float v5, long v6, double v7, float v8, HFA19 hfa)
        {
            return (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 +
#if NESTED_HFA
                Sum_HFA08(hfa.hfa08) + Sum_HFA11(hfa.hfa11);
#else
                hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4 + hfa.f5 + hfa.f6 + hfa.f7 + hfa.f8 + hfa.f9 + hfa.f10 + hfa.f11 + hfa.f12 + hfa.f13 + hfa.f14 + hfa.f15 + hfa.f16 + hfa.f17 + hfa.f18 + hfa.f19;
#endif
        }



        // --------------------------------------------------------------
        // Sum19 methods
        // --------------------------------------------------------------

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum19_HFA01(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA01 hfa)
        {
            return (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 + (float)v9 + (float)v10 + (float)v11 + (float)v12 + (float)v13 +
                hfa.f1;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum19_HFA02(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA02 hfa)
        {
            return (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 + (float)v9 + (float)v10 + (float)v11 + (float)v12 + (float)v13 +
#if NESTED_HFA
                Sum_HFA01(hfa.hfa01) + hfa.f2;
#else
                hfa.f1 + hfa.f2;
#endif
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum19_HFA03(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA03 hfa)
        {
            return (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 + (float)v9 + (float)v10 + (float)v11 + (float)v12 + (float)v13 +
#if NESTED_HFA
                Sum_HFA01(hfa.hfa01) + Sum_HFA02(hfa.hfa02);
#else
                hfa.f1 + hfa.f2 + hfa.f3;
#endif
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum19_HFA05(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA05 hfa)
        {
            return (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 + (float)v9 + (float)v10 + (float)v11 + (float)v12 + (float)v13 +
#if NESTED_HFA
                Sum_HFA02(hfa.hfa02) + Sum_HFA03(hfa.hfa03);
#else
                hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4 + hfa.f5;
#endif
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum19_HFA08(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA08 hfa)
        {
            return (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 + (float)v9 + (float)v10 + (float)v11 + (float)v12 + (float)v13 +
#if NESTED_HFA
                Sum_HFA03(hfa.hfa03) + Sum_HFA05(hfa.hfa05);
#else
                hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4 + hfa.f5 + hfa.f6 + hfa.f7 + hfa.f8;
#endif
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum19_HFA11(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA11 hfa)
        {
            return (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 + (float)v9 + (float)v10 + (float)v11 + (float)v12 + (float)v13 +
#if NESTED_HFA
                Sum_HFA03(hfa.hfa03) + Sum_HFA08(hfa.hfa08);
#else
                hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4 + hfa.f5 + hfa.f6 + hfa.f7 + hfa.f8 + hfa.f9 + hfa.f10 + hfa.f11;
#endif
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Sum19_HFA19(float v1, double v2, float v3, double v4, float v5, double v6, float v7, double v8, float v9, double v10, float v11, double v12, float v13, HFA19 hfa)
        {
            return (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 + (float)v9 + (float)v10 + (float)v11 + (float)v12 + (float)v13 +
#if NESTED_HFA
                Sum_HFA08(hfa.hfa08) + Sum_HFA11(hfa.hfa11);
#else
                hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4 + hfa.f5 + hfa.f6 + hfa.f7 + hfa.f8 + hfa.f9 + hfa.f10 + hfa.f11 + hfa.f12 + hfa.f13 + hfa.f14 + hfa.f15 + hfa.f16 + hfa.f17 + hfa.f18 + hfa.f19;
#endif
        }




        // --------------------------------------------------------------
        // Average methods
        // --------------------------------------------------------------

#if FLOAT64
        public static double
#else
        public static float
#endif
            Average_HFA01(HFA01 hfa)
        {
            return Sum_HFA01(hfa) / 1;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Average_HFA02(HFA02 hfa)
        {
            return Sum_HFA02(hfa) / 2;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Average_HFA03(HFA03 hfa)
        {
            return Sum_HFA03(hfa) / 3;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Average_HFA05(HFA05 hfa)
        {
            return Sum_HFA05(hfa) / 5;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Average_HFA08(HFA08 hfa)
        {
            return Sum_HFA08(hfa) / 8;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Average_HFA11(HFA11 hfa)
        {
            return Sum_HFA11(hfa) / 11;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Average_HFA19(HFA19 hfa)
        {
            return Sum_HFA19(hfa) / 19;
        }


        // --------------------------------------------------------------
        // Average3 methods
        // --------------------------------------------------------------

#if FLOAT64
        public static double
#else
        public static float
#endif
            Average3_HFA01(HFA01 hfa1, HFA01 hfa2, HFA01 hfa3)
        {
            return (Average_HFA01(hfa1) + Average_HFA01(hfa2) + Average_HFA01(hfa3)) / 3;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Average3_HFA02(HFA02 hfa1, HFA02 hfa2, HFA02 hfa3)
        {
            return (Average_HFA02(hfa1) + Average_HFA02(hfa2) + Average_HFA02(hfa3)) / 3;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Average3_HFA03(HFA03 hfa1, HFA03 hfa2, HFA03 hfa3)
        {
            return (Average_HFA03(hfa1) + Average_HFA03(hfa2) + Average_HFA03(hfa3)) / 3;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Average3_HFA05(HFA05 hfa1, HFA05 hfa2, HFA05 hfa3)
        {
            return (Average_HFA05(hfa1) + Average_HFA05(hfa2) + Average_HFA05(hfa3)) / 3;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Average3_HFA08(HFA08 hfa1, HFA08 hfa2, HFA08 hfa3)
        {
            return (Average_HFA08(hfa1) + Average_HFA08(hfa2) + Average_HFA08(hfa3)) / 3;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Average3_HFA11(HFA11 hfa1, HFA11 hfa2, HFA11 hfa3)
        {
            return (Average_HFA11(hfa1) + Average_HFA11(hfa2) + Average_HFA11(hfa3)) / 3;
        }


#if FLOAT64
        public static double
#else
        public static float
#endif
            Average3_HFA19(HFA19 hfa1, HFA19 hfa2, HFA19 hfa3)
        {
            return (Average_HFA19(hfa1) + Average_HFA19(hfa2) + Average_HFA19(hfa3)) / 3;
        }


        // --------------------------------------------------------------
        // Average5 methods
        // --------------------------------------------------------------

#if FLOAT64
        public static double
#else
        public static float
#endif
            Average5_HFA01(HFA01 hfa1, HFA01 hfa2, HFA01 hfa3, HFA01 hfa4, HFA01 hfa5)
        {
            return ((Average3_HFA01(hfa1, hfa2, hfa3) * 3) + Average_HFA01(hfa4) + Average_HFA01(hfa5)) / 5;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Average5_HFA02(HFA02 hfa1, HFA02 hfa2, HFA02 hfa3, HFA02 hfa4, HFA02 hfa5)
        {
            return ((Average3_HFA02(hfa1, hfa2, hfa3) * 3) + Average_HFA02(hfa4) + Average_HFA02(hfa5)) / 5;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Average5_HFA03(HFA03 hfa1, HFA03 hfa2, HFA03 hfa3, HFA03 hfa4, HFA03 hfa5)
        {
            return ((Average3_HFA03(hfa1, hfa2, hfa3) * 3) + Average_HFA03(hfa4) + Average_HFA03(hfa5)) / 5;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Average5_HFA05(HFA05 hfa1, HFA05 hfa2, HFA05 hfa3, HFA05 hfa4, HFA05 hfa5)
        {
            return ((Average3_HFA05(hfa1, hfa2, hfa3) * 3) + Average_HFA05(hfa4) + Average_HFA05(hfa5)) / 5;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Average5_HFA08(HFA08 hfa1, HFA08 hfa2, HFA08 hfa3, HFA08 hfa4, HFA08 hfa5)
        {
            return ((Average3_HFA08(hfa1, hfa2, hfa3) * 3) + Average_HFA08(hfa4) + Average_HFA08(hfa5)) / 5;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Average5_HFA11(HFA11 hfa1, HFA11 hfa2, HFA11 hfa3, HFA11 hfa4, HFA11 hfa5)
        {
            return ((Average3_HFA11(hfa1, hfa2, hfa3) * 3) + Average_HFA11(hfa4) + Average_HFA11(hfa5)) / 5;
        }


#if FLOAT64
        public static double
#else
        public static float
#endif
            Average5_HFA19(HFA19 hfa1, HFA19 hfa2, HFA19 hfa3, HFA19 hfa4, HFA19 hfa5)
        {
            return ((Average3_HFA19(hfa1, hfa2, hfa3) * 3) + Average_HFA19(hfa4) + Average_HFA19(hfa5)) / 5;
        }


        // --------------------------------------------------------------
        // Average8 methods
        // --------------------------------------------------------------

#if FLOAT64
        public static double
#else
        public static float
#endif
            Average8_HFA01(HFA01 hfa1, HFA01 hfa2, HFA01 hfa3, HFA01 hfa4, HFA01 hfa5, HFA01 hfa6, HFA01 hfa7, HFA01 hfa8)
        {
            return ((Average3_HFA01(hfa1, hfa2, hfa3) * 3) + (Average5_HFA01(hfa4, hfa5, hfa6, hfa7, hfa8) * 5)) / 8;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Average8_HFA02(HFA02 hfa1, HFA02 hfa2, HFA02 hfa3, HFA02 hfa4, HFA02 hfa5, HFA02 hfa6, HFA02 hfa7, HFA02 hfa8)
        {
            return ((Average3_HFA02(hfa1, hfa2, hfa3) * 3) + (Average5_HFA02(hfa4, hfa5, hfa6, hfa7, hfa8) * 5)) / 8;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Average8_HFA03(HFA03 hfa1, HFA03 hfa2, HFA03 hfa3, HFA03 hfa4, HFA03 hfa5, HFA03 hfa6, HFA03 hfa7, HFA03 hfa8)
        {
            return ((Average3_HFA03(hfa1, hfa2, hfa3) * 3) + (Average5_HFA03(hfa4, hfa5, hfa6, hfa7, hfa8) * 5)) / 8;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Average8_HFA05(HFA05 hfa1, HFA05 hfa2, HFA05 hfa3, HFA05 hfa4, HFA05 hfa5, HFA05 hfa6, HFA05 hfa7, HFA05 hfa8)
        {
            return ((Average3_HFA05(hfa1, hfa2, hfa3) * 3) + (Average5_HFA05(hfa4, hfa5, hfa6, hfa7, hfa8) * 5)) / 8;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Average8_HFA08(HFA08 hfa1, HFA08 hfa2, HFA08 hfa3, HFA08 hfa4, HFA08 hfa5, HFA08 hfa6, HFA08 hfa7, HFA08 hfa8)
        {
            return ((Average3_HFA08(hfa1, hfa2, hfa3) * 3) + (Average5_HFA08(hfa4, hfa5, hfa6, hfa7, hfa8) * 5)) / 8;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Average8_HFA11(HFA11 hfa1, HFA11 hfa2, HFA11 hfa3, HFA11 hfa4, HFA11 hfa5, HFA11 hfa6, HFA11 hfa7, HFA11 hfa8)
        {
            return ((Average3_HFA11(hfa1, hfa2, hfa3) * 3) + (Average5_HFA11(hfa4, hfa5, hfa6, hfa7, hfa8) * 5)) / 8;
        }


#if FLOAT64
        public static double
#else
        public static float
#endif
            Average8_HFA19(HFA19 hfa1, HFA19 hfa2, HFA19 hfa3, HFA19 hfa4, HFA19 hfa5, HFA19 hfa6, HFA19 hfa7, HFA19 hfa8)
        {
            return ((Average3_HFA19(hfa1, hfa2, hfa3) * 3) + (Average5_HFA19(hfa4, hfa5, hfa6, hfa7, hfa8) * 5)) / 8;
        }


        // --------------------------------------------------------------
        // Average11 methods
        // --------------------------------------------------------------

#if FLOAT64
        public static double
#else
        public static float
#endif
            Average11_HFA01(HFA01 hfa1, HFA01 hfa2, HFA01 hfa3, HFA01 hfa4, HFA01 hfa5, HFA01 hfa6, HFA01 hfa7, HFA01 hfa8, HFA01 hfa9, HFA01 hfa10, HFA01 hfa11)
        {
            return ((Average3_HFA01(hfa1, hfa2, hfa3) * 3) + (Average8_HFA01(hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11) * 8)) / 11;
        }


#if FLOAT64
        public static double
#else
        public static float
#endif
            Average11_HFA02(HFA02 hfa1, HFA02 hfa2, HFA02 hfa3, HFA02 hfa4, HFA02 hfa5, HFA02 hfa6, HFA02 hfa7, HFA02 hfa8, HFA02 hfa9, HFA02 hfa10, HFA02 hfa11)
        {
            return ((Average3_HFA02(hfa1, hfa2, hfa3) * 3) + (Average8_HFA02(hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11) * 8)) / 11;
        }


#if FLOAT64
        public static double
#else
        public static float
#endif
            Average11_HFA03(HFA03 hfa1, HFA03 hfa2, HFA03 hfa3, HFA03 hfa4, HFA03 hfa5, HFA03 hfa6, HFA03 hfa7, HFA03 hfa8, HFA03 hfa9, HFA03 hfa10, HFA03 hfa11)
        {
            return ((Average3_HFA03(hfa1, hfa2, hfa3) * 3) + (Average8_HFA03(hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11) * 8)) / 11;
        }


#if FLOAT64
        public static double
#else
        public static float
#endif
            Average11_HFA05(HFA05 hfa1, HFA05 hfa2, HFA05 hfa3, HFA05 hfa4, HFA05 hfa5, HFA05 hfa6, HFA05 hfa7, HFA05 hfa8, HFA05 hfa9, HFA05 hfa10, HFA05 hfa11)
        {
            return ((Average3_HFA05(hfa1, hfa2, hfa3) * 3) + (Average8_HFA05(hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11) * 8)) / 11;
        }


#if FLOAT64
        public static double
#else
        public static float
#endif
            Average11_HFA08(HFA08 hfa1, HFA08 hfa2, HFA08 hfa3, HFA08 hfa4, HFA08 hfa5, HFA08 hfa6, HFA08 hfa7, HFA08 hfa8, HFA08 hfa9, HFA08 hfa10, HFA08 hfa11)
        {
            return ((Average3_HFA08(hfa1, hfa2, hfa3) * 3) + (Average8_HFA08(hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11) * 8)) / 11;
        }


#if FLOAT64
        public static double
#else
        public static float
#endif
            Average11_HFA11(HFA11 hfa1, HFA11 hfa2, HFA11 hfa3, HFA11 hfa4, HFA11 hfa5, HFA11 hfa6, HFA11 hfa7, HFA11 hfa8, HFA11 hfa9, HFA11 hfa10, HFA11 hfa11)
        {
            return ((Average3_HFA11(hfa1, hfa2, hfa3) * 3) + (Average8_HFA11(hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11) * 8)) / 11;
        }


#if FLOAT64
        public static double
#else
        public static float
#endif
            Average11_HFA19(HFA19 hfa1, HFA19 hfa2, HFA19 hfa3, HFA19 hfa4, HFA19 hfa5, HFA19 hfa6, HFA19 hfa7, HFA19 hfa8, HFA19 hfa9, HFA19 hfa10, HFA19 hfa11)
        {
            return ((Average3_HFA19(hfa1, hfa2, hfa3) * 3) + (Average8_HFA19(hfa4, hfa5, hfa6, hfa7, hfa8, hfa9, hfa10, hfa11) * 8)) / 11;
        }



        // --------------------------------------------------------------
        // Average19 methods
        // --------------------------------------------------------------

#if FLOAT64
        public static double
#else
        public static float
#endif
            Average19_HFA01(HFA01 hfa1, HFA01 hfa2, HFA01 hfa3, HFA01 hfa4, HFA01 hfa5, HFA01 hfa6, HFA01 hfa7, HFA01 hfa8, HFA01 hfa9, HFA01 hfa10, HFA01 hfa11, HFA01 hfa12, HFA01 hfa13, HFA01 hfa14, HFA01 hfa15, HFA01 hfa16, HFA01 hfa17, HFA01 hfa18, HFA01 hfa19)
        {
            return ((Average8_HFA01(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8) * 8) + (Average11_HFA01(hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19) * 11)) / 19;
        }


#if FLOAT64
        public static double
#else
        public static float
#endif
            Average19_HFA02(HFA02 hfa1, HFA02 hfa2, HFA02 hfa3, HFA02 hfa4, HFA02 hfa5, HFA02 hfa6, HFA02 hfa7, HFA02 hfa8, HFA02 hfa9, HFA02 hfa10, HFA02 hfa11, HFA02 hfa12, HFA02 hfa13, HFA02 hfa14, HFA02 hfa15, HFA02 hfa16, HFA02 hfa17, HFA02 hfa18, HFA02 hfa19)
        {
            return ((Average8_HFA02(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8) * 8) + (Average11_HFA02(hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19) * 11)) / 19;
        }


#if FLOAT64
        public static double
#else
        public static float
#endif
            Average19_HFA03(HFA03 hfa1, HFA03 hfa2, HFA03 hfa3, HFA03 hfa4, HFA03 hfa5, HFA03 hfa6, HFA03 hfa7, HFA03 hfa8, HFA03 hfa9, HFA03 hfa10, HFA03 hfa11, HFA03 hfa12, HFA03 hfa13, HFA03 hfa14, HFA03 hfa15, HFA03 hfa16, HFA03 hfa17, HFA03 hfa18, HFA03 hfa19)
        {
            return ((Average8_HFA03(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8) * 8) + (Average11_HFA03(hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19) * 11)) / 19;
        }


#if FLOAT64
        public static double
#else
        public static float
#endif
            Average19_HFA05(HFA05 hfa1, HFA05 hfa2, HFA05 hfa3, HFA05 hfa4, HFA05 hfa5, HFA05 hfa6, HFA05 hfa7, HFA05 hfa8, HFA05 hfa9, HFA05 hfa10, HFA05 hfa11, HFA05 hfa12, HFA05 hfa13, HFA05 hfa14, HFA05 hfa15, HFA05 hfa16, HFA05 hfa17, HFA05 hfa18, HFA05 hfa19)
        {
            return ((Average8_HFA05(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8) * 8) + (Average11_HFA05(hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19) * 11)) / 19;
        }


#if FLOAT64
        public static double
#else
        public static float
#endif
            Average19_HFA08(HFA08 hfa1, HFA08 hfa2, HFA08 hfa3, HFA08 hfa4, HFA08 hfa5, HFA08 hfa6, HFA08 hfa7, HFA08 hfa8, HFA08 hfa9, HFA08 hfa10, HFA08 hfa11, HFA08 hfa12, HFA08 hfa13, HFA08 hfa14, HFA08 hfa15, HFA08 hfa16, HFA08 hfa17, HFA08 hfa18, HFA08 hfa19)
        {
            return ((Average8_HFA08(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8) * 8) + (Average11_HFA08(hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19) * 11)) / 19;
        }


#if FLOAT64
        public static double
#else
        public static float
#endif
            Average19_HFA11(HFA11 hfa1, HFA11 hfa2, HFA11 hfa3, HFA11 hfa4, HFA11 hfa5, HFA11 hfa6, HFA11 hfa7, HFA11 hfa8, HFA11 hfa9, HFA11 hfa10, HFA11 hfa11, HFA11 hfa12, HFA11 hfa13, HFA11 hfa14, HFA11 hfa15, HFA11 hfa16, HFA11 hfa17, HFA11 hfa18, HFA11 hfa19)
        {
            return ((Average8_HFA11(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8) * 8) + (Average11_HFA11(hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19) * 11)) / 19;
        }


#if FLOAT64
        public static double
#else
        public static float
#endif
            Average19_HFA19(HFA19 hfa1, HFA19 hfa2, HFA19 hfa3, HFA19 hfa4, HFA19 hfa5, HFA19 hfa6, HFA19 hfa7, HFA19 hfa8, HFA19 hfa9, HFA19 hfa10, HFA19 hfa11, HFA19 hfa12, HFA19 hfa13, HFA19 hfa14, HFA19 hfa15, HFA19 hfa16, HFA19 hfa17, HFA19 hfa18, HFA19 hfa19)
        {
            return ((Average8_HFA19(hfa1, hfa2, hfa3, hfa4, hfa5, hfa6, hfa7, hfa8) * 8) + (Average11_HFA19(hfa9, hfa10, hfa11, hfa12, hfa13, hfa14, hfa15, hfa16, hfa17, hfa18, hfa19) * 11)) / 19;
        }




        // --------------------------------------------------------------
        // Add01 methods
        // --------------------------------------------------------------

#if FLOAT64
        public static double
#else
        public static float
#endif
            Add01_HFA01(HFA01 hfa1, float v1, HFA01 hfa2, int v2, HFA01 hfa3, short v3, double v4, HFA01 hfa4, HFA01 hfa5, float v5, long v6, float v7, HFA01 hfa6, float v8, HFA01 hfa7)
        {
            return (Sum_HFA01(hfa1) + Sum_HFA01(hfa2) + Sum_HFA01(hfa3) + Sum_HFA01(hfa4) + Sum_HFA01(hfa5) + Sum_HFA01(hfa6) + Sum_HFA01(hfa7)) + (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Add01_HFA02(HFA02 hfa1, float v1, HFA02 hfa2, int v2, HFA02 hfa3, short v3, double v4, HFA02 hfa4, HFA02 hfa5, float v5, long v6, float v7, HFA02 hfa6, float v8, HFA02 hfa7)
        {
            return (Sum_HFA02(hfa1) + Sum_HFA02(hfa2) + Sum_HFA02(hfa3) + Sum_HFA02(hfa4) + Sum_HFA02(hfa5) + Sum_HFA02(hfa6) + Sum_HFA02(hfa7)) + (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Add01_HFA03(HFA03 hfa1, float v1, HFA03 hfa2, int v2, HFA03 hfa3, short v3, double v4, HFA03 hfa4, HFA03 hfa5, float v5, long v6, float v7, HFA03 hfa6, float v8, HFA03 hfa7)
        {
            return (Sum_HFA03(hfa1) + Sum_HFA03(hfa2) + Sum_HFA03(hfa3) + Sum_HFA03(hfa4) + Sum_HFA03(hfa5) + Sum_HFA03(hfa6) + Sum_HFA03(hfa7)) + (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Add01_HFA05(HFA05 hfa1, float v1, HFA05 hfa2, int v2, HFA05 hfa3, short v3, double v4, HFA05 hfa4, HFA05 hfa5, float v5, long v6, float v7, HFA05 hfa6, float v8, HFA05 hfa7)
        {
            return (Sum_HFA05(hfa1) + Sum_HFA05(hfa2) + Sum_HFA05(hfa3) + Sum_HFA05(hfa4) + Sum_HFA05(hfa5) + Sum_HFA05(hfa6) + Sum_HFA05(hfa7)) + (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Add01_HFA08(HFA08 hfa1, float v1, HFA08 hfa2, int v2, HFA08 hfa3, short v3, double v4, HFA08 hfa4, HFA08 hfa5, float v5, long v6, float v7, HFA08 hfa6, float v8, HFA08 hfa7)
        {
            return (Sum_HFA08(hfa1) + Sum_HFA08(hfa2) + Sum_HFA08(hfa3) + Sum_HFA08(hfa4) + Sum_HFA08(hfa5) + Sum_HFA08(hfa6) + Sum_HFA08(hfa7)) + (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Add01_HFA11(HFA11 hfa1, float v1, HFA11 hfa2, int v2, HFA11 hfa3, short v3, double v4, HFA11 hfa4, HFA11 hfa5, float v5, long v6, float v7, HFA11 hfa6, float v8, HFA11 hfa7)
        {
            return (Sum_HFA11(hfa1) + Sum_HFA11(hfa2) + Sum_HFA11(hfa3) + Sum_HFA11(hfa4) + Sum_HFA11(hfa5) + Sum_HFA11(hfa6) + Sum_HFA11(hfa7)) + (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Add01_HFA19(HFA19 hfa1, float v1, HFA19 hfa2, int v2, HFA19 hfa3, short v3, double v4, HFA19 hfa4, HFA19 hfa5, float v5, long v6, float v7, HFA19 hfa6, float v8, HFA19 hfa7)
        {
            return (Sum_HFA19(hfa1) + Sum_HFA19(hfa2) + Sum_HFA19(hfa3) + Sum_HFA19(hfa4) + Sum_HFA19(hfa5) + Sum_HFA19(hfa6) + Sum_HFA19(hfa7)) + (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Add01_HFA00(HFA03 hfa1, float v1, HFA02 hfa2, int v2, HFA19 hfa3, short v3, double v4, HFA05 hfa4, HFA08 hfa5, float v5, long v6, float v7, HFA11 hfa6, float v8, HFA01 hfa7)
        {
            return (Sum_HFA03(hfa1) + Sum_HFA02(hfa2) + Sum_HFA19(hfa3) + Sum_HFA05(hfa4) + Sum_HFA08(hfa5) + Sum_HFA11(hfa6) + Sum_HFA01(hfa7)) + (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8;
        }


        // --------------------------------------------------------------
        // Add02 methods
        // --------------------------------------------------------------

#if FLOAT64
        public static double
#else
        public static float
#endif
            Add02_HFA01(HFA01 hfa1, HFA01 hfa2, long v1, short v2, float v3, int v4, double v5, float v6, HFA01 hfa3, double v7, float v8, HFA01 hfa4, short v9, HFA01 hfa5, float v10, HFA01 hfa6, HFA01 hfa7)
        {
            return (Sum_HFA01(hfa1) + Sum_HFA01(hfa2) + Sum_HFA01(hfa3) + Sum_HFA01(hfa4) + Sum_HFA01(hfa5) + Sum_HFA01(hfa6) + Sum_HFA01(hfa7)) + (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 + (float)v9 + (float)v10;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Add02_HFA02(HFA02 hfa1, HFA02 hfa2, long v1, short v2, float v3, int v4, double v5, float v6, HFA02 hfa3, double v7, float v8, HFA02 hfa4, short v9, HFA02 hfa5, float v10, HFA02 hfa6, HFA02 hfa7)
        {
            return (Sum_HFA02(hfa1) + Sum_HFA02(hfa2) + Sum_HFA02(hfa3) + Sum_HFA02(hfa4) + Sum_HFA02(hfa5) + Sum_HFA02(hfa6) + Sum_HFA02(hfa7)) + (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 + (float)v9 + (float)v10;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Add02_HFA03(HFA03 hfa1, HFA03 hfa2, long v1, short v2, float v3, int v4, double v5, float v6, HFA03 hfa3, double v7, float v8, HFA03 hfa4, short v9, HFA03 hfa5, float v10, HFA03 hfa6, HFA03 hfa7)
        {
            return (Sum_HFA03(hfa1) + Sum_HFA03(hfa2) + Sum_HFA03(hfa3) + Sum_HFA03(hfa4) + Sum_HFA03(hfa5) + Sum_HFA03(hfa6) + Sum_HFA03(hfa7)) + (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 + (float)v9 + (float)v10;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Add02_HFA05(HFA05 hfa1, HFA05 hfa2, long v1, short v2, float v3, int v4, double v5, float v6, HFA05 hfa3, double v7, float v8, HFA05 hfa4, short v9, HFA05 hfa5, float v10, HFA05 hfa6, HFA05 hfa7)
        {
            return (Sum_HFA05(hfa1) + Sum_HFA05(hfa2) + Sum_HFA05(hfa3) + Sum_HFA05(hfa4) + Sum_HFA05(hfa5) + Sum_HFA05(hfa6) + Sum_HFA05(hfa7)) + (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 + (float)v9 + (float)v10;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Add02_HFA08(HFA08 hfa1, HFA08 hfa2, long v1, short v2, float v3, int v4, double v5, float v6, HFA08 hfa3, double v7, float v8, HFA08 hfa4, short v9, HFA08 hfa5, float v10, HFA08 hfa6, HFA08 hfa7)
        {
            return (Sum_HFA08(hfa1) + Sum_HFA08(hfa2) + Sum_HFA08(hfa3) + Sum_HFA08(hfa4) + Sum_HFA08(hfa5) + Sum_HFA08(hfa6) + Sum_HFA08(hfa7)) + (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 + (float)v9 + (float)v10;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Add02_HFA11(HFA11 hfa1, HFA11 hfa2, long v1, short v2, float v3, int v4, double v5, float v6, HFA11 hfa3, double v7, float v8, HFA11 hfa4, short v9, HFA11 hfa5, float v10, HFA11 hfa6, HFA11 hfa7)
        {
            return (Sum_HFA11(hfa1) + Sum_HFA11(hfa2) + Sum_HFA11(hfa3) + Sum_HFA11(hfa4) + Sum_HFA11(hfa5) + Sum_HFA11(hfa6) + Sum_HFA11(hfa7)) + (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 + (float)v9 + (float)v10;
        }


#if FLOAT64
        public static double
#else
        public static float
#endif
            Add02_HFA19(HFA19 hfa1, HFA19 hfa2, long v1, short v2, float v3, int v4, double v5, float v6, HFA19 hfa3, double v7, float v8, HFA19 hfa4, short v9, HFA19 hfa5, float v10, HFA19 hfa6, HFA19 hfa7)
        {
            return (Sum_HFA19(hfa1) + Sum_HFA19(hfa2) + Sum_HFA19(hfa3) + Sum_HFA19(hfa4) + Sum_HFA19(hfa5) + Sum_HFA19(hfa6) + Sum_HFA19(hfa7)) + (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 + (float)v9 + (float)v10;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Add02_HFA00(HFA01 hfa1, HFA05 hfa2, long v1, short v2, float v3, int v4, double v5, float v6, HFA03 hfa3, double v7, float v8, HFA11 hfa4, short v9, HFA19 hfa5, float v10, HFA08 hfa6, HFA02 hfa7)
        {
            return (Sum_HFA01(hfa1) + Sum_HFA05(hfa2) + Sum_HFA03(hfa3) + Sum_HFA11(hfa4) + Sum_HFA19(hfa5) + Sum_HFA08(hfa6) + Sum_HFA02(hfa7)) + (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 + (float)v9 + (float)v10;
        }


        // --------------------------------------------------------------
        // Add03 methods
        // --------------------------------------------------------------

#if FLOAT64
        public static double
#else
        public static float
#endif
            Add03_HFA01(float v1, sbyte v2, HFA01 hfa1, double v3, sbyte v4, HFA01 hfa2, long v5, short v6, int v7, HFA01 hfa3, HFA01 hfa4, float v8, HFA01 hfa5, float v9, HFA01 hfa6, float v10, HFA01 hfa7)
        {
            return (Sum_HFA01(hfa1) + Sum_HFA01(hfa2) + Sum_HFA01(hfa3) + Sum_HFA01(hfa4) + Sum_HFA01(hfa5) + Sum_HFA01(hfa6) + Sum_HFA01(hfa7)) + (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 + (float)v9 + (float)v10;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Add03_HFA02(float v1, sbyte v2, HFA02 hfa1, double v3, sbyte v4, HFA02 hfa2, long v5, short v6, int v7, HFA02 hfa3, HFA02 hfa4, float v8, HFA02 hfa5, float v9, HFA02 hfa6, float v10, HFA02 hfa7)
        {
            return (Sum_HFA02(hfa1) + Sum_HFA02(hfa2) + Sum_HFA02(hfa3) + Sum_HFA02(hfa4) + Sum_HFA02(hfa5) + Sum_HFA02(hfa6) + Sum_HFA02(hfa7)) + (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 + (float)v9 + (float)v10;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Add03_HFA03(float v1, sbyte v2, HFA03 hfa1, double v3, sbyte v4, HFA03 hfa2, long v5, short v6, int v7, HFA03 hfa3, HFA03 hfa4, float v8, HFA03 hfa5, float v9, HFA03 hfa6, float v10, HFA03 hfa7)
        {
            return (Sum_HFA03(hfa1) + Sum_HFA03(hfa2) + Sum_HFA03(hfa3) + Sum_HFA03(hfa4) + Sum_HFA03(hfa5) + Sum_HFA03(hfa6) + Sum_HFA03(hfa7)) + (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 + (float)v9 + (float)v10;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Add03_HFA05(float v1, sbyte v2, HFA05 hfa1, double v3, sbyte v4, HFA05 hfa2, long v5, short v6, int v7, HFA05 hfa3, HFA05 hfa4, float v8, HFA05 hfa5, float v9, HFA05 hfa6, float v10, HFA05 hfa7)
        {
            return (Sum_HFA05(hfa1) + Sum_HFA05(hfa2) + Sum_HFA05(hfa3) + Sum_HFA05(hfa4) + Sum_HFA05(hfa5) + Sum_HFA05(hfa6) + Sum_HFA05(hfa7)) + (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 + (float)v9 + (float)v10;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Add03_HFA08(float v1, sbyte v2, HFA08 hfa1, double v3, sbyte v4, HFA08 hfa2, long v5, short v6, int v7, HFA08 hfa3, HFA08 hfa4, float v8, HFA08 hfa5, float v9, HFA08 hfa6, float v10, HFA08 hfa7)
        {
            return (Sum_HFA08(hfa1) + Sum_HFA08(hfa2) + Sum_HFA08(hfa3) + Sum_HFA08(hfa4) + Sum_HFA08(hfa5) + Sum_HFA08(hfa6) + Sum_HFA08(hfa7)) + (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 + (float)v9 + (float)v10;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Add03_HFA11(float v1, sbyte v2, HFA11 hfa1, double v3, sbyte v4, HFA11 hfa2, long v5, short v6, int v7, HFA11 hfa3, HFA11 hfa4, float v8, HFA11 hfa5, float v9, HFA11 hfa6, float v10, HFA11 hfa7)
        {
            return (Sum_HFA11(hfa1) + Sum_HFA11(hfa2) + Sum_HFA11(hfa3) + Sum_HFA11(hfa4) + Sum_HFA11(hfa5) + Sum_HFA11(hfa6) + Sum_HFA11(hfa7)) + (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 + (float)v9 + (float)v10;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Add03_HFA19(float v1, sbyte v2, HFA19 hfa1, double v3, sbyte v4, HFA19 hfa2, long v5, short v6, int v7, HFA19 hfa3, HFA19 hfa4, float v8, HFA19 hfa5, float v9, HFA19 hfa6, float v10, HFA19 hfa7)
        {
            return (Sum_HFA19(hfa1) + Sum_HFA19(hfa2) + Sum_HFA19(hfa3) + Sum_HFA19(hfa4) + Sum_HFA19(hfa5) + Sum_HFA19(hfa6) + Sum_HFA19(hfa7)) + (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 + (float)v9 + (float)v10;
        }

#if FLOAT64
        public static double
#else
        public static float
#endif
            Add03_HFA00(float v1, sbyte v2, HFA08 hfa1, double v3, sbyte v4, HFA19 hfa2, long v5, short v6, int v7, HFA03 hfa3, HFA01 hfa4, float v8, HFA11 hfa5, float v9, HFA02 hfa6, float v10, HFA05 hfa7)
        {
            return (Sum_HFA08(hfa1) + Sum_HFA19(hfa2) + Sum_HFA03(hfa3) + Sum_HFA01(hfa4) + Sum_HFA11(hfa5) + Sum_HFA02(hfa6) + Sum_HFA05(hfa7)) + (float)v1 + (float)v2 + (float)v3 + (float)v4 + (float)v5 + (float)v6 + (float)v7 + (float)v8 + (float)v9 + (float)v10;
        }
    }
}
