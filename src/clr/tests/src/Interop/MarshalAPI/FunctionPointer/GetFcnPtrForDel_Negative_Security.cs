// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Security;
using System.Runtime.InteropServices;
#pragma warning disable 618

partial class FunctionPtr
{
    [DllImport("FunctionPointerNative", CallingConvention=CallingConvention.Cdecl)]
    public static extern bool CheckFcnPtr(IntPtr fcnptr);

    public delegate bool DelegateWithLong(long l); //Singlecast delegate
    public delegate void MultiDelegateWithLong(long l); //Multicast delegate

    public static DelegateWithLong del = new DelegateWithLong(FunctionPtr.Method);
    public static MultiDelegateWithLong multidel = new MultiDelegateWithLong(FunctionPtr.Method2);

    private static IntPtr fcnptr;

    public static int RunGetFncSecTest()
    {
        Console.WriteLine("\r\nTesting Marshal.GetDelegateForFunctionPointer().");

        bool pass = true;

        try
        {
            fcnptr = Marshal.GetFunctionPointerForDelegate<DelegateWithLong>(del);
            if (CheckFcnPtr(fcnptr) == true)
            {
                Console.WriteLine("\tPass - singlecast case");
            }
            else
            {
                pass = false;
                Console.WriteLine("\tFail - singlecast case, created a function pointer but the call failed");
            }
        }
        catch (Exception e)
        {
            pass = false;
            Console.WriteLine("\tFailure - singlecast case");
            Console.WriteLine(e);
        }

        try
        {
            fcnptr = Marshal.GetFunctionPointerForDelegate<MultiDelegateWithLong>(multidel);
            CheckFcnPtr(fcnptr);
            Console.WriteLine("\tPass - multicast case");
        }
        catch (Exception e)
        {
            pass = false;
            Console.WriteLine("\tFailure - multicast case");
            Console.WriteLine(e);
        }

        if (pass)
        {
            Console.WriteLine("Pass - the base case");
            return 100;
        }
        else
        {
            Console.WriteLine("Fail - the base case");
            return 99;
        }
    }

    public static bool Method(long l)
    {
        if (l != 999999999999)
            return false;
        else
            return true;
    }

    public static void Method2(long l)
    {
        if (l != 999999999999)
            throw new Exception("Failed multicast call");
    }
}
#pragma warning restore 618
