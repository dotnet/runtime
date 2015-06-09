// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class AA
    {
        static uint[] m_au = new uint[2];
        static void Main1()
        {
            int D = 18;
            do
            {
                m_au[0] = 0;
            } while (D == 0);
            throw new Exception();
        }
        static int Main()
        {
            try
            {
                Main1();
            }
            catch (Exception) { return 100; }
            return -1;
        }
    }
}
