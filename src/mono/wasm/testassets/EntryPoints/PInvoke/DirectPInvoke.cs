// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
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

// A struct with more than one field is passed by reference, which also lowers to (i32) -> i32.
if (Imports.StrLenStruct(new Chars { Low = BitConverter.ToInt32("hell"u8), High = BitConverter.ToInt32("o\0\0\0"u8) }) != 5)
    return 5;

#if !DIRECT_PINVOKE_WHOLE_MODULE
try
{
    Imports.NotListed();
    return 4;
}
catch (DllNotFoundException)
{
}
#endif

#if DIRECT_PINVOKE_CONFLICT
// Native functions that can't be declared with one signature: llabs lowers to different wasm types,
// and open is declared at two arities (C varargs). They build, and fail only when called.
if (Incompatible.Call(false) != 7)
    return 6;

try
{
    Incompatible.Call(true);
    return 7;
}
catch (DllNotFoundException)
{
}
#endif

Console.WriteLine("TestOutput -> DirectPInvoke sample passed");
return 42;

#if DIRECT_PINVOKE_CONFLICT
static unsafe class Incompatible
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Call(bool call)
    {
        if (!call)
            return 7;

        fixed (byte* path = "/missing\0"u8)
            return Imports.LLAbs(-1) + Imports.LLAbsInt32(-1) + Imports.Open2(path, 0) + Imports.Open3(path, 0, 0);
    }
}
#endif

struct Chars
{
    public int Low;
    public int High;
}

static class Imports
{
    [DllImport("libc", EntryPoint = "strlen")]
    public static extern nuint StrLen(string value);

    [DllImport("libc", EntryPoint = "strlen")]
    public static extern uint StrLenUInt32(string value);

    [DllImport("libc", EntryPoint = "strlen")]
    public static extern nuint StrLenAddress(int value);

    [DllImport("libc", EntryPoint = "strlen")]
    public static extern nuint StrLenStruct(Chars value);

#if DIRECT_PINVOKE_CONFLICT
    [DllImport("libc", EntryPoint = "llabs")]
    public static extern long LLAbs(long value);

    [DllImport("libc", EntryPoint = "llabs")]
    public static extern int LLAbsInt32(int value);

    [DllImport("libc", EntryPoint = "open")]
    public static extern unsafe int Open2(byte* path, int flags);

    [DllImport("libc", EntryPoint = "open")]
    public static extern unsafe int Open3(byte* path, int flags, int mode);
#endif

#if !DIRECT_PINVOKE_WHOLE_MODULE
    // Not listed in DirectPInvoke, so it stays unbound and fails only when called.
    [DllImport("libc", EntryPoint = "definitely_not_in_libc")]
    public static extern int NotListed();
#endif
}
