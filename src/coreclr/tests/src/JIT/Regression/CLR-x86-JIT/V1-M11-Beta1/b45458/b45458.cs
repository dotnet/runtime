// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace JitTest
{
    using System;

    class Test
    {
        static int Main()
        {
            ulong A = 0x3bbde5b000000000;
            uint B = 0xaeb84648;
            ulong C = checked(A + B);
            return 100;
        }
    }
}
