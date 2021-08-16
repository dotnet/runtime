// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal struct VT
{
    public float m1;
    public bool m2;
    public double m3;
    public double m3_1;
    public double m3_2;
    public double m3_3;
    public char m4;
}

internal unsafe class test
{
    private static unsafe bool CheckDoubleAlignment1(VT* p)
    {
        Console.WriteLine("Address {0}", (IntPtr)p);
        if (OperatingSystem.IsWindows() || (RuntimeInformation.ProcessArchitecture != Architecture.X86))
        {
            if ((int)(long)p % sizeof(double) != 0)
            {
                Console.WriteLine("not double aligned");
                return false;
            }
            else
            {
                return true;
            }
        }
        else
        {
            // Current JIT implementation doesn't use double alignment stack optimization for Linux/x86
            return true;
        }
    }

    public static int Main()
    {
        VT vt1 = new VT();
        VT vt2 = new VT();
        bool retVal;

        retVal = CheckDoubleAlignment1(&vt1);

        VT vt3 = new VT();
        retVal = CheckDoubleAlignment1(&vt2) && retVal;
        retVal = CheckDoubleAlignment1(&vt3) && retVal;

        if (retVal)
        {
            Console.WriteLine("PASS");
            return 100;
        }
        else
        {
            Console.WriteLine("FAIL");
            return 0;
        }
    }
}
