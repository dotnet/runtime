// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class BB
    {
        public static uint[] m_auForward5;
        public static uint[] Method2() { return null; }

        static int Main()
        {
            bool local3 = true;
            if (local3)
                try
                {
                    if (local3)
                        m_auForward5 = Method2();
                }
                catch (Exception)
                {
                }
            return 100;
        }
    }
}
