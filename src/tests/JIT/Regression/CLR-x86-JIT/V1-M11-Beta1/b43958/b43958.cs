// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    class AA
    {
        private static long[] m_alDummyStaticField = null;
        public int[] m_anField2 = new int[7];

        public virtual int[] Method2(object[] param1) { return null; }
        public static void Static1(ulong[] param1, ref double param2, int[] param3,
                                int[] param4, long param5) { }
        public static ulong[] Static3(ref double param1, ref ulong param2) { return new ulong[7]; }
    }

    struct BB
    {
        public AA[] Method2(object param1, long param2, object[] param3,
                    ulong[] param4, double[] param5, double param6) { return new AA[7]; }
    }

    public class App
    {
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                AA.Static1(
                    AA.Static3(ref m_d, ref AA.Static3(ref m_d, ref m_ul)[1000]),
                    ref m_d,
                    new BB().Method2(m_o, m_l, m_ao, m_aul, m_ad, 0.0)[1000].m_anField2,
                    new BB().Method2(m_o, m_l, m_ao, m_aul, m_ad, 0.0)[(int)m_o].Method2(new object[7]),
                    (long)(object)m_f - (39u + (uint)m_n)
                    );
            }
            catch (Exception) { }
            return 100;
        }

        static object m_o;
        static long m_l;
        static object[] m_ao;
        static ulong[] m_aul;
        static double[] m_ad;
        static double m_d;
        static ulong m_ul;
        static float m_f;
        static int m_n;
    }
}
