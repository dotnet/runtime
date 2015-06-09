// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class BB
    {
        static int[] Static2() { return new int[100]; }

        static void Method4()
        {
            bool[] local2 = new bool[2];
            if (local2[10])
            { //generate exception
                try { }
                finally
                {
                    int n = Static2()[0];
                    while (Static2()[0] != 0)
                    {
                        try { }
                        finally { }
                    }
                }
            }
        }
        static int Main()
        {
            try
            {
                Method4();
            }
            catch (Exception) { return 100; }
            return -1;
        }
    }
}
