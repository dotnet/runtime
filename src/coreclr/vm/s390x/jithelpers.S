// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

// Mark start of the code region that we patch at runtime
LEAF_ENTRY JIT_PatchedCodeStart, _TEXT
        br %r14
LEAF_END JIT_PatchedCodeStart, _TEXT


// There is an even more optimized version of these helpers possible which takes
// advantage of knowledge of which way the ephemeral heap is growing to only do 1/2
// that check (this is more significant in the JIT_WriteBarrier case).
//
// Additionally we can look into providing helpers which will take the src/dest from
// specific registers (like x86) which _could_ (??) make for easier register allocation
// for the JIT64, however it might lead to having to have some nasty code that treats
// these guys really special like... :(.
//
// Version that does the move, checks whether or not it's in the GC and whether or not
// it needs to have it's card updated
//
// void JIT_CheckedWriteBarrier(Object** dst, Object* src)
LEAF_ENTRY JIT_CheckedWriteBarrier, _TEXT
// NOT YET IMPLEMENTED
.long 0x31
LEAF_END_MARKED JIT_CheckedWriteBarrier, _TEXT


// This is used by the mechanism to hold either the JIT_WriteBarrier_PreGrow
// or JIT_WriteBarrier_PostGrow code (depending on the state of the GC). It _WILL_
// change at runtime as the GC changes. Initially it should simply be a copy of the
// larger of the two functions (JIT_WriteBarrier_PostGrow) to ensure we have created
// enough space to copy that code in.
.balign 16
LEAF_ENTRY JIT_WriteBarrier, _TEXT
// NOT YET IMPLEMENTED
.long 0x32
LEAF_END_MARKED JIT_WriteBarrier, _TEXT

// Mark start of the code region that we patch at runtime
LEAF_ENTRY JIT_PatchedCodeLast, _TEXT
        br %r14
LEAF_END JIT_PatchedCodeLast, _TEXT

// JIT_ByRefWriteBarrier has weird semantics, see usage in StubLinkerX86.cpp
//
// Entry:
//   RDI - address of ref-field (assigned to)
//   RSI - address of the data  (source)
//
//   Note: RyuJIT assumes that all volatile registers can be trashed by
//   the CORINFO_HELP_ASSIGN_BYREF helper (i.e. JIT_ByRefWriteBarrier)
//   except RDI and RSI. This helper uses and defines RDI and RSI, so
//   they remain as live GC refs or byrefs, and are not killed.
//
//
//   RCX is trashed
//   RAX is trashed
//   R10 is trashed
//   R11 is trashed on Debug build
// Exit:
//   RDI, RSI are incremented by SIZEOF(LPVOID)
LEAF_ENTRY JIT_ByRefWriteBarrier, _TEXT
// NOT YET IMPLEMENTED
.long 0x33
LEAF_END_MARKED JIT_ByRefWriteBarrier, _TEXT

// ------------------------------------------------------------------
// __declspec(naked) void F_CALL_CONV JIT_WriteBarrier_Callable(Object **dst, Object* val)
.balign 16
LEAF_ENTRY  JIT_WriteBarrier_Callable, _TEXT
// NOT YET IMPLEMENTED
.long 0x34
LEAF_END JIT_WriteBarrier_Callable, _TEXT
