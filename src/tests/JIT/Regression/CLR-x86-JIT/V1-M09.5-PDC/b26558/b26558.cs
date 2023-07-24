// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace DefaultNamespace
{
    using System;

    public class AA
    {
        [Fact]
        public static int TestEntryPoint()
        {
            uint[] local5 = new uint[7];
            int local6 = 18;
            if (local6 > 104)
            {
                if (local5[0] < 22)
                    GC.Collect();
                else
                {
                    GC.Collect();
                    while (local5[0] == 5)
                    {
                        GC.Collect();
                    }
                }
            }
            return 100;
        }
    }
}

/*
---------------------------
Assert Failure (PID 1052, Thread 972/3cc)        
---------------------------
Assertion failed 'optLoopTable[loopNum].lpEntry != bNext' in 'DefaultNamespace.AA.Main()'


..\flowgraph.cpp, Line: 10618

Abort - Kill program
Retry - Debug
Ignore - Keep running


Image:
D:\bugs\loop.exe

---------------------------
Abort   Retry   Ignore   
---------------------------
*/
