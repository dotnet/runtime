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
class BindHandle1
{
    public static int Main(string[] args)
    {
        return (new BindHandle1().RunTest());
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr CreateFile(String FileName, uint Access, uint Share, int Atts, uint Dispo, uint Flags, int Template);

    int RunTest()
    {
        try
        {
            try
            {
                using (SafeFileHandle sfh = new SafeFileHandle(CreateFile("test.txt", 0x40000000, 0, 0, 2, 0x40000000, 0), true))
                {

                    try
                    {
                        if (ThreadPool.BindHandle(sfh))
                        {
                            Console.WriteLine("BindHandle call succeeded");
                        }
                        else
                        {
                            Console.WriteLine("Unexpected: BindHandle call failed");
                            return (98);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Unexpected exception on 1st call: {0}", e);
                        return (92);
                    }

                    ThreadPool.BindHandle(sfh);
                }
            }
            catch (Exception ex)
            {
                if (ex.ToString().IndexOf("0x80070057") != -1) // E_INVALIDARG, we've already bound the handle.
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
        return (99);
    }


}