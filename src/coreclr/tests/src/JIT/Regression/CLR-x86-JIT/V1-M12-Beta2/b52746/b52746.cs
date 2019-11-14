// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    struct AA
    {
        static Array m_a;
        static bool[] m_ab;
        static object m_x;

        static int Main1()
        {
            if (m_ab[190])
            {
                object L = (object)(double[])m_x;
                int[] L3 = new int[0x7fffffff];
                try
                {
                    if (m_a == (String[])L)
                        return L3[0x7fffffff];
                }
                catch (Exception) { }
                bool b = (bool)L;
            }
            return 0;
        }
        static int Main()
        {
            try
            {
                return Main1();
            }
            catch (NullReferenceException)
            {
                return 100;
            }
        }
    }
}
