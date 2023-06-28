// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace D
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;

    public class X
    {
        internal static char f(int x)
        {
            return (char)(x >> 8);
        }

        [Fact]
        public static int TestEntryPoint()
        {
            f(123);
            return 100;
        }
    }
}
