// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class AA
    {
        public static uint[] m_auStatic1 = new uint[7];

        public static int[] Method1(uint[] param1, ref float param2, __arglist)
        { return null; }

        public static int[] Test(ref double[] param1, ref float[] param3)
        { return Method1(m_auStatic1, ref param3[2], __arglist()); }

        [Fact]
        public static int TestEntryPoint()
        {
            double[] ad = new double[16];
            float[] af = new float[16];
            Test(ref ad, ref af);
            return 100;
        }
    }
}
