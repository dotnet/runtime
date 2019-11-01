// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.Win32.SafeHandles;

class BindHandleInvalid
{
    public static int Main(string[] args)
    {
        return (new BindHandleInvalid().RunTest());
    }

    int RunTest()
    {
        SafeFileHandle sfh = new SafeFileHandle(IntPtr.Zero, false);

        try
        {
            ThreadPool.BindHandle(sfh);
        }
        catch (Exception ex)
        {
            if (ex.ToString().IndexOf("0x80070006") != -1) // E_HANDLE, we can't access hresult
            {
                Console.WriteLine("Test passed");
                return (100);
            }
            else
            {
                Console.WriteLine("Got wrong error: {0}", ex);
            }
        }
        Console.WriteLine("Didn't get argument null exception");
        return (99);
    }

    
}