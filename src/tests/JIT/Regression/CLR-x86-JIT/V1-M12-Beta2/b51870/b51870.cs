// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

namespace Test
{
    using System;

    struct BB
    {
        int m_iField4;

        static int Main()
        {
            BB local3 = new BB();
            bool b = false;
            if (local3.m_iField4 != local3.m_iField4)
            {
                while (b)
                {
                    while (b) { }
                }
            }
            return 100;
        }
    }
}
