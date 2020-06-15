// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    class AA
    {
        public static object[] m_axStatic2 = null;
        public static void Static3()
        {
            bool flag1 = false, flag2 = false, flag3 = false;
            double local4 = 0.19;
            do
            {
                GC.Collect();
                while (flag1) ;
                while (flag2) ;
                object oo;
#pragma warning disable 1718,0162
                for (; (local4 == local4); oo = AA.m_axStatic2)
#pragma warning restore 1718,0162
                    throw new Exception();
            } while (flag3);
        }
        static int Main()
        {
            try
            {
                AA.Static3();
            }
            catch (Exception) { }
            return 100;
        }
    }
}
