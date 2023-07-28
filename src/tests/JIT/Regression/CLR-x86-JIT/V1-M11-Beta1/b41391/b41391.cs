// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
/*
---------------------------
Assert Failure (PID 848, Thread 1036/40c)        
---------------------------
(argCnt < MAX_PTRARG_OFS)

d:\com99\src\vm\wks\..\eetwain.cpp, Line: 2076

Abort - Kill program
Retry - Debug
Ignore - Keep running


Image:
D:\bugs\bug.exe

---------------------------
Abort   Retry   Ignore   
---------------------------
*/
namespace Test
{
    using System;

    public struct AA
    {
        private double[] m_adDummyField1;
        private bool m_bDummyField2;
        private float m_fDummyField3;
        private ulong[] m_aulDummyField4;
        private double m_dDummyField5;
        private ulong m_ulDummyField6;

        static object m_axStatic2 = null;

        public static int Method1(AA param1, AA param2, ref AA param3,
                                uint[] param4, int[] param5, __arglist)
        {
            GC.Collect();
            return 0;
        }

        public static void Static2(AA[] param2)
        {
            AA aa = new AA();
            Method1(aa, param2[Method1(aa, aa, ref aa, null, null, __arglist(0.0f, aa))],
                    ref aa, null, null, __arglist());
            while ((bool)m_axStatic2) { }
        }

        [Fact]
        public static int TestEntryPoint() { try { Static2(null); } catch (NullReferenceException) { } return 100; }
    }
}
