// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    public struct AA
    {
        public static bool m_bFwd1;
        public void Method1()
        {
            if (m_bFwd1)
            {
                do
                {
                    while (m_bFwd1)
                    {
                        try
                        {
                            throw new Exception();
                        }
                        catch (DivideByZeroException) { }
                    }
                } while (m_bFwd1);
            }
        }
        static int Main()
        {
            new AA().Method1();
            return 100;
        }
    }
}
