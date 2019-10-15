// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    class AA
    {
        public int[] m_anField1 = new int[7];

        public static void Method1()
        {
            AA[] local2 = new AA[7];
            while (true)
            {
                local2[2].m_anField1 = new AA().m_anField1;	//this will blow up
            }
        }

        static int Main()
        {
            try
            {
                Method1();
            }
            catch (Exception)
            {
                Console.WriteLine("Exception caught.");
            }
            return 100;
        }
    }
}
