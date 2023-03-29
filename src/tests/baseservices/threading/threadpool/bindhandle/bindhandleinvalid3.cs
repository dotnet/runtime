// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Xunit;

/// <summary>
/// Verifies passing an invalid handle (not overlapped) to BindHandle works as expected
/// </summary>
public class BindHandleInvalid3
{
    [Fact]
    public static int TestEntryPoint()
    {
        return (new BindHandleInvalid3().RunTest());
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr CreateFile(String FileName, uint Access, uint Share, int Atts, uint Dispo, uint Flags, int Template);


    int RunTest()
    {
        try
        {
            try
            {
                using (SafeFileHandle sfh = new SafeFileHandle(CreateFile("test.txt", 0x40000000, 0, 0, 2, 0x80, 0), true))
                {

                    ThreadPool.BindHandle(sfh);
                }
            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == (uint)0x80070057) // E_INVALIDARG, the handle isn't overlapped
                {
                    Console.WriteLine("Test passed");
                    return (100);
                }
                else
                {
                    Console.WriteLine($"Got wrong error - HResult: 0x{ex.HResult:x}, Exception: {ex}");
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
