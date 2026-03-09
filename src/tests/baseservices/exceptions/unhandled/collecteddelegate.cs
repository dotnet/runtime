// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

unsafe
{
    IntPtr p = CreateCollectedDelegate("Hello");
    GC.Collect();
    GC.WaitForPendingFinalizers();
    ((delegate* unmanaged<void>)p)();
}

[MethodImpl(MethodImplOptions.NoInlining)]
static IntPtr CreateCollectedDelegate(string message)
{
    return Marshal.GetFunctionPointerForDelegate<Action>(() => Console.WriteLine(message));    
}
