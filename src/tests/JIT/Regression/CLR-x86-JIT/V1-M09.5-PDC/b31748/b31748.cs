// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class AA
    {
        public static int[] m_anStatic4 = new int[7];

        internal static void Static1(object[] param1, ref bool param2)
        {
            float local8 = 0.0f;
            AA[] local9 = new AA[7];
            while (param2)
            {
#pragma warning disable 1717
                param1 = param1;
#pragma warning restore 1717
                do
                {
                    m_anStatic4[0] = m_anStatic4[2] - 50;
#pragma warning disable 1718
                } while (local8 > local8);
#pragma warning restore 1718
                do
                {
                } while ((uint)param1[2] < 0);
            }
        }
        [Fact]
        public static int TestEntryPoint()
        {
            bool b = false;
            Static1(null, ref b);
            return 100;
        }
    }
}
