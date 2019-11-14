// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    public struct AA
    {
        public static bool m_bFwd2;
        public static int Main()
        {
            try
            {
                Main1();
                return 101;
            }
            catch (DivideByZeroException)
            {
                return 100;
            }
        }
        public static void Main1()
        {
            try
            {
                bool local24 = true;
                while (local24)
                {
                    throw new DivideByZeroException();
                }
            }
            finally
            {
                while (m_bFwd2) { }
            }
        }
    }
}
