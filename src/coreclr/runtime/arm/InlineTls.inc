// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Thumb with PIC version.
// Loads the address of a thread-local variable into r0. Trashes r1-r3, r12 and lr.
.macro INLINE_GET_TLS_VAR Var
        ldr     r0, 2f
1:
        add     r0, pc, r0
        bl      __tls_get_addr
        b       3f

        // Inline data
        // LLVM assembler has no concept of subsections and this is not expressible as
        // cross-section relocation.
        .p2align 2
2:
        .extern \Var
        .type \Var, tls_object
        .long  \Var(TLSGD) + (2b - 1b - 4)
3:
.endm
