// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class AA
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
        [Fact]
        public static int TestEntryPoint()
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
