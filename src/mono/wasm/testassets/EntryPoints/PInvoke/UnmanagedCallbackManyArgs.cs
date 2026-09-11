// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

public unsafe partial class Test
{
    public unsafe static int Main(string[] args)
    {
        Console.WriteLine($"main: {args.Length}");

        // Take the addresses of the [UnmanagedCallersOnly] methods so the trimmer keeps them
        // and their native export symbols are generated (they are only referenced from native code).
        GC.KeepAlive((IntPtr)(delegate* unmanaged<int, int, int, int, int, int, int, int, int>)&Sum8);
        GC.KeepAlive((IntPtr)(delegate* unmanaged<int, int, int, int, int, int, int, int, int, int>)&Sum9);
        GC.KeepAlive((IntPtr)(delegate* unmanaged<long, long, long, long, long, long, long, long, long, long, long, long, long, long, long, long, long>)&Sum16);
        GC.KeepAlive((IntPtr)(delegate* unmanaged<int, int, int, int, int, int, int, int, int, int, int, int, void>)&Void12);

        Console.WriteLine($"TestOutput -> ManagedSum8 returned {CallSum8()}");
        Console.WriteLine($"TestOutput -> ManagedSum9 returned {CallSum9()}");
        Console.WriteLine($"TestOutput -> ManagedSum16 returned {CallSum16()}");
        CallVoid12();
        Console.WriteLine($"TestOutput -> ManagedVoid12 stored {s_void12}");
        return 42;
    }

    private static int s_void12;

    // 8 args: exercises the specialized (<= MAX_INTERP_ENTRY_ARGS) entry path
    [UnmanagedCallersOnly(EntryPoint = "ManagedSum8")]
    public static int Sum8(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8)
        => a1 + a2 + a3 + a4 + a5 + a6 + a7 + a8;

    // 9 args: exercises the generic interp_entry_general path (> MAX_INTERP_ENTRY_ARGS)
    [UnmanagedCallersOnly(EntryPoint = "ManagedSum9")]
    public static int Sum9(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9)
        => a1 + a2 + a3 + a4 + a5 + a6 + a7 + a8 + a9;

    // 16 args with a 64-bit return, further stressing the many-args path
    [UnmanagedCallersOnly(EntryPoint = "ManagedSum16")]
    public static long Sum16(long a1, long a2, long a3, long a4, long a5, long a6, long a7, long a8,
                             long a9, long a10, long a11, long a12, long a13, long a14, long a15, long a16)
        => a1 + a2 + a3 + a4 + a5 + a6 + a7 + a8 + a9 + a10 + a11 + a12 + a13 + a14 + a15 + a16;

    // void return with > 8 args
    [UnmanagedCallersOnly(EntryPoint = "ManagedVoid12")]
    public static void Void12(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12)
        => s_void12 = a1 + a2 + a3 + a4 + a5 + a6 + a7 + a8 + a9 + a10 + a11 + a12;

    [DllImport("local", EntryPoint = "call_sum8")]
    public static extern int CallSum8();

    [DllImport("local", EntryPoint = "call_sum9")]
    public static extern int CallSum9();

    [DllImport("local", EntryPoint = "call_sum16")]
    public static extern long CallSum16();

    [DllImport("local", EntryPoint = "call_void12")]
    public static extern void CallVoid12();
}
