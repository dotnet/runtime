// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

namespace JitTest
{
    using System;

    public class Test
    {
        public static int Main()
        {
            ulong A = 0x3bbde5b000000000;
            uint B = 0xaeb84648;
            ulong C = checked(A + B);
            return 100;
        }
    }
}
