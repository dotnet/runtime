// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    class AA
    {
        public static int Main()
        {
            int L = -111;
            object O = null;
            while (L > 0)
            {
                bool[] bb;
                for (; (bool)O; bb = (bool[])O)
                {
                    while (285.34 >= L)
                    {
                        throw new Exception();
                    }
                }
            }
            return 100;
        }
    }
}
