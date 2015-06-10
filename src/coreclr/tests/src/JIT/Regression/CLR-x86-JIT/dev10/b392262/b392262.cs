// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/*
 * Regression testcase for JIT32: Assertion failed 'OKmask' and
 * NGen time "Assertion failed 'EA_SIZE(attr) != EA_1BYTE || (emitRegMask(ireg)
 * & SRM_BYTE_REGS)'" under Complus_JitStressBBProf=1

   The actual repro attached to the bug was to ngen one of the visual studio assemblies. 
   I was talking to brian about the problem, and he told me to use a struct, which contains four 
   value types (bools) to repro the same issue. Which it did. 

 * */


using System;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace b392262
{
    struct VT
    {
        public bool bool1;
        public bool bool2;
        public bool bool3;
        public bool bool4;
    }

    class Program
    {
        static bool result = false;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Test(VT vt)
        {
            result = (vt.bool1 && vt.bool2 && vt.bool3 && vt.bool4);
        }

        static int Main(string[] args)
        {
            VT vt = new VT();
            vt.bool1 = true;
            vt.bool2 = false;
            vt.bool3 = true;
            vt.bool4 = false;

            for (int i = 0; i < 100; i++)
                Test(vt);

            return 100;
        }
    }
}
