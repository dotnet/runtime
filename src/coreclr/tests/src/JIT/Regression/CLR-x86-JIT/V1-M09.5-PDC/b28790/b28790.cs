// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    class AA
    {
        bool m_bField3 = false;
        static double[] m_auForward3;

        static uint[] Static1(ref double[] param1) { return null; }
        static bool Static2() { return false; }

        static void Static4(ref double[] param1)
        {
            AA local2 = new AA();

#pragma warning disable 0162
            for (; Static2(); Static1(ref m_auForward3))
#pragma warning restore 0162
            {
                while (191 > Static1(ref m_auForward3)[2])
                {
                    while (Static2() && Static2())
                    {
                        while (Static1(ref m_auForward3)[2] > 175u) { }
                        //new float[7]; - not a valid statement, see VS7 #244656.
                        new float();
                    }
                    return;
                }
                //new AA[7]; - not a valid statement, see VS7 #244656.
                new AA();
                while (local2.m_bField3) { }
                return;
            }
        }

        static int Main()
        {
            double[] arg = null;
            Static4(ref arg);
            return 100;
        }
    }
}
/*
Assertion failed 'bNext->bbPreds->flBlock == block' in 'Test.AA.Static4(byref)'
..\flowgraph.cpp, Line: 10424

Compiler::fgCompactBlocks(BasicBlock * 0x00161b58, unsigned char 0) line 10424 + 46 bytes
Compiler::fgUpdateFlowGraph() line 11220
Compiler::compCompile(void * * 0x0012f3a0, unsigned long * 0x0012f4d8, void * * 0x0012f3a8, void * * 0x0012f394, void * * 0x0012f38c) line 1874
Compiler::compCompile(CORINFO_MODULE_STRUCT_ * 0x0015d280, ICorJitInfo * 0x0012f484, CORINFO_METHOD_INFO * 0x0012f4ec, void * * 0x0012f3a0, unsigned long * 0x0012f4d8, void * * 0x0012f3a8, void * * 0x0012f394, void * * 0x0012f38c, unsigned int 2593) line 2124
jitNativeCode(ICorJitInfo * 0x0012f484, CORINFO_METHOD_INFO * 0x0012f4ec, void * * 0x0012f3a0, unsigned long * 0x0012f4d8, void * * 0x0012f3a8, void * * 0x0012f394, void * * 0x0012f38c, unsigned int 2593) line 2464 + 47 bytes
CILJit::compileMethod(CILJit * const 0x02e1d2f8 CILJitBuff, ICorJitInfo * 0x0012f484, CORINFO_METHOD_INFO * 0x0012f4ec, unsigned int 2593, unsigned char * * 0x0012f53c, unsigned long * 0x0012f4d8) line 354 + 46 bytes
JITFunction(MethodDesc * 0x02d200a8, COR_ILMETHOD_DECODER * 0x0012f7c4, int * 0x0012f6d0) line 3434 + 45 bytes
MakeJitWorker(MethodDesc * 0x02d200a8, COR_ILMETHOD_DECODER * 0x0012f7c4, int 0, int 0, PrestubMethodFrame * 0x0012f910, OBJECTREF * 0x0012f8cc) line 10278 + 17 bytes
PreStubWorker(PrestubMethodFrame * 0x0012f910) line 10858 + 81 bytes
0013c7f1()
02e30056()
MethodDesc::Call(const __int64 * 0x0012fc3c) line 626
RunMain(MethodDesc * 0x02d200d8, short 1, long * 0x0012ffa4, CorEntryPointType EntryManagedMain, REF<PtrArray> * 0x00000000) line 3068 + 15 bytes
ClassLoader::ExecuteMainMethod(Module * 0x0015d280, long * 0x0012ffa4, REF<PtrArray> * 0x00000000) line 3246 + 23 bytes
Assembly::ExecuteMainMethod(long * 0x0012ffa4, REF<PtrArray> * 0x00000000) line 1827
SystemDomain::ExecuteMainMethod(PEFile * 0x0015c190, long * 0x0012ffa4) line 2006 + 14 bytes
ExecuteEXE(HINSTANCE__ * 0x00400000, long * 0x0012ffa4) line 1625 + 13 bytes
_CorExeMain() line 1345 + 17 bytes
_CorExeMain() line 2753 + 5 bytes
*/
