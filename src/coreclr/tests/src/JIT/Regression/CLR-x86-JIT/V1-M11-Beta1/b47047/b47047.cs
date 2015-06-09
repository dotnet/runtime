// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class BB
    {
        static void Method1() { }
        static int Main()
        {
            bool local1 = false;
            for (; local1; Method1()) { }
            return 100;
        }
    }
}
