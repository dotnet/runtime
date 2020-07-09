// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

namespace Test
{
    using System;

    class AA
    {
        public static bool m_bStatic1 = true;
    }

    struct BB
    {
        public int Method1()
        {
            try { }
            finally
            {
#pragma warning disable 1718
                while ((bool)(object)(AA.m_bStatic1 != AA.m_bStatic1))
#pragma warning restore
                {
                }
            }
            return 0;
        }
        static int Main()
        {
            new BB().Method1();
            return 100;
        }
    }
}
