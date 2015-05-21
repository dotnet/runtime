// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace JitTest
{
    internal struct CI
    {
        private int _m_v;

        private static int test(CI c1, CI c2, CI c3, int dummy, CI c4, CI c5)
        {
            c1._m_v = 10;
            c2._m_v = -10;
            return c1._m_v + c2._m_v + c3._m_v + c4._m_v + c5._m_v;
        }

        private static int Main()
        {
            CI c = new CI();
            return 100 + test(c, c, c, 0, c, c);
        }
    }
}
