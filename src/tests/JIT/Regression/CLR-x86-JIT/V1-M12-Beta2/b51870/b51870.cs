// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public struct BB
    {
        int m_iField4;

        [Fact]
        public static int TestEntryPoint()
        {
            BB local3 = new BB();
            bool b = false;
            if (local3.m_iField4 != local3.m_iField4)
            {
                while (b)
                {
                    while (b) { }
                }
            }
            return 100;
        }
    }
}
