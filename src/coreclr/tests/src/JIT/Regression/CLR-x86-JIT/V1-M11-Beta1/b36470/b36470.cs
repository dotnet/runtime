// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    class AA
    {
        int[] m_anField3 = new int[100];

        static bool Static1(ref int[] param1) { return false; }

        static int Main()
        {
            AA local5 = new AA();
            while (AA.Static1(ref local5.m_anField3)) ;
            return 100;
        }
    }
}
