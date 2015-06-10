// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class AA
    {
        public static int Main()
        {
            try
            {
                try
                {
                    // blah blah blah ...
                }
                finally
                {
                    int[] an = new int[2];
                    an[-1] = 0;
                }
            }
            catch (Exception) { }
            return 100;
        }
    }
}
