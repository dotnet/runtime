// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    class BB
    {
        public static BB[] m_axField4 = new BB[7];
        public double m_dField3 = 0.0d;
        public static object Method1()
        {
            return ((object)(m_axField4[2].m_dField3));
        }
        static int Main()
        {
            try
            {
                Method1();
            }
            catch (NullReferenceException)
            {
                Console.WriteLine("Exception handled.");
            }
            return 100;
        }
    }
}
