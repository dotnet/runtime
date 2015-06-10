// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class BB
    {
        static object m_xForward2;

        static void Method1(float param1, double[] ad) { }

        static int Main()
        {
            float[] local3 = new float[2];
            try
            {
                do
                {
                    Method1(local3[3], (double[])m_xForward2);
                } while (m_xForward2 == null);
            }
            catch (Exception)
            {
                Method1(local3[0], (double[])m_xForward2);
            }
            return 100;
        }
    }
}
