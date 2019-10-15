// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;

public class PerfNotIf
{
    /**@dll.import("kernel32.dll")*/
    //private static native int GetTickCount();

    int icount = 100000000;
    bool m_i;


    PerfNotIf()
    {
        m_i = true;
        /* JVM
                GetTickCount();
                int t1 = GetTickCount();
                notIf(m_i);
                int t2 = GetTickCount();
                System.out.println("Time for not & if:\t" + (t2-t1) + " ms");
        */
        /* SMC */
        int t1 = Environment.TickCount;
        notIf(m_i);
        int t2 = Environment.TickCount;
        Console.WriteLine("Time for not & if:\t" + (t2 - t1) + " ms");

    }

    private bool notIf(bool i)
    {
        for (int k = 0; k < icount; k++)
            if (i)
                i = !i;
            else
                i = !i;
        return i;
    }

    public static int Main(String[] args)
    {
        new PerfNotIf();
        return 100;
    }
}
