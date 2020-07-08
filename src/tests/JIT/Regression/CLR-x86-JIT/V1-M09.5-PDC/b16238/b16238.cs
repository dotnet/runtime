// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace DefaultNamespace
{
    using System;

    class AA
    {
#pragma warning disable 0414
        public uint m_field1 = 151;
        public bool m_field2 = false;
        public bool m_field3 = false;
        public int m_field4 = 78;
        public static bool m_static1 = false;
#pragma warning restore 0414
        public static int Static1(int param1, int param2)
        {
            try
            {
                throw new Exception();
            }
            catch (Exception)
            {
                GC.Collect();
            }
            return 457444902;
            /* 7 operator(s) emitted */
        }
    }

    class BB
    {
#pragma warning disable 0414
        public uint m_field1 = 91;
#pragma warning restore 0414
        public static int m_static1 = 34041;
        public uint Method1(bool param1)
        {
            int local2 = 135;
            if (new AA().m_field1 > new AA().m_field1)
            {
                AA.Static1(12299, BB.m_static1);
                AA.Static1(125, local2);
                AA.Static1(5196889, AA.Static1(13191820, new AA().m_field4));
            }
            else
                local2 = BB.m_static1;
            return 49548;
            /* 6 operator(s) emitted */
        }
        public static int Main()
        {
            new BB().Method1(false);
            return 100;
        }
    }

}
