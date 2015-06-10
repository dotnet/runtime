// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class AA
    {
        public static sbyte m_sb = 0;
    }
    struct CC
    {
        float Method1() { return 0; }
        static int Main()
        {
            CC[] cc = new CC[10];
            byte[] ab = new byte[10];
#pragma warning disable 1717
            cc[ab[0] ^ (AA.m_sb = AA.m_sb)].Method1();
#pragma warning restore
            return 100;
        }
    }
}
