// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class AA
    {
        double[] m_adField1 = new double[7];
        float m_fField3 = 366.56f;

        static void Method2(bool[] param1, uint[] param2, float param3, object[] param4, int param5)
        {
            do
            {
                while ((int)(new AA().m_adField1[2]) <= (int)param2[2])
                {
                    param5 = (int)param4[2];

                    do
                    {
                    } while (param5 != (uint)(new AA().m_fField3));

                    do
                    {
                    } while (param5 > 0);
                    return;
                }
            } while (param1[2]);
        }
        static int Main()
        {
            try
            {
                Method2(null, null, 0.0f, null, 22);
            }
            catch (NullReferenceException) { return 100; }
            return 1;
        }
    }
}
