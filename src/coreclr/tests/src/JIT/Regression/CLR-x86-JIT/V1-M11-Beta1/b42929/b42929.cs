// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    struct AA
    {
        private ulong[] m_aulDummyField;
        private static object[] m_axField4;

        private static bool Static1(object param1, bool[] param3) { return false; }

        static void Main1()
        {
            int local2 = 205;
            try
            {
                //.....
            }
            finally
            {
                long local8 = 230l;
                do
                {
                    object o = m_axField4[(int)local8 + local2 + local2];
                } while (AA.Static1(null, new bool[7]));
            }
        }
        static int Main()
        {
            try
            {
                Main1();
                return 1;
            }
            catch (NullReferenceException)
            {
                return 100;
            }
        }
    }
}
