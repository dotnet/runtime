// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class AA
    {
        static void Method1()
        {
            bool[] ab = new bool[7];
            if (ab[101])
            {
                int[] an = new int[2];
                while (an[-10] != 4)
                {
                    try { }
                    catch (Exception) { }
                }
            }
            else
            {
                try { }
                catch (Exception) { }
            }
        }
        public static int Main()
        {
            try
            {
                Method1();
            }
            catch (Exception) { }
            return 100;
        }
    }
}
