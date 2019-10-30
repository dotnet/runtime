// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace D
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;

    class X
    {
        internal static char f(int x)
        {
            return (char)(x >> 8);
        }

        public static int Main(String[] args)
        {
            f(123);
            return 100;
        }
    }
}
