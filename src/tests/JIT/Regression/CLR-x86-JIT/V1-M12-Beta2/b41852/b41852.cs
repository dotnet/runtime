// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public struct BB
    {
        private double[] m_adDummyField;
        private ulong[] m_aulField4;

        void Method1(ref ulong[] param2) { }
        static void Method1(BB param2, __arglist)
        {
            param2.Method1(ref param2.m_aulField4);
        }
        [Fact]
        public static int TestEntryPoint()
        {
            Method1(new BB(), __arglist());
            return 100;
        }
    }
}
