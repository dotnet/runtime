// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public struct AA
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
        [Fact]
        public static int TestEntryPoint()
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
