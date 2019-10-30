// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    class AA
    {
        static uint m_u;
        static int Main()
        {
            bool[] ab = new bool[4];
            uint uu;
            for (; ab[0]; uu = m_u & 1) { }
            return 100;
        }
    }
}
