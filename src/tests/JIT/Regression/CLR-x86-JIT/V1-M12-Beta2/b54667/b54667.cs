// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public struct BB
    {
        bool m_b;
        static void Static1(BB param3, ref bool param5) { }
        [Fact]
        public static int TestEntryPoint()
        {
            Static1(new BB(), ref new BB[] { new BB() }[0].m_b);
            return 100;
        }
    }
}
