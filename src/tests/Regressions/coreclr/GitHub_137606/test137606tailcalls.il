// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly test137606tailcalls {}

// The spin methods of test137606's epilog test. Each tail call has an explicit tail. prefix, so the JIT emits it as
// a jump whenever a fast tail call is possible, and these signatures (the same register arguments, no structs) always
// allow one. AggressiveOptimization gives each method one optimized code version for the whole run.
.class public abstract auto ansi sealed beforefieldinit TailCalls137606
       extends [System.Runtime]System.Object
{
    // Never set; the branch on it keeps the call to Cold.
    .field public static bool s_never
    .field public static object s_sink

    // The call to Cold needs a frame that saves callee-saved registers, and the tail call closes a cycle with no GC
    // safe point, so the method is fully interruptible and the first instruction of its epilog is GC-safe.
    .method public hidebysig static int64 PopSpin(object a, object b, object c, int64 n) cil managed noinlining aggressiveoptimization
    {
        .maxstack 5
        ldsfld bool TailCalls137606::s_never
        brfalse.s SPIN
        ldarg.0
        ldarg.1
        ldarg.2
        call void TailCalls137606::Cold(object, object, object)
    SPIN:
        ldarg.0
        ldarg.1
        ldarg.2
        ldarg.3
        ldc.i4.1
        conv.i8
        add
        tail. call int64 TailCalls137606::PopSpin2(object, object, object, int64)
        ret
    }

    .method public hidebysig static int64 PopSpin2(object a, object b, object c, int64 n) cil managed noinlining aggressiveoptimization
    {
        .maxstack 5
        ldsfld bool TailCalls137606::s_never
        brfalse.s SPIN
        ldarg.0
        ldarg.1
        ldarg.2
        call void TailCalls137606::Cold(object, object, object)
    SPIN:
        ldarg.0
        ldarg.1
        ldarg.2
        ldarg.3
        ldc.i4.1
        conv.i8
        add
        tail. call int64 TailCalls137606::PopSpin(object, object, object, int64)
        ret
    }

    .method public hidebysig static void Cold(object a, object b, object c) cil managed noinlining
    {
        .maxstack 1
        ldarg.0
        stsfld object TailCalls137606::s_sink
        ret
    }
}
