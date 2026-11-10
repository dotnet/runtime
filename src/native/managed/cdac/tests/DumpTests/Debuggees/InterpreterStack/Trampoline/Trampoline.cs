// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace InterpreterStack.Trampoline;

/// <summary>
/// Provides a JIT'd method in a separate assembly from the debuggee.
/// Since this assembly is NOT in <c>g_interpModule</c>, its methods are always
/// JIT-compiled, creating a gap between two InterpreterFrame regions on the stack.
/// </summary>
public static class JitTrampoline
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Bounce(Action callback)
    {
        callback();
    }
}
