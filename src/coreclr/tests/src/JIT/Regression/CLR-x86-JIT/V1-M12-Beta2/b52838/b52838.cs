// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;
    class CC
    {
        static sbyte m_sb;
        static void Finally() { }
        static void Main1()
        {
            try
            {
                while (checked(m_sb == m_sb)) { throw new Exception(); }
                try
                {
                    return;
                }
                catch (DivideByZeroException)
                {
                    return;
                }
            }
            finally
            {
                Finally();
            }
        }
        static int Main()
        {
            try
            {
                Main1();
                Console.WriteLine("this can't happen... fail");
                return 101;
            }
            catch (Exception)
            {
                Console.WriteLine("passed");
                return 100;
            }
        }
    }
}
