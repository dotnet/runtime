// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Security;
using System.Threading;
using System.Globalization;
using System.Runtime.InteropServices;

public partial class FunctionPtr
{  

    public static int RunGetDelForFcnPtrTest()
    {
        int retVal = 100;
        VoidDelegate md = new VoidDelegate(FunctionPtr.Method);
        IntPtr fcnptr = (IntPtr)0;
        Console.WriteLine("\r\nTesting Marshal.GetDelegateForFunctionPointer().");

        try
        {
            fcnptr = Marshal.GetFunctionPointerForDelegate<VoidDelegate>(md);
        }
        catch (Exception e)
        {
            retVal = 0;
            Console.WriteLine("Exception during initialization: {0}", e.ToString());
            return retVal;
        }

        try
        {
            Marshal.GetDelegateForFunctionPointer((IntPtr)0, typeof(MyDelegate));
            retVal = 0;
            Console.WriteLine("Failure - did not receive an exception while passing 0 as the function pointer");
        }
        catch (ArgumentNullException e)
        {
            Console.WriteLine("Pass - threw the right exception passing a 0 function pointer");
        }
        catch (Exception e)
        {
            retVal = 0;
            Console.WriteLine("Failure - receive an incorrect exception while passing 0 as the function pointer");
            Console.WriteLine(e);
        }

        try
        {
            Marshal.GetDelegateForFunctionPointer(fcnptr, null);
            retVal = 0;
            Console.WriteLine("Failure - did not receive an exception while passing a null type");
        }
        catch (ArgumentException e)
        {
             Console.WriteLine("Fail - passing a null type received the right exception type, wrong message, message was '{0}'", e.Message);
        }
        catch (Exception e)
        {
            retVal = 0;
            Console.WriteLine("Failure - receive an incorrect exception while passing a null type");
            Console.WriteLine(e);
        }

        try
        {
            Marshal.GetDelegateForFunctionPointer(fcnptr, typeof(Object));
            retVal = 0;
            Console.WriteLine("Failure - did not receive an exception while passing a non-delegate type");
        }
        catch (ArgumentException e)
        {
            Console.WriteLine("Faile - threw the right exception passing a non-delegate type, but a wrong message, message was '{0}'", e.Message);
        }
        catch (Exception e)
        {
            retVal = 0;
            Console.WriteLine("Failure - receive an incorrect exception while passing a non-delegate type");
            Console.WriteLine(e);
        }

        Console.WriteLine(retVal == 100 ? "Done - PASSED" : "Done - FAILED");
        return retVal;
    }

    public static void Method()
    {
        Console.WriteLine("Simple method to get a delegate for");
    }
}
