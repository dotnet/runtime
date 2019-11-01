// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    struct BB
    {
        private double[] m_adDummyField;
        private ulong[] m_aulField4;

        void Method1(ref ulong[] param2) { }
        static void Method1(BB param2, __arglist)
        {
            param2.Method1(ref param2.m_aulField4);
        }
        static int Main()
        {
            Method1(new BB(), __arglist());
            return 100;
        }
    }
}
