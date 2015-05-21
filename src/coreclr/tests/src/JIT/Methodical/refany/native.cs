// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
//THIS IS NOT A TEST

namespace JitTest
{
    internal class Test
    {
        private static int Main()
        {
            int N = 0x1492;
            TypedReference _ref = __makeref(N);
            if (__reftype(_ref) != typeof(int))
            {
                return 1;
            }
            if (__refvalue(_ref, int) != 0x1492)
            {
                return 2;
            }
            return 100;
        }
    }
}
