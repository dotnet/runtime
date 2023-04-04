// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class AA
    {
        static bool m_bFlag;
        static void Method1(ref byte param1)
        {
            for (; m_bFlag; param1 = param1)
            {
                Array[] a = new Array[2];
            }
        }
        [Fact]
        public static int TestEntryPoint()
        {
            byte b = 0;
            Method1(ref b);
            return 100;
        }
    }
}
