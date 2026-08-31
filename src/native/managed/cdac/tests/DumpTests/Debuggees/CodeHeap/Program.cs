// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

/// <summary>
/// Debuggee for cDAC dump tests — exercises EEJitManager code heap enumeration.
///
/// Two types of code heap are exercised:
///   LoaderCodeHeap  — created by the JIT for regularly compiled methods.
///   HostCodeHeap    — created by the JIT for DynamicMethod instances
///                     (System.Reflection.Emit.DynamicMethod).
///
/// The program creates and invokes a DynamicMethod to ensure a HostCodeHeap
/// is allocated, then crashes via FailFast from a regular JIT-compiled method
/// so that both heap types are present in the dump.
/// </summary>
internal static class Program
{
    // Keep the delegate rooted so the HostCodeHeap survives GC until the crash.
    private static Delegate? s_dynamicDelegate;

    private static void Main()
    {
        // Invoke a DynamicMethod so the JIT allocates a HostCodeHeap.
        InvokeDynamicMethod();

        // Crash from a regular JIT-compiled frame so a LoaderCodeHeap is on the stack.
        Crash();
    }

    /// <summary>
    /// Builds and invokes a trivial DynamicMethod (returns its argument plus 1).
    /// Creating and JIT-compiling the DynamicMethod causes the runtime to allocate
    /// a HostCodeHeap for the resulting native code.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void InvokeDynamicMethod()
    {
        var method = new DynamicMethod(
            name: "AddOne",
            returnType: typeof(int),
            parameterTypes: [typeof(int)]);

        ILGenerator il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);

        var fn = (Func<int, int>)method.CreateDelegate(typeof(Func<int, int>));
        s_dynamicDelegate = fn;
        _ = fn(41);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Crash()
    {
        Environment.FailFast("cDAC dump test: CodeHeap debuggee intentional crash");
    }
}
