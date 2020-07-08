// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

/* 
JIT JitDebuggable=0 JitDebugInfo=1
d:\com99\src\jit\il\dll\..\scopeinfo.cpp, Line 582 : Assertion failed 'lclVar->lvTracked' in 'Test.AA.Method1(int,int,byref):int'
*/
namespace Test
{
    using System;
    class AA
    {
        ulong m_ul;

        void Method1(uint param1, uint param2)
        {
            if (m_ul == 1u)
                param1 = param2;
        }
        static int Main()
        {
            new AA().Method1(0u, 0);
            return 100;
        }
    }
}
