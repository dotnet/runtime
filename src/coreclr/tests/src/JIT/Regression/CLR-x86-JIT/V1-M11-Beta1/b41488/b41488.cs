// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    struct AA
    {
        bool[] m_abDummyField1;
        bool[] m_abDummyField2;

        static int m_iStatic;
        static uint m_uStatic;

        static uint Method1(float param1) { return 0; }

        static void Static1()
        {
            int iLocal = 0;
            float[] af = null;
            while (true)
                Method1(af[(int)m_uStatic + (iLocal - m_iStatic)]);
        }

        static int Main()
        {
            try { Static1(); }
            catch (Exception) { }
            return 100;
        }
    }
}
