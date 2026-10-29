// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Loads the address of a thread-local variable into rax. Trashes all volatile registers.
.macro INLINE_GET_TLS_VAR Var
       .att_syntax
#if defined(__APPLE__)
        movq    _\Var@TLVP(%rip), %rdi
        callq   *(%rdi)
#else
        .byte 0x66  // data16 prefix - padding to have space for linker relaxations
        leaq    \Var@TLSGD(%rip), %rdi
        .byte 0x66  //
        .byte 0x66  //
        .byte 0x48  // rex.W prefix, also for padding
        callq   __tls_get_addr@PLT
#endif
       .intel_syntax noprefix
.endm
