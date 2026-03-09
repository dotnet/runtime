// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// Debuggee for cDAC dump tests — exercises vararg P/Invoke stack frames.
/// Uses __arglist to call sprintf, which triggers the VarargPInvokeStub
/// assembly thunk path rather than a DynamicMethodDesc ILStub.
/// Crashes inside native code so the stub frame is on the stack in the dump.
/// </summary>
internal static class Program
{
    // Vararg P/Invoke — uses __arglist which triggers the VarargPInvokeStub path.
    [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern unsafe int sprintf(byte* buffer, string format, __arglist);

    private static void Main()
    {
        CrashInVarargPInvoke();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe void CrashInVarargPInvoke()
    {
        // Use an invalid non-null address as the buffer to trigger an access violation
        // inside sprintf while the VarargPInvokeStub is on the stack.
        sprintf((byte*)0x1, "%d", __arglist(42));
    }
}
