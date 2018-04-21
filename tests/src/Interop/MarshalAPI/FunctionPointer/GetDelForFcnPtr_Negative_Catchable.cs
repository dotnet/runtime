// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Security;
using System.Threading;
using System.Globalization;
using System.Runtime.InteropServices;
#pragma warning disable 618

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
            Marshal.GetDelegateForFunctionPointer((IntPtr)0, typeof(VoidDelegate));
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
             Console.WriteLine("Pass - passing a null type received the right exception type, wrong message, message was '{0}'", e.Message);
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
            Console.WriteLine("Pass - threw the right exception passing a non-delegate type, but a wrong message, message was '{0}'", e.Message);
        }
        catch (Exception e)
        {
            retVal = 0;
            Console.WriteLine("Failure - receive an incorrect exception while passing a non-delegate type");
            Console.WriteLine(e);
        }

        // Delegate -> FcnPtr -> Delegate
        try
        {
            VoidDelegate del = (VoidDelegate)Marshal.GetDelegateForFunctionPointer(fcnptr, typeof(VoidDelegate));
            if (del.Target != md.Target)
            {
                retVal = 0;
                Console.WriteLine("Failure - the Target of the funcptr->delegate should be equal to the original method.");
                Console.WriteLine(del.Target);
            }

            if (del.Method != md.Method)
            {
                retVal = 0;
                Console.WriteLine("Failure - The Method of the funcptr->delegate should be equal to the MethodInfo of the original method.");
                Console.WriteLine(del.Method);
            }

            // Try to call it
            del();

            Console.WriteLine("Pass - got a delegate for the function pointer.");
        }
        catch (Exception e)
        {
            retVal = 0;
            Console.WriteLine("Failure - received exception while converting funcptr to delegate.");
            Console.WriteLine(e);
        }

        // Native FcnPtr -> Delegate
        IntPtr pNativeMemory = IntPtr.Zero;
        try
        {
            // Allocate a piece of native memory which in no way resembles valid function entry point.
            // CLR will read the first couple of bytes and try to match it to known patterns. We need something
            // which doesn't look like a reverse pinvoke thunk.
            pNativeMemory = Marshal.AllocCoTaskMem(64);
            Marshal.WriteInt32(pNativeMemory, 0);
            Marshal.WriteInt32(pNativeMemory + 4, 0);

            VoidDelegate del = (VoidDelegate)Marshal.GetDelegateForFunctionPointer(pNativeMemory, typeof(VoidDelegate));
            if (del.Target != null)
            {
                retVal = 0;
                Console.WriteLine("Failure - the Target of the funcptr->delegate should be null since we provided native funcptr.");
                Console.WriteLine(del.Target);
            }

            if (del.Method.Name != "Invoke")
            {
                retVal = 0;
                Console.WriteLine("Failure - The Method of the native funcptr->delegate should be the Invoke method.");
                Console.WriteLine(del.Method);
            }

            // Don't try to call it - it's a random address.

            Console.WriteLine("Pass - got a delegate for the function pointer.");
        }
        catch (Exception e)
        {
            retVal = 0;
            Console.WriteLine("Failure - received exception while converting funcptr to delegate.");
            Console.WriteLine(e);
        }
        finally
        {
            if (pNativeMemory != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(pNativeMemory);
            }
        }


        Console.WriteLine(retVal == 100 ? "Done - PASSED" : "Done - FAILED");
        return retVal;
    }

    public static void Method()
    {
        Console.WriteLine("Simple method to get a delegate for");
    }
}
#pragma warning restore 618
