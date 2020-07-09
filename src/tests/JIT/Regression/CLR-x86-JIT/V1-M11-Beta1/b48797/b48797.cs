// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
