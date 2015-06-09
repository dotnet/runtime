// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/*
COMPILE THIS WITH OPTIMIZATION TURNED OFF:
coolc /o- bug.cs
*/
namespace Test
{
    using System;

    class AA
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

        static int Main()
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
