// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;
    class AA
    {
        public static int Main()
        {
            bool[] cond = new bool[40];
            while (cond[0])
            {
                while (cond[1])
                {
                    while (cond[2])
                    {
                        GC.Collect();
                        while (cond[3]) ;
                        while (cond[4]) ;
                        while (cond[5]) ;
                        while (cond[6]) ;
                        while (cond[7]) ;
                    }
                    while (cond[8]) ;
                    while (cond[9]) ;
                    while (cond[10]) ;
                    while (cond[11]) ;
                }
                while (cond[12]) ;
                while (cond[13]) ;
                while (cond[14]) ;
            }
            while (cond[15]) ;
            while (cond[16]) ;
            return 100;
        }
    }
}
