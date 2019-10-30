// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
internal unsafe class testout1
{
    public struct VT_1
    {
        public short a0_1;
        public int a1_1;
        public long a4_1;
    }
    public class CL
    {
        public ulong a0 = 11235799373080166400UL;
    }
    private static int s_a3_1 = 1202448569;
    public static VT_1 vtstatic_1 = new VT_1();

    public static ulong Func_1(VT_1 vt_1)
    {
        ulong* a2_1 = stackalloc ulong[1];
        *a2_1 = 5565938416278830848UL;
        ulong retval_1 = Convert.ToUInt64(Convert.ToUInt64(Convert.ToUInt64(Convert.ToInt32((Convert.ToInt32((Convert.ToInt32(vt_1.a1_1)) % (Convert.ToInt32(s_a3_1))))) + Convert.ToInt64(Convert.ToInt64(Convert.ToInt16(vt_1.a0_1) + Convert.ToInt64(vtstatic_1.a4_1)))) + (*a2_1)));
        return retval_1;
    }

    public static int Main()
    {
        CL cl = new CL();

        VT_1 vt_1 = new VT_1();
        vt_1.a0_1 = 18266;
        vt_1.a1_1 = 2092284849;
        vt_1.a4_1 = 5669860955911480750L;
        ulong val_1 = Func_1(vt_1);

        if ((cl.a0) > (Convert.ToUInt64(cl.a0 - val_1)))
            Console.WriteLine("Func: > true");
        else
            Console.WriteLine("Func: > false");

        ulong retval = Convert.ToUInt64(Convert.ToUInt64(cl.a0 - val_1));
        return 100;
    }
}
