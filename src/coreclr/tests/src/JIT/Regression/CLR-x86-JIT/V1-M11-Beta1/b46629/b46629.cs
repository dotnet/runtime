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
            int L = 2;
            while (1u > L)
            {
                GC.Collect();
                break;
            }
            return 100;
        }
    }
}
