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
    delegate void VoidDelegate();

    public static int Main()
    {
        int retVal1 = RunGetFncSecTest();

        int retVal2 = 100;
        VoidDelegate md = new VoidDelegate(FunctionPtr.Method);
        Console.WriteLine("\r\nTesting Marshal.GetFunctionPointerForDelegate().");

        try
        {
            Marshal.GetFunctionPointerForDelegate<Object>(null);
            retVal2 = 0;
            Console.WriteLine("Failure - did not receive an exception while passing null as the delegate");
        }
        catch (ArgumentNullException e)
        {
            Console.WriteLine("Pass - threw the right exception passing null as the delegate");
        }
        catch (Exception e)
        {
            retVal2 = 0;
            Console.WriteLine("Failure - receive an incorrect exception while passing null as the delegate");
            Console.WriteLine(e);
        }

        int retVal3 = RunGetDelForFcnPtrTest();

        if (retVal1 != 100)
            return retVal1;
        if (retVal2 != 100)
            return retVal2;
        return retVal3;
    }
  
}
#pragma warning restore 618
