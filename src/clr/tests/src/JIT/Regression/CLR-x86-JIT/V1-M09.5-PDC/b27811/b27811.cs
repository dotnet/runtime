// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class BB
    {
        public float[] m_afField1 = new float[16];
        public int m_nField2 = 0;

        public static double[] SomeAlloc(ref int param2) { return null; }
        public static bool Static4(uint[] param1) { return false; }

        public static float FailingFunc(ref int param1)
        {
            bool flag = false;
            BB ptr = new BB();
            int local5 = 0;

            try
            {
                SomeAlloc(ref ptr.m_nField2);
                while (flag)
                {
                    SomeAlloc(ref param1);
                    while (new BB().m_nField2 != 5 && Static4(null)) { }
                    SomeAlloc(ref local5);
                }
            }
            catch (Exception)
            {
                return ptr.m_afField1[4];
            }
            return ptr.m_afField1[2];
        }

        public static int Main()
        {
            int N1 = 0;
            FailingFunc(ref N1);
            return 100;
        }
    }
}
/*
---------------------------
Assert Failure (PID 1024, Thread 1564/61c)        
---------------------------
((emitThisGCrefRegs & regMask) && (ins == INS_add)) || ((emitThisByrefRegs & regMask) && (ins == INS_add || ins == INS_sub))

..\emitx86.cpp, Line: 5903

Abort - Kill program
Retry - Debug
Ignore - Keep running


Image:
D:\bugs\bug.exe

---------------------------
Abort   Retry   Ignore   
---------------------------
*/
