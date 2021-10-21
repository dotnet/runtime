// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Runtime_56930
{
    class C0
    {
        public int F1;
        public C0(int f1)
        {
            F1 = f1;
        }
    }

    class Program
    {
        static C0 s_2 = new C0(1);

        public static int Main()
        {
            // Since the following statement is a dead store the next two statements will become
            // a NULLCHECK(s_2) followed by STOREIND(&s_2[8], 0)
            int val = s_2.F1;
            s_2.F1 = 0;

            // On Arm64 these will be compiled to
            //
            // ldr wzr, [x0, #8]
            // str wzr, [x0, #8]
            //
            // The issue was that the JIT falsely assumes the store to be redundant and eliminated
            // the instruction during pipehole optimizations.

            return s_2.F1 + 100;
        }
    }
}
