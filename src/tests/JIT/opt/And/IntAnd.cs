// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace CodeGenTests
{
    class IntAnd
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint Test_And_UInt32_MaxValue(uint i)
        {
            // X64: mov
            
            // X64-NOT: and
            return i & UInt32.MaxValue;
        }

        static int Main()
        {
            if (Test_And_UInt32_MaxValue(1234) != 1234)
                return 0;

            return 100;
        }
    }
}
