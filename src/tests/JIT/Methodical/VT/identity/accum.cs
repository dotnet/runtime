// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace JitTest_accum_cs
{
    public struct CI
    {
        private int _m_v;

        private static int test(CI c1, CI c2, CI c3, int dummy, CI c4, CI c5)
        {
            c1._m_v = 10;
            c2._m_v = -10;
            return c1._m_v + c2._m_v + c3._m_v + c4._m_v + c5._m_v;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            CI c = new CI();
            return 100 + test(c, c, c, 0, c, c);
        }
    }
}
