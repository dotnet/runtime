// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System;

class ClientPInvokeIntNativeTest
{
    [DllImport("PInvokeIntNative")]
    private static extern int Marshal_In([In]int intValue);

    [DllImport("PInvokeIntNative")]
    private static extern int Marshal_InOut([In, Out]int intValue);

    [DllImport("PInvokeIntNative")]
    private static extern int Marshal_Out([Out]int intValue);

    [DllImport("PInvokeIntNative")]
    private static extern int MarshalPointer_In([In]ref int pintValue);

    [DllImport("PInvokeIntNative")]
    private static extern int MarshalPointer_InOut(ref int pintValue);

    [DllImport("PInvokeIntNative")]
    private static extern int MarshalPointer_Out(out int pintValue);


    public static int Main(string[] args)
    {
        int failures = 0;
        int intManaged = (int)1000;
        int intNative = (int)2000;
        int intReturn = (int)3000;

        int int1 = intManaged;
        if(intReturn != Marshal_In(int1))
        {
            failures++;
            Console.WriteLine("In return value is wrong");
        }

        int int2 = intManaged;
        if (intReturn != Marshal_InOut(int2))
        {
            failures++;
            Console.WriteLine("InOut return value is wrong");
        }
        if (intManaged != int2)
        {
            failures++;
            Console.WriteLine("InOut byval parameter value changed.");
        }

        int int3 = intManaged;
        if (intReturn != Marshal_Out(int3))
        {
            failures++;
            Console.WriteLine("Out return value is wrong");
        }
        if (intManaged != int3)
        {
            failures++;
            Console.WriteLine("Out byval parameter value changed.");
        }

        int int4 = intManaged;
        if (intReturn != MarshalPointer_In(ref int4))
        {
            failures++;
            Console.WriteLine("RefIn return value is wrong");
        }
        if (intManaged != int4)
        {
            failures++;
            Console.WriteLine("In byref parameter value changed.");
        }

        int int5 = intManaged;
        if (intReturn != MarshalPointer_InOut(ref int5))
        {
            failures++;
            Console.WriteLine("RefInOut return value is wrong");
        }
        if (intNative != int5)
        {
            failures++;
            Console.WriteLine("InOut byref value is wrong.");
        }

        int int6 = intManaged;
        if (intReturn != MarshalPointer_Out(out int6))
        {
            failures++;
            Console.WriteLine("RefOut return value is wrong");
        }
        if (intNative != int6)
        {
            failures++;
            Console.WriteLine("Out byref value is wrong.");
        }

        return 100 + failures;
    }
}
