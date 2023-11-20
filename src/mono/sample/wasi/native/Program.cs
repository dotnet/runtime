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

    [DllImport("*")]
    public static unsafe extern void ReferenceFuncPtr(delegate* unmanaged<int,int> funcPtr);

    static bool AlwaysFalse(string [] args) => false;

    public unsafe static int Main(string[] args)
    {
        Console.WriteLine($"main: {args.Length}");
        MyImport();

        if (AlwaysFalse(args))
        {
            // never called (would an error on wasm) and doesn't actually do anything
            // but required for wasm_native_to_interp_ftndesc initialization
            // the lookup happens before main when this is here
            ReferenceFuncPtr(&MyExport);
        }

        return 0;
    }
}
