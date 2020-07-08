// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
