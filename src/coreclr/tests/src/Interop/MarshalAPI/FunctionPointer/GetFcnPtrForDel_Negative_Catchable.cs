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
    delegate void VoidDelegate();

    public static int Main()
    {
        RunGetFncSecTest();

        int retVal = 100;
        VoidDelegate md = new VoidDelegate(FunctionPtr.Method);
        Console.WriteLine("\r\nTesting Marshal.GetFunctionPointerForDelegate().");

        try
        {
            Marshal.GetFunctionPointerForDelegate<Object>(null);
            retVal = 0;
            Console.WriteLine("Failure - did not receive an exception while passing null as the delegate");
        }
        catch (ArgumentNullException e)
        {
            Console.WriteLine("Pass - threw the right exception passing null as the delegate");
        }
        catch (Exception e)
        {
            retVal = 0;
            Console.WriteLine("Failure - receive an incorrect exception while passing null as the delegate");
            Console.WriteLine(e);
        }
        RunGetDelForFcnPtrTest();
        return retVal;
    }
  
}