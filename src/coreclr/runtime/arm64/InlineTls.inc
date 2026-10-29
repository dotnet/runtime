// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Loads the address of a thread-local variable into the target register,
// which cannot be x0. Preserves all other registers.
.macro INLINE_GET_TLS_VAR target, var
    .ifc \target, x0
        .error "target cannot be x0"
    .endif

    stp     x0, lr, [sp,#-0x10]!
#if defined(__APPLE__)
    // Apple's tls_get_var trashes x16 (and possibly x17)
    stp     x16, x17, [sp,#-0x10]!
#endif

    // This sequence of instructions is recognized and potentially patched
    // by the linker (GD->IE/LE relaxation).
#if defined(__APPLE__)
    adrp    x0, \var@TLVPPAGE
    ldr     x0, [x0, \var@TLVPPAGEOFF]
    ldr     \target, [x0]

    blr     \target
    // End of the sequence

    mov     \target, x0
#else
    adrp    x0, :tlsdesc:\var
    ldr     \target, [x0, #:tlsdesc_lo12:\var]
    add     x0, x0, :tlsdesc_lo12:\var
    .tlsdesccall \var
    blr     \target
    // End of the sequence

    mrs     \target, tpidr_el0
    add     \target, \target, x0
#endif

#if defined(__APPLE__)
    ldp     x16, x17, [sp],#0x10
#endif
    ldp     x0, lr, [sp],#0x10
.endm
