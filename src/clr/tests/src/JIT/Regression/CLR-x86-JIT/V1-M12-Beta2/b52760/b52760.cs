// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    class CC
    {
        static ulong AA_Static1()
        {
            ulong loc = 10;
            return loc *= loc;
        }
        static int Main()
        {
            AA_Static1();
            return 100;
        }
    }
}
