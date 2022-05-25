// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
namespace Test_native
{
//THIS IS NOT A TEST

namespace JitTest
{
    public class Test
    {
        [Fact]
        public static int TestEntryPoint()
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
}
