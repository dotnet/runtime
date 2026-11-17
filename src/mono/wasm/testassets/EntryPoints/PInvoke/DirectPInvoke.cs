// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

if (Imports.StrLen("hello") != 5)
    return 1;

// The same entry point declared with other types that lower to the same wasm signature.
if (Imports.StrLenUInt32("hello") != 5)
    return 2;

unsafe
{
    fixed (byte* value = "hello\0"u8)
    {
        if (Imports.StrLenAddress((int)value) != 5)
            return 3;
    }
}

try
{
    Imports.NotListed();
    return 4;
}
catch (DllNotFoundException)
{
}

Console.WriteLine("TestOutput -> DirectPInvoke sample passed");
return 42;

static class Imports
{
    [DllImport("libc", EntryPoint = "strlen")]
    public static extern nuint StrLen(string value);

    [DllImport("libc", EntryPoint = "strlen")]
    public static extern uint StrLenUInt32(string value);

    [DllImport("libc", EntryPoint = "strlen")]
    public static extern nuint StrLenAddress(int value);

    // Not listed in DirectPInvoke, so it stays unbound and fails only when called.
    [DllImport("libc", EntryPoint = "definitely_not_in_libc")]
    public static extern int NotListed();
}
