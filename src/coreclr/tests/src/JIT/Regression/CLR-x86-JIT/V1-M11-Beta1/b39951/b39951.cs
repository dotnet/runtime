// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
