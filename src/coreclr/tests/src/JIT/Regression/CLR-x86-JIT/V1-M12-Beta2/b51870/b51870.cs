// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
