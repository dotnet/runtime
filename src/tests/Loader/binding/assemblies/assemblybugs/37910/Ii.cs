// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

public class Program
{
    [DllImport("libc", EntryPoint = "setlocale")]
    public static extern IntPtr setlocale(int category, [MarshalAs(UnmanagedType.LPStr)] string locale);

    [Fact]
    public static int TestEntryPoint()
    {
        Assembly a1 = Assembly.GetExecutingAssembly();

        // In case of Turkish locale:
        // towupper 'i' -> \x0130 (instead of 'I')
        // towlower 'I' -> \x0131 (instead of 'i')
        const string TRLocale = "tr_TR.UTF-8";
        IntPtr res = setlocale(6 /*LC_ALL*/, TRLocale);
        if (TRLocale != Marshal.PtrToStringAnsi(res))
        {
            Console.WriteLine("Skipped! " + TRLocale + " locale was not found in system!");
            return 100;
        }

        Assembly a2 = Assembly.Load("Ii");

        if (a1 != a2)
        {
            Console.WriteLine("Failed!");
            return -2;
        }

        Console.WriteLine("Passed!");
        return 100;
    }
}
