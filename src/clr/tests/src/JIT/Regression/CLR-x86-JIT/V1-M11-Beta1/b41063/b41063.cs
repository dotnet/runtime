// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    class AA
    {
        public static object m_xStatic3 = null;

        public int[] Method1() { return null; }
        public bool[] Method2() { return null; }
        public static bool[] Static3() { return null; }
        public static int[] Static1(bool param1, bool[] param3, int param5)
        { return null; }
    }

    struct BB
    {
        static AA[] m_axStatic1;
        static int m_nForward5;

        int Method1() { return 0; }

        int Method4(uint param1, double param2, long param3)
        { return new BB().Method1(); }

        static int Main()
        {
            try
            {
                AA.Static1(
                    AA.Static3()[100],
                    BB.m_axStatic1[(int)AA.m_xStatic3].Method2(),
                    BB.m_axStatic1[90].Method1()[0]
                );
                return new BB().Method4((uint)(3l * m_nForward5), 0.0d, 100);
            }
            catch (NullReferenceException) { return 100; }
        }
    }
}
