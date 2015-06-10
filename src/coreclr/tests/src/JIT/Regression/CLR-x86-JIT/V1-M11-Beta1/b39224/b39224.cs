// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class App
    {
        static uint[] m_au = new uint[10];
        static void Method1(uint param1) { }
        static int Main()
        {
            int a = 98;
            try
            {
#pragma warning disable 1718
                if (a < a)
                {
#pragma warning restore 1718
                    try
                    {
                        GC.Collect();
                    }
                    catch (Exception) { }
                }
                Method1(m_au[0]);
            }
            catch (Exception) { }
            return 100;
        }
    }
}
