// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class App
    {
        private static double[] m_ad = new double[2];
        private static uint m_u;

        public static double Static1()
        {
            float loc = -49.75f;
            return unchecked(m_ad[0] - (double)m_u * (m_ad[1] - loc));
        }

        [Fact]
        public static int TestEntryPoint()
        {
            Static1();
            return 100;
        }
    }
}
