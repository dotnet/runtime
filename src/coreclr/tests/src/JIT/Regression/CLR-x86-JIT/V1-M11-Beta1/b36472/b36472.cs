// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class BB
    {
        static object Method1(__arglist)
        {
            return (int)0;
        }
        object[] Method2(ref object[] param1, ref int[] param2, BB param3,
                                BB param4, BB param5, ref float[] param6)
        {
            return null;
        }
        static int Main()
        {
            return (int)BB.Method1(__arglist()) + 100;
        }
    }
}
