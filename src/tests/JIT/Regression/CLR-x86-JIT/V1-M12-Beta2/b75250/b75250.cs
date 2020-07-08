// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
class testout1
{

    public struct VT
    {
        public double a1;
        public double a2;
        public long a3;
        public double a4;
        public double a5;
        public long a6;
        public long a7;
    }

    public class CL
    {
        public int a0 = 5;
    }

    public static VT vtstatic = new VT();

    public static long Func(CL cl, VT vt)
    {

        vtstatic.a1 = 18;
        vtstatic.a2 = 2;
        vtstatic.a3 = 5L;
        vtstatic.a4 = 35;
        vtstatic.a5 = 8;
        vtstatic.a6 = -6L;
        vtstatic.a7 = 1L;
        long retval = Convert.ToInt64((((long)(Convert.ToInt32(cl.a0 / vtstatic.a5) + (long)(Convert.ToInt32(57) - (long)(-70L))) + (long)(vt.a6 * vt.a4)) + (long)((long)(Convert.ToInt32(1787522586) - (long)((vtstatic.a3 + (long)(Convert.ToInt32(1787522586) - (long)(56L))))) * (vt.a4 - vtstatic.a1)) - (long)(vtstatic.a7 * vt.a2)));
        return retval;
    }

    public static int Main()
    {
        VT vt = new VT();
        vt.a1 = 5;
        vt.a2 = 1;
        vt.a3 = 4L;
        vt.a4 = 3;
        vt.a5 = 2;
        vt.a6 = -1L;
        vt.a7 = 6L;
        CL cl = new CL();
        long val = Func(cl, vt);
        return 100;
    }

}
