// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class AA
    {
        static float[] m_af = new float[2];

        static int Main()
        {
            while (m_af[0] < m_af[1])
            {
                try
                {
                    while (0.0f > m_af[0]) { }
                }
                catch (DivideByZeroException) { return -1; }
            }
            return 100;
        }
    }
}
