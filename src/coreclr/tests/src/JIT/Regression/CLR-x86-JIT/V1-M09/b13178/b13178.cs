// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace DefaultNamespace
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;
    using System.IO;
    using System.Reflection;

    public class Ternary3b
    {
        public static int Main(String[] argv)
        {
            Ternary3b hmm = new Ternary3b();
            hmm.Problem();
            return 100;
        }

        // If these field members are in the Problem() function, the problem does Not repro!!
        internal int m_rr = -1;
        internal int m_cc = 2;

        public virtual void Problem()
        {
            int uu = 20;
            int oo = 12;
            int pp = 34;
            ///


            oo = (m_rr != -1) ? m_rr : ((uu + m_cc - 1) / m_cc);
            Console.WriteLine("Ternary Logic A1: == " + oo);


            if (m_rr != -1)
            {
                pp = m_rr;
            }
            else
            {
                pp = ((uu + m_cc - 1) / m_cc);
            }
            Console.WriteLine("IfElse Logic A1: == " + pp);


            if (oo == pp)
            {
                Console.WriteLine("Good, 1738huy");
            }
            else
            {
                Console.WriteLine("FAIL!, 2738hub");
            }
        }
    }


    /**  RESULTS PASTED IN FROM CMD.EXE SCREEN... Show error only when JIT ON...

    C:\Junk> jvc.exe -C Ternary3b.java
    Microsoft (R) Visual J++ Compiler Version 7.00.8474
    Copyright (C) Microsoft Corp 1996-1998. All rights reserved.


    C:\Junk> echo JIT is ON
    JIT is ON

    C:\Junk> Ternary3b.exe
    Ternary Logic A1: == 0
    IfElse Logic A1: == 10
    FAIL!, 2738hub

    C:\Junk> echo Now JIT is OFF
    Now JIT is OFF

    C:\Junk> Ternary3b.exe
    Ternary Logic A1: == 10
    IfElse Logic A1: == 10
    Good, 1738huy

    C:\Junk>
    **/
}
