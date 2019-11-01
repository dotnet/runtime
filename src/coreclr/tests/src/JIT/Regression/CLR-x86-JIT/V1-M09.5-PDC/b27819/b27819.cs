// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    class AA
    {
        bool m_bFlag = false;
        static int[] m_anStatic2;
        static void GoToEnd() { throw new Exception(); }

        static bool[] Method1()
        {
            int local4 = 0;
            try
            {
                do
                {
                    m_anStatic2 = null;
                    while (new AA().m_bFlag)
                    {
                        while (new AA().m_bFlag)
                            GC.Collect();
                    }
                    new AA();
                    while (local4 == 1)
                        GC.Collect();
                } while (false);

                GC.Collect();
                while (true)
                    GoToEnd();
            }
            catch (Exception)
            {
            }
            return new bool[7];
        }

        public static int Main()
        {
            Method1();
            return 100;
        }
    }
}
