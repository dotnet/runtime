// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    class BB
    {
        public static ulong m_ulStatic1 = 237u;
        public static int Main()
        {
            try { }
            finally
            {
                try
                {
#pragma warning disable 1718
                    while (BB.m_ulStatic1 < BB.m_ulStatic1) { }
#pragma warning restore
                }
                catch (Exception) { }
            }
            return 100;
        }
    }
}
