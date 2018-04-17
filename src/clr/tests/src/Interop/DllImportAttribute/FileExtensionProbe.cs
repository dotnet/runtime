// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Security;
using System.Runtime.InteropServices;
using TestLibrary;

public class FileExtensionProbe
{
    private static int s_failures = 0;

    [DllImport("ExeFile.exe", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Sum")]
    public extern static int Exe_Sum(int a, int b);

    [DllImport("DllFile.Probe", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Sum")]
    public extern static int FileNameContainDot_Sum(int a, int b);

    [DllImport("DllFileProbe", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Sum")]
    public extern static int Simple_Sum(int a, int b);

    private static void Simple()
    {
        try
        {
            if (5 != Simple_Sum(2, 3))
            {
                Console.WriteLine("Dll returns incorrectly result!");
                s_failures++;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Dll throws unexpected exception: " + e.Message);
            s_failures++;
        }
    }

    private static void FileNameContainDot()
    {
        try
        {
            if (7 != FileNameContainDot_Sum(3, 4))
            {
                Console.WriteLine("FileNameContainDot returns incorrectly result!");
                s_failures++;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("FileNameContainDot throws unexpected exception: " + e.Message);
            s_failures++;
        }
    }

    private static void Exe()
    {
        try
        {
            if (9 != Exe_Sum(5, 4))
            {
                Console.WriteLine("Exe_Sum returns incorrectly result!");
                s_failures++;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Exe_Sum throws unexpected exception: " + e.Message);
            s_failures++;
        }
    }

    public static int Main()
    {
        Simple();
        FileNameContainDot();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Exe();
        }

        if (s_failures > 0)
        {
            Console.WriteLine("Failed!");
            return 101;
        }
        else
        {
            Console.WriteLine("Succeed!");
            return 100;
        }
    }
}
