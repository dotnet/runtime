// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class AA
    {
        public int m_nField2 = 51;
        public static int[] Method1(int param1)
        {
            return null;
        }
        public static double[] Static3(object[] param1, int param2)
        {
            uint[] local5 = new uint[7];
            uint[] local6 = new uint[7];
            return BB.m_adStatic1;
        }
        static int Main()
        {
            try
            {
                Static3(null, Method1((int)Static3(null, new AA().m_nField2 + 2)[0])[0]);
            }
            catch (Exception)
            {
            }
            return 100;
        }
    }

    class BB
    {
        public static double[] m_adStatic1 = (new double[7]);
    }
}
