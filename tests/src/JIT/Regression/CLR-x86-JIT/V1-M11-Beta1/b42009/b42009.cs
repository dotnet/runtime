// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class BB
    {
        static int Static1(long arg1, ref int[] arg2, int[] arg3, int arg4)
        { return 0; }

        static void Static2(ref int[] arg)
        {
            Static1(
                Static1(0, ref arg, arg, arg[0]),
                ref arg,
                arg,
                arg[Static1(0, ref arg, arg, arg[0])]
            );
        }

        static int Main()
        {
            int[] an = new int[2];
            Static2(ref an);
            return 100;
        }
    }
}
