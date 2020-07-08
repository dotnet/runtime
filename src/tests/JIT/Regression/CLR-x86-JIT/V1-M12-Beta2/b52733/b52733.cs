// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Test
{
    using System;

    internal struct AA
    {
        private static float[] s_af;
        private static bool s_b;

        private static float[] Method1() { return s_af = new float[5]; }

        private static int Main()
        {
            bool b = false;
            if (b)
                b = __refvalue(__makeref(s_b), bool);
            else
                Method1();
            return 100;
        }
    }
}
