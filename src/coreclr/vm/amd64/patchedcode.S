// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

// On Apple platforms we emit the whole patched region as single function
// with .alt_entry labels for individual write barrier helpers. This ensures
// the linker doesn't relocate or split the code and treats it as single
// atom. We also need to be careful to produce the correct unwinding
// information.

//-----------------------------------------------------------------------------
// The following Macros help in WRITE_BARRIER Implementations
// WRITE_BARRIER_ENTRY
//
// Declare the start of a write barrier function. Use similarly to NESTED_ENTRY. This is the only legal way
// to declare a write barrier function.
//
.macro WRITE_BARRIER_ENTRY name
#if defined(__APPLE__)
    // .cfi_startproc/.cfi_endproc is workaround for https://github.com/dotnet/runtime/issues/119005
    .cfi_endproc
    .p2align        4
    .alt_entry C_FUNC(\name)
    .private_extern C_FUNC(\name)
C_FUNC(\name):
    .cfi_startproc
#else
    LEAF_ENTRY \name, _TEXT
#endif
.endm

// WRITE_BARRIER_END
//
// The partner to WRITE_BARRIER_ENTRY, used like NESTED_END.
//
.macro WRITE_BARRIER_END name
#if defined(__APPLE__)
    // .cfi_startproc/.cfi_endproc is workaround for https://github.com/dotnet/runtime/issues/119005
    .cfi_endproc
    .alt_entry C_FUNC(\name\()_End)
    .private_extern C_FUNC(\name\()_End)
C_FUNC(\name\()_End):
    .cfi_startproc
    // make sure this symbol gets its own address
    nop
#else
    LEAF_END_MARKED \name, _TEXT
#endif
.endm

// Mark start of the code region that we patch at runtime
#if defined(__APPLE__)
        .global C_FUNC(JIT_PatchedCodeStart)
C_FUNC(JIT_PatchedCodeStart):
        .cfi_startproc
        ret
#else
LEAF_ENTRY JIT_PatchedCodeStart, _TEXT
        ret
LEAF_END JIT_PatchedCodeStart, _TEXT
#endif


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
WRITE_BARRIER_ENTRY JIT_CheckedWriteBarrier

        // When WRITE_BARRIER_CHECK is defined _NotInHeap will write the reference
        // but if it isn't then it will just return.
        //
        // See if this is in GCHeap
        PREPARE_EXTERNAL_VAR g_lowest_address, rax
        cmp     rdi, [rax]
        // jb      LOCAL_LABEL(NotInHeap)
        .byte 0x72, 0x12
        PREPARE_EXTERNAL_VAR g_highest_address, rax
        cmp     rdi, [rax]

        // jnb     LOCAL_LABEL(NotInHeap)
        .byte 0x73, 0x06
        jmp     [rip + C_FUNC(JIT_WriteBarrier_Loc)]

    LOCAL_LABEL(NotInHeap):
        // See comment above about possible AV
        mov     [rdi], rsi
        ret
WRITE_BARRIER_END JIT_CheckedWriteBarrier


// WriteBarrierManager copies the selected write barrier into this buffer and
// verifies that every implementation fits during initialization.
.balign 16
WRITE_BARRIER_ENTRY JIT_WriteBarrier
#ifdef _DEBUG
        // In debug builds, this just contains jump to the debug version of the write barrier by default
        jmp C_FUNC(JIT_WriteBarrier_Debug)
#endif

        .fill JIT_WRITE_BARRIER_BUFFER_SIZE - (. - C_FUNC(JIT_WriteBarrier)), 1, 0xCC
WRITE_BARRIER_END JIT_WriteBarrier

// Mark start of the code region that we patch at runtime
#if defined(__APPLE__)
        .private_extern C_FUNC(JIT_PatchedCodeLast)
        .alt_entry C_FUNC(JIT_PatchedCodeLast)
C_FUNC(JIT_PatchedCodeLast):
        .cfi_endproc
        ret
#else
LEAF_ENTRY JIT_PatchedCodeLast, _TEXT
        ret
LEAF_END JIT_PatchedCodeLast, _TEXT
#endif
