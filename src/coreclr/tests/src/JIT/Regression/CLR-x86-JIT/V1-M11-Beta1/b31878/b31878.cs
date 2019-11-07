// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    class AA
    {
        float m_fField1 = 426.19f;
        static float Method1(ref object[] param1, int param2, ref bool[] param3, double[] param4)
        {
            while (param2 > (int)param4[2])
            {
                do
                {
                } while (210.11f == (new AA().m_fField1 - (float)param4[2]) +
                                    ((float)param4[2] + (float)param4[2]));
            }
            return 0.0f;
        }
        static int Main()
        {
            try
            {
                bool[] ab = null;
                object[] ao = null;
                Method1(ref ao, 0, ref ab, null);
            }
            catch (NullReferenceException)
            {
                return 100;
            }
            return -1;
        }
    }
}
