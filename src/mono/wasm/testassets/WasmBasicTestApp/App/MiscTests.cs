// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;

public partial class MiscTests
{
    [JSExport]
    public static unsafe int TestGetFunctionPointerForDelegate()
    {
        try
        {
            nint fptr = Marshal.GetFunctionPointerForDelegate(new Action(() => Console.WriteLine("Managed method callee")));
            ((delegate* unmanaged<void>)fptr)();
            Console.WriteLine("No Exception");
        }
        catch (Exception ex)
        {
            Console.WriteLine("TestOutput -> " + ex.Message);
            Console.WriteLine("TestOutput -> " + ex.GetType().FullName);
        }

        return 42;
    }
}
