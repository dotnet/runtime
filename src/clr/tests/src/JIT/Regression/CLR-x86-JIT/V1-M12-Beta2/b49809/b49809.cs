// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    struct AA
    {
        int[] m_an;

        static bool Test1(int[] param1) { return false; }

        static int[] Test(ref AA[] param1)
        {
            object P = null;
            while (Test1(null))
            {
                do
                {
                    if (Test1((int[])P))
                        Test1(param1[200].m_an);
                } while (Test1((int[])P));
            }
            return param1[0].m_an;
        }

        static int Main()
        {
            try
            {
                AA[] ax = null;
                Test(ref ax);
            }
            catch (NullReferenceException)
            {
                return 100;
            }
            return 1;
        }
    }
}
