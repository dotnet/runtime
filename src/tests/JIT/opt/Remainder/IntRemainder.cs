// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace CodeGenTests
{
    static class IntRemainder
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Int32_RemainderByOne(int value)
        {
            return value % 1;
        }

        static int Main()
        {
            if (Int32_RemainderByOne(-123) != 0)
                return 0;

            return 100;
        }
    }
}
