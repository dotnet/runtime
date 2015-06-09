// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
