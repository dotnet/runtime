// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class BB
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

        [Fact]
        public static int TestEntryPoint()
        {
            int[] an = new int[2];
            Static2(ref an);
            return 100;
        }
    }
}
