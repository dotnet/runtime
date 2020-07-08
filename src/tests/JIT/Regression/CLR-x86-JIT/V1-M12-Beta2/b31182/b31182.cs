// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    class App
    {
        public static bool[] m_abStatic1 = new bool[7];
        public uint Method1()
        {
            try
            {
                while (m_abStatic1[1]) { }
                for (; ; ) { throw new Exception(); }
                try
                {
                }
                finally
                {
                }
            }
            catch (DivideByZeroException)
            {
            }
            return 0;
        }
        static int Main()
        {
            try
            {
                new App().Method1();
            }
            catch (Exception)
            {
                Console.WriteLine("*** Passed ***");
                return 100;
            }
            return -1;
        }
    }
}
