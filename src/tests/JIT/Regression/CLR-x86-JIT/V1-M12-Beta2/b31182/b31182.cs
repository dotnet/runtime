// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

namespace Test
{
    using System;

    public class App
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
        public static int Main()
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
