// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    class BB
    {
        private bool m_bUnusedField1 = false;
        private object m_xUnusedField2 = null;
        private static float[] m_afUnusedStatic1 = new float[10];
        private static uint[] m_auUnusedStatic1 = new uint[10];

        static bool[] m_abField2 = new bool[10];

        static void Method1()
        {
            try
            {
                bool b = m_abField2[10000];		//blow exception
                object[] local5 = new object[7];
                while (m_abField2[1000])
                {
                    try
                    {
                        while ((bool)local5[0]) { }
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception)
            {
                bool bb = m_abField2[10000];	//blow another exception
            }
        }

        static int Main()
        {
            try { Method1(); }
            catch (Exception) { }
            return 100;
        }
    }
}
