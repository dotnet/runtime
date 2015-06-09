// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
