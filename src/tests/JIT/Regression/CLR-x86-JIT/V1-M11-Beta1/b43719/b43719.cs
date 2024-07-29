// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public struct BB
    {
        static uint m_uForward4;
        static float[] m_afForward3;
        static long Static1(ref bool param1, ref bool param2) { return new BB().m_nField1; }
        static float Static2() { return 0.0f; }

        int m_nField1;
        double[] m_adField5;
        bool Method2() { return true; }
        void Method3(long param1, double[] param2, BB[] param4, float param5)
        { while (param4[0].Method2()) { } }

        static void Main1()
        {
            bool[] ab = new bool[7];
            while (ab[9])
            {
                BB[] bb = new BB[7];
                int N = -9;
                while (bb[0].Method2())
                    new BB().Method3(Static1(ref ab[N], ref ab[N]), bb[N].m_adField5, bb, Static2());
            }
        }
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                Main1();
                return -1;
            }
            catch (IndexOutOfRangeException)
            {
                return 100;
            }
        }
    }
}
