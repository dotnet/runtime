// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

namespace Test
{
    using System;

    public class AA
    {
        static bool m_bFlag = false;
        static int Main()
        {
            bool B = false;
            if (B)
            {
                try
                {
                    GC.Collect();
                }
                finally
                {
                    if (m_bFlag)
                    {
                        try
                        {
                            throw new Exception();
                        }
                        finally
                        {
                            while (m_bFlag) { }
                        }
                    }
                }
            }
            return 100;
        }
    }
}
