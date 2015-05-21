// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace MS
{
    internal struct VT
    {
        private int _v;

        private static void Test(VT arg1, ref VT arg2)
        {
            arg2._v = 100;
            arg1._v = 10;
        }

        private static int Main()
        {
            VT vt;
            vt._v = 99;
            Test(vt, ref vt);
            return vt._v;
        }
    }
}
