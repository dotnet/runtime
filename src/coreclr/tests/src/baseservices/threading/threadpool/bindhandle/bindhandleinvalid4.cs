// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

/// <summary>
/// Verifies passing an invalid handle (not overlapped) to BindHandle works as expected
/// </summary>
class BindHandleInvalid3
{
    public static int Main(string[] args)
    {
        return (new BindHandleInvalid3().RunTest());
    }

    int RunTest()
    {
        try
        {
            try
            {
                using (FileStream fs1 = new FileStream("test.txt", FileMode.Create))
                {
                    ThreadPool.BindHandle(fs1.SafeFileHandle);
                }
            }
            catch (Exception ex)
            {
                if (ex.ToString().IndexOf("0x80070057") != -1) // E_INVALIDARG, the handle isn't overlapped
                {
                    Console.WriteLine("Test passed");
                    return (100);
                }
                else
                {
                    Console.WriteLine("Got wrong error: {0}", ex);
                }
            }
        }
        finally
        {
            if (File.Exists("test.txt"))
            {
                File.Delete("test.txt");
            }
        }
        Console.WriteLine("Didn't get argument null exception");
        return (99);
    }


}