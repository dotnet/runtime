// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    class AA
    {
        double[] m_adField1 = null;
        static object[] s_axStatic1 = null;

        static void Static1()
        {
            AA local4 = null;
            bool local6 = false;
            while ((bool)s_axStatic1[2])
            {
                new AA();
                while (local6)
                {
                    while (0 == local4.m_adField1[2]) { }
                    break;
                }
            }
        }

        static int Main()
        {
            try
            {
                Static1();
            }
            catch (Exception)
            {
                return 100;
            }
            return -1;
        }
    }
}
