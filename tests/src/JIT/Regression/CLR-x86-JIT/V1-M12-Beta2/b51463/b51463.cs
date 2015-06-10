// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class BB
    {
        public static long[] m_alStatic1 = null;
    }

    struct AA
    {
        bool Method1() { return false; }

        static void Method4(int param1, ref uint param2)
        {
            AA[] local3 = null;
            while (local3[0].Method1())
            {
                BB.m_alStatic1[param1] = param1 | param2;
            }
        }

        static int Main()
        {
            try
            {
                uint n = 0;
                Method4(0, ref n);
                return 101;
            }
            catch (NullReferenceException)
            {
                return 100;
            }
        }
    }
}
