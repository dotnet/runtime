// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace DefaultNamespace
{
    using System;

    class BB
    {
        public static int Main()
        {
            int i = 10;
            bool f = false;
            while (f)
                GC.Collect();
            while (f)
                while (i > 39)
                    while (f)
                        GC.Collect();
            return 100;
        }
    }
}
/*
---------------------------
Assert Failure (PID 948, Thread 628/274)        
---------------------------
Assertion failed 'block->bbWeight == bNext->bbWeight' in 'DefaultNamespace.BB.Main()'


..\flowgraph.cpp, Line: 10492

Abort - Kill program
Retry - Debug
Ignore - Keep running


Image:
D:\bugs\loop.exe

---------------------------
Abort   Retry   Ignore   
---------------------------
*/
