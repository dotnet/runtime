// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class AA
    {
        static uint Method1(__arglist) { return 0; }

        static void Static1(ref uint param1, ref bool[] param2, bool[] param3)
        {
            Method1(__arglist(Method1(__arglist())));
        }

        [Fact]
        public static int TestEntryPoint()
        {
            uint u = 0;
            bool[] ab = null;
            Static1(ref u, ref ab, ab);
            return 100;
        }
    }
}
