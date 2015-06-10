// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class AA
    {
        static void Main1()
        {
            bool F = true;
            while (F)
            {
                do
                {
                    int N = 260;
                    byte B = checked((byte)N);	//an exception!
                } while (F);
            }
        }
        static int Main()
        {
            try
            {
                Main1();
                return -1;
            }
            catch (OverflowException)
            {
                return 100;
            }
        }
    }
}
