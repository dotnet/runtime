// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
namespace Test
{
    using System;

    public enum TestEnum
    {
        red = 1,
        green = 2,
        blue = 4,
    }

    // In the original program ApplicationException was used, but for the
    // the purposes of this test any exception other than the one thrown works.
    class OtherException : Exception
    {
    }

    public class BB
    {
    }

    public struct AA
    {
        public uint Method1(int[] param1)
        {
            uint[] local3 = new uint[1];
            long local4 = App.m_lFwd1;
            {
                float[] local5 = new float[] { 108.0f };
                {
                    byte[] local7 = new byte[] { };
                    sbyte local8 = App.m_sbFwd3;
                    long local9 = App.m_lFwd1;
                    sbyte[] local10 = (new sbyte[4]);
                    App.m_asbFwd6 = (new sbyte[111]);
                    try
                    {
                        sbyte[] local11 = new sbyte[] { };
                        throw new Exception();
                    }
                    catch (OtherException) { }
                }
                {
                    sbyte local7 = App.m_sbFwd3;
                    String[] local8 = new String[] { };
                    char[] local9 = (new char[81]);
                    BB local10 = new BB();
                    object[] local11 = new object[] { null, null, null, null, null };
                    double[] local12 = new double[] { 109.0 };
                    {
                        BB[] local13 = (new BB[22]);
                        sbyte local14 = App.m_sbFwd3;
                        ulong[] local15 = App.m_aulFwd7;
                        for (long[] b244656 = new long[] { local4 }; App.m_bFwd2;)
                        {
                            int[] local16 = (new int[30]);
                            TestEnum local17 = new TestEnum();
                            BB local18 = new BB();
                            float local19 = 55.0f;
                            BB local20 = new BB();
                            local15 = (new ulong[77]);
                        }
                    }
                }
                if (App.m_bFwd2)
                {
                    try
                    {
                        TestEnum local7 = new TestEnum();
                        String local8 = "109";
                        bool local9 = false;
                        float local10 = 110.0f;
                        long[] local11 = App.m_alFwd8;
                        TestEnum[] local12 = new TestEnum[] { new TestEnum(), new TestEnum() };
                        byte[] local13 = App.m_abFwd9;
                        throw new IndexOutOfRangeException();
                    }
                    finally
                    {
                        Array[] local7 = App.m_axFwd10;
                        String local8 = "122";
                        float local9 = 22.0f;
                        int[] local10 = (new int[69]);
                        String[] local11 = (new String[75]);
                        ulong[] local12 = (new ulong[81]);
                        uint local13 = 67u;
                        while (App.m_bFwd2)
                        {
                            int[] local14 = new int[] { 1, 50, 79 };
                            byte[] local15 = App.m_asiFwd11;
                            ulong[] local16 = (new ulong[20]);
                        }
                    }
                }
            }
            {
                int local5 = 18;
                object local6 = null;
                ulong[] local7 = App.m_aulFwd7;
                TestEnum local8 = new TestEnum();
                long[] local9 = App.m_alFwd8;
                sbyte[] local10 = App.m_asuFwd12;
                try
                {
                    char[] local11 = new char[] { '\x25' };
                    byte[] local12 = App.m_asiFwd11;
                    double local13 = (0.0);
                    throw new NullReferenceException();
                }
                catch (Exception)
                {
                }
            }
            return 72u;
        }
    }

    public class App
    {
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                new AA().Method1(new int[1]);
                return 1;
            }
            catch (Exception)
            {
                return 100;
            }
        }

        public static long m_lFwd1;
        public static bool m_bFwd2;
        public static sbyte m_sbFwd3;
        public static int m_iFwd4;
        public static char m_cFwd5;
        public static sbyte[] m_asbFwd6;
        public static ulong[] m_aulFwd7;
        public static long[] m_alFwd8;
        public static byte[] m_abFwd9;
        public static Array[] m_axFwd10;
        public static byte[] m_asiFwd11;
        public static sbyte[] m_asuFwd12;
        public static sbyte m_suFwd13;
    }
}
