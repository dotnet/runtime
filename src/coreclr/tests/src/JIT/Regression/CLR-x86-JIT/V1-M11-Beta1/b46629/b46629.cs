// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
