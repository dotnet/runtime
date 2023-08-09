// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    class AA
    {
        public static bool m_bStatic1 = true;
    }

    public struct BB
    {
        public int Method1()
        {
            try { }
            finally
            {
#pragma warning disable 1718
                while ((bool)(object)(AA.m_bStatic1 != AA.m_bStatic1))
#pragma warning restore
                {
                }
            }
            return 0;
        }
        [Fact]
        public static int TestEntryPoint()
        {
            new BB().Method1();
            return 100;
        }
    }
}
