// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// Debuggee for cDAC dump tests — exercises ILStub stack frames.
/// Uses SetLastError=true on the P/Invoke to force the runtime to generate
/// an ILStub (DynamicMethodDesc) rather than using a direct inline call.
/// The ILStub frame will be on the managed stack in the crash dump.
/// </summary>
internal static class Program
{
    // SetLastError=true forces the runtime to generate an ILStub even for
    // blittable signatures, because the stub must capture GetLastError().
    [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = true, ExactSpelling = true)]
    private static extern unsafe void* memcpy(void* dest, void* src, nuint count);

    private static void Main()
    {
        CrashInILStubPInvoke();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe void CrashInILStubPInvoke()
    {
        // Passing null destination to memcpy triggers an access violation
        // inside native code while the P/Invoke ILStub is on the stack.
        memcpy(null, null, 1);
    }
}
