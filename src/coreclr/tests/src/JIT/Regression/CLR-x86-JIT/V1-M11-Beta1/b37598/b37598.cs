// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class AA
    {
        static uint Method1(__arglist) { return 0; }

        static void Static1(ref uint param1, ref bool[] param2, bool[] param3)
        {
            Method1(__arglist(Method1(__arglist())));
        }

        static int Main()
        {
            uint u = 0;
            bool[] ab = null;
            Static1(ref u, ref ab, ab);
            return 100;
        }
    }
}
