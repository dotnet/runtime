// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

namespace Test
{
    using System;

    public class AA
    {
        static uint m_u;
        public static int Main()
        {
            bool[] ab = new bool[4];
            uint uu;
            for (; ab[0]; uu = m_u & 1) { }
            return 100;
        }
    }
}
