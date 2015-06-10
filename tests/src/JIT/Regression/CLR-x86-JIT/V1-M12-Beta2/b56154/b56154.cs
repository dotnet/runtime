// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    public class AA
    {
        static bool m_bFlag;
        static void Method1(ref byte param1)
        {
            for (; m_bFlag; param1 = param1)
            {
                Array[] a = new Array[2];
            }
        }
        static int Main()
        {
            byte b = 0;
            Method1(ref b);
            return 100;
        }
    }
}
