// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class App
    {
        int m_n = 6;
        bool[] m_ab = null;
        static App[] m_ax = new App[7];

        internal static void Method1() { }

        public bool[] Method1(ref int param1, App param4)
        {
            return new App().m_ab;
        }

        static void Method2()
        {
            double local4 = 0.0;
            new App().Method1(ref m_ax[2].m_n, m_ax[2]);
        }

        static void Main1()
        {
            Method1();
            Method2();
        }
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                Main1();
            }
            catch (NullReferenceException) { return 100; }
            return -1;
        }
    }
}
