// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class BB
    {
        static void Static2(__arglist) { }

        static bool[] Static3(ref int param1, uint[] param2, ref double param3,
            object param4, ref float[] param5, ref object[] param6) { return null; }

        static int Main() { Static2(__arglist()); return 100; }
    }
}
