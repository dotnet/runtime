// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
