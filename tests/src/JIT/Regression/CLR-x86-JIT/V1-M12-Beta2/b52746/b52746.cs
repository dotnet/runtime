// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
