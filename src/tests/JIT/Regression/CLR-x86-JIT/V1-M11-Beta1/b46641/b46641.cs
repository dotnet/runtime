// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
