// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    class App
    {
        static void Method1(float param2) { }

        static int Main()
        {
            ulong local3 = 168u;
            try { Method1((float)local3 + App.m_afForward5[0]); }
            catch (Exception) { }
            try { Method1((float)local3 + App.m_afForward5[0]); }
            catch (Exception) { }
            try { Method1((float)local3 + App.m_afForward5[0]); }
            catch (Exception) { }
            try { Method1((float)local3 + App.m_afForward5[0]); }
            catch (Exception) { }
            try { Method1((float)local3 + App.m_afForward5[0]); }
            catch (Exception) { }
            try { Method1((float)local3 + App.m_afForward5[0]); }
            catch (Exception) { }
            try { Method1((float)local3 + App.m_afForward5[0]); }
            catch (Exception) { }
            try { Method1((float)local3 + App.m_afForward5[0]); }
            catch (Exception) { }
            try { Method1((float)local3 + App.m_afForward5[0]); }
            catch (Exception) { }
            return 100;
        }

        public static float[] m_afForward5 = null;
    }
}
/*
---------------------------
Assert Failure (PID 856, Thread 1076/434)        
---------------------------
conv >= 0

d:\com99\src\vm\wks\..\jitinterface.cpp, Line: 5970

Abort - Kill program
Retry - Debug
Ignore - Keep running


Image:
D:\bugs\bug.exe

---------------------------
Abort   Retry   Ignore   
---------------------------
*/
