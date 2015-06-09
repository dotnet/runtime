// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class AA
    {
        static ulong m_ul;
        static int Main()
        {
            try
            {
                GC.Collect();
            }
            catch (DivideByZeroException)
            {
                while (checked(m_ul > m_ul))
                {
                    try
                    {
                        GC.Collect();
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
            return 100;
        }
    }
}
