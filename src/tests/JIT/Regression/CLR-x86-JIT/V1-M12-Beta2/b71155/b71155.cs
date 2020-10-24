// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
