// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
class testout1
{
    public struct VT
    {
        public long a2;
        public double a5;
    }
    public class CL
    {
        public double a1 = 4;
    }
    static int[, ,] arr3d = new int[5, 11, 4];
    public static VT vtstatic = new VT();
    public static CL clstatic = new CL();
    public static long Func()
    {
        vtstatic.a2 = -4L;
        vtstatic.a5 = -8;
        arr3d[4, 0, 3] = 5;
        long retval = Convert.ToInt64((long)(Convert.ToInt32((Convert.ToInt32(clstatic.a1 - ((double)(vtstatic.a2 * vtstatic.a5))))) - (long)((long)(Convert.ToInt32(arr3d[4, 0, 3]) - (long)((long)(Convert.ToInt32(arr3d[4, 0, 3]) - (long)(((long)(vtstatic.a2 / 1L)))))))));
        return retval;
    }
    public static int Main()
    {
        Func();
        return 100;
    }
}
