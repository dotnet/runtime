// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
