// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    class AA
    {
        public static uint[] m_auForward3 = new uint[16];
        public static void Static1(double param1, uint param4) { }
    }

    class BB
    {
        static int Main()
        {
            double local3 = 133.28;
            AA.Static1(local3, AA.m_auForward3[2]);
            return 100;
        }
    }
}
