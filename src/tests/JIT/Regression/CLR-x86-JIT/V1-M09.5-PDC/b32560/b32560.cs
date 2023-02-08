// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class AA
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
        [Fact]
        public static int TestEntryPoint()
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
