// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
