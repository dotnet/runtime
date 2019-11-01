// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    class AA
    {
        public int[] m_anField1 = (new int[7]);
        public bool[] m_abField2 = (new bool[7]);
        public float[] m_afField3 = (new float[7]);

        public static void Method1()
        {
            object local1 = 0.0;
            AA[] local2 = new AA[7];
            while (true)
            {
                local2[2].m_anField1 = new AA().m_anField1;

                AA a = new AA();
                while (a.m_abField2[2]) { }
                while ((double)a.m_afField3[2] >= (double)local1) { }
            }
        }

        static int Main()
        {
            try
            {
                Console.WriteLine("Testing AA::Method1");
                AA.Method1();
            }
            catch (Exception)
            {
                Console.WriteLine("Exception handled.");
            }
            return 100;
        }
    }
}
/*
---------------------------
Assert Failure (PID 1204, Thread 1056/420)        
---------------------------
pPrevNestedInfo
.\excep.cpp, Line: 2322
---------------------------

COMPlusNestedExceptionHandler(_EXCEPTION_RECORD * 0x0012e574, _EXCEPTION_REGISTRATION_RECORD * 0x0012f430, _CONTEXT * 0x0012e2a8, void * 0x0012e61c) line 2322 + 31 bytes
NTDLL! 77f92538()
NTDLL! 77f861f8()
KERNEL32! 77ea13f5()
KERNEL32! 77ea146c()
NTDLL! 77f92538()
NTDLL! 77f8af89()
NTDLL! 77f9fb9a()
EEJitManager::ResumeAtJitEH(CrawlFrame * 0x0012ee84, unsigned long 36, unsigned long 0, Thread * 0x00135120, int 1) line 845
COMPlusUnwindCallback(CrawlFrame * 0x0012ee84, ThrowCallbackType * 0x0012f3b0) line 1918 + 39 bytes
Thread::StackWalkFramesEx(_REGDISPLAY * 0x0012efe4, StackWalkAction (CrawlFrame *, void *)* 0x6d0f6fd0 COMPlusUnwindCallback(CrawlFrame *, ThrowCallbackType *), void * 0x0012f3b0, unsigned int 3, Frame * 0x0012f3fc) line 206 + 16 bytes
Thread::StackWalkFrames(StackWalkAction (CrawlFrame *, void *)* 0x6d0f6fd0 COMPlusUnwindCallback(CrawlFrame *, ThrowCallbackType *), void * 0x0012f3b0, unsigned int 3, Frame * 0x0012f3fc) line 457 + 34 bytes
UnwindFrames(Thread * 0x00135120, ThrowCallbackType * 0x0012f3b0) line 1425
CPFH_RealFirstPassHandler(_EXCEPTION_RECORD * 0x0012f5fc, _EXCEPTION_REGISTRATION_RECORD * 0x0012f9dc, _CONTEXT * 0x0012f618, void * 0x0012f5d4, int 0) line 950
CPFH_FirstPassHandler(_EXCEPTION_RECORD * 0x0012f5fc, _EXCEPTION_REGISTRATION_RECORD * 0x0012f9dc, _CONTEXT * 0x0012f618, void * 0x0012f5d4) line 1092 + 25 bytes
COMPlusFrameHandler(_EXCEPTION_RECORD * 0x0012f5fc, _EXCEPTION_REGISTRATION_RECORD * 0x0012f9dc, _CONTEXT * 0x0012f618, void * 0x0012f5d4) line 1223 + 21 bytes
NTDLL! 77f92538()
NTDLL! 77f8af89()
NTDLL! 77f9fb9a()
*/
