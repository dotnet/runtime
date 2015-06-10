// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class App
    {
        private static double[] m_ad = new double[2];
        private static uint m_u;

        public static double Static1()
        {
            float loc = -49.75f;
            return unchecked(m_ad[0] - (double)m_u * (m_ad[1] - loc));
        }

        static int Main()
        {
            Static1();
            return 100;
        }
    }
}
