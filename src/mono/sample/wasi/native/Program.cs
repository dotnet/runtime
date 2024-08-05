// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

public unsafe class Test
{
    [UnmanagedCallersOnly(EntryPoint = "ManagedFunc")]
    public static int MyExport(int number)
    {
        // called from MyImport aka UnmanagedFunc
        Console.WriteLine($"MyExport({number}) -> 42");
        return 42;
    }

    [DllImport("*", EntryPoint = "UnmanagedFunc")]
    public static extern void MyImport(); // calls ManagedFunc aka MyExport

    public unsafe static int Main(string[] args)
    {
        Console.WriteLine($"main: {args.Length}");
        MyImport();
        return 0;
    }
}
