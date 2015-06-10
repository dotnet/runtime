// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class AA
    {
        static int Main()
        {
            int[] an = new int[2];
            bool b = false;
            try
            {
                //do anything here...
            }
            catch (Exception)
            {
                while (b)
                {
                    an[0] = 1;
                }
            }
            while (b) { }
            return 100;
        }
    }
}
