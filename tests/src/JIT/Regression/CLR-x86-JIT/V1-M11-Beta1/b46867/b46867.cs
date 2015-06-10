// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class AA
    {
        static void Method2(double param3, long param4, __arglist)
        {
            param3 = (double)param4;
        }
        static int Main()
        {
            Method2(1.0d, 1, __arglist());
            return 100;
        }
    }
}
