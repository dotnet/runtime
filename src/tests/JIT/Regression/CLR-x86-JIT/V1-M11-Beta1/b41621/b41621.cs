// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    struct AA
    {
        private ulong m_ulDummyField2;
        private ulong m_ulDummyField3;
        private object[] m_axDummyField4;
        private bool[] m_abDummyField5;

        public double m_dField1;

        public uint Method1(uint[] param1, long[] param2, ulong[] param3, uint param4)
        {
            return 0;
        }
    }

    class BB
    {
        public uint m_uField2 = 141u;
        public static object m_xStatic1 = null;
        public static uint m_uForward4;

        void Method2(__arglist) { }

        static void Static1(ref uint[] param1)
        {
            new BB().Method2(
                __arglist(
                    new AA().m_dField1,
                    (int)m_xStatic1,
                    (float)m_uForward4 * (float)(new AA().Method1(param1, null, null, 0u)
            )));
            new AA().Method1(
                param1,
                new long[4],
                new ulong[4],
                new AA().Method1(param1, new long[4], new ulong[4], new BB().m_uField2));
        }
        static int Main()
        {
            try
            {
                uint[] au = null;
                Static1(ref au);
            }
            catch (Exception) { }
            return 100;
        }
    }
}
