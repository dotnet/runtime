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
            int L = 1;
            try
            {
                return 100;
            }
            catch (Exception)
            {
                while (L * L < 0) { };
            }
            while (L * L < 0) { };
            return -1;
        }
    }
}
