// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Test
{
    using System;

    internal class AA
    {
        private object[] _axField1 = new object[7];
        private static AA[] s_axForward1;
        private static uint[] s_auForward2;

        private static float[] Static1(ref uint[] param1)
        {
            uint local3 = 200u;
            while ((bool)(new AA()._axField1[2]))
            {
                GC.Collect();
                GC.Collect();
                s_axForward1 = new AA[7];
                GC.Collect();
                while ((bool)new AA()._axField1[2])
                {
                }
                for (; (249u <= ((24 - 71) - ((int)(local3)))); new AA())
                {
                }
            }
            GC.Collect();
            GC.Collect();
            GC.Collect();
            GC.Collect();
            GC.Collect();
            return new float[7];
        }
        private static int Main()
        {
            try
            {
                Console.WriteLine("Testing AA::Static1");
                Static1(ref s_auForward2);
            }
            catch (Exception)
            {
                Console.WriteLine("Exception handled.");
            }

            return 100;
        }
    }
}
