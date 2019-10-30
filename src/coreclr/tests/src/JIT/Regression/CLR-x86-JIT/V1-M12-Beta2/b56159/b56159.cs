// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    public class AA
    {
        static uint m_uFwd6;
        static void Method1(ref bool param5)
        {
            while (param5)
            {
                do
                {
                    for (m_uFwd6 = m_uFwd6; param5; m_uFwd6 = m_uFwd6)
                    {
                        try
                        {
                            return;
                        }
                        catch (Exception) { }
                    }
                } while (param5);
            }
            return;
        }
        static int Main()
        {
            bool b = false;
            Method1(ref b);
            return 100;
        }
    }
}
