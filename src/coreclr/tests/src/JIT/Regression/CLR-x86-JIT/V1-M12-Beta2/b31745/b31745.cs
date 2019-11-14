// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    class AA
    {
        public static uint[] m_auStatic1 = new uint[7];

        public static int[] Method1(uint[] param1, ref float param2, __arglist)
        { return null; }

        public static int[] Test(ref double[] param1, ref float[] param3)
        { return Method1(m_auStatic1, ref param3[2], __arglist()); }

        static int Main()
        {
            double[] ad = new double[16];
            float[] af = new float[16];
            Test(ref ad, ref af);
            return 100;
        }
    }
}
