// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    class App
    {
        public byte[] Method1()
        {
            while (m_bFwd1)
            {
                while (m_bFwd1)
                {
                    try
                    {
                        throw new Exception();
                    }
                    catch (IndexOutOfRangeException)
                    {
                        return m_abFwd6;
                    }
                }
            }
            return m_abFwd6;
        }
        static int Main()
        {
            new App().Method1();
            return 100;
        }
        public static bool m_bFwd1;
        public static byte[] m_abFwd6;
    }
}
