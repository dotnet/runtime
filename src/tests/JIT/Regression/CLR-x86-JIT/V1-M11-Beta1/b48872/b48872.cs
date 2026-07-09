// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace b48872
{
    using System;

    public class AA
    {
        static uint m_u;
        [OuterLoop]
        [Fact]
        public static void TestEntryPoint()
        {
            bool[] ab = new bool[4];
            uint uu;
            for (; ab[0]; uu = m_u & 1) { }
        }
    }
}
