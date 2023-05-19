// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
namespace Test
{
    using System;
    using System.Collections;

    public enum TestEnum
    {
        red = 1,
        green = 2,
        blue = 4,
    }

    public struct AA
    {
        public Array m_xField1;
        public object m_xField2;
        public long[] m_alField3;
        public sbyte[] m_asbField4;
        public int m_iField5;
        public static TestEnum[] m_axStatic1;

        public void Break() { throw new Exception(); }
        public ulong Method1()
        {
            String[] local1 = new String[] { };
            byte[] local2 = new byte[] { };
            bool local3 = true;
            TypedReference local4 = __makeref(App.m_xFwd1);
            String[] local5 = new String[] { "120", "70", "105" };
            if (local3)
                while (local3)
                {
                    TestEnum[] local6 = new TestEnum[] { TestEnum.red };
                    do
                    {
                        sbyte[] local7 = (new sbyte[117]);
                        sbyte local8 = App.m_suFwd2;
                        double[] local9 = new double[] { 72.0 };
                        char[] local10 = (new char[118]);
                        int[] local11 = new int[] { 98, 126, 35 };
                        for (new TestEnum(); local3; new sbyte())
                        {
                            int[] local12 = new int[] { };
                            String[] local13 = (new String[9]);
                            ulong[] local14 = (new ulong[56]);
                            App.m_asuFwd3 = (new sbyte[116]);
                            Break();
                        }
                        try
                        {
                            double local12 = 48.0;
                            TestEnum[] local13 = AA.m_axStatic1;
                            char[] local14 = (new char[83]);
                            sbyte local15 = App.m_suFwd2;
                            Array[] local16 = (new Array[73]);
                            throw new IndexOutOfRangeException();
                        }
                        catch (IndexOutOfRangeException)
                        {
                            byte[] local12 = (new byte[19]);
                            sbyte[] local13 = App.m_asbFwd4;
                            TestEnum local14 = TestEnum.red;
                            return App.m_ulFwd5;
                        }
                    }
                    while (local3);
                }
            else
                goto label1;
            while (local3)
            {
                ulong local6 = App.m_ulFwd5;
                object local7 = null;
                double[] local8 = (new double[71]);
                byte local9 = App.m_bFwd6;
                AA[] local10 = (new AA[47]);
                do
                {
                    object local11 = null;
                    float local12 = 72.0f;
                    int[] local13 = new int[] { 76 };
                    int local14 = 23;
                    for (local14 = local14; local3; new double())
                    {
                        int[] local15 = new int[] { 91, 54 };
                        byte[] local16 = (new byte[14]);
                        double local17 = 94.0;
                        TestEnum local18 = 0;
                        local10 = local10;
                    }
                }
                while (local3);
            }
            if (local3)
                try
                {
                    float[] local6 = new float[] { 113.0f, 23.0f };
                    sbyte local7 = App.m_suFwd2;
                    TestEnum[] local8 = AA.m_axStatic1;
                    uint local9 = 1u;
                    throw new InvalidOperationException();
                }
                finally
                {
                    double local6 = 61.0;
                    object local7 = null;
                    uint[] local8 = new uint[] { 38u };
                    char[] local9 = (new char[17]);
                    for (App.m_iFwd7 = App.m_iFwd7; local3; local7 = local7)
                    {
                        char[] local10 = new char[] { };
                        sbyte[] local11 = (new sbyte[39]);
                        object[] local12 = (new object[50]);
                        Array[] local13 = App.m_axFwd8;
                        local11 = local11;
                    }
                }
            label1:
            return App.m_ulFwd5;
        }
    }

    public class App
    {
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                new AA().Method1();
            }
            catch (Exception)
            {
                Console.WriteLine("Passed.");
                return 100;
            }
            return 101;
        }
        public static AA m_xFwd1;
        public static sbyte m_suFwd2;
        public static sbyte[] m_asuFwd3;
        public static sbyte[] m_asbFwd4;
        public static ulong m_ulFwd5;
        public static byte m_bFwd6;
        public static int m_iFwd7;
        public static Array[] m_axFwd8;
        public static char m_cFwd9;
        public static bool m_bFwd10;
        public static byte m_siFwd11;
        public static long m_lFwd12;
        public static ulong[] m_aulFwd13;
        public static byte[] m_asiFwd14;
        public static object m_xFwd15;
        public static String m_xFwd16;
        public static double m_dFwd17;
        public static Array m_xFwd18;
        public static float[] m_afFwd19;
        public static long[] m_alFwd20;
    }
}
