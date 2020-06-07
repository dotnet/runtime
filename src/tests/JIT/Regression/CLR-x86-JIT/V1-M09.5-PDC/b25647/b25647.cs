// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace DefaultNamespace
{
    using System;

    class AA
    {
        public static int Main()
        {
            bool[] loc1 = new bool[7];
            loc1[2] = false;
            uint loc2 = 215;

            if (loc1[2])
            {
                if (loc1[2])
                {
                    if (loc2 == 378)
                    {
                        if (loc1[2])
                            loc2 = 11;
                        throw new Exception();
                    }
                }
            }
            return 100;
        }
    }
}
