// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
/*
COMPILE THIS WITH OPTIMIZATION TURNED OFF:
coolc /o- bug.cs
*/
namespace Test
{
    using System;

    public class AA
    {
        static int m_nStatic1 = 0;

        static void BlowUp() { throw new Exception(); }

        static void Method1(int[] param1)
        {
            float[] local3 = new float[2];
            for (; true; param1 = param1)
            {
                if (false)
                    GC.Collect();
                if (m_nStatic1 >= param1[2])
                    BlowUp();
            }
        }

        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                Method1(null);
            }
            catch (Exception) { }
            return 100;
        }
    }
}
