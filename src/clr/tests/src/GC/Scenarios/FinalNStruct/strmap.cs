// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Runtime.InteropServices;
using System;


namespace NStruct
{
    public static class FinalizeCount
    {
        public static int icCreat;
        public static int icFinal;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class STRMAP
    { //allocat 31KB memory per struct
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
        internal bool[] Bool;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
        internal byte[] b; //1KB

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
        internal char[] c; //2KB

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
        internal short[] s;//2KB

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
        internal int[] i; //4KB

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
        internal long[] l; //8KB

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
        internal float[] f; //4KB

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
        internal double[] d; //8KB

        public STRMAP()
        {
            Bool = new bool[1024];
            Bool[0] = true;
            Bool[1023] = true;
            b = new byte[1024];
            b[0] = 1;
            b[1023] = 1;
            c = new char[1024];
            c[0] = (char)1;
            c[1023] = (char)1;
            s = new short[1024];
            s[0] = 1;
            s[1023] = 1;
            i = new int[1024];
            i[0] = 1;
            i[1023] = 1;
            l = new long[1024];
            l[0] = 1;
            l[1023] = 2;
            f = new float[1024];
            f[0] = (float)1.0;
            f[1023] = (float)1.0;
            d = new double[1024];
            d[0] = 1;
            d[1023] = 2;
            FinalizeCount.icCreat++;
        }

        ~STRMAP()
        {
            FinalizeCount.icFinal++;
        }

        /*to access elements in NStruct to make sure the object is alive.*/
        public virtual void AccessElement()
        {
            bool bBool = Bool[1023];
            byte bB = b[1023];
            char cC = c[1023];
            int iI = i[1023];
            float fF = f[1023];
            short sS = s[1023];
            double dD = d[1023];
            long lL = l[1023];
        }
    }
}
