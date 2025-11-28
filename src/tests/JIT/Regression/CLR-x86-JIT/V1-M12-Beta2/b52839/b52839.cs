// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace b52839
{
    using System;

    class AA
    {
        public static sbyte m_sb = 0;
    }
    public struct CC
    {
        float Method1() { return 0; }
        [OuterLoop]
        [Fact]
        public static void TestEntryPoint()
        {
            CC[] cc = new CC[10];
            byte[] ab = new byte[10];
#pragma warning disable 1717
            cc[ab[0] ^ (AA.m_sb = AA.m_sb)].Method1();
#pragma warning restore
        }
    }
}
