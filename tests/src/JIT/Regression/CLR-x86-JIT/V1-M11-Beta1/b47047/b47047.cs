// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
