// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
