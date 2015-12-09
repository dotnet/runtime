// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
