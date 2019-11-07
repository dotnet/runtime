// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
