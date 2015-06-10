// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    struct AA
    {
        float[] m_af1;

        static long m_l;
        static float[] m_af;
        static bool[] m_ab;

        public static uint[] Static1(float p1, ref float[] p2, float[] p3,
                float[] p4, object p5, object p6)
        {
            long local8 = 142l;
            if (p4[2] == 0.0f)
                p2 = null;
            else
                m_l = 45l;
            return null;
        }

        ulong Method1(AA p1, uint[] p2, ref float p4, ref float[] p5, long p6) { return 0; }
        long Method4(long p1) { return 0; }

        static int Main()
        {
            try
            {
                Console.WriteLine("Testing AA::Static1");
                AA.Static1(
                    0.0f,
                    ref m_af,
                    new AA().m_af1,
                    m_af,
                    (object)(new AA().Method1(
                        new AA(),
                        AA.Static1(0.0f, ref m_af, null, m_af, null, null),
                        ref m_af[2],
                        ref m_af,
                        new AA().Method4(m_l))),
                    (object)(new AA().Method1(
                        new AA(),
                        AA.Static1(0.0f, ref m_af, m_af, m_af, null, null),
                        ref m_af[2],
                        ref m_af,
                        0)));
            }
            catch (NullReferenceException) { }
            return 100;
        }
    }
}
