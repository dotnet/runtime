// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
