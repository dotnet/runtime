// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A) }
.assembly WasmGCPollsTailCalls { }

.class public abstract sealed auto ansi beforefieldinit WasmGCPollsTailCalls
       extends [System.Runtime]System.Object
{
    .method public hidebysig static int32 SingleBlockTailCall(int32 iterations, int32 sum) cil managed noinlining
    {
        .maxstack 2

        ldarg.0
        ldarg.1
        tail.
        call int32 WasmGCPollsTailCalls::First(int32, int32)
        ret
    }

    // Both conditional outcomes target Forward in IL. Optimized import folds
    // this to BBJ_ALWAYS before inline poll insertion.
    .method public hidebysig static int32 SharedSuccessor(int32 iterations, bool condition) cil managed noinlining
    {
        .maxstack 2

        ldarg.1
        brtrue.s Forward

    Forward:
        ldarg.0
        ldc.i4.0
        tail.
        call int32 WasmGCPollsTailCalls::First(int32, int32)
        ret
    }

    // AOT compilation exercises an empty BBJ_ALWAYS self-loop. Do not execute it.
    .method public hidebysig static void EmptyCycle() cil managed noinlining
    {
        .maxstack 0

    Loop:
        br.s Loop
    }

    // Mutual tail calls avoid self-recursion-to-loop conversion. Each method
    // needs a poll on the implicit edge from its tail call back through entry.
    .method public hidebysig static int32 First(int32 iterations, int32 sum) cil managed noinlining
    {
        .maxstack 4

        ldarg.0
        ldc.i4.0
        ble.s Done

        ldarg.0
        ldc.i4.1
        sub
        ldarg.1
        ldarg.0
        ldc.i4.1
        sub
        add
        tail.
        call int32 WasmGCPollsTailCalls::Second(int32, int32)
        ret

    Done:
        ldarg.1
        ret
    }

    .method public hidebysig static int32 Second(int32 iterations, int32 sum) cil managed noinlining
    {
        .maxstack 4

        ldarg.0
        ldc.i4.0
        ble.s Done

        ldarg.0
        ldc.i4.1
        sub
        ldarg.1
        ldarg.0
        ldc.i4.1
        sub
        add
        tail.
        call int32 WasmGCPollsTailCalls::First(int32, int32)
        ret

    Done:
        ldarg.1
        ret
    }
}
